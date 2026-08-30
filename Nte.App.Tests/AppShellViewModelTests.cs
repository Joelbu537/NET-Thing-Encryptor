using NET_Thing_Encryptor;
using Nte.App.Tests.Fakes;
using Nte.App.Services;
using Nte.App.ViewModels;

namespace Nte.App.Tests;

public sealed class AppShellViewModelTests
{
    [Fact]
    public async Task Initialize_ShowsUnlockPage()
    {
        var vault = new FakeVaultApplicationService();
        using var shell = new AppShellViewModel(vault, new FakeFilePickerService());

        await shell.InitializeAsync();

        Assert.IsType<UnlockViewModel>(shell.CurrentPage);
    }

    [Fact]
    public async Task SuccessfulUnlock_ShowsVaultPage()
    {
        var vault = new FakeVaultApplicationService();
        using var shell = new AppShellViewModel(vault, new FakeFilePickerService());
        await shell.InitializeAsync();
        var unlock = Assert.IsType<UnlockViewModel>(shell.CurrentPage);
        unlock.Password = "correct horse battery staple";

        await unlock.UnlockCommand.ExecuteAsync();

        Assert.Equal(1, vault.UnlockCalls);
        Assert.IsType<VaultViewModel>(shell.CurrentPage);
        Assert.Equal("Tresor entsperrt.", shell.StatusMessage);
    }

    [Fact]
    public async Task FailedUnlock_StaysLockedAndReportsError()
    {
        var vault = new FakeVaultApplicationService { UnlockResult = false };
        using var shell = new AppShellViewModel(vault, new FakeFilePickerService());
        await shell.InitializeAsync();
        var unlock = Assert.IsType<UnlockViewModel>(shell.CurrentPage);
        unlock.Password = "wrong";

        await unlock.UnlockCommand.ExecuteAsync();

        Assert.Same(unlock, shell.CurrentPage);
        Assert.Contains("nicht korrekt", unlock.ErrorMessage);
    }

    [Fact]
    public async Task Notification_IsExposedAsStatusAndDisposeReleasesService()
    {
        var vault = new FakeVaultApplicationService();
        var shell = new AppShellViewModel(vault, new FakeFilePickerService());
        await shell.InitializeAsync();

        vault.RaiseNotification("Hinweis", "Testmeldung");

        Assert.Equal("Hinweis: Testmeldung", shell.StatusMessage);
        shell.Dispose();
        Assert.True(vault.Disposed);
    }

    [Fact]
    public async Task Dispose_ClosesActiveDecryptedDocumentBeforeReleasingService()
    {
        var folder = new VaultItem(
            10,
            "Dokumente",
            FileType.folder,
            0,
            string.Empty,
            new DateOnly(2026, 8, 30));
        var file = new VaultItem(
            20,
            "Notiz",
            FileType.text,
            6,
            "txt",
            new DateOnly(2026, 8, 30));
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [folder];
        vault.Folders[10] = [file];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "secret"u8.ToArray());
        var shell = new AppShellViewModel(vault, new FakeFilePickerService());
        await shell.InitializeAsync();
        var unlock = Assert.IsType<UnlockViewModel>(shell.CurrentPage);
        unlock.Password = "correct horse battery staple";
        await unlock.UnlockCommand.ExecuteAsync();
        var page = Assert.IsType<VaultViewModel>(shell.CurrentPage);
        page.SelectedItem = Assert.Single(page.Items);
        await page.OpenSelectedCommand.ExecuteAsync();
        page.SelectedItem = Assert.Single(page.Items);
        await page.OpenSelectedCommand.ExecuteAsync();
        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(page.ActiveDocument);

        shell.Dispose();

        Assert.Equal(string.Empty, document.Text);
        Assert.True(vault.Disposed);
    }

    [Fact]
    public async Task BackgroundLock_ClearsSessionAndReturnsToUnlockPage()
    {
        var vault = new FakeVaultApplicationService();
        using var shell = new AppShellViewModel(vault, new FakeFilePickerService());
        await shell.InitializeAsync();
        var unlock = Assert.IsType<UnlockViewModel>(shell.CurrentPage);
        unlock.Password = "correct horse battery staple";
        await unlock.UnlockCommand.ExecuteAsync();

        bool handled = shell.LockForSecurity(SessionLockReason.Background);

        Assert.True(handled);
        Assert.False(vault.IsUnlocked);
        Assert.IsType<UnlockViewModel>(shell.CurrentPage);
        Assert.Contains("Verlassen", shell.StatusMessage);
    }

    [Fact]
    public async Task BackAtVaultRoot_LocksInsteadOfClosingApplication()
    {
        var vault = new FakeVaultApplicationService();
        using var shell = new AppShellViewModel(vault, new FakeFilePickerService());
        await shell.InitializeAsync();
        var unlock = Assert.IsType<UnlockViewModel>(shell.CurrentPage);
        unlock.Password = "correct horse battery staple";
        await unlock.UnlockCommand.ExecuteAsync();

        bool handled = shell.HandleBackRequested();

        Assert.True(handled);
        Assert.False(vault.IsUnlocked);
        Assert.IsType<UnlockViewModel>(shell.CurrentPage);
    }
}
