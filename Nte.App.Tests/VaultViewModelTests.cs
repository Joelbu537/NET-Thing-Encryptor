using NET_Thing_Encryptor;
using Nte.App.Services;
using Nte.App.Tests.Fakes;
using Nte.App.ViewModels;

namespace Nte.App.Tests;

public sealed class VaultViewModelTests
{
    private static readonly VaultItem Folder = new(
        10,
        "Dokumente",
        FileType.folder,
        0,
        string.Empty,
        new DateOnly(2026, 8, 29));
    private static readonly VaultItem TextFile = new(
        20,
        "Notiz",
        FileType.text,
        7,
        "txt",
        new DateOnly(2026, 8, 29));

    [Fact]
    public async Task Navigation_OpensFolderAndReturnsToRoot()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);

        await viewModel.OpenSelectedCommand.ExecuteAsync();

        Assert.Equal((ulong)10, viewModel.CurrentFolderId);
        Assert.Equal("Tresor  ›  Dokumente", viewModel.Breadcrumb);
        Assert.Equal("Notiz", Assert.Single(viewModel.Items).Name);

        await viewModel.BackCommand.ExecuteAsync();
        Assert.Equal((ulong)0, viewModel.CurrentFolderId);
    }

    [Fact]
    public async Task SystemBack_LeavesNestedFolderBeforeLockingVault()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();

        bool handled = viewModel.HandleBackRequested();

        Assert.True(handled);
        Assert.Equal((ulong)0, viewModel.CurrentFolderId);
    }

    [Fact]
    public async Task DocumentImport_IsDisabledAtRootAndUsesUniqueNamesInsideFolder()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        var picker = new FakeFilePickerService
        {
            Documents =
            [
                new MemoryExternalFile("Notiz.txt", "first"u8.ToArray()),
                new MemoryExternalFile("Notiz.md", "second"u8.ToArray())
            ]
        };
        var viewModel = CreateViewModel(vault, picker);
        await viewModel.InitializeAsync();
        Assert.False(viewModel.ImportDocumentsCommand.CanExecute(null));

        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        await viewModel.ImportDocumentsCommand.ExecuteAsync();

        Assert.Equal(2, vault.Imports.Count);
        Assert.Equal("Notiz (2)", vault.Imports[0].ObjectName);
        Assert.Equal("Notiz (3)", vault.Imports[1].ObjectName);
        Assert.All(vault.Imports, item => Assert.Equal((ulong)10, item.ParentId));
    }

    [Fact]
    public async Task SelectedDocument_IsExportedThroughCallerOwnedStream()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        var picker = new FakeFilePickerService();
        var viewModel = CreateViewModel(vault, picker);
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);

        await viewModel.ExportSelectedCommand.ExecuteAsync();

        Assert.Equal([(ulong)20], vault.Exports);
        Assert.Equal(vault.ExportedFileContent, picker.DocumentExport!.WrittenContent);
        Assert.Equal("Notiz.txt", picker.DocumentExport.Name);
    }

    [Fact]
    public async Task MultipleAndFolderExport_IsRecursiveAndNeverOverwritesExistingNames()
    {
        var nestedFolder = Folder with { Id = 11, Name = "Unterordner" };
        var image = new VaultItem(
            30,
            "Bild",
            FileType.image,
            4,
            "png",
            new DateOnly(2026, 8, 29));
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [nestedFolder, TextFile];
        vault.Folders[11] = [image];
        vault.ExportedFileContents[20] = "text"u8.ToArray();
        vault.ExportedFileContents[30] = "image"u8.ToArray();
        var exportFolder = new MemoryExternalFolder("Ziel");
        exportFolder.AddFolder("Unterordner");
        MemoryExternalFile existingFile = exportFolder.AddFile("Notiz.txt", "existing"u8.ToArray());
        var picker = new FakeFilePickerService { ExportFolder = exportFolder };
        var statuses = new List<string>();
        var viewModel = new VaultViewModel(vault, picker, () => { }, statuses.Add);
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        viewModel.SetSelectedItems(viewModel.Items);

        await viewModel.ExportSelectedCommand.ExecuteAsync();

        Assert.Equal([(ulong)30, (ulong)20], vault.Exports);
        Assert.Equal("existing"u8.ToArray(), existingFile.WrittenContent);
        Assert.Equal("text"u8.ToArray(), exportFolder.Files["Notiz (2).txt"].WrittenContent);
        MemoryExternalFolder nestedExport = exportFolder.Folders["Unterordner (2)"];
        Assert.Equal("image"u8.ToArray(), nestedExport.Files["Bild.png"].WrittenContent);
        Assert.Contains("2 Dateien, 1 Ordner", statuses[^1]);
        Assert.Contains("nicht überschrieben", statuses[^1]);
    }

    [Fact]
    public async Task OpeningImage_ProvidesCurrentFolderAsSeries()
    {
        var firstImage = new VaultItem(
            30,
            "Bild 1",
            FileType.image,
            4,
            "png",
            new DateOnly(2026, 8, 29));
        var secondImage = firstImage with { Id = 31, Name = "Bild 2" };
        byte[] png = OnePixelPng();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [firstImage, secondImage];
        vault.FileContents[30] = new VaultFileContent(30, "Bild 1", FileType.image, "png", png);
        vault.FileContents[31] = new VaultFileContent(31, "Bild 2", FileType.image, "png", png);
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        viewModel.SelectedItem = viewModel.Items[0];

        await viewModel.OpenSelectedCommand.ExecuteAsync();

        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        Assert.True(document.IsImageSeries);
        Assert.Equal("1 / 2", document.ImagePositionText);
    }

    [Fact]
    public async Task FolderCreationAndArchiveExportUseApplicationService()
    {
        var vault = new FakeVaultApplicationService();
        var picker = new FakeFilePickerService();
        var viewModel = CreateViewModel(vault, picker);
        await viewModel.InitializeAsync();
        viewModel.NewFolderName = "  Fotos  ";

        await viewModel.CreateFolderCommand.ExecuteAsync();
        await viewModel.ExportVaultCommand.ExecuteAsync();

        Assert.Equal(("Fotos", (ulong)0), Assert.Single(vault.CreatedFolders));
        Assert.Equal(1, vault.ArchiveExportCalls);
        Assert.Equal("archive"u8.ToArray(), picker.VaultExport!.WrittenContent);
        Assert.EndsWith(".ntevault", picker.VaultExport.Name);
    }

    [Fact]
    public async Task LocalAndGlobalSearch_FilterAndExposeResultLocation()
    {
        var secondFolder = Folder with { Id = 11, Name = "Fotos" };
        var globalResult = TextFile with { Location = "Tresor/Dokumente" };
        var vault = new FakeVaultApplicationService
        {
            SearchResults = [globalResult]
        };
        vault.Folders[0] = [Folder, secondFolder];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();

        viewModel.SearchQuery = "foto";
        Assert.Equal("Fotos", Assert.Single(viewModel.Items).Name);

        viewModel.SearchGlobally = true;
        viewModel.SearchQuery = "not";
        viewModel.SearchExtension = "txt";
        viewModel.SelectedSearchType = "Text";
        await viewModel.SearchCommand.ExecuteAsync();

        Assert.True(viewModel.IsShowingGlobalResults);
        Assert.Equal("Tresor/Dokumente", Assert.Single(viewModel.Items).Location);
        Assert.Equal(FileType.text, vault.LastSearchCriteria?.Type);
        Assert.Equal("txt", vault.LastSearchCriteria?.Extension);
    }

    [Fact]
    public async Task RenameMoveAndConfirmedDelete_UseApplicationService()
    {
        var target = new VaultFolderTarget(30, "Tresor › Archiv");
        var vault = new FakeVaultApplicationService { FolderTargets = [new(0, "Tresor"), target] };
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();

        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        viewModel.RenameName = "Final";
        await viewModel.RenameCommand.ExecuteAsync();
        Assert.Equal(((ulong)20, "Final"), Assert.Single(vault.Renames));

        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        viewModel.SelectedMoveTarget = viewModel.FolderTargets.Single(item => item.Id == 30);
        await viewModel.MoveCommand.ExecuteAsync();
        Assert.Equal(((ulong)20, (ulong)30), Assert.Single(vault.Moves));

        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.RequestDeleteCommand.ExecuteAsync();
        Assert.True(viewModel.ShowDeleteConfirmation);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync();
        Assert.Equal([(ulong)20], vault.Deletes);
    }

    [Fact]
    public async Task MultipleSelection_MovesAndDeletesEverySelectedObject()
    {
        VaultItem second = Folder with { Id = 11, Name = "Fotos" };
        var target = new VaultFolderTarget(30, "Tresor › Archiv");
        var vault = new FakeVaultApplicationService { FolderTargets = [new(0, "Tresor"), target] };
        vault.Folders[0] = [Folder, second];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();

        viewModel.SetSelectedItems(viewModel.Items);
        Assert.Equal("2 Objekte ausgewählt", viewModel.SelectionCountText);
        viewModel.SelectedMoveTarget = viewModel.FolderTargets.Single(item => item.Id == 30);
        await viewModel.MoveCommand.ExecuteAsync();

        Assert.Equal(2, vault.Moves.Count);
        viewModel.SetSelectedItems(viewModel.Items);
        await viewModel.RequestDeleteCommand.ExecuteAsync();
        Assert.Contains("2 ausgewählte", viewModel.DeleteConfirmationText);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync();
        Assert.Equal(2, vault.Deletes.Count);
    }

    [Fact]
    public async Task TextContent_CanBeOpenedEditedAndSaved()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "vorher"u8.ToArray());
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);

        await viewModel.OpenSelectedCommand.ExecuteAsync();
        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        await document.ToggleEditingCommand.ExecuteAsync();
        document.Text = "nachher";
        await document.SaveCommand.ExecuteAsync();

        Assert.Equal("nachher"u8.ToArray(), Assert.Single(vault.SavedContents).Content);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public async Task Preferences_AreAppliedAtUnlockAndPersistedOnSave()
    {
        var initial = new VaultPreferences(false, 12, 3, 4, true, 9, true);
        var applied = new List<VaultPreferences>();
        var vault = new FakeVaultApplicationService { Preferences = initial };
        var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            applied.Add);

        await viewModel.InitializeAsync();
        Assert.Equal(initial, Assert.Single(applied));
        viewModel.DarkMode = true;
        viewModel.AutoLockMinutes = 0;
        await viewModel.SavePreferencesCommand.ExecuteAsync();

        Assert.True(vault.Preferences.DarkMode);
        Assert.Equal(0, vault.Preferences.AutoLockMinutes);
        Assert.Equal(vault.Preferences, applied[^1]);
    }

    private static VaultViewModel CreateViewModel(
        FakeVaultApplicationService vault,
        FakeFilePickerService picker) => new(vault, picker, () => { }, _ => { });

    private static byte[] OnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAXSURBVBhXY/jPwPCfoYHhPwMDw38wAABD1Al4TlSdlQAAAABJRU5ErkJggg==");
}
