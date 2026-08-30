using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class VaultDocumentView : UserControl
{
    public VaultDocumentView() => AvaloniaXamlLoader.Load(this);

    private void Undo_Click(object? sender, RoutedEventArgs args) => TextEditor.Undo();
    private void Redo_Click(object? sender, RoutedEventArgs args) => TextEditor.Redo();

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
