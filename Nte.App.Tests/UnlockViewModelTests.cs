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
        viewModel.Password = "password for a new vault";
        viewModel.PasswordConfirmation = "password for a new vault";

        Assert.True(viewModel.ImportVaultCommand.CanExecute(null));
        await viewModel.ImportVaultCommand.ExecuteAsync();

        Assert.Equal(1, vault.ArchiveImportCalls);
        Assert.False(viewModel.CanImportVault);
        Assert.True(viewModel.HasPersistedVault);
        Assert.False(viewModel.IsPasswordConfirmationVisible);
        Assert.False(viewModel.IsVaultSourceSelectionVisible);
        Assert.Empty(viewModel.Password);
        Assert.Empty(viewModel.PasswordConfirmation);
        Assert.Equal("Tresor entsperren", viewModel.Heading);
        Assert.Equal(
            "Gib das zum Entsperren des Tresors benötigte Passwort ein.",
            viewModel.InstructionText);
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
        Assert.False(viewModel.IsVaultSourceSelectionVisible);
        Assert.False(viewModel.CanConnectRemoteVault);
    }

    [Fact]
    public void EmptyVault_ShowsPasswordSetupCopyAndConfirmation()
    {
        var viewModel = new UnlockViewModel(
            new FakeVaultApplicationService { HasPersistedVault = false },
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            _ => { });

        Assert.False(viewModel.HasPersistedVault);
        Assert.True(viewModel.IsPasswordConfirmationVisible);
        Assert.True(viewModel.IsVaultSourceSelectionVisible);
        Assert.True(viewModel.ImportVaultCommand.CanExecute(null));
        Assert.False(viewModel.CanConnectRemoteVault);
        Assert.Contains("zukünftigen Version", viewModel.RemoteVaultConnectionHint);
        Assert.Equal("Passwort festlegen", viewModel.Heading);
        Assert.Equal(
            "Gib ein Passwort zum Verschlüsseln deines Tresors ein.",
            viewModel.InstructionText);
    }

    [Fact]
    public async Task EmptyVault_DoesNotUnlockWhenConfirmationIsMissingOrDifferent()
    {
        var vault = new FakeVaultApplicationService { HasPersistedVault = false };
        var viewModel = new UnlockViewModel(
            vault,
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            _ => { })
        {
            Password = "correct horse battery staple"
        };

        await viewModel.UnlockCommand.ExecuteAsync();

        Assert.Equal(0, vault.UnlockCalls);
        Assert.Contains("bestätigen", viewModel.ErrorMessage);

        viewModel.PasswordConfirmation = "different password";
        await viewModel.UnlockCommand.ExecuteAsync();

        Assert.Equal(0, vault.UnlockCalls);
        Assert.Contains("nicht überein", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task EmptyVault_UnlocksWhenPasswordsMatch()
    {
        var vault = new FakeVaultApplicationService { HasPersistedVault = false };
        bool unlocked = false;
        var viewModel = new UnlockViewModel(
            vault,
            new FakeFilePickerService(),
            () =>
            {
                unlocked = true;
                return Task.CompletedTask;
            },
            _ => { })
        {
            Password = "correct horse battery staple",
            PasswordConfirmation = "correct horse battery staple"
        };

        await viewModel.UnlockCommand.ExecuteAsync();

        Assert.Equal(1, vault.UnlockCalls);
        Assert.True(unlocked);
        Assert.Empty(viewModel.Password);
        Assert.Empty(viewModel.PasswordConfirmation);
    }

    [Fact]
    public async Task ExistingVault_UnlocksWithoutConfirmation()
    {
        var vault = new FakeVaultApplicationService { HasPersistedVault = true };
        var viewModel = new UnlockViewModel(
            vault,
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            _ => { })
        {
            Password = "correct horse battery staple"
        };

        await viewModel.UnlockCommand.ExecuteAsync();

        Assert.Equal(1, vault.UnlockCalls);
        Assert.False(viewModel.IsPasswordConfirmationVisible);
    }
}
