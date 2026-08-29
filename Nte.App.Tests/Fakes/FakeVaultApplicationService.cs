using NET_Thing_Encryptor;
using Nte.App.Services;

namespace Nte.App.Tests.Fakes;

internal sealed class FakeVaultApplicationService : IVaultApplicationService
{
    public event EventHandler<VaultNotificationEventArgs>? NotificationRaised;

    public bool HasPersistedVault { get; set; }
    public bool IsUnlocked { get; private set; }
    public bool InitializeResult { get; set; } = true;
    public bool UnlockResult { get; set; } = true;
    public int ArchiveImportResult { get; set; } = 4;
    public int ArchiveExportResult { get; set; } = 5;
    public byte[] ExportedFileContent { get; set; } = "exported"u8.ToArray();
    public Dictionary<ulong, IReadOnlyList<VaultItem>> Folders { get; } = [];
    public List<(string Name, ulong ParentId)> CreatedFolders { get; } = [];
    public List<(string FileName, ulong ParentId, string ObjectName, byte[] Content)> Imports { get; } = [];
    public List<ulong> Exports { get; } = [];
    public int UnlockCalls { get; private set; }
    public int ArchiveImportCalls { get; private set; }
    public int ArchiveExportCalls { get; private set; }
    public bool Disposed { get; private set; }

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(InitializeResult);

    public Task<bool> UnlockAsync(string password, CancellationToken cancellationToken = default)
    {
        UnlockCalls++;
        IsUnlocked = UnlockResult;
        return Task.FromResult(UnlockResult);
    }

    public void Lock() => IsUnlocked = false;

    public Task<IReadOnlyList<VaultItem>> GetFolderItemsAsync(
        ulong folderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Folders.GetValueOrDefault(folderId, []));

    public Task CreateFolderAsync(
        string name,
        ulong parentFolderId,
        CancellationToken cancellationToken = default)
    {
        CreatedFolders.Add((name, parentFolderId));
        return Task.CompletedTask;
    }

    public async Task ImportFileAsync(
        Stream source,
        string fileName,
        ulong parentFolderId,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        using var content = new MemoryStream();
        await source.CopyToAsync(content, cancellationToken);
        Imports.Add((fileName, parentFolderId, objectName, content.ToArray()));
    }

    public async Task ExportFileAsync(
        ulong fileId,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        Exports.Add(fileId);
        await destination.WriteAsync(ExportedFileContent, cancellationToken);
    }

    public async Task<int> ExportVaultAsync(
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArchiveExportCalls++;
        await destination.WriteAsync("archive"u8.ToArray(), cancellationToken);
        return ArchiveExportResult;
    }

    public async Task<int> ImportVaultAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArchiveImportCalls++;
        using var content = new MemoryStream();
        await source.CopyToAsync(content, cancellationToken);
        HasPersistedVault = true;
        return ArchiveImportResult;
    }

    public void RaiseNotification(string title, string message) =>
        NotificationRaised?.Invoke(
            this,
            new VaultNotificationEventArgs(title, message, VaultNotificationSeverity.Information));

    public void Dispose() => Disposed = true;
}
