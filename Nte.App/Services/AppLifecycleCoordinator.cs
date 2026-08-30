namespace Nte.App.Services;

public enum SessionLockReason
{
    Background,
    Inactivity
}

/// <summary>
/// Coordinates automatic session locking with native file pickers that temporarily
/// move an Android activity into the background.
/// </summary>
public sealed class AppLifecycleCoordinator : IDisposable
{
    private readonly object _gate = new();
    private readonly Action<SessionLockReason> _requestLock;
    private TimeSpan? _inactivityTimeout;
    private readonly TimeSpan _externalInteractionGracePeriod;
    private readonly Func<DateTimeOffset> _getUtcNow;
    private readonly Timer _timer;
    private readonly Timer _backgroundTimer;
    private DateTimeOffset _lastInteraction;
    private DateTimeOffset _backgroundedAt;
    private int _externalInteractionCount;
    private bool _isBackgrounded;
    private bool _lockDispatched;
    private bool _disposed;

    public AppLifecycleCoordinator(
        Action<SessionLockReason> requestLock,
        TimeSpan inactivityTimeout,
        Func<DateTimeOffset>? getUtcNow = null,
        TimeSpan? externalInteractionGracePeriod = null)
    {
        _requestLock = requestLock ?? throw new ArgumentNullException(nameof(requestLock));
        if (inactivityTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(inactivityTimeout));

        _inactivityTimeout = inactivityTimeout;
        _externalInteractionGracePeriod = externalInteractionGracePeriod ?? TimeSpan.FromMinutes(2);
        if (_externalInteractionGracePeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(externalInteractionGracePeriod));
        _getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);
        _lastInteraction = _getUtcNow();
        _timer = new Timer(
            static state => ((AppLifecycleCoordinator)state!).CheckInactivity(),
            this,
            inactivityTimeout,
            Timeout.InfiniteTimeSpan);
        _backgroundTimer = new Timer(
            static state => ((AppLifecycleCoordinator)state!).CheckDeferredBackgroundLock(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    public bool IsBackgrounded
    {
        get
        {
            lock (_gate)
                return _isBackgrounded;
        }
    }

    public int ExternalInteractionCount
    {
        get
        {
            lock (_gate)
                return _externalInteractionCount;
        }
    }

    public IDisposable BeginExternalInteraction()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _externalInteractionCount++;
        }
        return new ExternalInteraction(this);
    }

    public void NotifyBackgrounded()
    {
        bool shouldLock;
        lock (_gate)
        {
            if (_disposed)
                return;
            _isBackgrounded = true;
            _backgroundedAt = _getUtcNow();
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            if (_externalInteractionCount == 0)
            {
                _backgroundTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                shouldLock = TryDispatchLockLocked();
            }
            else
            {
                shouldLock = false;
                _backgroundTimer.Change(
                    _externalInteractionGracePeriod,
                    Timeout.InfiniteTimeSpan);
            }
        }

        if (shouldLock)
            _requestLock(SessionLockReason.Background);
    }

    public void NotifyForegrounded()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _isBackgrounded = false;
            _lockDispatched = false;
            _backgroundTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _lastInteraction = _getUtcNow();
            ScheduleInactivityCheckLocked();
        }
    }

    public void NotifyUserInteraction()
    {
        lock (_gate)
        {
            if (_disposed || _isBackgrounded)
                return;
            _lockDispatched = false;
            _lastInteraction = _getUtcNow();
            ScheduleInactivityCheckLocked();
        }
    }

    public void UpdateInactivityTimeout(TimeSpan? inactivityTimeout)
    {
        if (inactivityTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(inactivityTimeout));

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _inactivityTimeout = inactivityTimeout;
            _lastInteraction = _getUtcNow();
            _lockDispatched = false;
            ScheduleInactivityCheckLocked();
        }
    }

    public void CheckInactivity()
    {
        bool shouldLock = false;
        lock (_gate)
        {
            if (_disposed || _isBackgrounded || _lockDispatched)
                return;

            if (!_inactivityTimeout.HasValue)
                return;
            TimeSpan timeout = _inactivityTimeout.Value;
            TimeSpan idle = _getUtcNow() - _lastInteraction;
            if (idle >= timeout)
            {
                _lockDispatched = true;
                shouldLock = true;
            }
            else
            {
                ScheduleInactivityCheckLocked(timeout - idle);
            }
        }

        if (shouldLock)
            _requestLock(SessionLockReason.Inactivity);
    }

    public void CheckDeferredBackgroundLock()
    {
        bool shouldLock = false;
        lock (_gate)
        {
            if (_disposed || !_isBackgrounded || _externalInteractionCount == 0 || _lockDispatched)
                return;

            TimeSpan backgroundDuration = _getUtcNow() - _backgroundedAt;
            if (backgroundDuration >= _externalInteractionGracePeriod)
            {
                _lockDispatched = true;
                shouldLock = true;
            }
            else
            {
                _backgroundTimer.Change(
                    _externalInteractionGracePeriod - backgroundDuration,
                    Timeout.InfiniteTimeSpan);
            }
        }

        if (shouldLock)
            _requestLock(SessionLockReason.Background);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
        }
        _timer.Dispose();
        _backgroundTimer.Dispose();
    }

    private bool TryDispatchLockLocked()
    {
        if (_lockDispatched)
            return false;
        _lockDispatched = true;
        return true;
    }

    private void EndExternalInteraction()
    {
        bool shouldLock = false;
        lock (_gate)
        {
            if (_externalInteractionCount == 0)
                return;
            _externalInteractionCount--;
            if (!_disposed && _isBackgrounded && _externalInteractionCount == 0)
            {
                _backgroundTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                shouldLock = TryDispatchLockLocked();
            }
        }

        if (shouldLock)
            _requestLock(SessionLockReason.Background);
    }

    private void ScheduleInactivityCheckLocked()
    {
        if (_isBackgrounded || !_inactivityTimeout.HasValue)
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }

        TimeSpan dueTime = _inactivityTimeout.Value - (_getUtcNow() - _lastInteraction);
        _timer.Change(
            dueTime <= TimeSpan.Zero ? TimeSpan.Zero : dueTime,
            Timeout.InfiniteTimeSpan);
    }

    private void ScheduleInactivityCheckLocked(TimeSpan dueTime) =>
        _timer.Change(dueTime <= TimeSpan.Zero ? TimeSpan.Zero : dueTime, Timeout.InfiniteTimeSpan);

    private sealed class ExternalInteraction(AppLifecycleCoordinator owner) : IDisposable
    {
        private AppLifecycleCoordinator? _owner = owner;

        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.EndExternalInteraction();
    }
}
