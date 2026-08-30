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
    public IReadOnlyList<VaultItem> SearchResults { get; set; } = [];
    public VaultSearchCriteria? LastSearchCriteria { get; private set; }
    public IReadOnlyList<VaultFolderTarget> FolderTargets { get; set; } =
        [new VaultFolderTarget(0, "Tresor")];
    public VaultPreferences Preferences { get; set; } = new(
        true, 5, 1, 2, false, 5, false);
    public List<(string Name, ulong ParentId)> CreatedFolders { get; } = [];
    public List<(string FileName, ulong ParentId, string ObjectName, byte[] Content)> Imports { get; } = [];
    public List<ulong> Exports { get; } = [];
    public List<(ulong Id, string Name)> Renames { get; } = [];
    public List<(ulong Id, ulong TargetId)> Moves { get; } = [];
    public List<ulong> Deletes { get; } = [];
    public List<(ulong Id, byte[] Content)> SavedContents { get; } = [];
    public Dictionary<ulong, VaultFileContent> FileContents { get; } = [];
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

    public Task<IReadOnlyList<VaultItem>> SearchFilesAsync(
        VaultSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        LastSearchCriteria = criteria;
        return Task.FromResult(SearchResults);
    }

    public Task<IReadOnlyList<VaultFolderTarget>> GetFolderTargetsAsync(
        CancellationToken cancellationToken = default) => Task.FromResult(FolderTargets);

    public Task RenameObjectAsync(
        ulong id,
        string newName,
        CancellationToken cancellationToken = default)
    {
        Renames.Add((id, newName));
        return Task.CompletedTask;
    }

    public Task MoveObjectAsync(
        ulong id,
        ulong targetFolderId,
        CancellationToken cancellationToken = default)
    {
        Moves.Add((id, targetFolderId));
        return Task.CompletedTask;
    }

    public Task DeleteObjectAsync(ulong id, CancellationToken cancellationToken = default)
    {
        Deletes.Add(id);
        return Task.CompletedTask;
    }

    public Task<VaultFileContent> ReadFileAsync(
        ulong id,
        CancellationToken cancellationToken = default) => Task.FromResult(
            FileContents.GetValueOrDefault(id)
            ?? new VaultFileContent(id, "file", FileType.other, "bin", [1, 2, 3]));

    public Task SaveFileContentAsync(
        ulong id,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        SavedContents.Add((id, content.ToArray()));
        return Task.CompletedTask;
    }

    public Task<VaultPreferences> GetPreferencesAsync(
        CancellationToken cancellationToken = default) => Task.FromResult(Preferences);

    public Task SavePreferencesAsync(
        VaultPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        Preferences = preferences;
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
