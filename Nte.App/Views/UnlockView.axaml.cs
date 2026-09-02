using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Nte.App.Views;

public sealed partial class UnlockView : UserControl
{
    public UnlockView() => AvaloniaXamlLoader.Load(this);

    private void UnlockView_Loaded(object? sender, RoutedEventArgs args)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!PasswordTextBox.IsAttachedToVisualTree() ||
                !PasswordTextBox.IsEffectivelyVisible ||
                !PasswordTextBox.IsEnabled)
            {
                return;
            }

            PasswordTextBox.Focus();
            PasswordTextBox.SelectAll();
        }, DispatcherPriority.Input);
    }
}
