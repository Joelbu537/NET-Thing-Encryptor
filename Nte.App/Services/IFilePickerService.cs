namespace Nte.App.Services;

public interface IReadableExternalFile
{
    string Name { get; }
    Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default);
}

public interface IWritableExternalFile
{
    string Name { get; }
    Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default);
}

public interface IFilePickerService
{
    Task<IReadOnlyList<IReadableExternalFile>> PickDocumentsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadableExternalFile?> PickVaultArchiveAsync(
        CancellationToken cancellationToken = default);
    Task<IWritableExternalFile?> PickDocumentExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);
    Task<IWritableExternalFile?> PickVaultArchiveExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);
}
