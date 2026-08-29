using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Nte.App.Services;
using Nte.App.ViewModels;
using Nte.App.Views;

namespace Nte.App;

public sealed partial class App : Application
{
    private static Func<IVaultApplicationService>? _vaultServiceFactory;
    private static bool _exitAfterInitialization;

    public static void ConfigureVaultService(Func<IVaultApplicationService> factory) =>
        _vaultServiceFactory = factory ?? throw new ArgumentNullException(nameof(factory));

    public static void ConfigureStartupProbe(bool exitAfterInitialization) =>
        _exitAfterInitialization = exitAfterInitialization;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IVaultApplicationService vault = _vaultServiceFactory?.Invoke()
                ?? throw new InvalidOperationException("No vault service has been configured.");
            var window = new MainWindow();
            var picker = new AvaloniaFilePickerService(() => window);
            var shell = new AppShellViewModel(vault, picker);
            window.DataContext = shell;
            desktop.MainWindow = window;
            window.Opened += async (_, _) =>
            {
                await shell.InitializeAsync();
                if (_exitAfterInitialization)
                    DispatcherTimer.RunOnce(window.Close, TimeSpan.FromMilliseconds(100));
            };
            desktop.Exit += (_, _) => shell.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
