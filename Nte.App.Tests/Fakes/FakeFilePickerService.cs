using Nte.App.Services;

namespace Nte.App.Tests.Fakes;

internal sealed class FakeFilePickerService : IFilePickerService
{
    public IReadOnlyList<IReadableExternalFile> Documents { get; set; } = [];
    public IReadableExternalFile? VaultImport { get; set; }
    public MemoryExternalFile? DocumentExport { get; set; }
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

    public Task<IWritableExternalFile?> PickVaultArchiveExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        VaultExport ??= new MemoryExternalFile(suggestedFileName, []);
        return Task.FromResult<IWritableExternalFile?>(VaultExport);
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
