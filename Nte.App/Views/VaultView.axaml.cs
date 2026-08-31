using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nte.App.ViewModels;

namespace Nte.App.Views;

public sealed partial class VaultView : UserControl
{
    private VaultViewModel? _subscribedViewModel;
    private SettingsWindow? _settingsWindow;
    private bool _isUnloaded;
    private bool _isActivatingItem;

    public VaultView()
    {
        AvaloniaXamlLoader.Load(this);
        DataContextChanged += (_, _) => AttachViewModel();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs args)
    {
        _isUnloaded = false;
        AttachViewModel();
        UpdateSettingsPresentation();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs args)
    {
        _isUnloaded = true;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _subscribedViewModel = null;
        _settingsWindow?.Close();
        _settingsWindow = null;
    }

    private void AttachViewModel()
    {
        if (ReferenceEquals(_subscribedViewModel, DataContext))
            return;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _subscribedViewModel = DataContext as VaultViewModel;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(VaultViewModel.ShowSettings))
            Dispatcher.UIThread.Post(UpdateSettingsPresentation);
    }

    private void UpdateSettingsPresentation()
    {
        if (_isUnloaded || DataContext is not VaultViewModel viewModel)
            return;

        if (!viewModel.ShowSettings)
        {
            _settingsWindow?.Close();
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow { DataContext = viewModel };
        _settingsWindow = window;
        _ = ShowSettingsWindowAsync(window, owner, viewModel);
    }

    private async Task ShowSettingsWindowAsync(
        SettingsWindow window,
        Window owner,
        VaultViewModel viewModel)
    {
        try
        {
            await window.ShowDialog(owner);
        }
        finally
        {
            if (ReferenceEquals(_settingsWindow, window))
                _settingsWindow = null;
            if (!_isUnloaded && ReferenceEquals(DataContext, viewModel) && viewModel.ShowSettings)
                viewModel.DismissSettings();
        }
    }

    private void Items_SelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (sender is ListBox listBox && DataContext is VaultViewModel viewModel)
        {
            viewModel.SetSelectedItems(
                listBox.SelectedItems?.OfType<VaultItemViewModel>() ?? []);
        }
    }

    private void Item_PointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (sender is not Control { DataContext: VaultItemViewModel item } control)
            return;
        PointerPoint point = args.GetCurrentPoint(control);
        if (point.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
            return;

        ListBox? list = control.FindAncestorOfType<ListBox>();
        bool keepSelection = list?.SelectedItems?.Contains(item) == true;
        SelectItem(control, item, keepSelection);
    }

    private async void Item_DoubleTapped(object? sender, TappedEventArgs args)
    {
        if (sender is not Control { DataContext: VaultItemViewModel item } ||
            DataContext is not VaultViewModel viewModel || _isActivatingItem)
            return;
        args.Handled = true;
        _isActivatingItem = true;
        try
        {
            await viewModel.ActivateItemAsync(item);
        }
        finally
        {
            _isActivatingItem = false;
        }
    }

    private void ItemActionsButton_Click(object? sender, RoutedEventArgs args)
    {
        if (sender is Button { DataContext: VaultItemViewModel item } button)
            SelectItem(button, item, preserveExisting: false);
    }

    private void SelectItem(Control origin, VaultItemViewModel item, bool preserveExisting)
    {
        ListBox? list = origin.FindAncestorOfType<ListBox>();
        if (list is null)
            return;
        if (!preserveExisting)
            list.SelectedItems?.Clear();
        if (list.SelectedItems?.Contains(item) != true)
            list.SelectedItems?.Add(item);
        if (DataContext is VaultViewModel viewModel)
            viewModel.SetSelectedItems(list.SelectedItems?.OfType<VaultItemViewModel>() ?? [item]);
    }

    private async void OpenMenuItem_Click(object? sender, RoutedEventArgs args) =>
        await ExecuteAsync(viewModel => viewModel.OpenSelectedCommand);

    private async void ExportMenuItem_Click(object? sender, RoutedEventArgs args) =>
        await ExecuteAsync(viewModel => viewModel.ExportSelectedCommand);

    private async void RenameMenuItem_Click(object? sender, RoutedEventArgs args) =>
        await ExecuteAsync(viewModel => viewModel.RequestRenameCommand);

    private async void MoveMenuItem_Click(object? sender, RoutedEventArgs args) =>
        await ExecuteAsync(viewModel => viewModel.RequestMoveCommand);

    private async void DeleteMenuItem_Click(object? sender, RoutedEventArgs args) =>
        await ExecuteAsync(viewModel => viewModel.RequestDeleteCommand);

    private async Task ExecuteAsync(Func<VaultViewModel, AsyncCommand> selectCommand)
    {
        if (DataContext is VaultViewModel viewModel)
            await selectCommand(viewModel).ExecuteAsync();
    }
}
