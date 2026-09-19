using NET_Thing_Encryptor;

namespace Nte.App.Services;

public interface IVaultApplicationService : IDisposable
{
    event EventHandler<VaultNotificationEventArgs>? NotificationRaised;

    bool HasPersistedVault { get; }
    bool IsUnlocked { get; }
    bool IsRemoteVault { get; }
    string StorageLocation { get; }

    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    Task ConnectRemoteVaultAsync(
        string address,
        string accessPassword,
        CancellationToken cancellationToken = default);
    Task UseLocalVaultAsync(CancellationToken cancellationToken = default);
    Task<bool> UnlockAsync(string password, CancellationToken cancellationToken = default);
    void Lock();
    Task<IReadOnlyList<VaultItem>> GetFolderItemsAsync(
        ulong folderId,
        CancellationToken cancellationToken = default);
    Task CreateFolderAsync(
        string name,
        ulong parentFolderId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultItem>> SearchFilesAsync(
        VaultSearchCriteria criteria,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultFolderTarget>> GetFolderTargetsAsync(
        CancellationToken cancellationToken = default);
    Task RenameObjectAsync(
        ulong id,
        string newName,
        CancellationToken cancellationToken = default);
    Task MoveObjectAsync(
        ulong id,
        ulong targetFolderId,
        CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(
        ulong id,
        CancellationToken cancellationToken = default);
    Task<VaultFileContent> ReadFileAsync(
        ulong id,
        CancellationToken cancellationToken = default);
    Task SaveFileContentAsync(
        ulong id,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);
    Task<VaultPreferences> GetPreferencesAsync(
        CancellationToken cancellationToken = default);
    Task SavePreferencesAsync(
        VaultPreferences preferences,
        CancellationToken cancellationToken = default);
    Task ImportFileAsync(
        Stream source,
        string fileName,
        ulong parentFolderId,
        string objectName,
        CancellationToken cancellationToken = default);
    Task ExportFileAsync(
        ulong fileId,
        Stream destination,
        CancellationToken cancellationToken = default);
    Task<int> ExportVaultAsync(
        Stream destination,
        CancellationToken cancellationToken = default,
        IProgress<VaultArchiveExportProgress>? progress = null);
    Task<int> ImportVaultAsync(
        Stream source,
        CancellationToken cancellationToken = default,
        IProgress<VaultArchiveImportProgress>? progress = null);
}
