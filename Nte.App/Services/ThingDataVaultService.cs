using NET_Thing_Encryptor;

namespace Nte.App.Services;

public sealed class ThingDataVaultService : IVaultApplicationService
{
    private readonly IVaultStorage _storage;
    private readonly VaultArchiveService _archiveService;
    private bool _disposed;

    public ThingDataVaultService(IVaultStorage storage, VaultArchiveService? archiveService = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _archiveService = archiveService ?? new VaultArchiveService();
        ThingData.ConfigureStorage(_storage);
        ThingData.NotificationRaised += ForwardNotification;
    }

    public event EventHandler<VaultNotificationEventArgs>? NotificationRaised;

    public bool HasPersistedVault => _storage.Exists(0);
    public bool IsUnlocked => ThingData.IsSessionUnlocked;

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        return ThingData.LoadMainData();
    }

    public async Task<bool> UnlockAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        bool unlocked = await ThingData.AttemptDecrypt(password).ConfigureAwait(false);
        if (unlocked)
            await ThingData.SaveRootAsync().ConfigureAwait(false);
        return unlocked;
    }

    public void Lock()
    {
        ThrowIfDisposed();
        ThingData.LockSession();
    }

    public async Task<IReadOnlyList<VaultItem>> GetFolderItemsAsync(
        ulong folderId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        List<ThingObjectLink> content = await ThingData.LoadFolderContent(folderId)
            .ConfigureAwait(false);
        return content
            .OrderByDescending(item => item.Type == FileType.folder)
            .ThenBy(item => item.Name, new NaturalStringComparer())
            .Select(item => new VaultItem(
                item.ID,
                item.Name,
                item.Type,
                item.Size,
                item.Extension,
                item.CreatedAt))
            .ToArray();
    }

    public async Task CreateFolderAsync(
        string name,
        ulong parentFolderId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await ThingData.CreateFolderAsync(name, parentFolderId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ImportFileAsync(
        Stream source,
        string fileName,
        ulong parentFolderId,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await ThingData.ImportFileAsync(
            source,
            fileName,
            parentFolderId,
            objectName,
            cancellationToken).ConfigureAwait(false);
    }

    public Task ExportFileAsync(
        ulong fileId,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return ThingData.ExportFileAsync(fileId, destination, cancellationToken);
    }

    public async Task<int> ExportVaultAsync(
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        VaultArchiveExportResult result = await _archiveService.ExportCurrentAsync(
            destination,
            cancellationToken).ConfigureAwait(false);
        return result.ObjectCount;
    }

    public async Task<int> ImportVaultAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (IsUnlocked)
            throw new InvalidOperationException("Lock the current vault before importing an archive.");

        VaultArchiveImportResult result = await _archiveService.ImportAsync(
            source,
            _storage,
            cancellationToken).ConfigureAwait(false);
        ThingData.LockSession();
        if (!await ThingData.LoadMainData().ConfigureAwait(false))
            throw new InvalidDataException("The imported vault root could not be loaded.");
        return result.ObjectCount;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ThingData.NotificationRaised -= ForwardNotification;
        ThingData.LockSession();
        _disposed = true;
    }

    private void ForwardNotification(object? sender, VaultNotificationEventArgs args) =>
        NotificationRaised?.Invoke(this, args);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
