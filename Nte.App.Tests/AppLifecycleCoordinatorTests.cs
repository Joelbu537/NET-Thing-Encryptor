using Nte.App.Services;

namespace Nte.App.Tests;

public sealed class AppLifecycleCoordinatorTests
{
    [Fact]
    public void BackgroundWithoutExternalInteraction_LocksImmediately()
    {
        SessionLockReason? reason = null;
        using var coordinator = new AppLifecycleCoordinator(
            value => reason = value,
            TimeSpan.FromMinutes(5));

        coordinator.NotifyBackgrounded();

        Assert.Equal(SessionLockReason.Background, reason);
        Assert.True(coordinator.IsBackgrounded);
    }

    [Fact]
    public void NativePicker_DefersBackgroundLockUntilInteractionEnds()
    {
        var reasons = new List<SessionLockReason>();
        using var coordinator = new AppLifecycleCoordinator(
            reasons.Add,
            TimeSpan.FromMinutes(5));
        IDisposable interaction = coordinator.BeginExternalInteraction();

        coordinator.NotifyBackgrounded();

        Assert.Empty(reasons);
        interaction.Dispose();
        Assert.Equal([SessionLockReason.Background], reasons);
    }

    [Fact]
    public void NativePickerReturningToForeground_DoesNotLockSession()
    {
        var reasons = new List<SessionLockReason>();
        using var coordinator = new AppLifecycleCoordinator(
            reasons.Add,
            TimeSpan.FromMinutes(5));
        using IDisposable interaction = coordinator.BeginExternalInteraction();

        coordinator.NotifyBackgrounded();
        coordinator.NotifyForegrounded();

        interaction.Dispose();
        Assert.Empty(reasons);
    }

    [Fact]
    public void NativePickerLeftInBackground_LocksAfterGracePeriod()
    {
        DateTimeOffset now = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        var reasons = new List<SessionLockReason>();
        using var coordinator = new AppLifecycleCoordinator(
            reasons.Add,
            TimeSpan.FromMinutes(5),
            () => now,
            TimeSpan.FromMinutes(2));
        using IDisposable interaction = coordinator.BeginExternalInteraction();
        coordinator.NotifyBackgrounded();
        now = now.AddMinutes(2);

        coordinator.CheckDeferredBackgroundLock();

        Assert.Equal([SessionLockReason.Background], reasons);
    }

    [Fact]
    public void InactivityTimeout_RequestsLockOnce()
    {
        DateTimeOffset now = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        var reasons = new List<SessionLockReason>();
        using var coordinator = new AppLifecycleCoordinator(
            reasons.Add,
            TimeSpan.FromMinutes(5),
            () => now);
        coordinator.NotifyForegrounded();
        now = now.AddMinutes(5);

        coordinator.CheckInactivity();
        coordinator.CheckInactivity();

        Assert.Equal([SessionLockReason.Inactivity], reasons);
    }

    [Fact]
    public void UpdatedInactivityTimeout_IsAppliedAndCanBeDisabled()
    {
        DateTimeOffset now = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        var reasons = new List<SessionLockReason>();
        using var coordinator = new AppLifecycleCoordinator(
            reasons.Add,
            TimeSpan.FromMinutes(5),
            () => now);

        coordinator.UpdateInactivityTimeout(null);
        now = now.AddHours(1);
        coordinator.CheckInactivity();
        Assert.Empty(reasons);

        coordinator.UpdateInactivityTimeout(TimeSpan.FromMinutes(2));
        now = now.AddMinutes(2);
        coordinator.CheckInactivity();
        Assert.Equal([SessionLockReason.Inactivity], reasons);
    }
}
