using System.Security.Cryptography;
using Avalonia.Controls;
using Nte.App.Services;

namespace Nte.App.Tests.Fakes;

internal sealed class FakeMediaPlaybackService : IMediaPlaybackService
{
    public FakeMediaPlaybackSession Session { get; private set; } = null!;
    public MediaPlaybackKind? LastKind { get; private set; }

    public IMediaPlaybackSession CreateSession(byte[] decryptedContent, MediaPlaybackKind kind)
    {
        LastKind = kind;
        Session = new FakeMediaPlaybackSession(decryptedContent, kind);
        return Session;
    }

    public void Dispose() => Session?.Dispose();
}

internal sealed class FakeMediaPlaybackSession(
    byte[] content,
    MediaPlaybackKind kind) : IMediaPlaybackSession
{
    public Control? Surface { get; } = kind == MediaPlaybackKind.Video ? new Border() : null;
    public int PlayCount { get; private set; }
    public int PauseCount { get; private set; }
    public int DisposeCount { get; private set; }
    public long LastSeekMilliseconds { get; private set; } = -1;
    public bool IsDisposed => DisposeCount != 0;
    public bool ContentIsCleared => content.All(value => value == 0);

    public event EventHandler<MediaPlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<MediaPlaybackFailedEventArgs>? Failed
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
            new MediaPlaybackStateChangedEventArgs(
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
