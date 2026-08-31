using System.Security.Cryptography;
using Avalonia.Controls;
using Nte.App.Services;

namespace Nte.App.Tests.Fakes;

internal sealed class FakeVideoPlaybackService : IVideoPlaybackService
{
    public FakeVideoPlaybackSession Session { get; private set; } = null!;

    public IVideoPlaybackSession CreateSession(byte[] decryptedContent)
    {
        Session = new FakeVideoPlaybackSession(decryptedContent);
        return Session;
    }

    public void Dispose() => Session?.Dispose();
}

internal sealed class FakeVideoPlaybackSession(byte[] content) : IVideoPlaybackSession
{
    public Control Surface { get; } = new Border();
    public int PlayCount { get; private set; }
    public int PauseCount { get; private set; }
    public int DisposeCount { get; private set; }
    public long LastSeekMilliseconds { get; private set; } = -1;
    public bool IsDisposed => DisposeCount != 0;
    public bool ContentIsCleared => content.All(value => value == 0);

    public event EventHandler<VideoPlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<VideoPlaybackFailedEventArgs>? Failed
    {
        add { }
        remove { }
    }

    public bool Play()
    {
        PlayCount++;
        return true;
    }

    public void Pause() => PauseCount++;

    public void Seek(long positionMilliseconds) =>
        LastSeekMilliseconds = positionMilliseconds;

    public void Publish(
        bool isPlaying,
        bool canSeek,
        long positionMilliseconds,
        long durationMilliseconds) =>
        StateChanged?.Invoke(
            this,
            new VideoPlaybackStateChangedEventArgs(
                isPlaying,
                canSeek,
                positionMilliseconds,
                durationMilliseconds));

    public void Dispose()
    {
        if (IsDisposed)
            return;
        DisposeCount++;
        CryptographicOperations.ZeroMemory(content);
        StateChanged = null;
    }
}
