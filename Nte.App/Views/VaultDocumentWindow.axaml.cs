using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class VaultDocumentWindow : Window
{
    private bool _allowClose;

    public VaultDocumentWindow()
    {
        AvaloniaXamlLoader.Load(this);
        (this.FindControl<WindowFrame>("Frame")
            ?? throw new InvalidOperationException("The document window frame is unavailable."))
            .Configure(this);
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
        Opened += OnOpened;
        Closing += OnClosing;
    }

    public void CloseFromViewModel()
    {
        _allowClose = true;
        Close();
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs args) =>
        App.NotifyUserInteraction();

    private void OnOpened(object? sender, EventArgs args)
    {
        if (DataContext is VaultDocumentViewModel { IsVideo: true })
            WindowState = WindowState.Maximized;
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        App.NotifyUserInteraction();
        if (args.Key != Key.Escape || DataContext is not VaultDocumentViewModel viewModel)
            return;
        args.Handled = viewModel.HandleBackRequested();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_allowClose || DataContext is not VaultDocumentViewModel viewModel)
            return;

        if (args.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown ||
            (args.CloseReason == WindowCloseReason.OwnerWindowClosing && !viewModel.IsDirty))
        {
            return;
        }

        args.Cancel = true;
        _ = viewModel.CloseCommand.ExecuteAsync();
    }
}
