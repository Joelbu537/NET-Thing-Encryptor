using NET_Thing_Encryptor;
using Nte.App.Services;
using System.ComponentModel;

namespace Nte.App.ViewModels;

public sealed class AppShellViewModel : ObservableObject, IDisposable
{
    private readonly IVaultApplicationService _vault;
    private readonly IFilePickerService _filePicker;
    private readonly Action<VaultPreferences> _applyPreferences;
    private readonly bool _useDocumentWindows;
    private readonly SynchronizationContext? _synchronizationContext;
    private object _currentPage = new LoadingViewModel();
    private string _statusMessage = string.Empty;
    private bool _disposed;

    public AppShellViewModel(
        IVaultApplicationService vault,
        IFilePickerService filePicker,
        SynchronizationContext? synchronizationContext = null,
        Action<VaultPreferences>? applyPreferences = null,
        bool useDocumentWindows = false)
    {
        _vault = vault;
        _filePicker = filePicker;
        _applyPreferences = applyPreferences ?? (_ => { });
        _useDocumentWindows = useDocumentWindows;
        _synchronizationContext = synchronizationContext ?? SynchronizationContext.Current;
        _vault.NotificationRaised += OnNotificationRaised;
    }

    public object CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (ReferenceEquals(_currentPage, value))
                return;
            if (_currentPage is VaultViewModel previousVault)
                previousVault.PropertyChanged -= OnCurrentPagePropertyChanged;
            if (!SetProperty(ref _currentPage, value))
                return;
            if (value is VaultViewModel currentVault)
                currentVault.PropertyChanged += OnCurrentPagePropertyChanged;
            OnPropertyChanged(nameof(FolderStatisticsText));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string VersionText
    {
        get
        {
            Version? version = typeof(AppShellViewModel).Assembly.GetName().Version;
            return version is null ? "v?" : $"v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string FolderStatisticsText =>
        CurrentPage is VaultViewModel vault ? vault.FolderStatisticsText : string.Empty;

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

    public bool HandleBackRequested() =>
        CurrentPage is VaultViewModel vault && vault.HandleBackRequested();

    public bool LockForSecurity(SessionLockReason reason)
    {
        if (CurrentPage is not VaultViewModel vault)
            return false;

        string message = reason == SessionLockReason.Background
            ? "Tresor beim Verlassen der App automatisch gesperrt."
            : string.Empty;
        vault.LockImmediately(message);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _vault.NotificationRaised -= OnNotificationRaised;
        if (CurrentPage is VaultViewModel vaultPage)
        {
            vaultPage.PropertyChanged -= OnCurrentPagePropertyChanged;
            vaultPage.Dispose();
        }
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
        var page = new VaultViewModel(
            _vault,
            _filePicker,
            () => ShowUnlock(),
            SetStatus,
            _applyPreferences,
            _useDocumentWindows);
        CurrentPage = page;
        await page.InitializeAsync();
    }

    private void SetStatus(string message) => StatusMessage = message;

    private void OnCurrentPagePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(VaultViewModel.FolderStatisticsText))
            OnPropertyChanged(nameof(FolderStatisticsText));
    }

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
