using Nte.App.Tests.Fakes;
using Nte.App.ViewModels;

namespace Nte.App.Tests;

public sealed class UnlockViewModelTests
{
    [Fact]
    public async Task ArchiveImport_IsAvailableOnlyForEmptyVaultAndReloadsMetadata()
    {
        var vault = new FakeVaultApplicationService { HasPersistedVault = false };
        var picker = new FakeFilePickerService
        {
            VaultImport = new MemoryExternalFile("backup.ntevault", "archive"u8.ToArray())
        };
        string status = string.Empty;
        var viewModel = new UnlockViewModel(
            vault,
            picker,
            () => Task.CompletedTask,
            message => status = message);

        Assert.True(viewModel.ImportVaultCommand.CanExecute(null));
        await viewModel.ImportVaultCommand.ExecuteAsync();

        Assert.Equal(1, vault.ArchiveImportCalls);
        Assert.False(viewModel.CanImportVault);
        Assert.Contains("4 Objekte", status);
    }

    [Fact]
    public void ExistingVault_DisablesArchiveImport()
    {
        var viewModel = new UnlockViewModel(
            new FakeVaultApplicationService { HasPersistedVault = true },
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            _ => { });

        Assert.False(viewModel.ImportVaultCommand.CanExecute(null));
    }
}
