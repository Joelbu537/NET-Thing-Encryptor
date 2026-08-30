using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class VaultView : UserControl
{
    public VaultView() => AvaloniaXamlLoader.Load(this);

    private void Items_SelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (sender is ListBox listBox && DataContext is VaultViewModel viewModel)
        {
            viewModel.SetSelectedItems(
                listBox.SelectedItems?.OfType<VaultItemViewModel>() ?? []);
        }
    }
}
