using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Nte.App.Services;

public sealed class AvaloniaFilePickerService(
    Func<TopLevel?> getTopLevel,
    AppLifecycleCoordinator? lifecycleCoordinator = null) : IFilePickerService
{
    private static readonly FilePickerFileType VaultArchiveFileType = new("NTE-Tresorarchiv")
    {
        Patterns = ["*.ntevault"],
        MimeTypes = ["application/zip", "application/octet-stream"]
    };

    public async Task<IReadOnlyList<IReadableExternalFile>> PickDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: false);
        using IDisposable? interaction = lifecycleCoordinator?.BeginExternalInteraction();
        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Dokumente importieren",
                AllowMultiple = true
            });
        cancellationToken.ThrowIfCancellationRequested();
        return files.Select(file => (IReadableExternalFile)new ReadableStorageFile(file)).ToArray();
    }

    public async Task<IReadableExternalFile?> PickVaultArchiveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: false);
        using IDisposable? interaction = lifecycleCoordinator?.BeginExternalInteraction();
        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Tresorarchiv importieren",
                AllowMultiple = false,
                FileTypeFilter = [VaultArchiveFileType]
            });
        cancellationToken.ThrowIfCancellationRequested();
        return files.Count == 0 ? null : new ReadableStorageFile(files[0]);
    }

    public async Task<IWritableExternalFile?> PickDocumentExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: true);
        using IDisposable? interaction = lifecycleCoordinator?.BeginExternalInteraction();
        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Dokument exportieren",
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true
        });
        cancellationToken.ThrowIfCancellationRequested();
        return file is null ? null : new WritableStorageFile(file);
    }

    public async Task<IWritableExternalFolder?> PickExportFolderAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: false, requireFolder: true);
        using IDisposable? interaction = lifecycleCoordinator?.BeginExternalInteraction();
        IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Zielordner für die Auswahl wählen",
                AllowMultiple = false
            });
        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count == 0 ? null : new WritableStorageFolder(folders[0]);
    }

    public async Task<IWritableExternalFile?> PickVaultArchiveExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: true);
        using IDisposable? interaction = lifecycleCoordinator?.BeginExternalInteraction();
        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Tresorarchiv exportieren",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "ntevault",
            FileTypeChoices = [VaultArchiveFileType],
            SuggestedFileType = VaultArchiveFileType,
            ShowOverwritePrompt = true
        });
        cancellationToken.ThrowIfCancellationRequested();
        return file is null ? null : new WritableStorageFile(file);
    }

    private IStorageProvider GetStorageProvider(bool requireSave, bool requireFolder = false)
    {
        TopLevel topLevel = getTopLevel()
            ?? throw new InvalidOperationException("The application window is not available.");
        IStorageProvider storage = topLevel.StorageProvider;
        bool isUnavailable = requireFolder
            ? !storage.CanPickFolder
            : requireSave ? !storage.CanSave : !storage.CanOpen;
        if (isUnavailable)
            throw new NotSupportedException("The platform does not provide the required file picker.");
        return storage;
    }

    private sealed class ReadableStorageFile(IStorageFile file) : IReadableExternalFile
    {
        public string Name => file.Name;

        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await file.OpenReadAsync();
        }
    }

    private sealed class WritableStorageFile(IStorageFile file) : IWritableExternalFile
    {
        public string Name => file.Name;

        public async Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await file.OpenWriteAsync();
        }
    }

    private sealed class WritableStorageFolder(IStorageFolder folder) : IWritableExternalFolder
    {
        public string Name => folder.Name;

        public async Task<IWritableExternalFile> CreateUniqueFileAsync(
            string suggestedFileName,
            CancellationToken cancellationToken = default)
        {
            string name = await GetUniqueNameAsync(
                suggestedFileName,
                preserveExtension: true,
                cancellationToken);
            IStorageFile? file = await folder.CreateFileAsync(name);
            cancellationToken.ThrowIfCancellationRequested();
            if (file is null)
                throw new IOException($"The platform could not create the export file '{name}'.");
            return new WritableStorageFile(file);
        }

        public async Task<IWritableExternalFolder> CreateUniqueFolderAsync(
            string suggestedFolderName,
            CancellationToken cancellationToken = default)
        {
            string name = await GetUniqueNameAsync(
                suggestedFolderName,
                preserveExtension: false,
                cancellationToken);
            IStorageFolder? child = await folder.CreateFolderAsync(name);
            cancellationToken.ThrowIfCancellationRequested();
            if (child is null)
                throw new IOException($"The platform could not create the export folder '{name}'.");
            return new WritableStorageFolder(child);
        }

        private async Task<string> GetUniqueNameAsync(
            string suggestedName,
            bool preserveExtension,
            CancellationToken cancellationToken)
        {
            var existingNames = new List<string>();
            await foreach (IStorageItem item in folder.GetItemsAsync().WithCancellation(cancellationToken))
                existingNames.Add(item.Name);
            return ExternalExportNamePolicy.CreateUnique(
                suggestedName,
                existingNames,
                preserveExtension);
        }
    }
}
