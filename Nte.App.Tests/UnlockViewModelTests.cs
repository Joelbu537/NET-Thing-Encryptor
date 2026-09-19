using NET_Thing_Encryptor;
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
        Assert.Equal(0, viewModel.SelectedVaultSourceIndex);
        Assert.True(viewModel.IsVaultAccessVisible);
        await viewModel.ImportVaultCommand.ExecuteAsync();

        Assert.Equal(1, vault.ArchiveImportCalls);
        Assert.False(viewModel.CanImportVault);
        Assert.True(viewModel.HasPersistedVault);
        Assert.False(viewModel.IsPasswordConfirmationVisible);
        Assert.True(viewModel.IsVaultSourceSelectionVisible);
        Assert.False(viewModel.IsArchiveImportVisible);
        Assert.Empty(viewModel.Password);
        Assert.Empty(viewModel.PasswordConfirmation);
        Assert.Equal("Tresor entsperren", viewModel.Heading);
        Assert.Equal(
            "Gib das Tresorpasswort ein. Es wird nur auf diesem Gerät zur Entschlüsselung verwendet.",
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
        Assert.True(viewModel.IsVaultSourceSelectionVisible);
        Assert.False(viewModel.IsArchiveImportVisible);
        Assert.False(viewModel.ConnectRemoteVaultCommand.CanExecute(null));
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
        Assert.True(viewModel.IsArchiveImportVisible);
        Assert.True(viewModel.ImportVaultCommand.CanExecute(null));
        Assert.False(viewModel.ConnectRemoteVaultCommand.CanExecute(null));
        Assert.Equal("Aktiver Tresor: dieses Gerät", viewModel.StorageDescription);
        Assert.Equal("Passwort festlegen", viewModel.Heading);
        Assert.Equal(
            "Gib ein Tresorpasswort zur clientseitigen Verschlüsselung ein.",
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

    [Fact]
    public async Task RemoteConnection_UsesSeparateAccessPasswordAndRefreshesSetupState()
    {
        var vault = new FakeVaultApplicationService { HasPersistedVault = false };
        string status = string.Empty;
        var viewModel = new UnlockViewModel(
            vault,
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            message => status = message)
        {
            RemoteAddress = "https://vault.example.net/",
            RemoteAccessPassword = "server secret"
        };

        Assert.True(viewModel.ConnectRemoteVaultCommand.CanExecute(null));
        await viewModel.ConnectRemoteVaultCommand.ExecuteAsync();

        Assert.Equal(1, vault.RemoteConnectCalls);
        Assert.Equal("https://vault.example.net/", vault.LastRemoteAddress);
        Assert.Equal("server secret", vault.LastRemoteAccessPassword);
        Assert.True(viewModel.IsRemoteVault);
        Assert.Equal(1, viewModel.SelectedVaultSourceIndex);
        Assert.True(viewModel.IsVaultAccessVisible);
        Assert.False(viewModel.IsRemoteConnectionVisible);
        Assert.Equal("Remote-Tresor einrichten", viewModel.Heading);
        Assert.Equal("Aktiver Remote-Tresor: https://vault.example.net", viewModel.StorageDescription);
        Assert.Empty(viewModel.RemoteAccessPassword);
        Assert.Contains("leeren Remote-Speicher", status);
    }

    [Fact]
    public async Task FailedRemoteConnection_KeepsCurrentVaultAndShowsError()
    {
        var vault = new FakeVaultApplicationService
        {
            HasPersistedVault = true,
            RemoteConnectException = new IOException("unauthorized")
        };
        var viewModel = new UnlockViewModel(
            vault,
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            _ => { })
        {
            RemoteAddress = "https://vault.example.net",
            RemoteAccessPassword = "wrong"
        };

        await viewModel.ConnectRemoteVaultCommand.ExecuteAsync();

        Assert.False(viewModel.IsRemoteVault);
        Assert.True(viewModel.HasPersistedVault);
        Assert.Contains("unauthorized", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LocalAndRemoteTabs_SwitchStorageExplicitlyWithoutAccidentalDisconnect()
    {
        var vault = new FakeVaultApplicationService
        {
            HasPersistedVault = true,
            LocalHasPersistedVault = true
        };
        var viewModel = new UnlockViewModel(
            vault,
            new FakeFilePickerService(),
            () => Task.CompletedTask,
            _ => { })
        {
            RemoteAddress = "vault.example.net",
            RemoteAccessPassword = "server secret"
        };

        viewModel.SelectedVaultSourceIndex = 1;
        Assert.False(viewModel.IsVaultAccessVisible);
        Assert.True(viewModel.IsRemoteConnectionVisible);
        await viewModel.ConnectRemoteVaultCommand.ExecuteAsync();

        Assert.True(viewModel.IsRemoteVault);
        Assert.True(viewModel.IsVaultAccessVisible);
        Assert.False(viewModel.IsRemoteConnectionVisible);

        viewModel.SelectedVaultSourceIndex = 0;
        Assert.False(viewModel.IsVaultAccessVisible);
        Assert.False(viewModel.IsRemoteConnectionVisible);
        Assert.True(viewModel.UseLocalVaultCommand.CanExecute(null));
        await viewModel.UseLocalVaultCommand.ExecuteAsync();

        Assert.Equal(1, vault.UseLocalVaultCalls);
        Assert.True(viewModel.IsLocalVault);
        Assert.False(viewModel.IsRemoteVault);
        Assert.True(viewModel.IsVaultAccessVisible);
        Assert.False(viewModel.IsRemoteConnectionVisible);
        Assert.Equal("Aktiver Tresor: dieses Gerät", viewModel.StorageDescription);

        viewModel.SelectedVaultSourceIndex = 1;
        Assert.True(viewModel.IsRemoteConnectionVisible);
    }

    [Fact]
    public async Task ArchiveImport_ExposesProgressWhileRunning()
    {
        var releaseImport = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var progressVisible = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var vault = new FakeVaultApplicationService
        {
            HasPersistedVault = false,
            ImportVaultAsyncHandler = async (_, progress, cancellationToken) =>
            {
                progress?.Report(new VaultArchiveImportProgress(
                    3,
                    6,
                    1,
                    2,
                    VaultArchiveImportPhase.WritingObjects));
                await releaseImport.Task.WaitAsync(cancellationToken);
                progress?.Report(new VaultArchiveImportProgress(
                    6,
                    6,
                    2,
                    2,
                    VaultArchiveImportPhase.Completing));
                return 2;
            }
        };
        var picker = new FakeFilePickerService
        {
            VaultImport = new MemoryExternalFile("backup.ntevault", "archive"u8.ToArray())
        };
        var viewModel = new UnlockViewModel(
            vault,
            picker,
            () => Task.CompletedTask,
            _ => { });
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UnlockViewModel.VaultImportProgress) &&
                viewModel.VaultImportProgress == 50)
            {
                progressVisible.TrySetResult();
            }
        };

        Task import = viewModel.ImportVaultCommand.ExecuteAsync();
        await progressVisible.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsVaultImportProgressVisible);
        Assert.False(viewModel.IsVaultImportProgressIndeterminate);
        Assert.Equal("50%", viewModel.VaultImportProgressPercentText);
        Assert.Contains("1 von 2", viewModel.VaultImportProgressDetail);
        Assert.Equal("Tresorarchiv wird importiert …", viewModel.ImportVaultButtonText);

        releaseImport.TrySetResult();
        await import.WaitAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsVaultImportProgressVisible);
        Assert.Equal(100, viewModel.VaultImportProgress);
        Assert.Equal("Tresorarchiv importieren …", viewModel.ImportVaultButtonText);
    }
}
