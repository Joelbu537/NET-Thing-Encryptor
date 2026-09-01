using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class VaultDocumentView : UserControl
{
    public VaultDocumentView()
    {
        AvaloniaXamlLoader.Load(this);
        AddHandler(KeyDownEvent, VaultDocumentView_KeyDown, RoutingStrategies.Tunnel);
    }

    private async void VaultDocumentView_Loaded(object? sender, RoutedEventArgs args)
    {
        Focus();
        if (DataContext is VaultDocumentViewModel viewModel)
            await viewModel.StartMediaCommand.ExecuteAsync();
    }

    private void Undo_Click(object? sender, RoutedEventArgs args) => TextEditor.Undo();
    private void Redo_Click(object? sender, RoutedEventArgs args) => TextEditor.Redo();

    private async void PreviousImageArea_Tapped(object? sender, TappedEventArgs args)
    {
        Focus();
        if (DataContext is VaultDocumentViewModel viewModel)
            await viewModel.PreviousImageCommand.ExecuteAsync();

        args.Handled = true;
    }

    private async void NextImageArea_Tapped(object? sender, TappedEventArgs args)
    {
        Focus();
        if (DataContext is VaultDocumentViewModel viewModel)
            await viewModel.NextImageCommand.ExecuteAsync();

        args.Handled = true;
    }

    private async void VaultDocumentView_KeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Handled || DataContext is not VaultDocumentViewModel viewModel)
            return;

        AsyncCommand? command = viewModel.IsMedia
            ? args.Key switch
            {
                Key.Space => viewModel.ToggleMediaPlaybackCommand,
                Key.Left or Key.J => viewModel.SeekMediaBackwardCommand,
                Key.Right or Key.L => viewModel.SeekMediaForwardCommand,
                _ => null
            }
            : viewModel.IsImageDocument
                ? args.Key switch
                {
                    Key.Left => viewModel.PreviousImageCommand,
                    Key.Right => viewModel.NextImageCommand,
                    _ => null
                }
                : null;

        if (command is null)
            return;

        args.Handled = true;
        await command.ExecuteAsync();
    }

    private void MediaTimeline_PointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (DataContext is VaultDocumentViewModel viewModel)
            viewModel.BeginMediaSeek();
    }

    private void MediaTimeline_PointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (DataContext is VaultDocumentViewModel viewModel)
            viewModel.CompleteMediaSeek();
    }

    private void MediaTimeline_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs args)
    {
        if (DataContext is VaultDocumentViewModel viewModel)
            viewModel.CompleteMediaSeek();
    }

    private void TextEditor_KeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.S || !args.KeyModifiers.HasFlag(KeyModifiers.Control) ||
            DataContext is not VaultDocumentViewModel viewModel)
        {
            return;
        }

        viewModel.SaveCommand.Execute(null);
        args.Handled = true;
    }
}
