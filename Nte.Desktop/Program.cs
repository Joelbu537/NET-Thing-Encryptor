using Avalonia;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using NET_Thing_Encryptor;
using Nte.App.Services;
using AvaloniaApplication = Nte.App.App;

namespace Nte.Desktop;

internal static class Program
{
    private const string ApplicationMutexName = "NET Thing Encryptor";

    [STAThread]
    public static void Main(string[] args)
    {
        using Mutex applicationMutex = new(
            initiallyOwned: true,
            ApplicationMutexName,
            out bool ownsApplicationMutex);
        if (!ownsApplicationMutex)
            return;

        if (args.Contains("--media-runtime-probe", StringComparer.Ordinal))
        {
            VerifyMediaRuntime();
            return;
        }

        bool startupProbe = args.Contains("--startup-probe", StringComparer.Ordinal);
        string? configuredDataDirectory = Environment.GetEnvironmentVariable("NTE_DATA_DIRECTORY");
        string? probeDirectory = startupProbe && string.IsNullOrWhiteSpace(configuredDataDirectory)
            ? Path.Combine(Path.GetTempPath(), $"nte-m3-startup-{Guid.NewGuid():N}")
            : null;
        string dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
            ? probeDirectory ?? AppPaths.DataDirectory
            : Path.GetFullPath(configuredDataDirectory);
        IReadOnlyList<string> legacyDirectories = probeDirectory is null &&
            string.IsNullOrWhiteSpace(configuredDataDirectory)
                ? AppPaths.LegacyDataDirectories
                : [];

        AvaloniaApplication.ConfigureVaultService(() => new ThingDataVaultService(
            new FileSystemVaultStorage(
                dataDirectory,
                dataDirectory,
                legacyDirectories)));
        AvaloniaApplication.ConfigureVideoSurfaceFactory(mediaPlayer =>
        {
            var videoView = new VideoView
            {
                MediaPlayer = mediaPlayer
            };
            return new VideoSurfaceRegistration(
                videoView,
                () => videoView.MediaPlayer = null);
        });
        AvaloniaApplication.ConfigureStartupProbe(startupProbe);
        string[] lifetimeArguments = args
            .Where(argument => !string.Equals(argument, "--startup-probe", StringComparison.Ordinal))
            .ToArray();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(lifetimeArguments);
        }
        finally
        {
            if (probeDirectory is not null && Directory.Exists(probeDirectory))
                Directory.Delete(probeDirectory, recursive: true);
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<AvaloniaApplication>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void VerifyMediaRuntime()
    {
        Core.Initialize();
        using var libVlc = new LibVLC("--no-video-title-show", "--quiet");
        _ = libVlc.Version;
    }
}
