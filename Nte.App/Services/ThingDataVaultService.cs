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

    public async Task<IReadOnlyList<VaultItem>> SearchFilesAsync(
        VaultSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(criteria);
        IReadOnlyList<GlobalFileSearchResult> results = await GlobalFileSearch.SearchAsync(
            new GlobalFileSearchCriteria(
                criteria.Name,
                criteria.Type,
                criteria.Extension,
                criteria.MinimumSize,
                criteria.MaximumSize,
                criteria.CreatedFrom,
                criteria.CreatedTo),
            cancellationToken).ConfigureAwait(false);
        return results.Select(result => new VaultItem(
            result.ID,
            result.Name,
            result.Type,
            result.Size,
            result.Extension,
            result.CreatedAt,
            result.FolderPath)).ToArray();
    }

    public async Task<IReadOnlyList<VaultFolderTarget>> GetFolderTargetsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThingRoot root = ThingData.Root
            ?? throw new InvalidOperationException("The root data has not been loaded.");
        var targets = new List<VaultFolderTarget> { new(0, "Tresor") };
        var visited = new HashSet<ulong>();
        await CollectFolderTargetsAsync(
            root.Content ?? [],
            "Tresor",
            targets,
            visited,
            cancellationToken).ConfigureAwait(false);
        return targets;
    }

    public Task RenameObjectAsync(
        ulong id,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        return ThingData.RenameObjectAsync(id, newName);
    }

    public async Task MoveObjectAsync(
        ulong id,
        ulong targetFolderId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ThingObject obj = await ThingData.LoadFileAsync(id).ConfigureAwait(false)
            ?? throw new FileNotFoundException("The object could not be loaded.");
        if (obj is ThingFolder folder)
        {
            await ThingData.MoveFolderToFolderAsync(folder.ID, targetFolderId)
                .ConfigureAwait(false);
            return;
        }

        if (obj is not ThingFile file)
            throw new InvalidDataException("The stored object has an unsupported type.");
        try
        {
            await ThingData.MoveFileToFolderAsync(file, targetFolderId).ConfigureAwait(false);
        }
        finally
        {
            file.ReleaseContent();
        }
    }

    public Task DeleteObjectAsync(
        ulong id,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        return ThingData.DeleteObject(id);
    }

    public async Task<VaultFileContent> ReadFileAsync(
        ulong id,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ThingFile file = await ThingData.LoadFileAsync<ThingFile>(id).ConfigureAwait(false)
            ?? throw new FileNotFoundException("The file could not be loaded.");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] content = file.DetachContent()
                ?? throw new InvalidDataException("The stored file has no content.");
            return new VaultFileContent(
                file.ID,
                file.Name,
                file.Type,
                file.Extension,
                content);
        }
        finally
        {
            file.ReleaseContent();
        }
    }

    public Task SaveFileContentAsync(
        ulong id,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return ThingData.ReplaceFileContentAsync(id, content, cancellationToken);
    }

    public Task<VaultPreferences> GetPreferencesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ThingRootSettings settings = ThingData.GetRootSettings();
        return Task.FromResult(new VaultPreferences(
            settings.DarkMode,
            settings.AutoLockMinutes,
            settings.ImageViewerPreviousBufferCount,
            settings.ImageViewerNextBufferCount,
            settings.RandomiseSelectedImage,
            settings.ImageAutoplayIntervalSeconds,
            settings.LoopOnAutoplay));
    }

    public Task SavePreferencesAsync(
        VaultPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(preferences);
        return ThingData.UpdateRootSettingsAsync(
            new ThingRootSettings(
                preferences.DarkMode,
                preferences.AutoLockMinutes,
                preferences.PreviousImageBufferCount,
                preferences.NextImageBufferCount,
                preferences.IncludeSelectedImageWhenRandomising,
                preferences.ImageAutoplayIntervalSeconds,
                preferences.LoopImageAutoplay),
            cancellationToken);
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

    private static async Task CollectFolderTargetsAsync(
        IEnumerable<ThingObjectLink> content,
        string parentPath,
        ICollection<VaultFolderTarget> targets,
        ISet<ulong> visited,
        CancellationToken cancellationToken)
    {
        foreach (ThingObjectLink link in content.Where(item => item.Type == FileType.folder)
                     .OrderBy(item => item.Name, new NaturalStringComparer()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(link.ID))
                continue;
            string path = $"{parentPath} › {link.Name}";
            targets.Add(new VaultFolderTarget(link.ID, path));
            ThingFolder? folder = await ThingData.LoadFileAsync<ThingFolder>(link.ID)
                .ConfigureAwait(false);
            if (folder is not null)
            {
                await CollectFolderTargetsAsync(
                    folder.Content,
                    path,
                    targets,
                    visited,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
