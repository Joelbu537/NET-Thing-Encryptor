using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Nte.App.Services;
using Nte.App.ViewModels;
using Nte.App.Views;

namespace Nte.App;

public sealed partial class App : Application
{
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(5);
    private static Func<IVaultApplicationService>? _vaultServiceFactory;
    private static Action<IApplicationLifetime, Func<Control>>? _singleViewLifetimeConfigurator;
    private static bool _exitAfterInitialization;
    private AppShellViewModel? _shell;
    private AppLifecycleCoordinator? _lifecycleCoordinator;
    private AppShellView? _activeShellView;
    private TopLevel? _activeTopLevel;
    private Task? _initializationTask;

    public static void ConfigureVaultService(Func<IVaultApplicationService> factory) =>
        _vaultServiceFactory = factory ?? throw new ArgumentNullException(nameof(factory));

    public static void ConfigureSingleViewLifetime(
        Action<IApplicationLifetime, Func<Control>> configurator) =>
        _singleViewLifetimeConfigurator = configurator
            ?? throw new ArgumentNullException(nameof(configurator));

    public static void ConfigureStartupProbe(bool exitAfterInitialization) =>
        _exitAfterInitialization = exitAfterInitialization;

    public static void NotifyBackgrounded() =>
        (Current as App)?._lifecycleCoordinator?.NotifyBackgrounded();

    public static void NotifyForegrounded() =>
        (Current as App)?._lifecycleCoordinator?.NotifyForegrounded();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        IVaultApplicationService vault = _vaultServiceFactory?.Invoke()
            ?? throw new InvalidOperationException("No vault service has been configured.");
        _lifecycleCoordinator = new AppLifecycleCoordinator(
            reason => Dispatcher.UIThread.Post(() => _shell?.LockForSecurity(reason)),
            InactivityTimeout);
        var picker = new AvaloniaFilePickerService(
            () => _activeShellView is null ? null : TopLevel.GetTopLevel(_activeShellView),
            _lifecycleCoordinator);
        _shell = new AppShellViewModel(vault, picker, applyPreferences: ApplyPreferences);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow
            {
                Content = CreateShellView()
            };
            desktop.MainWindow = window;
            window.Opened += async (_, _) =>
            {
                await EnsureInitializedAsync();
                if (_exitAfterInitialization)
                    DispatcherTimer.RunOnce(window.Close, TimeSpan.FromMilliseconds(100));
            };
            desktop.Exit += (_, _) => DisposeApplication();
        }
        else if (_singleViewLifetimeConfigurator is not null && ApplicationLifetime is { } lifetime)
        {
            _singleViewLifetimeConfigurator(lifetime, CreateShellView);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = CreateShellView();
        }
        else
        {
            DisposeApplication();
            throw new NotSupportedException("The current Avalonia application lifetime is not supported.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private Control CreateShellView()
    {
        var view = new AppShellView
        {
            DataContext = _shell
        };
        view.Loaded += OnShellViewLoaded;
        view.Unloaded += OnShellViewUnloaded;
        _activeShellView = view;
        return view;
    }

    private async void OnShellViewLoaded(object? sender, RoutedEventArgs args)
    {
        if (sender is not AppShellView view)
            return;

        _activeShellView = view;
        AttachTopLevel(TopLevel.GetTopLevel(view));
        _lifecycleCoordinator?.NotifyUserInteraction();
        await EnsureInitializedAsync();
    }

    private void OnShellViewUnloaded(object? sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _activeShellView))
            DetachTopLevel();
    }

    private Task EnsureInitializedAsync() =>
        _initializationTask ??= _shell?.InitializeAsync()
            ?? Task.FromException(new InvalidOperationException("The application shell is unavailable."));

    private void AttachTopLevel(TopLevel? topLevel)
    {
        if (ReferenceEquals(_activeTopLevel, topLevel))
            return;

        DetachTopLevel();
        _activeTopLevel = topLevel;
        if (topLevel is null)
            return;
        topLevel.BackRequested += OnBackRequested;
        topLevel.PointerPressed += OnPointerPressed;
        topLevel.KeyDown += OnKeyDown;
    }

    private void DetachTopLevel()
    {
        if (_activeTopLevel is null)
            return;
        _activeTopLevel.BackRequested -= OnBackRequested;
        _activeTopLevel.PointerPressed -= OnPointerPressed;
        _activeTopLevel.KeyDown -= OnKeyDown;
        _activeTopLevel = null;
    }

    private void OnBackRequested(object? sender, RoutedEventArgs args)
    {
        if (_shell?.HandleBackRequested() == true)
            args.Handled = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs args) =>
        _lifecycleCoordinator?.NotifyUserInteraction();

    private void OnKeyDown(object? sender, KeyEventArgs args) =>
        _lifecycleCoordinator?.NotifyUserInteraction();

    private void ApplyPreferences(VaultPreferences preferences)
    {
        RequestedThemeVariant = preferences.DarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        _lifecycleCoordinator?.UpdateInactivityTimeout(
            preferences.AutoLockMinutes == 0
                ? null
                : TimeSpan.FromMinutes(preferences.AutoLockMinutes));
    }

    private void DisposeApplication()
    {
        DetachTopLevel();
        _lifecycleCoordinator?.Dispose();
        _lifecycleCoordinator = null;
        _shell?.Dispose();
        _shell = null;
    }
}
