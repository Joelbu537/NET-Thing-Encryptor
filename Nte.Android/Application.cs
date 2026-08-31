using Android.App;
using Android.Runtime;
using Avalonia.Android;
using Avalonia.Controls.ApplicationLifetimes;
using LibVLCSharp.Shared;
using NET_Thing_Encryptor;
using Nte.App.Services;
using AvaloniaApplication = Nte.App.App;

namespace Nte.Android;

[Application]
public class NteAndroidApplication : AvaloniaAndroidApplication<AvaloniaApplication>
{
    protected NteAndroidApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
        AvaloniaApplication.ConfigureVaultService(CreateVaultService);
        AvaloniaApplication.ConfigureSingleViewLifetime(ConfigureSingleViewLifetime);
        AvaloniaApplication.ConfigureVideoSurfaceFactory(CreateVideoSurface);
    }

    private ThingDataVaultService CreateVaultService()
    {
        string filesDirectory = FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("Android did not provide a private files directory.");
        return new ThingDataVaultService(new AppSandboxVaultStorage(filesDirectory));
    }

    private static VideoSurfaceRegistration CreateVideoSurface(MediaPlayer mediaPlayer)
    {
        var host = new AndroidVideoSurfaceHost(mediaPlayer);
        return new VideoSurfaceRegistration(host, host.Detach);
    }

    private static void ConfigureSingleViewLifetime(
        IApplicationLifetime lifetime,
        Func<Avalonia.Controls.Control> createView)
    {
        if (lifetime is not IActivityApplicationLifetime activityLifetime)
            throw new NotSupportedException("The Android activity lifetime is unavailable.");
        activityLifetime.MainViewFactory = createView;
    }
}
