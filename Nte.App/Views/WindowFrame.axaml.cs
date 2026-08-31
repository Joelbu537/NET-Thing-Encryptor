using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Nte.App.Views;

public sealed partial class WindowFrame : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<WindowFrame, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<object?> WindowContentProperty =
        AvaloniaProperty.Register<WindowFrame, object?>(nameof(WindowContent));

    public static readonly StyledProperty<bool> ShowMinimizeButtonProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(ShowMinimizeButton), true);

    public static readonly StyledProperty<bool> ShowMaximizeButtonProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(ShowMaximizeButton), true);

    private Window? _window;

    public WindowFrame()
    {
        AvaloniaXamlLoader.Load(this);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? WindowContent
    {
        get => GetValue(WindowContentProperty);
        set => SetValue(WindowContentProperty, value);
    }

    public bool ShowMinimizeButton
    {
        get => GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public void Configure(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
        bool useCustomChrome = OperatingSystem.IsWindows();
        GetTitleBar().IsVisible = useCustomChrome;
        GetResizeLayer().IsVisible = useCustomChrome && window.CanResize;
        GetFrameBorder().BorderThickness = useCustomChrome ? new Thickness(1) : new Thickness(0);
        if (!useCustomChrome)
            return;

        window.WindowDecorations = WindowDecorations.None;
        window.ExtendClientAreaToDecorationsHint = true;
        window.ExtendClientAreaTitleBarHeightHint = 36;
        UpdateWindowStateVisuals();
    }

    private void OnLoaded(object? sender, RoutedEventArgs args)
    {
        Window? window = TopLevel.GetTopLevel(this) as Window;
        if (window is null)
            return;
        if (!ReferenceEquals(_window, window))
            Configure(window);
        window.PropertyChanged -= OnWindowPropertyChanged;
        window.PropertyChanged += OnWindowPropertyChanged;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs args)
    {
        if (_window is not null)
            _window.PropertyChanged -= OnWindowPropertyChanged;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property == Window.WindowStateProperty)
            UpdateWindowStateVisuals();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs args)
    {
        App.NotifyUserInteraction();
        if (_window is null || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        _window.BeginMoveDrag(args);
        args.Handled = true;
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs args)
    {
        if (_window is null || !ShowMaximizeButton || !_window.CanMaximize)
            return;
        ToggleMaximized();
        args.Handled = true;
    }

    private void Minimize_Click(object? sender, RoutedEventArgs args)
    {
        App.NotifyUserInteraction();
        if (_window is { CanMinimize: true })
            _window.WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object? sender, RoutedEventArgs args)
    {
        App.NotifyUserInteraction();
        if (_window is { CanMaximize: true })
            ToggleMaximized();
    }

    private void Close_Click(object? sender, RoutedEventArgs args)
    {
        App.NotifyUserInteraction();
        _window?.Close();
    }

    private void ResizeBorder_PointerPressed(object? sender, PointerPressedEventArgs args)
    {
        App.NotifyUserInteraction();
        if (_window is not { CanResize: true, WindowState: WindowState.Normal } ||
            sender is not Control { Tag: string edgeName } ||
            !Enum.TryParse(edgeName, out WindowEdge edge) ||
            !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _window.BeginResizeDrag(edge, args);
        args.Handled = true;
    }

    private void ToggleMaximized()
    {
        if (_window is null)
            return;
        _window.WindowState = _window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateWindowStateVisuals()
    {
        if (_window is null)
            return;
        bool maximized = _window.WindowState == WindowState.Maximized;
        Button maximizeButton = GetMaximizeButton();
        maximizeButton.Content = maximized ? "❐" : "□";
        ToolTip.SetTip(maximizeButton, maximized ? "Wiederherstellen" : "Maximieren");
        AutomationProperties.SetName(maximizeButton, maximized ? "Wiederherstellen" : "Maximieren");
        GetFrameBorder().BorderThickness = OperatingSystem.IsWindows() && !maximized
            ? new Thickness(1)
            : new Thickness(0);
        GetResizeLayer().IsVisible = OperatingSystem.IsWindows() && !maximized && _window.CanResize;
    }

    private Border GetTitleBar() =>
        this.FindControl<Border>("TitleBar")
        ?? throw new InvalidOperationException("The custom title bar is unavailable.");

    private Grid GetResizeLayer() =>
        this.FindControl<Grid>("ResizeLayer")
        ?? throw new InvalidOperationException("The custom resize layer is unavailable.");

    private Border GetFrameBorder() =>
        this.FindControl<Border>("FrameBorder")
        ?? throw new InvalidOperationException("The custom window border is unavailable.");

    private Button GetMaximizeButton() =>
        this.FindControl<Button>("MaximizeButton")
        ?? throw new InvalidOperationException("The maximize button is unavailable.");
}
