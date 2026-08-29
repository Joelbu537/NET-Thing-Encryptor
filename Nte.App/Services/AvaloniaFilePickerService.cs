using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Nte.App.Services;

public sealed class AvaloniaFilePickerService(Func<TopLevel?> getTopLevel) : IFilePickerService
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
        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Dokumente importieren",
                AllowMultiple = true
            });
        return files.Select(file => (IReadableExternalFile)new ReadableStorageFile(file)).ToArray();
    }

    public async Task<IReadableExternalFile?> PickVaultArchiveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: false);
        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Tresorarchiv importieren",
                AllowMultiple = false,
                FileTypeFilter = [VaultArchiveFileType]
            });
        return files.Count == 0 ? null : new ReadableStorageFile(files[0]);
    }

    public async Task<IWritableExternalFile?> PickDocumentExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: true);
        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Dokument exportieren",
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true
        });
        return file is null ? null : new WritableStorageFile(file);
    }

    public async Task<IWritableExternalFile?> PickVaultArchiveExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);
        cancellationToken.ThrowIfCancellationRequested();
        IStorageProvider storage = GetStorageProvider(requireSave: true);
        IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Tresorarchiv exportieren",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "ntevault",
            FileTypeChoices = [VaultArchiveFileType],
            SuggestedFileType = VaultArchiveFileType,
            ShowOverwritePrompt = true
        });
        return file is null ? null : new WritableStorageFile(file);
    }

    private IStorageProvider GetStorageProvider(bool requireSave)
    {
        TopLevel topLevel = getTopLevel()
            ?? throw new InvalidOperationException("The application window is not available.");
        IStorageProvider storage = topLevel.StorageProvider;
        if (requireSave ? !storage.CanSave : !storage.CanOpen)
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
}
