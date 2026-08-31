using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);
        (this.FindControl<WindowFrame>("Frame")
            ?? throw new InvalidOperationException("The settings window frame is unavailable."))
            .Configure(this);
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
        Closing += OnClosing;
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs args) =>
        App.NotifyUserInteraction();

    private static void OnKeyDown(object? sender, KeyEventArgs args) =>
        App.NotifyUserInteraction();

    private void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (DataContext is VaultViewModel { IsBusy: true, ShowSettings: true })
            args.Cancel = true;
    }
}
