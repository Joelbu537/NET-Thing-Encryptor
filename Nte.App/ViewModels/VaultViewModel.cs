using System.Collections.ObjectModel;
using NET_Thing_Encryptor;
using Nte.App.Services;

namespace Nte.App.ViewModels;

public sealed class VaultViewModel : ObservableObject, IDisposable
{
    private static readonly string[] SearchTypeValues =
        ["Alle Typen", "Text", "Bild", "Audio", "Video", "Andere Dateien"];
    private readonly IVaultApplicationService _vault;
    private readonly IFilePickerService _filePicker;
    private readonly Action _onLocked;
    private readonly Action<string> _setStatus;
    private readonly Action<VaultPreferences> _applyPreferences;
    private readonly List<(ulong Id, string Name)> _path = [(0, "Tresor")];
    private readonly List<VaultItemViewModel> _folderItems = [];
    private readonly List<VaultItemViewModel> _selectedItems = [];
    private VaultItemViewModel? _selectedItem;
    private VaultFolderTarget? _selectedMoveTarget;
    private VaultDocumentViewModel? _activeDocument;
    private string _newFolderName = string.Empty;
    private string _renameName = string.Empty;
    private string _searchQuery = string.Empty;
    private string _searchExtension = string.Empty;
    private string _minimumSizeText = string.Empty;
    private string _maximumSizeText = string.Empty;
    private string _selectedSearchType = SearchTypeValues[0];
    private string _errorMessage = string.Empty;
    private string _resultSummary = string.Empty;
    private DateTimeOffset? _createdFrom;
    private DateTimeOffset? _createdTo;
    private bool _searchGlobally;
    private bool _isShowingGlobalResults;
    private bool _isBusy;
    private bool _isLocked;
    private bool _showCreateFolderDialog;
    private bool _showRenameDialog;
    private bool _showMoveDialog;
    private bool _showDeleteConfirmation;
    private bool _showSettings;
    private VaultPreferences? _settingsSnapshot;
    private bool _darkMode;
    private int _autoLockMinutes = 5;
    private int _previousImageBufferCount = 1;
    private int _nextImageBufferCount = 2;
    private bool _includeSelectedImageWhenRandomising;
    private int _imageAutoplayIntervalSeconds = 5;
    private bool _loopImageAutoplay;
    private CancellationTokenSource? _operationCancellation;

    public VaultViewModel(
        IVaultApplicationService vault,
        IFilePickerService filePicker,
        Action onLocked,
        Action<string> setStatus,
        Action<VaultPreferences>? applyPreferences = null)
    {
        _vault = vault;
        _filePicker = filePicker;
        _onLocked = onLocked;
        _setStatus = setStatus;
        _applyPreferences = applyPreferences ?? (_ => { });

        BackCommand = new AsyncCommand(GoBackAsync, () => !IsBusy && _path.Count > 1);
        GoRootCommand = new AsyncCommand(GoRootAsync, () => !IsBusy && _path.Count > 1);
        OpenSelectedCommand = new AsyncCommand(
            OpenSelectedAsync,
            () => !IsBusy && SelectedItem is not null && _selectedItems.Count == 1);
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        ImportDocumentsCommand = new AsyncCommand(
            ImportDocumentsAsync,
            () => !IsBusy && CurrentFolderId != 0 && !IsShowingGlobalResults);
        ExportSelectedCommand = new AsyncCommand(
            ExportSelectedAsync,
            () => !IsBusy && _selectedItems.Count != 0);
        CreateFolderCommand = new AsyncCommand(
            CreateFolderAsync,
            () => !IsBusy && !IsShowingGlobalResults && !string.IsNullOrWhiteSpace(NewFolderName));
        RequestCreateFolderCommand = new AsyncCommand(
            RequestCreateFolderAsync,
            () => !IsBusy && !IsShowingGlobalResults);
        ExportVaultCommand = new AsyncCommand(ExportVaultAsync, () => !IsBusy);
        LockCommand = new AsyncCommand(LockAsync, () => !IsBusy);
        SearchCommand = new AsyncCommand(SearchAsync, () => !IsBusy);
        ClearSearchCommand = new AsyncCommand(ClearSearchAsync, () => !IsBusy);
        RenameCommand = new AsyncCommand(
            RenameAsync,
            () => !IsBusy && SelectedItem is not null && _selectedItems.Count == 1 &&
                  !string.IsNullOrWhiteSpace(RenameName));
        RequestRenameCommand = new AsyncCommand(
            RequestRenameAsync,
            () => !IsBusy && SelectedItem is not null && _selectedItems.Count == 1);
        MoveCommand = new AsyncCommand(MoveAsync, CanMove);
        RequestMoveCommand = new AsyncCommand(
            RequestMoveAsync,
            () => !IsBusy && _selectedItems.Count != 0);
        RequestDeleteCommand = new AsyncCommand(RequestDeleteAsync, () => !IsBusy && _selectedItems.Count != 0);
        ConfirmDeleteCommand = new AsyncCommand(DeleteAsync, () => !IsBusy && ShowDeleteConfirmation);
        CancelDeleteCommand = new AsyncCommand(CancelDeleteAsync, () => !IsBusy && ShowDeleteConfirmation);
        CancelActionDialogCommand = new AsyncCommand(CancelActionDialogAsync, () => !IsBusy && HasActionDialog);
        ToggleSettingsCommand = new AsyncCommand(ToggleSettingsAsync, () => !IsBusy);
        SavePreferencesCommand = new AsyncCommand(SavePreferencesAsync, () => !IsBusy);
    }

    public ObservableCollection<VaultItemViewModel> Items { get; } = [];
    public ObservableCollection<VaultFolderTarget> FolderTargets { get; } = [];
    public IReadOnlyList<string> SearchTypes => SearchTypeValues;

    public VaultItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
                return;
            _selectedItems.Clear();
            if (value is not null)
                _selectedItems.Add(value);
            RenameName = value?.Name ?? string.Empty;
            CloseActionDialogs();
            NotifySelectionCommands();
            OnPropertyChanged(nameof(SelectionCountText));
        }
    }

    public VaultFolderTarget? SelectedMoveTarget
    {
        get => _selectedMoveTarget;
        set
        {
            if (SetProperty(ref _selectedMoveTarget, value))
                MoveCommand.NotifyCanExecuteChanged();
        }
    }

    public VaultDocumentViewModel? ActiveDocument
    {
        get => _activeDocument;
        private set
        {
            if (!SetProperty(ref _activeDocument, value))
                return;
            OnPropertyChanged(nameof(HasActiveDocument));
            OnPropertyChanged(nameof(ShowVaultContent));
        }
    }

    public bool HasActiveDocument => ActiveDocument is not null;
    public bool ShowVaultContent => ActiveDocument is null;

    public string NewFolderName
    {
        get => _newFolderName;
        set
        {
            if (SetProperty(ref _newFolderName, value))
                CreateFolderCommand.NotifyCanExecuteChanged();
        }
    }

    public string RenameName
    {
        get => _renameName;
        set
        {
            if (SetProperty(ref _renameName, value))
                RenameCommand.NotifyCanExecuteChanged();
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value))
                return;
            if (!SearchGlobally && !IsShowingGlobalResults)
                ApplyLocalFilter();
        }
    }

    public bool SearchGlobally
    {
        get => _searchGlobally;
        set
        {
            if (!SetProperty(ref _searchGlobally, value))
                return;
            OnPropertyChanged(nameof(ShowAdvancedSearch));
            if (!value && !IsShowingGlobalResults)
                ApplyLocalFilter();
        }
    }

    public bool ShowAdvancedSearch => SearchGlobally;
    public string SearchExtension { get => _searchExtension; set => SetProperty(ref _searchExtension, value); }
    public string MinimumSizeText { get => _minimumSizeText; set => SetProperty(ref _minimumSizeText, value); }
    public string MaximumSizeText { get => _maximumSizeText; set => SetProperty(ref _maximumSizeText, value); }
    public string SelectedSearchType { get => _selectedSearchType; set => SetProperty(ref _selectedSearchType, value); }
    public DateTimeOffset? CreatedFrom { get => _createdFrom; set => SetProperty(ref _createdFrom, value); }
    public DateTimeOffset? CreatedTo { get => _createdTo; set => SetProperty(ref _createdTo, value); }

    public bool IsShowingGlobalResults
    {
        get => _isShowingGlobalResults;
        private set
        {
            if (!SetProperty(ref _isShowingGlobalResults, value))
                return;
            OnPropertyChanged(nameof(EmptyMessage));
            NotifyCommands();
        }
    }

    public string ResultSummary { get => _resultSummary; private set => SetProperty(ref _resultSummary, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

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

    public bool ShowDeleteConfirmation
    {
        get => _showDeleteConfirmation;
        private set
        {
            if (!SetProperty(ref _showDeleteConfirmation, value))
                return;
            ConfirmDeleteCommand.NotifyCanExecuteChanged();
            CancelDeleteCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasActionDialog));
            CancelActionDialogCommand.NotifyCanExecuteChanged();
        }
    }

    public bool ShowCreateFolderDialog
    {
        get => _showCreateFolderDialog;
        private set => SetActionDialogProperty(ref _showCreateFolderDialog, value, nameof(ShowCreateFolderDialog));
    }

    public bool ShowRenameDialog
    {
        get => _showRenameDialog;
        private set => SetActionDialogProperty(ref _showRenameDialog, value, nameof(ShowRenameDialog));
    }

    public bool ShowMoveDialog
    {
        get => _showMoveDialog;
        private set => SetActionDialogProperty(ref _showMoveDialog, value, nameof(ShowMoveDialog));
    }

    public bool HasActionDialog =>
        ShowCreateFolderDialog || ShowRenameDialog || ShowMoveDialog || ShowDeleteConfirmation;

    public string DeleteConfirmationText => _selectedItems.Count switch
    {
        0 => string.Empty,
        > 1 => $"{_selectedItems.Count} ausgewählte Objekte endgültig löschen? Enthaltene Ordner werden rekursiv gelöscht.",
        _ when SelectedItem!.IsFolder =>
            $"„{SelectedItem.Name}“ und alle enthaltenen Objekte endgültig löschen?",
        _ => $"„{SelectedItem!.SuggestedFileName}“ endgültig löschen?"
    };

    public bool ShowSettings { get => _showSettings; private set => SetProperty(ref _showSettings, value); }
    public bool DarkMode { get => _darkMode; set => SetProperty(ref _darkMode, value); }
    public int AutoLockMinutes
    {
        get => _autoLockMinutes;
        set => SetProperty(ref _autoLockMinutes, Math.Clamp(value, 0, ThingRoot.MaximumAutoLockMinutes));
    }
    public int PreviousImageBufferCount
    {
        get => _previousImageBufferCount;
        set => SetProperty(ref _previousImageBufferCount, Math.Clamp(value, 0, ThingRoot.MaximumImageViewerBufferCount));
    }
    public int NextImageBufferCount
    {
        get => _nextImageBufferCount;
        set => SetProperty(ref _nextImageBufferCount, Math.Clamp(value, 0, ThingRoot.MaximumImageViewerBufferCount));
    }
    public bool IncludeSelectedImageWhenRandomising
    {
        get => _includeSelectedImageWhenRandomising;
        set => SetProperty(ref _includeSelectedImageWhenRandomising, value);
    }
    public int ImageAutoplayIntervalSeconds
    {
        get => _imageAutoplayIntervalSeconds;
        set => SetProperty(ref _imageAutoplayIntervalSeconds, Math.Clamp(value, 1, ThingRoot.MaximumAutoplayIntervalSeconds));
    }
    public bool LoopImageAutoplay { get => _loopImageAutoplay; set => SetProperty(ref _loopImageAutoplay, value); }

    public ulong CurrentFolderId => _path[^1].Id;
    public string Breadcrumb => string.Join("  ›  ", _path.Select(part => part.Name));
    public bool IsRoot => CurrentFolderId == 0;
    public bool HasItems => Items.Count != 0;
    public string SelectionCountText => _selectedItems.Count switch
    {
        0 => "Keine Auswahl",
        1 => "1 Objekt ausgewählt",
        _ => $"{_selectedItems.Count} Objekte ausgewählt"
    };
    public string FolderStatisticsText
    {
        get
        {
            int folders = Items.Count(item => item.IsFolder);
            int files = Items.Count - folders;
            long totalSize = Items.Where(item => !item.IsFolder).Sum(item => item.Size);
            string fileText = files == 1 ? "1 Datei" : $"{files} Dateien";
            string folderText = folders == 1 ? "1 Ordner" : $"{folders} Ordner";
            return $"{fileText}   {folderText}   {totalSize.Sizeify()}";
        }
    }
    public string EmptyMessage => IsShowingGlobalResults
        ? "Die globale Suche hat keine Dokumente gefunden."
        : !string.IsNullOrWhiteSpace(SearchQuery)
            ? "Im aktuellen Ordner gibt es keine passenden Einträge."
            : IsRoot
                ? "Noch keine Ordner vorhanden. Lege den ersten Ordner an."
                : "Dieser Ordner ist leer.";

    public AsyncCommand BackCommand { get; }
    public AsyncCommand GoRootCommand { get; }
    public AsyncCommand OpenSelectedCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand ImportDocumentsCommand { get; }
    public AsyncCommand ExportSelectedCommand { get; }
    public AsyncCommand RequestCreateFolderCommand { get; }
    public AsyncCommand CreateFolderCommand { get; }
    public AsyncCommand ExportVaultCommand { get; }
    public AsyncCommand LockCommand { get; }
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand ClearSearchCommand { get; }
    public AsyncCommand RequestRenameCommand { get; }
    public AsyncCommand RenameCommand { get; }
    public AsyncCommand RequestMoveCommand { get; }
    public AsyncCommand MoveCommand { get; }
    public AsyncCommand RequestDeleteCommand { get; }
    public AsyncCommand ConfirmDeleteCommand { get; }
    public AsyncCommand CancelDeleteCommand { get; }
    public AsyncCommand CancelActionDialogCommand { get; }
    public AsyncCommand ToggleSettingsCommand { get; }
    public AsyncCommand SavePreferencesCommand { get; }

    public Task InitializeAsync() => RunBusyAsync(async cancellationToken =>
    {
        ApplyPreferences(await _vault.GetPreferencesAsync(cancellationToken));
        await ReloadFolderTargetsAsync(cancellationToken);
        await ReloadFolderItemsAsync(cancellationToken);
    }, "Der Tresor konnte nicht geladen werden");

    public void SetSelectedItems(IEnumerable<VaultItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _selectedItems.Clear();
        _selectedItems.AddRange(items.DistinctBy(item => item.Id));
        _selectedItem = _selectedItems.LastOrDefault();
        OnPropertyChanged(nameof(SelectedItem));
        RenameName = _selectedItems.Count == 1 ? _selectedItem!.Name : string.Empty;
        CloseActionDialogs();
        OnPropertyChanged(nameof(SelectionCountText));
        NotifySelectionCommands();
    }

    public Task ActivateItemAsync(VaultItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        SetSelectedItems([item]);
        return OpenSelectedAsync();
    }

    public bool HandleBackRequested()
    {
        if (_isLocked)
            return false;
        if (ActiveDocument is not null)
            return ActiveDocument.HandleBackRequested();
        if (HasActionDialog)
        {
            CloseActionDialogs();
            return true;
        }
        if (ShowSettings)
        {
            CloseSettings(restoreSnapshot: true);
            return true;
        }
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
        _settingsSnapshot = null;
        ShowSettings = false;
        CloseActionDialogs();
        CloseActiveDocument();
        _vault.Lock();
        _folderItems.Clear();
        Items.Clear();
        SelectedItem = null;
        OnPropertyChanged(nameof(FolderStatisticsText));
        _setStatus(statusMessage);
        _onLocked();
    }

    public void Dispose()
    {
        _operationCancellation?.Cancel();
        _settingsSnapshot = null;
        ShowSettings = false;
        CloseActionDialogs();
        CloseActiveDocument();
    }

    public void DismissSettings() => CloseSettings(restoreSnapshot: true);

    private Task RefreshAsync() => IsShowingGlobalResults ? SearchAsync() : RunBusyAsync(
        async cancellationToken =>
        {
            await ReloadFolderTargetsAsync(cancellationToken);
            await ReloadFolderItemsAsync(cancellationToken);
        },
        "Der Ordner konnte nicht geladen werden");

    private async Task OpenSelectedAsync()
    {
        VaultItemViewModel? item = SelectedItem;
        if (item is null)
            return;
        if (item.IsFolder)
        {
            _path.Add((item.Id, item.Name));
            SearchQuery = string.Empty;
            IsShowingGlobalResults = false;
            NotifyLocationChanged();
            await RefreshAsync();
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            VaultFileContent content = await _vault.ReadFileAsync(item.Id, cancellationToken);
            VaultImageSeriesOptions? imageSeries = item.Type == FileType.image
                ? CreateImageSeriesOptions()
                : null;
            ActiveDocument = new VaultDocumentViewModel(
                content,
                (data, token) => _vault.SaveFileContentAsync(item.Id, data, token),
                CloseActiveDocumentAndRefresh,
                _setStatus,
                imageSeries,
                imageSeries is null ? null : _vault.ReadFileAsync);
        }, "Der Inhalt konnte nicht geöffnet werden");
    }

    private VaultImageSeriesOptions CreateImageSeriesOptions()
    {
        IEnumerable<VaultItemViewModel> candidates = IsShowingGlobalResults
            ? Items
            : _folderItems;
        VaultImageReference[] images = candidates
            .Where(candidate => candidate.Type == FileType.image)
            .Select(candidate => new VaultImageReference(
                candidate.Id,
                candidate.Name,
                candidate.Extension))
            .ToArray();
        return new VaultImageSeriesOptions(
            images,
            IncludeSelectedImageWhenRandomising,
            ImageAutoplayIntervalSeconds,
            LoopImageAutoplay);
    }

    private async Task GoBackAsync()
    {
        if (_path.Count <= 1)
            return;
        _path.RemoveAt(_path.Count - 1);
        SearchQuery = string.Empty;
        IsShowingGlobalResults = false;
        NotifyLocationChanged();
        await RefreshAsync();
    }

    private async Task GoRootAsync()
    {
        if (_path.Count <= 1)
            return;
        _path.RemoveRange(1, _path.Count - 1);
        SearchQuery = string.Empty;
        IsShowingGlobalResults = false;
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
        ShowCreateFolderDialog = false;
        _setStatus($"Ordner „{name}“ erstellt.");
        await RefreshAsync();
    }

    private async Task ImportDocumentsAsync()
    {
        if (CurrentFolderId == 0)
            return;
        await RunBusyAsync(async cancellationToken =>
        {
            IReadOnlyList<IReadableExternalFile> files = await _filePicker.PickDocumentsAsync(cancellationToken);
            if (files.Count == 0)
                return;
            var names = new HashSet<string>(_folderItems.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
            int imported = 0;
            foreach (IReadableExternalFile file in files)
            {
                string objectName = CreateUniqueName(file.Name, names);
                await using Stream source = await file.OpenReadAsync(cancellationToken);
                await _vault.ImportFileAsync(source, file.Name, CurrentFolderId, objectName, cancellationToken);
                names.Add(objectName);
                imported++;
            }
            _setStatus(imported == 1 ? "Ein Dokument importiert." : $"{imported} Dokumente importiert.");
            await ReloadFolderItemsAsync(cancellationToken);
        }, "Dokumente konnten nicht importiert werden");
    }

    private async Task ExportSelectedAsync()
    {
        VaultItemViewModel[] items = _selectedItems.ToArray();
        if (items.Length == 0)
            return;

        if (items.Length == 1 && !items[0].IsFolder)
        {
            VaultItemViewModel item = items[0];
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
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            IWritableExternalFolder? exportFolder = await _filePicker.PickExportFolderAsync(cancellationToken);
            if (exportFolder is null)
                return;

            var activeFolderPath = new HashSet<ulong>();
            var count = new ExportCount();
            foreach (VaultItemViewModel item in items)
            {
                if (item.IsFolder)
                {
                    IWritableExternalFolder destination = await exportFolder.CreateUniqueFolderAsync(
                        item.Name,
                        cancellationToken);
                    ExportCount childCount = await ExportFolderAsync(
                        item.Id,
                        destination,
                        activeFolderPath,
                        cancellationToken);
                    count = new ExportCount(
                        count.Files + childCount.Files,
                        count.Folders + childCount.Folders + 1);
                }
                else
                {
                    await ExportFileToFolderAsync(
                        item.Id,
                        item.SuggestedFileName,
                        exportFolder,
                        cancellationToken);
                    count = count with { Files = count.Files + 1 };
                }
            }

            string fileText = count.Files == 1 ? "1 Datei" : $"{count.Files} Dateien";
            string folderText = count.Folders == 1 ? "1 Ordner" : $"{count.Folders} Ordner";
            _setStatus($"Auswahl exportiert: {fileText}, {folderText}. Vorhandene Namen wurden nicht überschrieben.");
        }, "Die Auswahl konnte nicht vollständig exportiert werden");
    }

    private async Task<ExportCount> ExportFolderAsync(
        ulong folderId,
        IWritableExternalFolder destination,
        ISet<ulong> activeFolderPath,
        CancellationToken cancellationToken)
    {
        if (!activeFolderPath.Add(folderId))
            throw new InvalidDataException("Der Tresor enthält einen Ordnerzyklus; der Export wurde abgebrochen.");

        try
        {
            int fileCount = 0;
            int folderCount = 0;
            IReadOnlyList<VaultItem> children = await _vault.GetFolderItemsAsync(folderId, cancellationToken);
            foreach (VaultItem child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (child.IsFolder)
                {
                    IWritableExternalFolder childDestination = await destination.CreateUniqueFolderAsync(
                        child.Name,
                        cancellationToken);
                    ExportCount childCount = await ExportFolderAsync(
                        child.Id,
                        childDestination,
                        activeFolderPath,
                        cancellationToken);
                    fileCount += childCount.Files;
                    folderCount += childCount.Folders + 1;
                }
                else
                {
                    await ExportFileToFolderAsync(
                        child.Id,
                        CreateSuggestedFileName(child.Name, child.Extension),
                        destination,
                        cancellationToken);
                    fileCount++;
                }
            }
            return new ExportCount(fileCount, folderCount);
        }
        finally
        {
            activeFolderPath.Remove(folderId);
        }
    }

    private async Task ExportFileToFolderAsync(
        ulong fileId,
        string suggestedFileName,
        IWritableExternalFolder destination,
        CancellationToken cancellationToken)
    {
        IWritableExternalFile file = await destination.CreateUniqueFileAsync(
            suggestedFileName,
            cancellationToken);
        await using Stream output = await file.OpenWriteAsync(cancellationToken);
        await _vault.ExportFileAsync(fileId, output, cancellationToken);
    }

    private Task SearchAsync()
    {
        if (!SearchGlobally && !IsShowingGlobalResults)
        {
            ApplyLocalFilter();
            return Task.CompletedTask;
        }
        return RunBusyAsync(async cancellationToken =>
        {
            long? minimumSize = ParseOptionalSize(MinimumSizeText, "Mindestgröße");
            long? maximumSize = ParseOptionalSize(MaximumSizeText, "Maximalgröße");
            if (minimumSize.HasValue && maximumSize.HasValue && minimumSize > maximumSize)
                throw new ArgumentException("Die Mindestgröße darf nicht über der Maximalgröße liegen.");
            DateOnly? createdFrom = CreatedFrom.HasValue ? DateOnly.FromDateTime(CreatedFrom.Value.Date) : null;
            DateOnly? createdTo = CreatedTo.HasValue ? DateOnly.FromDateTime(CreatedTo.Value.Date) : null;
            if (createdFrom.HasValue && createdTo.HasValue && createdFrom > createdTo)
                throw new ArgumentException("Das Startdatum darf nicht nach dem Enddatum liegen.");

            IReadOnlyList<VaultItem> results = await _vault.SearchFilesAsync(
                new VaultSearchCriteria(
                    SearchQuery.Trim(),
                    MapSearchType(SelectedSearchType),
                    SearchExtension.Trim(),
                    minimumSize,
                    maximumSize,
                    createdFrom,
                    createdTo),
                cancellationToken);
            _folderItems.Clear();
            ReplaceItems(results.Select(item => new VaultItemViewModel(item)));
            IsShowingGlobalResults = true;
            ResultSummary = results.Count == 1 ? "1 globaler Treffer" : $"{results.Count} globale Treffer";
        }, "Die globale Suche ist fehlgeschlagen");
    }

    private async Task ClearSearchAsync()
    {
        SearchQuery = string.Empty;
        SearchExtension = string.Empty;
        MinimumSizeText = string.Empty;
        MaximumSizeText = string.Empty;
        SelectedSearchType = SearchTypeValues[0];
        CreatedFrom = null;
        CreatedTo = null;
        SearchGlobally = false;
        IsShowingGlobalResults = false;
        ResultSummary = string.Empty;
        await RefreshAsync();
    }

    private async Task RenameAsync()
    {
        VaultItemViewModel? item = SelectedItem;
        string newName = RenameName.Trim();
        if (item is null || string.IsNullOrEmpty(newName))
            return;
        bool succeeded = await RunBusyAsync(
            cancellationToken => _vault.RenameObjectAsync(item.Id, newName, cancellationToken),
            "Das Objekt konnte nicht umbenannt werden");
        if (!succeeded)
            return;
        ShowRenameDialog = false;
        _setStatus($"„{item.Name}“ wurde in „{newName}“ umbenannt.");
        await RefreshAsync();
    }

    private async Task MoveAsync()
    {
        VaultItemViewModel[] items = _selectedItems.ToArray();
        VaultFolderTarget? target = SelectedMoveTarget;
        if (items.Length == 0 || target is null || (items.Any(item => !item.IsFolder) && target.Id == 0))
            return;
        bool succeeded = await RunBusyAsync(
            async cancellationToken =>
            {
                foreach (VaultItemViewModel item in items)
                    await _vault.MoveObjectAsync(item.Id, target.Id, cancellationToken);
            },
            "Das Objekt konnte nicht verschoben werden");
        if (!succeeded)
            return;
        ShowMoveDialog = false;
        _setStatus(items.Length == 1
            ? $"„{items[0].Name}“ wurde nach „{target.Path}“ verschoben."
            : $"{items.Length} Objekte wurden nach „{target.Path}“ verschoben.");
        await RefreshAsync();
    }

    private Task RequestDeleteAsync()
    {
        CloseActionDialogs();
        ShowDeleteConfirmation = _selectedItems.Count != 0;
        OnPropertyChanged(nameof(DeleteConfirmationText));
        return Task.CompletedTask;
    }

    private async Task DeleteAsync()
    {
        VaultItemViewModel[] items = _selectedItems.ToArray();
        if (items.Length == 0 || !ShowDeleteConfirmation)
            return;
        bool succeeded = await RunBusyAsync(
            async cancellationToken =>
            {
                foreach (VaultItemViewModel item in items)
                    await _vault.DeleteObjectAsync(item.Id, cancellationToken);
            },
            "Das Objekt konnte nicht gelöscht werden");
        if (!succeeded)
            return;
        ShowDeleteConfirmation = false;
        _setStatus(items.Length == 1
            ? $"„{items[0].Name}“ wurde gelöscht."
            : $"{items.Length} Objekte wurden gelöscht.");
        await RefreshAsync();
    }

    private Task CancelDeleteAsync()
    {
        CloseActionDialogs();
        return Task.CompletedTask;
    }

    private Task RequestCreateFolderAsync()
    {
        CloseActionDialogs();
        NewFolderName = string.Empty;
        ShowCreateFolderDialog = true;
        return Task.CompletedTask;
    }

    private Task RequestRenameAsync()
    {
        CloseActionDialogs();
        RenameName = SelectedItem?.Name ?? string.Empty;
        ShowRenameDialog = SelectedItem is not null && _selectedItems.Count == 1;
        return Task.CompletedTask;
    }

    private Task RequestMoveAsync()
    {
        CloseActionDialogs();
        SelectedMoveTarget = null;
        ShowMoveDialog = _selectedItems.Count != 0;
        return Task.CompletedTask;
    }

    private Task CancelActionDialogAsync()
    {
        CloseActionDialogs();
        return Task.CompletedTask;
    }

    private Task ToggleSettingsAsync()
    {
        if (ShowSettings)
        {
            CloseSettings(restoreSnapshot: true);
        }
        else
        {
            CloseActionDialogs();
            _settingsSnapshot = CapturePreferences();
            ShowSettings = true;
        }
        return Task.CompletedTask;
    }

    private async Task SavePreferencesAsync()
    {
        VaultPreferences preferences = CapturePreferences();
        bool succeeded = await RunBusyAsync(
            cancellationToken => _vault.SavePreferencesAsync(preferences, cancellationToken),
            "Die Einstellungen konnten nicht gespeichert werden");
        if (!succeeded)
            return;
        _applyPreferences(preferences);
        _setStatus("Einstellungen gespeichert und angewendet.");
        _settingsSnapshot = null;
        ShowSettings = false;
    }

    private async Task ExportVaultAsync()
    {
        await RunBusyAsync(async cancellationToken =>
        {
            string suggestedName = $"NET-Thing-Encryptor-{DateTime.Now:yyyy-MM-dd}.ntevault";
            IWritableExternalFile? file = await _filePicker.PickVaultArchiveExportAsync(suggestedName, cancellationToken);
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

    private async Task ReloadFolderItemsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<VaultItem> items = await _vault.GetFolderItemsAsync(CurrentFolderId, cancellationToken);
        _folderItems.Clear();
        _folderItems.AddRange(items.Select(item => new VaultItemViewModel(item)));
        IsShowingGlobalResults = false;
        ResultSummary = string.Empty;
        ApplyLocalFilter();
    }

    private async Task ReloadFolderTargetsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<VaultFolderTarget> targets = await _vault.GetFolderTargetsAsync(cancellationToken);
        FolderTargets.Clear();
        foreach (VaultFolderTarget target in targets)
            FolderTargets.Add(target);
        SelectedMoveTarget = null;
    }

    private void ApplyLocalFilter()
    {
        string query = SearchQuery.Trim();
        IEnumerable<VaultItemViewModel> filtered = string.IsNullOrEmpty(query)
            ? _folderItems
            : _folderItems.Where(item =>
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Extension.Contains(query.TrimStart('.'), StringComparison.OrdinalIgnoreCase) ||
                item.KindText.Contains(query, StringComparison.OrdinalIgnoreCase));
        ReplaceItems(filtered);
        ResultSummary = string.IsNullOrEmpty(query)
            ? string.Empty
            : Items.Count == 1 ? "1 lokaler Treffer" : $"{Items.Count} lokale Treffer";
    }

    private void ReplaceItems(IEnumerable<VaultItemViewModel> items)
    {
        Items.Clear();
        foreach (VaultItemViewModel item in items)
            Items.Add(item);
        SelectedItem = null;
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(FolderStatisticsText));
    }

    private void ApplyPreferences(VaultPreferences preferences)
    {
        DarkMode = preferences.DarkMode;
        AutoLockMinutes = preferences.AutoLockMinutes;
        PreviousImageBufferCount = preferences.PreviousImageBufferCount;
        NextImageBufferCount = preferences.NextImageBufferCount;
        IncludeSelectedImageWhenRandomising = preferences.IncludeSelectedImageWhenRandomising;
        ImageAutoplayIntervalSeconds = preferences.ImageAutoplayIntervalSeconds;
        LoopImageAutoplay = preferences.LoopImageAutoplay;
        _applyPreferences(preferences);
    }

    private async Task<bool> RunBusyAsync(Func<CancellationToken, Task> action, string errorPrefix)
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

    private bool CanMove()
    {
        if (IsBusy || _selectedItems.Count == 0 || SelectedMoveTarget is null)
            return false;
        return _selectedItems.All(item => item.IsFolder) || SelectedMoveTarget.Id != 0;
    }

    private void CloseActiveDocument()
    {
        VaultDocumentViewModel? document = ActiveDocument;
        ActiveDocument = null;
        document?.Dispose();
    }

    private void CloseActiveDocumentAndRefresh()
    {
        CloseActiveDocument();
        if (!_isLocked)
            _ = RefreshAsync();
    }

    private void NotifyLocationChanged()
    {
        OnPropertyChanged(nameof(CurrentFolderId));
        OnPropertyChanged(nameof(Breadcrumb));
        OnPropertyChanged(nameof(IsRoot));
        OnPropertyChanged(nameof(EmptyMessage));
        NotifyCommands();
    }

    private void NotifySelectionCommands()
    {
        OpenSelectedCommand.NotifyCanExecuteChanged();
        ExportSelectedCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        MoveCommand.NotifyCanExecuteChanged();
        RequestDeleteCommand.NotifyCanExecuteChanged();
        RequestRenameCommand.NotifyCanExecuteChanged();
        RequestMoveCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(DeleteConfirmationText));
    }

    private void NotifyCommands()
    {
        BackCommand.NotifyCanExecuteChanged();
        GoRootCommand.NotifyCanExecuteChanged();
        OpenSelectedCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        ImportDocumentsCommand.NotifyCanExecuteChanged();
        ExportSelectedCommand.NotifyCanExecuteChanged();
        RequestCreateFolderCommand.NotifyCanExecuteChanged();
        CreateFolderCommand.NotifyCanExecuteChanged();
        ExportVaultCommand.NotifyCanExecuteChanged();
        LockCommand.NotifyCanExecuteChanged();
        SearchCommand.NotifyCanExecuteChanged();
        ClearSearchCommand.NotifyCanExecuteChanged();
        RequestRenameCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        RequestMoveCommand.NotifyCanExecuteChanged();
        MoveCommand.NotifyCanExecuteChanged();
        RequestDeleteCommand.NotifyCanExecuteChanged();
        ConfirmDeleteCommand.NotifyCanExecuteChanged();
        CancelDeleteCommand.NotifyCanExecuteChanged();
        CancelActionDialogCommand.NotifyCanExecuteChanged();
        ToggleSettingsCommand.NotifyCanExecuteChanged();
        SavePreferencesCommand.NotifyCanExecuteChanged();
    }

    private static long? ParseOptionalSize(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!long.TryParse(value.Trim(), out long result) || result < 0)
            throw new ArgumentException($"{label} muss eine nicht negative Bytezahl sein.");
        return result;
    }

    private static FileType? MapSearchType(string value) => value switch
    {
        "Text" => FileType.text,
        "Bild" => FileType.image,
        "Audio" => FileType.audio,
        "Video" => FileType.video,
        "Andere Dateien" => FileType.other,
        _ => null
    };

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

    private static string CreateSuggestedFileName(string name, string extension) =>
        string.IsNullOrWhiteSpace(extension)
            ? name
            : $"{name}.{extension.TrimStart('.')}";

    private void SetActionDialogProperty(ref bool field, bool value, string propertyName)
    {
        if (!SetProperty(ref field, value, propertyName))
            return;
        OnPropertyChanged(nameof(HasActionDialog));
        CancelActionDialogCommand.NotifyCanExecuteChanged();
    }

    private void CloseActionDialogs()
    {
        ShowCreateFolderDialog = false;
        ShowRenameDialog = false;
        ShowMoveDialog = false;
        ShowDeleteConfirmation = false;
    }

    private VaultPreferences CapturePreferences() => new(
        DarkMode,
        AutoLockMinutes,
        PreviousImageBufferCount,
        NextImageBufferCount,
        IncludeSelectedImageWhenRandomising,
        ImageAutoplayIntervalSeconds,
        LoopImageAutoplay);

    private void CloseSettings(bool restoreSnapshot)
    {
        if (restoreSnapshot && _settingsSnapshot is { } snapshot)
        {
            DarkMode = snapshot.DarkMode;
            AutoLockMinutes = snapshot.AutoLockMinutes;
            PreviousImageBufferCount = snapshot.PreviousImageBufferCount;
            NextImageBufferCount = snapshot.NextImageBufferCount;
            IncludeSelectedImageWhenRandomising = snapshot.IncludeSelectedImageWhenRandomising;
            ImageAutoplayIntervalSeconds = snapshot.ImageAutoplayIntervalSeconds;
            LoopImageAutoplay = snapshot.LoopImageAutoplay;
        }
        _settingsSnapshot = null;
        ShowSettings = false;
    }

    private readonly record struct ExportCount(int Files = 0, int Folders = 0);
}
