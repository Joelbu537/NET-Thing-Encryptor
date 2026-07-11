using System.Text;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class ObjectOperationTests
{
    [Fact]
    public async Task FileLifecycle_MoveRenameResizeAndDelete_UpdatesParentLinks()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("documents");
        await ThingData.SaveFileAsync(folder);
        await ThingData.SaveRootAsync();

        ThingFile file = environment.CreateFile("draft", ".txt", Encoding.UTF8.GetBytes("hello"));
        environment.Root.Content!.Add(new ThingObjectLink(file.ID, file.Name, file.Type, 5));
        await ThingData.SaveFileAsync(file);
        await ThingData.SaveRootAsync();

        await ThingData.MoveFileToFolderAsync(file, folder.ID);
        ThingFolder movedParent = (await ThingData.LoadFileAsync<ThingFolder>(folder.ID))!;
        Assert.Contains(movedParent.Content, link => link.ID == file.ID);
        Assert.DoesNotContain(environment.Root.Content!, link => link.ID == file.ID);

        await ThingData.RenameObjectAsync(file.ID, "final");
        await ThingData.UpdateObjectSizeAsync(file.ID, 999);
        ThingFile renamed = (await ThingData.LoadFileAsync<ThingFile>(file.ID))!;
        ThingFolder updatedParent = (await ThingData.LoadFileAsync<ThingFolder>(folder.ID))!;
        ThingObjectLink link = Assert.Single(updatedParent.Content, item => item.ID == file.ID);
        Assert.Equal("final", renamed.Name);
        Assert.Equal("final", link.Name);
        Assert.Equal(999, link.Size);

        await ThingData.DeleteObject(file.ID);
        Assert.DoesNotContain((await ThingData.LoadFolderContent(folder.ID)), item => item.ID == file.ID);
        Assert.Throws<FileNotFoundException>(() => ThingData.GetFilePath(file.ID));
    }

    [Fact]
    public async Task FolderLifecycle_MovesHierarchyAndDeletesRecursively()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder parent = environment.CreateRootFolder("parent");
        ThingFolder child = environment.CreateRootFolder("child");
        await ThingData.SaveFileAsync(parent);
        await ThingData.SaveFileAsync(child);
        await ThingData.SaveRootAsync();

        await ThingData.MoveFolderToFolderAsync(child.ID, parent.ID);
        Assert.Equal(parent.ID, (await ThingData.LoadFileAsync<ThingFolder>(child.ID))!.ParentID);
        Assert.Contains((await ThingData.LoadFolderContent(parent.ID)), item => item.ID == child.ID);

        ThingFile nestedFile = environment.CreateFile("nested", ".bin", [1, 2], child.ID);
        await ThingData.SaveFileAsync(nestedFile);
        ThingFolder storedChild = (await ThingData.LoadFileAsync<ThingFolder>(child.ID))!;
        storedChild.Content.Add(new ThingObjectLink(nestedFile.ID, nestedFile.Name, nestedFile.Type, 2));
        await ThingData.SaveFileAsync(storedChild);

        await ThingData.DeleteObject(parent.ID);
        Assert.DoesNotContain(environment.Root.Content!, item => item.ID == parent.ID);
        Assert.Throws<FileNotFoundException>(() => ThingData.GetFilePath(parent.ID));
        Assert.Throws<FileNotFoundException>(() => ThingData.GetFilePath(child.ID));
        Assert.Throws<FileNotFoundException>(() => ThingData.GetFilePath(nestedFile.ID));
    }

    [Fact]
    public async Task FolderMove_RejectsCyclesAndRollsBackState()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder parent = environment.CreateRootFolder("parent");
        ThingFolder child = environment.CreateRootFolder("child");
        await ThingData.SaveFileAsync(parent);
        await ThingData.SaveFileAsync(child);
        await ThingData.SaveRootAsync();
        await ThingData.MoveFolderToFolderAsync(child.ID, parent.ID);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ThingData.MoveFolderToFolderAsync(parent.ID, child.ID));

        Assert.Equal(0UL, (await ThingData.LoadFileAsync<ThingFolder>(parent.ID))!.ParentID);
        Assert.Equal(parent.ID, (await ThingData.LoadFileAsync<ThingFolder>(child.ID))!.ParentID);
        Assert.Contains(environment.Root.Content!, link => link.ID == parent.ID);
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task Rename_RejectsCaseInsensitiveSiblingConflict()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder first = environment.CreateRootFolder("Alpha");
        ThingFolder second = environment.CreateRootFolder("Beta");
        await ThingData.SaveFileAsync(first);
        await ThingData.SaveFileAsync(second);
        await ThingData.SaveRootAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ThingData.RenameObjectAsync(second.ID, "alpha"));

        Assert.Equal("Beta", (await ThingData.LoadFileAsync<ThingFolder>(second.ID))!.Name);
        Assert.Contains(environment.Root.Content!, link => link.ID == second.ID && link.Name == "Beta");
    }

    [Fact]
    public async Task RootAndFolderHelpers_ValidateMembershipAndReturnCopies()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("folder");
        Assert.Throws<InvalidOperationException>(() => folder.AddToRoot());
        await ThingData.SaveFileAsync(folder);

        List<ThingObjectLink> rootContent = await ThingData.LoadFolderContent(0);
        rootContent.Clear();
        Assert.Single(environment.Root.Content!);

        ThingFile file = environment.CreateFile("not-a-folder", ".txt", [1]);
        await ThingData.SaveFileAsync(file);
        await Assert.ThrowsAsync<ArgumentException>(() => ThingData.LoadFolderContent(file.ID));
        await Assert.ThrowsAsync<ArgumentException>(() => ThingData.DeleteObject(0));
        await Assert.ThrowsAsync<ArgumentException>(() => ThingData.RenameObjectAsync(0, "name"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ThingData.UpdateObjectSizeAsync(folder.ID, -1));
    }

    [Fact]
    public async Task Delete_IgnoresLockedUnrelatedVaultFiles()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile target = environment.CreateFile("target", ".txt", [1, 2, 3]);
        environment.Root.Content!.Add(new ThingObjectLink(target.ID, target.Name, target.Type, 3));
        await ThingData.SaveFileAsync(target);
        await ThingData.SaveRootAsync();

        string unrelatedPath = Path.Combine(environment.Directory, "AAAAAAAAAAAAAAAA.nte");
        await File.WriteAllBytesAsync(unrelatedPath, new byte[1024], TestContext.Current.CancellationToken);
        await using var unrelatedLock = new FileStream(
            unrelatedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        await ThingData.DeleteObject(target.ID);

        Assert.False(File.Exists(Path.Combine(environment.Directory, ThingData.IDToHex(target.ID) + ".nte")));
        Assert.True(File.Exists(unrelatedPath));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task MoveFile_RejectsRootAndTreatsCurrentParentAsNoOp()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("folder");
        await ThingData.SaveFileAsync(folder);
        ThingFile file = environment.CreateFile("file", ".txt", [1], folder.ID);
        folder.Content.Add(new ThingObjectLink(file.ID, file.Name, file.Type, 1));
        await ThingData.SaveFileAsync(file);
        await ThingData.SaveFileAsync(folder);

        await Assert.ThrowsAsync<ArgumentException>(() => ThingData.MoveFileToFolderAsync(file, 0));
        await ThingData.MoveFileToFolderAsync(file, folder.ID);

        Assert.Single((await ThingData.LoadFileAsync<ThingFolder>(folder.ID))!.Content);
        Assert.Equal(folder.ID, (await ThingData.LoadFileAsync<ThingFile>(file.ID))!.ParentID);
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task MoveFile_NameConflictRollsBackBothParentsAndFile()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder source = environment.CreateRootFolder("source");
        ThingFolder target = environment.CreateRootFolder("target");
        ThingFile moving = environment.CreateFile("duplicate", ".txt", [1], source.ID);
        source.Content.Add(new ThingObjectLink(moving.ID, moving.Name, moving.Type, 1));
        target.Content.Add(new ThingObjectLink(999, "DUPLICATE", FileType.text, 2));
        await ThingData.SaveFileAsync(source);
        await ThingData.SaveFileAsync(target);
        await ThingData.SaveFileAsync(moving);
        await ThingData.SaveRootAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ThingData.MoveFileToFolderAsync(moving, target.ID));

        Assert.Contains((await ThingData.LoadFileAsync<ThingFolder>(source.ID))!.Content, link => link.ID == moving.ID);
        Assert.DoesNotContain((await ThingData.LoadFileAsync<ThingFolder>(target.ID))!.Content, link => link.ID == moving.ID);
        Assert.Equal(source.ID, (await ThingData.LoadFileAsync<ThingFile>(moving.ID))!.ParentID);
    }

    [Fact]
    public async Task NestedRenameAndKnownParentSizeUpdate_ModifyOnlyTheParentLink()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder parent = environment.CreateRootFolder("parent");
        ThingFile file = environment.CreateFile("old", ".txt", [1, 2], parent.ID);
        parent.Content.Add(new ThingObjectLink(file.ID, file.Name, file.Type, 2));
        await ThingData.SaveFileAsync(parent);
        await ThingData.SaveFileAsync(file);
        await ThingData.SaveRootAsync();

        await ThingData.RenameObjectAsync(file.ID, "new");
        await ThingData.UpdateObjectSizeAsync(file.ID, 1234, parent.ID);

        ThingObjectLink link = Assert.Single((await ThingData.LoadFileAsync<ThingFolder>(parent.ID))!.Content);
        Assert.Equal("new", link.Name);
        Assert.Equal(1234, link.Size);
        Assert.Equal("new", (await ThingData.LoadFileAsync<ThingFile>(file.ID))!.Name);
    }

    [Theory]
    [InlineData(0UL, 1UL)]
    [InlineData(1UL, 1UL)]
    public async Task FolderMove_RejectsRootOrSelf(ulong folderId, ulong parentId)
    {
        await using var environment = await TestEnvironment.CreateAsync();
        if (folderId == 1)
            await Assert.ThrowsAsync<InvalidOperationException>(() => ThingData.MoveFolderToFolderAsync(folderId, parentId));
        else
            await Assert.ThrowsAsync<ArgumentException>(() => ThingData.MoveFolderToFolderAsync(folderId, parentId));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task FailedDeleteOfLockedTarget_RestoresSavingStateAndReferences()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile target = environment.CreateFile("locked", ".txt", [1]);
        environment.Root.Content!.Add(new ThingObjectLink(target.ID, target.Name, target.Type, 1));
        await ThingData.SaveFileAsync(target);
        await ThingData.SaveRootAsync();
        string targetPath = ThingData.GetFilePath(target.ID);
        await using var targetLock = new FileStream(
            targetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        await Assert.ThrowsAnyAsync<IOException>(() => ThingData.DeleteObject(target.ID));

        Assert.True(File.Exists(targetPath));
        Assert.Contains(environment.Root.Content!, link => link.ID == target.ID);
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task ConcurrentDeletes_AreSerializedAndPersistBothRemovals()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile first = environment.CreateFile("first", ".txt", [1]);
        ThingFile second = environment.CreateFile("second", ".txt", [2]);
        environment.Root.Content!.Add(new ThingObjectLink(first.ID, first.Name, first.Type, 1));
        environment.Root.Content.Add(new ThingObjectLink(second.ID, second.Name, second.Type, 1));
        await ThingData.SaveFileAsync(first);
        await ThingData.SaveFileAsync(second);
        await ThingData.SaveRootAsync();

        await Task.WhenAll(ThingData.DeleteObject(first.ID), ThingData.DeleteObject(second.ID));

        Assert.Empty(environment.Root.Content);
        Assert.False(File.Exists(Path.Combine(environment.Directory, ThingData.IDToHex(first.ID) + ".nte")));
        Assert.False(File.Exists(Path.Combine(environment.Directory, ThingData.IDToHex(second.ID) + ".nte")));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task MoveNoOp_StillDetectsCorruptedParentReferences()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder parent = environment.CreateRootFolder("parent");
        ThingFile orphanedFile = environment.CreateFile("orphaned", ".txt", [1], parent.ID);
        await ThingData.SaveFileAsync(parent);
        await ThingData.SaveFileAsync(orphanedFile);
        await ThingData.SaveRootAsync();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => ThingData.MoveFileToFolderAsync(orphanedFile, parent.ID));

        ThingFolder orphanedFolder = new("orphaned-folder") { ParentID = parent.ID };
        await ThingData.SaveFileAsync(orphanedFolder);
        await Assert.ThrowsAsync<InvalidDataException>(
            () => ThingData.MoveFolderToFolderAsync(orphanedFolder.ID, parent.ID));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task RenameAndMove_IgnoreLockedUnrelatedVaultFiles()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("folder");
        ThingFile file = environment.CreateFile("before", ".txt", [1]);
        environment.Root.Content!.Add(new ThingObjectLink(file.ID, file.Name, file.Type, 1));
        await ThingData.SaveFileAsync(folder);
        await ThingData.SaveFileAsync(file);
        await ThingData.SaveRootAsync();

        string unrelatedPath = Path.Combine(environment.Directory, "BBBBBBBBBBBBBBBB.nte");
        await File.WriteAllBytesAsync(unrelatedPath, new byte[1024], TestContext.Current.CancellationToken);
        await using var unrelatedLock = new FileStream(
            unrelatedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        await ThingData.RenameObjectAsync(file.ID, "after");
        ThingFile renamed = (await ThingData.LoadFileAsync<ThingFile>(file.ID))!;
        await ThingData.MoveFileToFolderAsync(renamed, folder.ID);

        Assert.Equal("after", (await ThingData.LoadFileAsync<ThingFile>(file.ID))!.Name);
        Assert.Contains((await ThingData.LoadFileAsync<ThingFolder>(folder.ID))!.Content,
            link => link.ID == file.ID && link.Name == "after");
        Assert.True(File.Exists(unrelatedPath));
        Assert.Equal(0, ThingData.Saving);
    }
}
