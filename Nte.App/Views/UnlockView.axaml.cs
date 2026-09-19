using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class UnlockView : UserControl
{
    public UnlockView()
    {
        AvaloniaXamlLoader.Load(this);

        if (OperatingSystem.IsAndroid())
        {
            UnlockMenu.Margin = new Thickness(0);
            UnlockMenu.Padding = new Thickness(16);
            UnlockMenu.BorderThickness = new Thickness(0);
            UnlockMenu.CornerRadius = new CornerRadius(0);
            UnlockMenu.MaxWidth = double.PositiveInfinity;
            UnlockMenu.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }
    }

    private void UnlockView_Loaded(object? sender, RoutedEventArgs args)
    {
        // Avalonia clears generated x:Name fields when the view is detached. Keep the
        // control that raised this load cycle alive while the queued focus request runs.
        if (PasswordTextBox is not { } passwordTextBox)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (!passwordTextBox.IsAttachedToVisualTree() ||
                !passwordTextBox.IsEffectivelyVisible ||
                !passwordTextBox.IsEnabled)
            {
                return;
            }

            passwordTextBox.Focus();
            passwordTextBox.SelectAll();
        }, DispatcherPriority.Input);
    }

    private async void RemoteInput_KeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter ||
            DataContext is not UnlockViewModel viewModel ||
            !viewModel.ConnectRemoteVaultCommand.CanExecute(null))
        {
            return;
        }

        args.Handled = true;
        await viewModel.ConnectRemoteVaultCommand.ExecuteAsync();
        if (viewModel.IsRemoteVault && PasswordTextBox is { } passwordTextBox)
        {
            passwordTextBox.Focus();
            passwordTextBox.SelectAll();
        }
    }

    private async void VaultInput_KeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter ||
            DataContext is not UnlockViewModel viewModel ||
            !viewModel.UnlockCommand.CanExecute(null))
        {
            return;
        }

        args.Handled = true;
        await viewModel.UnlockCommand.ExecuteAsync();
    }
}
