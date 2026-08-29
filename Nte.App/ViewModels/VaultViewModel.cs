using System.Collections.ObjectModel;
using Nte.App.Services;

namespace Nte.App.ViewModels;

public sealed class VaultViewModel : ObservableObject
{
    private readonly IVaultApplicationService _vault;
    private readonly IFilePickerService _filePicker;
    private readonly Action _onLocked;
    private readonly Action<string> _setStatus;
    private readonly List<(ulong Id, string Name)> _path = [(0, "Tresor")];
    private VaultItemViewModel? _selectedItem;
    private string _newFolderName = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isLocked;
    private CancellationTokenSource? _operationCancellation;

    public VaultViewModel(
        IVaultApplicationService vault,
        IFilePickerService filePicker,
        Action onLocked,
        Action<string> setStatus)
    {
        _vault = vault;
        _filePicker = filePicker;
        _onLocked = onLocked;
        _setStatus = setStatus;

        BackCommand = new AsyncCommand(GoBackAsync, () => !IsBusy && _path.Count > 1);
        OpenSelectedCommand = new AsyncCommand(
            OpenSelectedAsync,
            () => !IsBusy && SelectedItem?.IsFolder == true);
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        ImportDocumentsCommand = new AsyncCommand(
            ImportDocumentsAsync,
            () => !IsBusy && CurrentFolderId != 0);
        ExportSelectedCommand = new AsyncCommand(
            ExportSelectedAsync,
            () => !IsBusy && SelectedItem is { IsFolder: false });
        CreateFolderCommand = new AsyncCommand(
            CreateFolderAsync,
            () => !IsBusy && !string.IsNullOrWhiteSpace(NewFolderName));
        ExportVaultCommand = new AsyncCommand(ExportVaultAsync, () => !IsBusy);
        LockCommand = new AsyncCommand(LockAsync, () => !IsBusy);
    }

    public ObservableCollection<VaultItemViewModel> Items { get; } = [];

    public VaultItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
                return;
            OpenSelectedCommand.NotifyCanExecuteChanged();
            ExportSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    public string NewFolderName
    {
        get => _newFolderName;
        set
        {
            if (SetProperty(ref _newFolderName, value))
                CreateFolderCommand.NotifyCanExecuteChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;
            NotifyCommands();
        }
    }

    public ulong CurrentFolderId => _path[^1].Id;
    public string Breadcrumb => string.Join("  ›  ", _path.Select(part => part.Name));
    public bool IsRoot => CurrentFolderId == 0;
    public bool HasItems => Items.Count != 0;
    public string EmptyMessage => IsRoot
        ? "Noch keine Ordner vorhanden. Lege den ersten Ordner an."
        : "Dieser Ordner ist leer.";

    public AsyncCommand BackCommand { get; }
    public AsyncCommand OpenSelectedCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand ImportDocumentsCommand { get; }
    public AsyncCommand ExportSelectedCommand { get; }
    public AsyncCommand CreateFolderCommand { get; }
    public AsyncCommand ExportVaultCommand { get; }
    public AsyncCommand LockCommand { get; }

    public Task InitializeAsync() => RefreshAsync();

    public bool HandleBackRequested()
    {
        if (_isLocked)
            return false;
        if (IsBusy)
            return true;
        if (!IsRoot)
        {
            _ = BackCommand.ExecuteAsync();
            return true;
        }

        LockImmediately("Tresor gesperrt.");
        return true;
    }

    public void LockImmediately(string statusMessage)
    {
        if (_isLocked)
            return;

        _isLocked = true;
        _operationCancellation?.Cancel();
        _vault.Lock();
        Items.Clear();
        SelectedItem = null;
        _setStatus(statusMessage);
        _onLocked();
    }

    private async Task RefreshAsync()
    {
        await RunBusyAsync(async cancellationToken =>
        {
            IReadOnlyList<VaultItem> items = await _vault.GetFolderItemsAsync(
                CurrentFolderId,
                cancellationToken);
            Items.Clear();
            foreach (VaultItem item in items)
                Items.Add(new VaultItemViewModel(item));
            SelectedItem = null;
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(EmptyMessage));
        }, "Der Ordner konnte nicht geladen werden");
    }

    private async Task OpenSelectedAsync()
    {
        VaultItemViewModel? item = SelectedItem;
        if (item?.IsFolder != true)
            return;

        _path.Add((item.Id, item.Name));
        NotifyLocationChanged();
        await RefreshAsync();
    }

    private async Task GoBackAsync()
    {
        if (_path.Count <= 1)
            return;
        _path.RemoveAt(_path.Count - 1);
        NotifyLocationChanged();
        await RefreshAsync();
    }

    private async Task CreateFolderAsync()
    {
        string name = NewFolderName.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        bool succeeded = await RunBusyAsync(
            cancellationToken => _vault.CreateFolderAsync(name, CurrentFolderId, cancellationToken),
            "Der Ordner konnte nicht erstellt werden");
        if (!succeeded)
            return;

        NewFolderName = string.Empty;
        _setStatus($"Ordner „{name}“ erstellt.");
        await RefreshAsync();
    }

    private async Task ImportDocumentsAsync()
    {
        if (CurrentFolderId == 0)
            return;

        await RunBusyAsync(async cancellationToken =>
        {
            IReadOnlyList<IReadableExternalFile> files = await _filePicker.PickDocumentsAsync(
                cancellationToken);
            if (files.Count == 0)
                return;

            var names = Items.Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int imported = 0;
            foreach (IReadableExternalFile file in files)
            {
                string objectName = CreateUniqueName(file.Name, names);
                await using Stream source = await file.OpenReadAsync(cancellationToken);
                await _vault.ImportFileAsync(
                    source,
                    file.Name,
                    CurrentFolderId,
                    objectName,
                    cancellationToken);
                names.Add(objectName);
                imported++;
            }

            _setStatus(imported == 1
                ? "Ein Dokument importiert."
                : $"{imported} Dokumente importiert.");
            await ReloadItemsAsync(cancellationToken);
        }, "Dokumente konnten nicht importiert werden");
    }

    private async Task ExportSelectedAsync()
    {
        VaultItemViewModel? item = SelectedItem;
        if (item is null || item.IsFolder)
            return;

        await RunBusyAsync(async cancellationToken =>
        {
            IWritableExternalFile? file = await _filePicker.PickDocumentExportAsync(
                item.SuggestedFileName,
                cancellationToken);
            if (file is null)
                return;

            await using Stream destination = await file.OpenWriteAsync(cancellationToken);
            await _vault.ExportFileAsync(item.Id, destination, cancellationToken);
            _setStatus($"„{item.SuggestedFileName}“ exportiert.");
        }, "Das Dokument konnte nicht exportiert werden");
    }

    private async Task ExportVaultAsync()
    {
        await RunBusyAsync(async cancellationToken =>
        {
            string suggestedName = $"NET-Thing-Encryptor-{DateTime.Now:yyyy-MM-dd}.ntevault";
            IWritableExternalFile? file = await _filePicker.PickVaultArchiveExportAsync(
                suggestedName,
                cancellationToken);
            if (file is null)
                return;

            await using Stream destination = await file.OpenWriteAsync(cancellationToken);
            int count = await _vault.ExportVaultAsync(destination, cancellationToken);
            _setStatus($"Tresorarchiv exportiert ({count} Objekte). Bewahre es wie den Tresor geschützt auf.");
        }, "Das Tresorarchiv konnte nicht exportiert werden");
    }

    private Task LockAsync()
    {
        LockImmediately("Tresor gesperrt.");
        return Task.CompletedTask;
    }

    private async Task ReloadItemsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<VaultItem> items = await _vault.GetFolderItemsAsync(
            CurrentFolderId,
            cancellationToken);
        Items.Clear();
        foreach (VaultItem item in items)
            Items.Add(new VaultItemViewModel(item));
        SelectedItem = null;
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private async Task<bool> RunBusyAsync(
        Func<CancellationToken, Task> action,
        string errorPrefix)
    {
        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await action(cancellation.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{errorPrefix}: {ex.Message}";
            return false;
        }
        finally
        {
            if (ReferenceEquals(_operationCancellation, cancellation))
                _operationCancellation = null;
            IsBusy = false;
        }
    }

    private void NotifyLocationChanged()
    {
        OnPropertyChanged(nameof(CurrentFolderId));
        OnPropertyChanged(nameof(Breadcrumb));
        OnPropertyChanged(nameof(IsRoot));
        OnPropertyChanged(nameof(EmptyMessage));
        NotifyCommands();
    }

    private void NotifyCommands()
    {
        BackCommand.NotifyCanExecuteChanged();
        OpenSelectedCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        ImportDocumentsCommand.NotifyCanExecuteChanged();
        ExportSelectedCommand.NotifyCanExecuteChanged();
        CreateFolderCommand.NotifyCanExecuteChanged();
        ExportVaultCommand.NotifyCanExecuteChanged();
        LockCommand.NotifyCanExecuteChanged();
    }

    private static string CreateUniqueName(string fileName, ISet<string> existingNames)
    {
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = fileName;
        if (!existingNames.Contains(baseName))
            return baseName;

        int suffix = 2;
        while (existingNames.Contains($"{baseName} ({suffix})"))
            suffix++;
        return $"{baseName} ({suffix})";
    }
}
