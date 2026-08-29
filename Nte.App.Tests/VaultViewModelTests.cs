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

    private static VaultViewModel CreateViewModel(
        FakeVaultApplicationService vault,
        FakeFilePickerService picker) => new(vault, picker, () => { }, _ => { });
}
