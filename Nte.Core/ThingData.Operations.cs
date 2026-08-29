using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NET_Thing_Encryptor;

public static partial class ThingData
{
    private static async Task RunMutationAsync(
        Func<Task> mutation,
        Func<Task<IReadOnlyCollection<ulong>>>? backupIdProvider = null)
    {
        BeginSaving();
        await MutationLock.WaitAsync();
        IVaultStorageSnapshot? backup = null;
        ThingRoot liveRoot = RequireRoot();
        ThingRoot rootSnapshot = CloneRootForRollback(liveRoot);
        try
        {
            IReadOnlyCollection<ulong>? backupIds = backupIdProvider is null
                ? null
                : await backupIdProvider();
            backup = await CurrentStorage.CreateSnapshotAsync(backupIds).ConfigureAwait(false);
            await mutation();
        }
        catch
        {
            RestoreRootFromSnapshot(liveRoot, rootSnapshot);
            Root = liveRoot;
            if (backup is not null)
                await backup.RestoreAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (backup is not null)
                await backup.DisposeAsync().ConfigureAwait(false);
            MutationLock.Release();
            EndSaving();
        }
    }

    private static ThingRoot CloneRootForRollback(ThingRoot root)
    {
        ThingRoot clone = (ThingRoot)root.Clone();
        clone.Content = root.Content?
            .Select(link => new ThingObjectLink(
                link.ID,
                link.Name,
                link.Type,
                link.Size,
                link.PreviewContent is null ? null : (byte[])link.PreviewContent.Clone())
            {
                CreatedAt = link.CreatedAt,
                Extension = link.Extension
            })
            .ToList();
        return clone;
    }

    private static void RestoreRootFromSnapshot(ThingRoot destination, ThingRoot snapshot)
    {
        destination.Name = snapshot.Name;
        destination.ID = snapshot.ID;
        destination.ParentID = snapshot.ParentID;
        destination.Salt = (byte[])snapshot.Salt.Clone();
        destination.SaveLocation = snapshot.SaveLocation;
        destination.ImportLocation = snapshot.ImportLocation;
        destination.ExportLocation = snapshot.ExportLocation;
        destination.DarkMode = snapshot.DarkMode;
        destination.ImageViewerPreviousBufferCount = snapshot.ImageViewerPreviousBufferCount;
        destination.ImageViewerNextBufferCount = snapshot.ImageViewerNextBufferCount;
        destination.AutoLockMinutes = snapshot.AutoLockMinutes;
        destination.RandomiseSelectedImage = snapshot.RandomiseSelectedImage;
        destination.ImageAutoplayIntervalSeconds = snapshot.ImageAutoplayIntervalSeconds;
        destination.LoopOnAutoplay = snapshot.LoopOnAutoplay;
        destination.ContentEncrypted = snapshot.ContentEncrypted;
        destination.Content = snapshot.Content?
            .Select(link => new ThingObjectLink(
                link.ID,
                link.Name,
                link.Type,
                link.Size,
                link.PreviewContent is null ? null : (byte[])link.PreviewContent.Clone())
            {
                CreatedAt = link.CreatedAt,
                Extension = link.Extension
            })
            .ToList();
    }

    private static bool ContainsNameConflict(
        IEnumerable<ThingObjectLink> content,
        string name,
        ulong excludedID = 0)
    {
        return content.Any(link =>
            link.ID != excludedID &&
            string.Equals(link.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task MoveFileToFolderAsync(ThingFile file, ulong folderID)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (folderID == 0)
            throw new ArgumentException("Files cannot be placed directly in the root.", nameof(folderID));

        await RunMutationAsync(async () =>
        {
            ThingRoot root = RequireRoot();
            ThingFolder folder = await LoadFileAsync<ThingFolder>(folderID)
                ?? throw new FileNotFoundException("The target folder could not be loaded.");

            if (file.ParentID == folderID)
            {
                if (folder.Content.Any(x => x.ID == file.ID))
                    return;
                throw new InvalidDataException("The current parent does not reference this file.");
            }
            if (folder.Content.Any(x => x.ID == file.ID))
                throw new InvalidOperationException("The target folder already contains this file.");
            if (ContainsNameConflict(folder.Content, file.Name, file.ID))
                throw new InvalidOperationException("The target folder already contains an item with this name.");

            ThingObjectLink? link;

            if (file.ParentID == 0)
            {
                link = root.Content?.FirstOrDefault(x => x.ID == file.ID);
                if (link is not null)
                {
                    root.Content!.Remove(link);
                    await SaveRootAsync();
                }
            }
            else
            {
                ThingFolder oldFolder = await LoadFileAsync<ThingFolder>(file.ParentID)
                    ?? throw new FileNotFoundException("The current parent folder could not be loaded.");
                link = oldFolder.Content.FirstOrDefault(x => x.ID == file.ID);
                if (link is null)
                    throw new InvalidDataException("The current parent does not reference this file.");
                oldFolder.Content.Remove(link);
                await SaveFileAsync(oldFolder);
            }

            link ??= new ThingObjectLink(
                file.ID,
                file.Name,
                file.Type,
                file.Content?.LongLength ?? 0,
                extension: file.Extension);
            link.Name = file.Name;
            link.Type = file.Type;
            link.Size = file.Content?.LongLength ?? link.Size;
            link.Extension = file.Extension;

            file.ParentID = folder.ID;
            await SaveFileAsync(file);
            folder.Content.Add(link);
            await SaveFileAsync(folder);
        }, () => Task.FromResult<IReadOnlyCollection<ulong>>(
            [file.ID, file.ParentID, folderID]));
    }

    public static async Task MoveFolderToFolderAsync(ulong folderID, ulong parentFolderID)
    {
        if (folderID == 0)
            throw new ArgumentException("The root folder cannot be moved.", nameof(folderID));
        if (folderID == parentFolderID)
            throw new InvalidOperationException("A folder cannot contain itself.");

        await RunMutationAsync(async () =>
        {
            ThingRoot root = RequireRoot();
            ThingFolder folder = await LoadFileAsync<ThingFolder>(folderID)
                ?? throw new FileNotFoundException("The folder could not be loaded.");
            if (folder.ParentID == parentFolderID)
            {
                bool parentReferencesFolder;
                if (parentFolderID == 0)
                {
                    parentReferencesFolder = root.Content?.Any(x => x.ID == folder.ID) == true;
                }
                else
                {
                    ThingFolder currentParent = await LoadFileAsync<ThingFolder>(parentFolderID)
                        ?? throw new FileNotFoundException("The current parent folder could not be loaded.");
                    parentReferencesFolder = currentParent.Content.Any(x => x.ID == folder.ID);
                }

                if (parentReferencesFolder)
                    return;
                throw new InvalidDataException("The current parent does not reference this folder.");
            }

            ulong ancestorID = parentFolderID;
            while (ancestorID != 0)
            {
                if (ancestorID == folderID)
                    throw new InvalidOperationException("A folder cannot be moved into one of its descendants.");
                ThingFolder ancestor = await LoadFileAsync<ThingFolder>(ancestorID)
                    ?? throw new FileNotFoundException("An ancestor folder could not be loaded.");
                ancestorID = ancestor.ParentID;
            }

            ThingFolder? oldParent = null;
            ThingObjectLink? link;
            if (folder.ParentID == 0)
            {
                link = root.Content?.FirstOrDefault(x => x.ID == folder.ID);
            }
            else
            {
                oldParent = await LoadFileAsync<ThingFolder>(folder.ParentID)
                    ?? throw new FileNotFoundException("The current parent folder could not be loaded.");
                link = oldParent.Content.FirstOrDefault(x => x.ID == folder.ID);
            }

            if (link is null)
                throw new InvalidDataException("The current parent does not reference this folder.");

            ThingFolder? newParent = parentFolderID == 0
                ? null
                : await LoadFileAsync<ThingFolder>(parentFolderID);
            if (parentFolderID != 0 && newParent is null)
                throw new FileNotFoundException("The target parent folder could not be loaded.");
            if (newParent?.Content.Any(x => x.ID == folder.ID) == true ||
                (parentFolderID == 0 && root.Content?.Any(x => x.ID == folder.ID) == true))
                throw new InvalidOperationException("The target parent already contains this folder.");
            if (newParent is not null &&
                ContainsNameConflict(newParent.Content, folder.Name, folder.ID))
                throw new InvalidOperationException("The target parent already contains an item with this name.");
            if (parentFolderID == 0 &&
                ContainsNameConflict(root.Content ?? [], folder.Name, folder.ID))
                throw new InvalidOperationException("The root already contains an item with this name.");

            root.Content?.RemoveAll(x => x.ID == folder.ID);
            oldParent?.Content.RemoveAll(x => x.ID == folder.ID);

            folder.ParentID = parentFolderID;
            await SaveFileAsync(folder);

            if (newParent is null)
            {
                root.Content ??= [];
                root.Content.Add(link);
                await SaveRootAsync();
            }
            else
            {
                newParent.Content.Add(link);
                await SaveFileAsync(newParent);
            }

            if (oldParent is not null)
                await SaveFileAsync(oldParent);
            else if (parentFolderID != 0)
                await SaveRootAsync();
        }, () => CollectObjectAndParentBackupIdsAsync(folderID, parentFolderID));
    }

    public static async Task RenameObjectAsync(ulong id, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        if (id == 0)
            throw new ArgumentException("The root object cannot be renamed.", nameof(id));

        await RunMutationAsync(async () =>
        {
            ThingRoot root = RequireRoot();
            ThingObject obj = await LoadFileAsync(id)
                ?? throw new FileNotFoundException("The object could not be loaded.");
            if (obj.ParentID == 0)
            {
                if (ContainsNameConflict(root.Content ?? [], newName, id))
                    throw new InvalidOperationException("The root already contains an item with this name.");
            }
            else
            {
                ThingFolder parentForNameCheck = await LoadFileAsync<ThingFolder>(obj.ParentID)
                    ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                if (ContainsNameConflict(parentForNameCheck.Content, newName, id))
                    throw new InvalidOperationException("The parent folder already contains an item with this name.");
            }

            obj.Name = newName;
            await SaveFileAsync(obj);

            if (obj.ParentID == 0)
            {
                ThingObjectLink link = root.Content?.FirstOrDefault(x => x.ID == id)
                    ?? throw new InvalidDataException("The root does not reference the renamed object.");
                link.Name = newName;
                await SaveRootAsync();
            }
            else
            {
                ThingFolder parent = await LoadFileAsync<ThingFolder>(obj.ParentID)
                    ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                ThingObjectLink link = parent.Content.FirstOrDefault(x => x.ID == id)
                    ?? throw new InvalidDataException(
                        "The parent folder does not reference the renamed object.");
                link.Name = newName;
                await SaveFileAsync(parent);
            }
        }, () => CollectObjectAndParentBackupIdsAsync(id));
    }

    private static async Task<IReadOnlyCollection<ulong>> CollectObjectAndParentBackupIdsAsync(
        ulong id,
        ulong additionalId = 0)
    {
        ThingObject obj = await LoadFileAsync(id)
            ?? throw new FileNotFoundException("The object could not be loaded.");
        var ids = new HashSet<ulong> { id };
        if (obj.ParentID != 0)
            ids.Add(obj.ParentID);
        if (additionalId != 0)
            ids.Add(additionalId);
        return ids;
    }

    public static async Task UpdateObjectSizeAsync(
        ulong id,
        long size,
        ulong? knownParentID = null)
    {
        if (id == 0)
            throw new ArgumentException("The root object has no parent link.", nameof(id));
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        BeginSaving();
        await MutationLock.WaitAsync();
        try
        {
            ThingRoot root = RequireRoot();
            ulong parentID;
            if (knownParentID.HasValue)
            {
                parentID = knownParentID.Value;
            }
            else
            {
                ThingObject obj = await LoadFileAsync(id)
                    ?? throw new FileNotFoundException("The object could not be loaded.");
                parentID = obj.ParentID;
                if (obj is ThingFile file)
                {
                    long releasedBytes = file.Content?.LongLength ?? 0;
                    file.ReleaseContent();
                    MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
                }
            }

            if (parentID == 0)
            {
                ThingObjectLink? link = root.Content?.FirstOrDefault(x => x.ID == id);
                if (link is not null && link.Size != size)
                {
                    link.Size = size;
                    await SaveRootAsync();
                }
            }
            else
            {
                ThingFolder parent = await LoadFileAsync<ThingFolder>(parentID)
                    ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                ThingObjectLink? link = parent.Content.FirstOrDefault(x => x.ID == id);
                if (link is not null && link.Size != size)
                {
                    link.Size = size;
                    await SaveFileAsync(parent);
                }
            }
        }
        finally
        {
            MutationLock.Release();
            EndSaving();
        }
    }

    private static async Task DeleteFileCoreAsync(ulong fileID)
    {
        ThingRoot root = RequireRoot();
        ThingFile file = await LoadFileAsync<ThingFile>(fileID)
            ?? throw new FileNotFoundException("The file could not be loaded.");
        long releasedBytes = file.Content?.LongLength ?? 0;
        file.ReleaseContent();
        MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
        Debug.WriteLine("Attempting to delete file: " + file.Name);

        if (file.ParentID == 0)
        {
            root.Content?.RemoveAll(x => x.ID == file.ID);
            await SaveRootAsync();
        }
        else
        {
            ThingFolder parent = await LoadFileAsync<ThingFolder>(file.ParentID)
                ?? throw new FileNotFoundException("The parent folder could not be loaded.");
            parent.Content.RemoveAll(x => x.ID == file.ID);
            await SaveFileAsync(parent);
        }

        await CurrentStorage.DeleteAsync(fileID).ConfigureAwait(false);
    }

    private static async Task DeleteFolderCoreAsync(ulong folderID)
    {
        if (folderID == 0)
            throw new ArgumentException("The root folder cannot be deleted.", nameof(folderID));

        ThingRoot root = RequireRoot();
        ThingFolder folder = await LoadFileAsync<ThingFolder>(folderID)
            ?? throw new FileNotFoundException("The folder could not be loaded.");
        if (folder.Content.Count != 0)
            throw new InvalidOperationException("A non-empty folder cannot be deleted directly.");

        if (folder.ParentID == 0)
        {
            root.Content?.RemoveAll(x => x.ID == folder.ID);
            await SaveRootAsync();
        }
        else
        {
            ThingFolder parent = await LoadFileAsync<ThingFolder>(folder.ParentID)
                ?? throw new FileNotFoundException("The parent folder could not be loaded.");
            parent.Content.RemoveAll(x => x.ID == folder.ID);
            await SaveFileAsync(parent);
        }

        await CurrentStorage.DeleteAsync(folderID).ConfigureAwait(false);
    }

    public static async Task DeleteObject(ulong id)
    {
        if (id == 0)
            throw new ArgumentException("The root object cannot be deleted.", nameof(id));

        await RunMutationAsync(
            () => DeleteObjectCoreAsync(id),
            () => CollectDeleteBackupIdsAsync(id));
    }

    private static async Task<IReadOnlyCollection<ulong>> CollectDeleteBackupIdsAsync(ulong id)
    {
        var ids = new HashSet<ulong>();
        await CollectDeleteBackupIdsCoreAsync(id, ids);
        return ids;
    }

    private static async Task CollectDeleteBackupIdsCoreAsync(ulong id, HashSet<ulong> ids)
    {
        if (!ids.Add(id))
            return;

        ThingObject obj = await LoadFileAsync(id)
            ?? throw new FileNotFoundException("The object could not be loaded.");
        if (obj.ParentID != 0)
            ids.Add(obj.ParentID);

        if (obj is ThingFolder folder)
        {
            foreach (ThingObjectLink child in folder.Content)
                await CollectDeleteBackupIdsCoreAsync(child.ID, ids);
        }
    }

    private static async Task DeleteObjectCoreAsync(ulong id)
    {
        ThingObject obj = await LoadFileAsync(id)
            ?? throw new FileNotFoundException("The object could not be loaded.");

        if (obj is ThingFile file)
        {
            await DeleteFileCoreAsync(file.ID);
            return;
        }

        if (obj is not ThingFolder folder)
            throw new InvalidDataException("The stored object has an unsupported type.");

        foreach (ThingObjectLink link in folder.Content.ToList())
            await DeleteObjectCoreAsync(link.ID);

        await DeleteFolderCoreAsync(folder.ID);
    }

    public static async Task SaveRootAsync()
    {
        BeginSaving();
        try
        {
            Debug.WriteLine("Saving Root to main file");
            ThingRoot root = RequireRoot();
            ThingRoot tempRoot = (ThingRoot)root.Clone();
            string encryptedContent = await Encrypt(Magic + JsonSerializer.Serialize(root.Content));
            tempRoot.ContentEncrypted = encryptedContent;
            tempRoot.Content = null;
            string rootContent = JsonSerializer.Serialize(tempRoot);
            using var content = new MemoryStream(Encoding.UTF8.GetBytes(rootContent), writable: false);
            await CurrentStorage.WriteAtomicallyAsync(0, content).ConfigureAwait(false);
            root.ContentEncrypted = encryptedContent;
        }
        finally
        {
            EndSaving();
        }
    }
    public static ThingFolder AddToRoot(this ThingFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ThingRoot root = RequireRoot();
        root.Content ??= [];
        if (root.Content.Any(x => x.ID == folder.ID))
            throw new InvalidOperationException("The root already contains this folder.");
        folder.ParentID = 0;
        root.Content.Add(new ThingObjectLink(folder.ID, folder.Name, FileType.folder, 0));
        return folder;
    }
    public static async Task<List<ThingObjectLink>> LoadFolderContent(ulong id)
    {
        List<ThingObjectLink> content = [];

        if (id == 0)
        {
            ThingRoot root = RequireRoot();
            foreach (ThingObjectLink link in root.Content ?? [])
            {
                content.Add(link);
            }
            return content;
        }
        else
        {
            ThingObject? file = await LoadFileAsync(id);
            if(file is ThingFolder folder)
            {
                foreach(ThingObjectLink link in folder.Content)
                {
                    content.Add(link);
                }
                return content;
            }
        }
        throw new ArgumentException("The provided ID does not correspond to a folder.", nameof(id));
    }

    private static ThingRoot RequireRoot()
    {
        return Root ?? throw new InvalidOperationException("The root data has not been loaded.");
    }
    public static string Sizeify(this long sizeInBytes)
    {
        string[] sizes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        double len = sizeInBytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
