using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace Nte.App.Services;

public sealed record VideoSurfaceRegistration(Control Control, Action Detach);

public enum MediaPlaybackKind
{
    Audio,
    Video
}

public sealed class MediaPlaybackStateChangedEventArgs(
    bool isPlaying,
    bool canSeek,
    long positionMilliseconds,
    long durationMilliseconds) : EventArgs
{
    public bool IsPlaying { get; } = isPlaying;
    public bool CanSeek { get; } = canSeek;
    public long PositionMilliseconds { get; } = Math.Max(0, positionMilliseconds);
    public long DurationMilliseconds { get; } = Math.Max(0, durationMilliseconds);
}

public sealed class MediaPlaybackFailedEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public interface IMediaPlaybackSession : IDisposable
{
    Control? Surface { get; }
    event EventHandler<MediaPlaybackStateChangedEventArgs>? StateChanged;
    event EventHandler<MediaPlaybackFailedEventArgs>? Failed;
    bool Play();
    void Pause();
    void Seek(long positionMilliseconds);
}

public interface IMediaPlaybackService : IDisposable
{
    IMediaPlaybackSession CreateSession(byte[] decryptedContent, MediaPlaybackKind kind);
}

public sealed class LibVlcMediaPlaybackService : IMediaPlaybackService
{
    private readonly Func<MediaPlayer, VideoSurfaceRegistration> _surfaceFactory;
    private readonly object _sync = new();
    private readonly HashSet<LibVlcMediaPlaybackSession> _sessions = [];
    private LibVLC? _libVlc;
    private bool _disposed;

    public LibVlcMediaPlaybackService(
        Func<MediaPlayer, VideoSurfaceRegistration> surfaceFactory)
    {
        _surfaceFactory = surfaceFactory
            ?? throw new ArgumentNullException(nameof(surfaceFactory));
    }

    public IMediaPlaybackSession CreateSession(
        byte[] decryptedContent,
        MediaPlaybackKind kind)
    {
        ArgumentNullException.ThrowIfNull(decryptedContent);

        try
        {
            if (decryptedContent.Length == 0)
                throw new ArgumentException("The media content must not be empty.", nameof(decryptedContent));
            if (!Enum.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_libVlc is null)
                {
                    Core.Initialize();
                    _libVlc = new LibVLC("--no-video-title-show", "--quiet");
                }

                var session = new LibVlcMediaPlaybackSession(
                    _libVlc,
                    _surfaceFactory,
                    decryptedContent,
                    kind,
                    RemoveSession);
                try
                {
                    _sessions.Add(session);
                    return session;
                }
                catch
                {
                    session.Dispose();
                    throw;
                }
            }
        }
        catch
        {
            CryptographicOperations.ZeroMemory(decryptedContent);
            throw;
        }
    }

    public void Dispose()
    {
        LibVlcMediaPlaybackSession[] sessions;
        LibVLC? libVlc;
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            sessions = [.. _sessions];
            _sessions.Clear();
            libVlc = _libVlc;
            _libVlc = null;
        }

        foreach (LibVlcMediaPlaybackSession session in sessions)
            session.Dispose();
        libVlc?.Dispose();
    }

    private void RemoveSession(LibVlcMediaPlaybackSession session)
    {
        lock (_sync)
            _sessions.Remove(session);
    }
}

internal sealed class LibVlcMediaPlaybackSession : IMediaPlaybackSession
{
    private readonly byte[] _decryptedContent;
    private readonly Action<LibVlcMediaPlaybackSession> _onDisposed;
    private readonly object _stateSync = new();
    private MemoryStream? _stream;
    private StreamMediaInput? _input;
    private Media? _media;
    private MediaPlayer? _player;
    private VideoSurfaceRegistration? _surface;
    private long _positionMilliseconds;
    private long _durationMilliseconds;
    private bool _isPlaying;
    private bool _canSeek;
    private bool _ended;
    private long _stateRevision;
    private long _lastDeliveredStateRevision;
    private int _disposed;

    public LibVlcMediaPlaybackSession(
        LibVLC libVlc,
        Func<MediaPlayer, VideoSurfaceRegistration> surfaceFactory,
        byte[] decryptedContent,
        MediaPlaybackKind kind,
        Action<LibVlcMediaPlaybackSession> onDisposed)
    {
        _decryptedContent = decryptedContent;
        _onDisposed = onDisposed;

        try
        {
            _stream = new MemoryStream(decryptedContent, writable: false);
            _input = new StreamMediaInput(_stream);
            _media = new Media(libVlc, _input);
            if (kind == MediaPlaybackKind.Audio)
                _media.AddOption(":no-video");
            _player = new MediaPlayer(libVlc)
            {
                EnableHardwareDecoding = kind == MediaPlaybackKind.Video,
                Media = _media
            };
            if (kind == MediaPlaybackKind.Video)
            {
                _surface = surfaceFactory(_player)
                    ?? throw new InvalidOperationException("The platform did not create a video surface.");
                ArgumentNullException.ThrowIfNull(_surface.Control);
                ArgumentNullException.ThrowIfNull(_surface.Detach);
            }
            Subscribe(_player);
        }
        catch
        {
            DisposeResources(stopPlayer: false);
            throw;
        }
    }

    public Control? Surface => _surface?.Control;

    public event EventHandler<MediaPlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<MediaPlaybackFailedEventArgs>? Failed;

    public bool Play()
    {
        MediaPlayer? player = _player;
        if (Volatile.Read(ref _disposed) != 0 || player is null)
            return false;

        try
        {
            bool restart;
            lock (_stateSync)
            {
                restart = _ended;
                _ended = false;
            }
            if (restart)
                player.Time = 0;

            bool started = player.Play();
            if (!started)
                PublishFailure("Die Medienwiedergabe konnte nicht gestartet werden.");
            return started;
        }
        catch (Exception ex)
        {
            PublishFailure($"Die Medienwiedergabe konnte nicht gestartet werden: {ex.Message}");
            return false;
        }
    }

    public void Pause()
    {
        MediaPlayer? player = _player;
        if (Volatile.Read(ref _disposed) != 0 || player is null)
            return;

        try
        {
            player.SetPause(true);
        }
        catch (Exception ex)
        {
            PublishFailure($"Die Medienwiedergabe konnte nicht pausiert werden: {ex.Message}");
        }
    }

    public void Seek(long positionMilliseconds)
    {
        MediaPlayer? player = _player;
        if (Volatile.Read(ref _disposed) != 0 || player is null)
            return;

        long maximum;
        lock (_stateSync)
        {
            if (!_canSeek)
                return;
            maximum = Math.Max(0, _durationMilliseconds);
        }
        long target = Math.Clamp(positionMilliseconds, 0, maximum);
        try
        {
            player.Time = target;
            lock (_stateSync)
            {
                _positionMilliseconds = target;
                _ended = maximum > 0 && target >= maximum;
            }
            PublishState();
        }
        catch (Exception ex)
        {
            PublishFailure($"Die Wiedergabeposition konnte nicht geändert werden: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            MediaPlayer? player = _player;
            if (player is not null)
                Unsubscribe(player);
            DisposeResources(stopPlayer: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(_decryptedContent);
            StateChanged = null;
            Failed = null;
            _onDisposed(this);
        }
    }

    private void Subscribe(MediaPlayer player)
    {
        player.Playing += Player_Playing;
        player.Paused += Player_Paused;
        player.Stopped += Player_Stopped;
        player.EndReached += Player_EndReached;
        player.EncounteredError += Player_EncounteredError;
        player.TimeChanged += Player_TimeChanged;
        player.LengthChanged += Player_LengthChanged;
        player.SeekableChanged += Player_SeekableChanged;
    }

    private void Unsubscribe(MediaPlayer player)
    {
        player.Playing -= Player_Playing;
        player.Paused -= Player_Paused;
        player.Stopped -= Player_Stopped;
        player.EndReached -= Player_EndReached;
        player.EncounteredError -= Player_EncounteredError;
        player.TimeChanged -= Player_TimeChanged;
        player.LengthChanged -= Player_LengthChanged;
        player.SeekableChanged -= Player_SeekableChanged;
    }

    private void Player_Playing(object? sender, EventArgs args)
    {
        lock (_stateSync)
        {
            _isPlaying = true;
            _ended = false;
        }
        PublishState();
    }

    private void Player_Paused(object? sender, EventArgs args)
    {
        lock (_stateSync)
            _isPlaying = false;
        PublishState();
    }

    private void Player_Stopped(object? sender, EventArgs args)
    {
        lock (_stateSync)
            _isPlaying = false;
        PublishState();
    }

    private void Player_EndReached(object? sender, EventArgs args)
    {
        lock (_stateSync)
        {
            _isPlaying = false;
            _ended = true;
            if (_durationMilliseconds > 0)
                _positionMilliseconds = _durationMilliseconds;
        }
        PublishState();
    }

    private void Player_EncounteredError(object? sender, EventArgs args)
    {
        lock (_stateSync)
            _isPlaying = false;
        PublishState();
        PublishFailure("Die Mediendatei konnte nicht dekodiert oder wiedergegeben werden.");
    }

    private void Player_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs args)
    {
        lock (_stateSync)
            _positionMilliseconds = Math.Max(0, args.Time);
        PublishState();
    }

    private void Player_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs args)
    {
        lock (_stateSync)
            _durationMilliseconds = Math.Max(0, args.Length);
        PublishState();
    }

    private void Player_SeekableChanged(object? sender, MediaPlayerSeekableChangedEventArgs args)
    {
        lock (_stateSync)
            _canSeek = args.Seekable != 0;
        PublishState();
    }

    private void PublishState()
    {
        MediaPlaybackStateChangedEventArgs args;
        long revision;
        lock (_stateSync)
        {
            args = new MediaPlaybackStateChangedEventArgs(
                _isPlaying,
                _canSeek,
                _positionMilliseconds,
                _durationMilliseconds);
            revision = ++_stateRevision;
        }
        PostToUi(() =>
        {
            if (revision <= Volatile.Read(ref _lastDeliveredStateRevision))
                return;
            Volatile.Write(ref _lastDeliveredStateRevision, revision);
            StateChanged?.Invoke(this, args);
        });
    }

    private void PublishFailure(string message)
    {
        var args = new MediaPlaybackFailedEventArgs(message);
        PostToUi(() => Failed?.Invoke(this, args));
    }

    private void PostToUi(Action action)
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (Volatile.Read(ref _disposed) == 0)
                action();
        });
    }

    private void DisposeResources(bool stopPlayer)
    {
        try
        {
            _surface?.Detach();
        }
        catch
        {
        }

        MediaPlayer? player = _player;
        _player = null;
        if (player is not null)
        {
            if (stopPlayer)
            {
                try
                {
                    player.Stop();
                }
                catch
                {
                }
            }
            try
            {
                player.Dispose();
            }
            catch
            {
            }
        }

        _surface = null;
        try
        {
            _media?.Dispose();
        }
        catch
        {
        }
        _media = null;
        try
        {
            _input?.Dispose();
        }
        catch
        {
        }
        _input = null;
        try
        {
            _stream?.Dispose();
        }
        catch
        {
        }
        _stream = null;
    }
}
