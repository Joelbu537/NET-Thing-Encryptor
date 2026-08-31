using Nte.App.Services;

namespace Nte.App.Tests.Fakes;

internal sealed class FakeFilePickerService : IFilePickerService
{
    public IReadOnlyList<IReadableExternalFile> Documents { get; set; } = [];
    public IReadableExternalFile? VaultImport { get; set; }
    public MemoryExternalFile? DocumentExport { get; set; }
    public MemoryExternalFolder? ExportFolder { get; set; }
    public MemoryExternalFile? VaultExport { get; set; }

    public Task<IReadOnlyList<IReadableExternalFile>> PickDocumentsAsync(
        CancellationToken cancellationToken = default) => Task.FromResult(Documents);

    public Task<IReadableExternalFile?> PickVaultArchiveAsync(
        CancellationToken cancellationToken = default) => Task.FromResult(VaultImport);

    public Task<IWritableExternalFile?> PickDocumentExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        DocumentExport ??= new MemoryExternalFile(suggestedFileName, []);
        return Task.FromResult<IWritableExternalFile?>(DocumentExport);
    }

    public Task<IWritableExternalFolder?> PickExportFolderAsync(
        CancellationToken cancellationToken = default)
    {
        ExportFolder ??= new MemoryExternalFolder("Export");
        return Task.FromResult<IWritableExternalFolder?>(ExportFolder);
    }

    public Task<IWritableExternalFile?> PickVaultArchiveExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        VaultExport ??= new MemoryExternalFile(suggestedFileName, []);
        return Task.FromResult<IWritableExternalFile?>(VaultExport);
    }
}

internal sealed class MemoryExternalFolder(string name) : IWritableExternalFolder
{
    public string Name { get; } = name;
    public Dictionary<string, MemoryExternalFile> Files { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MemoryExternalFolder> Folders { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<IWritableExternalFile> CreateUniqueFileAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string uniqueName = ExternalExportNamePolicy.CreateUnique(
            suggestedFileName,
            Files.Keys.Concat(Folders.Keys),
            preserveExtension: true);
        var file = new MemoryExternalFile(uniqueName, []);
        Files.Add(uniqueName, file);
        return Task.FromResult<IWritableExternalFile>(file);
    }

    public Task<IWritableExternalFolder> CreateUniqueFolderAsync(
        string suggestedFolderName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string uniqueName = ExternalExportNamePolicy.CreateUnique(
            suggestedFolderName,
            Files.Keys.Concat(Folders.Keys),
            preserveExtension: false);
        var folder = new MemoryExternalFolder(uniqueName);
        Folders.Add(uniqueName, folder);
        return Task.FromResult<IWritableExternalFolder>(folder);
    }

    public MemoryExternalFile AddFile(string fileName, byte[] content)
    {
        var file = new MemoryExternalFile(fileName, []);
        Files.Add(fileName, file);
        using Stream destination = file.OpenWriteAsync().GetAwaiter().GetResult();
        destination.Write(content);
        return file;
    }

    public MemoryExternalFolder AddFolder(string folderName)
    {
        var folder = new MemoryExternalFolder(folderName);
        Folders.Add(folderName, folder);
        return folder;
    }
}

internal sealed class MemoryExternalFile(string name, byte[] content)
    : IReadableExternalFile, IWritableExternalFile
{
    private MemoryStream _writtenContent = new();

    public string Name { get; } = name;
    public byte[] SourceContent { get; } = content;
    public byte[] WrittenContent => _writtenContent.ToArray();

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream(SourceContent, writable: false));

    public Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
    {
        _writtenContent = new MemoryStream();
        return Task.FromResult<Stream>(_writtenContent);
    }
}
