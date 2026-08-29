using NET_Thing_Encryptor;
using Nte.App.Services;

namespace Nte.App.ViewModels;

public sealed class AppShellViewModel : ObservableObject, IDisposable
{
    private readonly IVaultApplicationService _vault;
    private readonly IFilePickerService _filePicker;
    private readonly SynchronizationContext? _synchronizationContext;
    private object _currentPage = new LoadingViewModel();
    private string _statusMessage = "Bereit";
    private bool _disposed;

    public AppShellViewModel(
        IVaultApplicationService vault,
        IFilePickerService filePicker,
        SynchronizationContext? synchronizationContext = null)
    {
        _vault = vault;
        _filePicker = filePicker;
        _synchronizationContext = synchronizationContext ?? SynchronizationContext.Current;
        _vault.NotificationRaised += OnNotificationRaised;
    }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task InitializeAsync()
    {
        CurrentPage = new LoadingViewModel();
        string error = string.Empty;
        try
        {
            if (!await _vault.InitializeAsync())
                error = "Die Tresordaten konnten nicht geladen werden.";
        }
        catch (Exception ex)
        {
            error = $"Die Tresordaten konnten nicht geladen werden: {ex.Message}";
        }

        ShowUnlock(error);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _vault.NotificationRaised -= OnNotificationRaised;
        _vault.Dispose();
        _disposed = true;
    }

    private void ShowUnlock(string error = "")
    {
        CurrentPage = new UnlockViewModel(
            _vault,
            _filePicker,
            ShowVaultAsync,
            SetStatus,
            error);
    }

    private async Task ShowVaultAsync()
    {
        var page = new VaultViewModel(_vault, _filePicker, () => ShowUnlock(), SetStatus);
        CurrentPage = page;
        await page.InitializeAsync();
    }

    private void SetStatus(string message) => StatusMessage = message;

    private void OnNotificationRaised(object? sender, VaultNotificationEventArgs args)
    {
        string message = $"{args.Title}: {args.Message}";
        if (_synchronizationContext is not null &&
            SynchronizationContext.Current != _synchronizationContext)
        {
            _synchronizationContext.Post(_ => SetStatus(message), null);
            return;
        }

        SetStatus(message);
    }
}
