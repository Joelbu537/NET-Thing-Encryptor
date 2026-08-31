using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;
using AndroidVideoView = LibVLCSharp.Platforms.Android.VideoView;

namespace Nte.Android;

internal sealed class AndroidVideoSurfaceHost : NativeControlHost
{
    private readonly MediaPlayer _mediaPlayer;
    private AndroidVideoView? _videoView;
    private bool _released;

    public AndroidVideoSurfaceHost(MediaPlayer mediaPlayer)
    {
        _mediaPlayer = mediaPlayer ?? throw new ArgumentNullException(nameof(mediaPlayer));
    }

    public void Detach()
    {
        _released = true;
        DetachNativeView();
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        global::Android.Content.Context context =
            (parent as AndroidViewControlHandle)?.View.Context
            ?? global::Android.App.Application.Context;

        var videoView = new AndroidVideoView(context);
        if (!_released)
            videoView.MediaPlayer = _mediaPlayer;

        _videoView = videoView;
        return new AndroidViewControlHandle(videoView);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        DetachNativeView();
        base.DestroyNativeControlCore(control);
    }

    private void DetachNativeView()
    {
        AndroidVideoView? videoView = _videoView;
        _videoView = null;
        if (videoView is not null)
            videoView.MediaPlayer = null;
    }
}
