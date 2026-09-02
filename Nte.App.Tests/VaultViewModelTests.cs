using System.ComponentModel;
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
        Assert.Equal("/Dokumente", viewModel.Breadcrumb);
        Assert.Equal("Notiz", Assert.Single(viewModel.Items).Name);

        await viewModel.BackCommand.ExecuteAsync();
        Assert.Equal((ulong)0, viewModel.CurrentFolderId);
    }

    [Fact]
    public async Task ActivateItem_OpensTheClickedFolderAndGoRootReturnsDirectly()
    {
        var nestedFolder = Folder with { Id = 11, Name = "Unterordner" };
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [nestedFolder];
        vault.Folders[11] = [];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();

        await viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        await viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        Assert.Equal((ulong)11, viewModel.CurrentFolderId);

        await viewModel.GoRootCommand.ExecuteAsync();

        Assert.Equal((ulong)0, viewModel.CurrentFolderId);
        Assert.Equal("/", viewModel.Breadcrumb);
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
    public async Task RootToolbarCommandsStayInteractiveAndImportExplainsFolderRequirement()
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
        Assert.True(viewModel.BackCommand.CanExecute(null));
        Assert.True(viewModel.GoRootCommand.CanExecute(null));
        Assert.True(viewModel.ImportDocumentsCommand.CanExecute(null));

        await viewModel.ImportDocumentsCommand.ExecuteAsync();

        Assert.Empty(vault.Imports);
        Assert.Contains("innerhalb eines Ordners", viewModel.ErrorMessage);

        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        await viewModel.ImportDocumentsCommand.ExecuteAsync();

        Assert.Equal(2, vault.Imports.Count);
        Assert.Equal("Notiz (2)", vault.Imports[0].ObjectName);
        Assert.Equal("Notiz (3)", vault.Imports[1].ObjectName);
        Assert.All(vault.Imports, item => Assert.Equal((ulong)10, item.ParentId));
    }

    [Fact]
    public void FolderSizeText_UsesTheActualSizeInsteadOfRepeatingTheType()
    {
        var folder = new VaultItemViewModel(Folder with { Size = 1536 });

        Assert.Equal("Ordner", folder.KindText);
        Assert.Equal(1536L.Sizeify(), folder.SizeText);
        Assert.NotEqual(folder.KindText, folder.SizeText);
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
        await document.LoadAsync();
        Assert.True(document.IsImageSeries);
        Assert.Equal("1 / 2", document.ImagePositionText);
    }

    [Fact]
    public async Task DesktopDocumentPresentation_KeepsVaultVisibleAndDisablesInlineViewer()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder];
        vault.Folders[10] = [TextFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Inhalt"u8.ToArray());
        var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.OpenSelectedCommand.ExecuteAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);

        await viewModel.OpenSelectedCommand.ExecuteAsync();

        Assert.NotNull(viewModel.ActiveDocument);
        Assert.True(viewModel.UseDocumentWindows);
        Assert.True(viewModel.ShowVaultContent);
        Assert.False(viewModel.ShowInlineDocument);
    }

    [Fact]
    public async Task DesktopDocuments_RemainOpenAndCloseIndependently()
    {
        VaultItem secondFile = TextFile with { Id = 21, Name = "Zweite Notiz" };
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [TextFile, secondFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Erster Inhalt"u8.ToArray());
        vault.FileContents[21] = new VaultFileContent(
            21,
            "Zweite Notiz",
            FileType.text,
            "txt",
            "Zweiter Inhalt"u8.ToArray());
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();

        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 20));
        VaultDocumentViewModel firstDocument = Assert.Single(viewModel.OpenDocuments);
        await firstDocument.LoadAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 21));
        VaultDocumentViewModel secondDocument = viewModel.OpenDocuments.Single(document => document.Id == 21);
        await secondDocument.LoadAsync();

        Assert.Equal(
            new ulong[] { 20, 21 },
            viewModel.OpenDocuments.Select(document => document.Id).ToArray());
        Assert.Equal("Erster Inhalt", firstDocument.Text);
        Assert.Equal("Zweiter Inhalt", secondDocument.Text);

        await firstDocument.CloseCommand.ExecuteAsync();

        Assert.Equal(
            new ulong[] { 21 },
            viewModel.OpenDocuments.Select(document => document.Id).ToArray());
        Assert.Empty(firstDocument.Text);
        Assert.Same(secondDocument, viewModel.ActiveDocument);
        Assert.Equal("Zweiter Inhalt", secondDocument.Text);
        Assert.True(secondDocument.IsContentReady);

        await secondDocument.CloseCommand.ExecuteAsync();

        Assert.Empty(viewModel.OpenDocuments);
        Assert.Null(viewModel.ActiveDocument);
        Assert.Empty(secondDocument.Text);
    }

    [Fact]
    public async Task DesktopDocuments_LoadConcurrentlyAndKeepTheirOwnContent()
    {
        VaultItem secondFile = TextFile with { Id = 21, Name = "Zweite Notiz" };
        var firstReadStarted = NewCompletion();
        var secondReadStarted = NewCompletion();
        var firstReadCompletion = NewCompletion<VaultFileContent>();
        var secondReadCompletion = NewCompletion<VaultFileContent>();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [TextFile, secondFile];
        vault.ReadFileAsyncHandler = (id, cancellationToken) =>
        {
            TaskCompletionSource started = id == 20 ? firstReadStarted : secondReadStarted;
            TaskCompletionSource<VaultFileContent> completion = id == 20
                ? firstReadCompletion
                : secondReadCompletion;
            started.TrySetResult();
            return completion.Task.WaitAsync(cancellationToken);
        };
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();

        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 20));
        await firstReadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 21));
        await secondReadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        VaultDocumentViewModel firstDocument = viewModel.OpenDocuments.Single(document => document.Id == 20);
        VaultDocumentViewModel secondDocument = viewModel.OpenDocuments.Single(document => document.Id == 21);

        Task secondLoaded = WaitForLoadingToFinishAsync(secondDocument);
        secondReadCompletion.SetResult(new VaultFileContent(
            21,
            "Zweite Notiz",
            FileType.text,
            "txt",
            "Zweiter Inhalt"u8.ToArray()));
        await secondLoaded.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(firstDocument.IsLoading);
        Assert.Equal("Zweiter Inhalt", secondDocument.Text);

        Task firstLoaded = WaitForLoadingToFinishAsync(firstDocument);
        firstReadCompletion.SetResult(new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Erster Inhalt"u8.ToArray()));
        await firstLoaded.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Erster Inhalt", firstDocument.Text);
        Assert.Equal("Zweiter Inhalt", secondDocument.Text);
        Assert.All(viewModel.OpenDocuments, document => Assert.True(document.IsContentReady));
    }

    [Fact]
    public async Task ClosingDesktopDocuments_SerializesTheirFolderRefreshes()
    {
        VaultItem secondFile = TextFile with { Id = 21, Name = "Zweite Notiz" };
        var firstRefreshStarted = NewCompletion();
        var secondRefreshStarted = NewCompletion();
        var allowFirstRefresh = NewCompletion();
        var allowSecondRefresh = NewCompletion();
        int refreshCount = 0;
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [TextFile, secondFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Erster Inhalt"u8.ToArray());
        vault.FileContents[21] = new VaultFileContent(
            21,
            "Zweite Notiz",
            FileType.text,
            "txt",
            "Zweiter Inhalt"u8.ToArray());
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 20));
        await viewModel.OpenDocuments.Single().LoadAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 21));
        await viewModel.OpenDocuments.Single(document => document.Id == 21).LoadAsync();
        VaultDocumentViewModel[] documents = viewModel.OpenDocuments.ToArray();
        vault.GetFolderItemsAsyncHandler = async (folderId, cancellationToken) =>
        {
            int refresh = Interlocked.Increment(ref refreshCount);
            TaskCompletionSource started = refresh == 1 ? firstRefreshStarted : secondRefreshStarted;
            TaskCompletionSource release = refresh == 1 ? allowFirstRefresh : allowSecondRefresh;
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return vault.Folders.GetValueOrDefault(folderId, []);
        };

        await documents[0].CloseCommand.ExecuteAsync();
        await firstRefreshStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await documents[1].CloseCommand.ExecuteAsync();

        Assert.False(secondRefreshStarted.Task.IsCompleted);
        Assert.Equal(1, Volatile.Read(ref refreshCount));

        allowFirstRefresh.SetResult();
        await secondRefreshStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task refreshesFinished = WaitForBusyToFinishAsync(viewModel);
        allowSecondRefresh.SetResult();
        await refreshesFinished.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, refreshCount);
        Assert.Empty(viewModel.OpenDocuments);
    }

    [Fact]
    public async Task LockingDuringUncooperativeRefresh_CancelsWaitingRefreshAndRejectsLateItems()
    {
        VaultItem secondFolder = Folder with { Id = 11, Name = "Zweiter Ordner" };
        var firstRefreshStarted = NewCompletion<CancellationToken>();
        var releaseFirstRefresh = NewCompletion<IReadOnlyList<VaultItem>>();
        int refreshCount = 0;
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder, secondFolder];
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();
        VaultItemViewModel firstFolder = viewModel.Items.Single(item => item.Id == Folder.Id);
        VaultItemViewModel otherFolder = viewModel.Items.Single(item => item.Id == secondFolder.Id);
        vault.GetFolderItemsAsyncHandler = (_, cancellationToken) =>
        {
            Interlocked.Increment(ref refreshCount);
            firstRefreshStarted.TrySetResult(cancellationToken);
            return releaseFirstRefresh.Task;
        };

        Task activeRefresh = viewModel.ActivateItemAsync(firstFolder);
        CancellationToken activeToken = await firstRefreshStarted.Task.WaitAsync(
            TestContext.Current.CancellationToken);
        Task waitingRefresh = viewModel.ActivateItemAsync(otherFolder);
        Assert.False(waitingRefresh.IsCompleted);

        viewModel.LockImmediately("Tresor gesperrt.");

        await waitingRefresh.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(activeToken.IsCancellationRequested);
        Assert.False(activeRefresh.IsCompleted);
        Assert.Equal(1, Volatile.Read(ref refreshCount));
        Assert.Empty(viewModel.Items);

        releaseFirstRefresh.SetResult([TextFile]);
        await activeRefresh.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, refreshCount);
        Assert.Empty(viewModel.Items);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task LockingDesktopVault_ClosesEveryOpenDocument()
    {
        VaultItem secondFile = TextFile with { Id = 21, Name = "Zweite Notiz" };
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [TextFile, secondFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Erster Inhalt"u8.ToArray());
        vault.FileContents[21] = new VaultFileContent(
            21,
            "Zweite Notiz",
            FileType.text,
            "txt",
            "Zweiter Inhalt"u8.ToArray());
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();

        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 20));
        await viewModel.OpenDocuments.Single().LoadAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 21));
        await viewModel.OpenDocuments.Single(document => document.Id == 21).LoadAsync();
        VaultDocumentViewModel[] documents = viewModel.OpenDocuments.ToArray();

        viewModel.LockImmediately("Tresor gesperrt.");

        Assert.Empty(viewModel.OpenDocuments);
        Assert.All(documents, document => Assert.Empty(document.Text));
        Assert.Empty(viewModel.Items);
    }

    [Fact]
    public async Task DetachingDesktopView_ClosesDocumentsWithoutLockingVaultContent()
    {
        VaultItem secondFile = TextFile with { Id = 21, Name = "Zweite Notiz" };
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [TextFile, secondFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Erster Inhalt"u8.ToArray());
        vault.FileContents[21] = new VaultFileContent(
            21,
            "Zweite Notiz",
            FileType.text,
            "txt",
            "Zweiter Inhalt"u8.ToArray());
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true);
        await viewModel.InitializeAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 20));
        await viewModel.OpenDocuments.Single().LoadAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 21));
        await viewModel.OpenDocuments.Single(document => document.Id == 21).LoadAsync();
        VaultDocumentViewModel[] documents = viewModel.OpenDocuments.ToArray();

        viewModel.CloseOpenDocuments();

        Assert.Empty(viewModel.OpenDocuments);
        Assert.Null(viewModel.ActiveDocument);
        Assert.All(documents, document => Assert.Empty(document.Text));
        Assert.Equal(2, viewModel.Items.Count);
    }

    [Fact]
    public async Task InlineDocument_ReplacesAndDisposesThePreviouslyOpenedDocument()
    {
        VaultItem secondFile = TextFile with { Id = 21, Name = "Zweite Notiz" };
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [TextFile, secondFile];
        vault.FileContents[20] = new VaultFileContent(
            20,
            "Notiz",
            FileType.text,
            "txt",
            "Erster Inhalt"u8.ToArray());
        vault.FileContents[21] = new VaultFileContent(
            21,
            "Zweite Notiz",
            FileType.text,
            "txt",
            "Zweiter Inhalt"u8.ToArray());
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: false);
        await viewModel.InitializeAsync();

        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 20));
        VaultDocumentViewModel firstDocument = Assert.Single(viewModel.OpenDocuments);
        await firstDocument.LoadAsync();
        await viewModel.ActivateItemAsync(viewModel.Items.Single(item => item.Id == 21));
        VaultDocumentViewModel secondDocument = Assert.Single(viewModel.OpenDocuments);
        await secondDocument.LoadAsync();

        Assert.Equal((ulong)21, secondDocument.Id);
        Assert.Same(secondDocument, viewModel.ActiveDocument);
        Assert.Empty(firstDocument.Text);
        Assert.Equal("Zweiter Inhalt", secondDocument.Text);
    }

    [Theory]
    [InlineData(FileType.text, "Notiz", "txt")]
    [InlineData(FileType.image, "Bild", "png")]
    [InlineData(FileType.audio, "Lied", "mp3")]
    [InlineData(FileType.video, "Film", "mp4")]
    public async Task DelayedPlayerRead_PublishesLoadingDocumentImmediatelyAndCompletesItInPlace(
        FileType type,
        string name,
        string extension)
    {
        var item = CreatePlayerItem(type, name, extension);
        var readStarted = NewCompletion<CancellationToken>();
        var readCompletion = NewCompletion<VaultFileContent>();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [item];
        vault.ReadFileAsyncHandler = (id, cancellationToken) =>
        {
            Assert.Equal(item.Id, id);
            readStarted.TrySetResult(cancellationToken);
            return readCompletion.Task.WaitAsync(cancellationToken);
        };
        var playback = new FakeMediaPlaybackService();
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true,
            mediaPlaybackService: playback);
        await viewModel.InitializeAsync();

        Task activation = viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        Assert.Equal(type, document.Type);
        Assert.Equal($"{name}.{extension}", document.DisplayName);
        Assert.True(document.IsLoading);
        Assert.False(document.IsContentReady);
        Assert.False(document.HasLoadFailed);
        Assert.True(viewModel.ShowVaultContent);
        Assert.False(viewModel.ShowInlineDocument);
        Assert.False(document.ShowMediaPlaceholder);

        Task loaded = WaitForLoadingToFinishAsync(document);
        readCompletion.SetResult(CreatePlayerContent(item));
        await Task.WhenAll(activation, loaded).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Same(document, viewModel.ActiveDocument);
        Assert.False(document.IsLoading);
        Assert.True(document.IsContentReady);
        Assert.False(document.HasLoadFailed);
        if (type != FileType.image)
            Assert.Empty(document.ErrorMessage);
        switch (type)
        {
            case FileType.text:
                Assert.Equal("Geladener Text", document.Text);
                break;
            case FileType.image:
                Assert.True(document.IsImageDocument);
                break;
            case FileType.audio:
                Assert.True(document.HasMediaPlayback);
                Assert.Equal(MediaPlaybackKind.Audio, playback.LastKind);
                break;
            case FileType.video:
                Assert.True(document.HasMediaPlayback);
                Assert.Equal(MediaPlaybackKind.Video, playback.LastKind);
                break;
        }
    }

    [Theory]
    [InlineData(FileType.text, "txt")]
    [InlineData(FileType.image, "png")]
    [InlineData(FileType.audio, "mp3")]
    [InlineData(FileType.video, "mp4")]
    public async Task FailedPlayerRead_KeepsPublishedDocumentOpenAndShowsTheError(
        FileType type,
        string extension)
    {
        var item = CreatePlayerItem(type, "Defekt", extension);
        var readStarted = NewCompletion<CancellationToken>();
        var readCompletion = NewCompletion<VaultFileContent>();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [item];
        vault.ReadFileAsyncHandler = (_, cancellationToken) =>
        {
            readStarted.TrySetResult(cancellationToken);
            return readCompletion.Task.WaitAsync(cancellationToken);
        };
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true,
            mediaPlaybackService: new FakeMediaPlaybackService());
        await viewModel.InitializeAsync();

        Task activation = viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        Task loaded = WaitForLoadingToFinishAsync(document);

        readCompletion.SetException(new InvalidDataException("Testinhalt ist beschädigt"));
        await Task.WhenAll(activation, loaded).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Same(document, viewModel.ActiveDocument);
        Assert.False(document.IsLoading);
        Assert.False(document.IsContentReady);
        Assert.True(document.HasLoadFailed);
        Assert.Contains("Testinhalt ist beschädigt", document.ErrorMessage);
        Assert.Empty(viewModel.ErrorMessage);
        Assert.True(document.CloseCommand.CanExecute(null));
    }

    [Fact]
    public async Task ClosingPlayerWhileLoading_CancelsReadAndDoesNotRestoreDocument()
    {
        var item = CreatePlayerItem(FileType.video, "Lang", "mp4");
        var readStarted = NewCompletion<CancellationToken>();
        var readCancelled = NewCompletion();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [item];
        vault.ReadFileAsyncHandler = async (_, cancellationToken) =>
        {
            readStarted.TrySetResult(cancellationToken);
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => readCancelled.TrySetResult());
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        };
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true,
            mediaPlaybackService: new FakeMediaPlaybackService());
        await viewModel.InitializeAsync();

        Task activation = viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        Task loading = document.LoadAsync();
        Assert.True(document.CloseCommand.CanExecute(null));

        await document.CloseCommand.ExecuteAsync();
        await readCancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(activation, loading).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.ActiveDocument);
        Assert.True(Assert.Single(vault.ReadFileRequests).CancellationToken.IsCancellationRequested);
        Assert.Empty(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LockingWhilePlayerLoads_CancelsReadAndNeverRestoresDocument()
    {
        var item = CreatePlayerItem(FileType.audio, "Lang", "flac");
        var readStarted = NewCompletion<CancellationToken>();
        var readCancelled = NewCompletion();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [item];
        vault.ReadFileAsyncHandler = async (_, cancellationToken) =>
        {
            readStarted.TrySetResult(cancellationToken);
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => readCancelled.TrySetResult());
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        };
        int lockNotifications = 0;
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => lockNotifications++,
            _ => { },
            mediaPlaybackService: new FakeMediaPlaybackService());
        await viewModel.InitializeAsync();

        Task activation = viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        Task loading = document.LoadAsync();

        viewModel.LockImmediately("Gesperrt");
        await readCancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(activation, loading).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.ActiveDocument);
        Assert.Empty(viewModel.Items);
        Assert.Equal(1, lockNotifications);
        Assert.True(Assert.Single(vault.ReadFileRequests).CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task LateContentAfterClose_IsZeroedAndNeverCreatesPlaybackSession()
    {
        var item = CreatePlayerItem(FileType.video, "Verspätet", "mp4");
        var readStarted = NewCompletion<CancellationToken>();
        var readCompletion = NewCompletion<VaultFileContent>();
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [item];
        vault.ReadFileAsyncHandler = (_, cancellationToken) =>
        {
            readStarted.TrySetResult(cancellationToken);
            return readCompletion.Task;
        };
        var playback = new FakeMediaPlaybackService();
        using var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            useDocumentWindows: true,
            mediaPlaybackService: playback);
        await viewModel.InitializeAsync();

        Task activation = viewModel.ActivateItemAsync(Assert.Single(viewModel.Items));
        CancellationToken readToken = await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        Task loading = document.LoadAsync();
        await document.CloseCommand.ExecuteAsync();
        Assert.True(readToken.IsCancellationRequested);

        byte[] lateContent = [11, 22, 33, 44, 55];
        readCompletion.SetResult(new VaultFileContent(
            item.Id,
            item.Name,
            item.Type,
            item.Extension,
            lateContent));
        await Task.WhenAll(activation, loading).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Null(viewModel.ActiveDocument);
        Assert.Null(playback.LastKind);
        Assert.All(lateContent, value => Assert.Equal((byte)0, value));
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
    public async Task VisibleFolderStatisticsFollowTheDisplayedItems()
    {
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [Folder, TextFile];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());

        await viewModel.InitializeAsync();

        Assert.Contains("1 Datei", viewModel.FolderStatisticsText);
        Assert.Contains("1 Ordner", viewModel.FolderStatisticsText);
        Assert.Contains("7 Bytes", viewModel.FolderStatisticsText);

        viewModel.SearchQuery = "Doku";

        Assert.Contains("0 Dateien", viewModel.FolderStatisticsText);
        Assert.Contains("1 Ordner", viewModel.FolderStatisticsText);
        Assert.Contains("0 Bytes", viewModel.FolderStatisticsText);
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
    public async Task ContextActionsOpenOneModalEditorAndBackClosesIt()
    {
        var vault = new FakeVaultApplicationService
        {
            FolderTargets = [new(0, "Tresor"), new(30, "Tresor › Archiv")]
        };
        vault.Folders[0] = [Folder];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        await viewModel.RequestRenameCommand.ExecuteAsync();
        Assert.True(viewModel.ShowRenameDialog);
        Assert.True(viewModel.HasActionDialog);
        Assert.Contains(nameof(VaultViewModel.ShowRenameDialog), changedProperties);

        Assert.True(viewModel.HandleBackRequested());
        Assert.False(viewModel.HasActionDialog);

        await viewModel.RequestMoveCommand.ExecuteAsync();
        Assert.True(viewModel.ShowMoveDialog);
        await viewModel.CancelActionDialogCommand.ExecuteAsync();
        Assert.False(viewModel.HasActionDialog);

        await viewModel.RequestDeleteCommand.ExecuteAsync();
        Assert.True(viewModel.ShowDeleteConfirmation);
        await viewModel.CancelDeleteCommand.ExecuteAsync();
        Assert.False(viewModel.HasActionDialog);

        viewModel.SelectedItem = null;
        await viewModel.RequestCreateFolderCommand.ExecuteAsync();
        Assert.True(viewModel.ShowCreateFolderDialog);
        Assert.Contains(nameof(VaultViewModel.ShowCreateFolderDialog), changedProperties);
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
        await document.LoadAsync();
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

    [Fact]
    public async Task ClosingSettingsRestoresDraftAndLockClosesAllDialogs()
    {
        var initial = new VaultPreferences(false, 12, 3, 4, true, 9, true);
        var vault = new FakeVaultApplicationService { Preferences = initial };
        vault.Folders[0] = [Folder];
        var viewModel = CreateViewModel(vault, new FakeFilePickerService());
        await viewModel.InitializeAsync();

        await viewModel.ToggleSettingsCommand.ExecuteAsync();
        viewModel.DarkMode = true;
        viewModel.AutoLockMinutes = 1;
        await viewModel.ToggleSettingsCommand.ExecuteAsync();

        Assert.False(viewModel.ShowSettings);
        Assert.Equal(initial.DarkMode, viewModel.DarkMode);
        Assert.Equal(initial.AutoLockMinutes, viewModel.AutoLockMinutes);

        viewModel.SelectedItem = Assert.Single(viewModel.Items);
        await viewModel.RequestRenameCommand.ExecuteAsync();
        await viewModel.ToggleSettingsCommand.ExecuteAsync();
        Assert.True(viewModel.ShowSettings);
        Assert.False(viewModel.HasActionDialog);

        viewModel.LockImmediately("Gesperrt");

        Assert.False(viewModel.ShowSettings);
        Assert.False(viewModel.HasActionDialog);
        Assert.Empty(viewModel.Items);
    }

    [Theory]
    [InlineData(FileType.video, "Clip", "mp4")]
    [InlineData(FileType.audio, "Song", "mp3")]
    public async Task LockImmediately_ClosesMediaAndClearsItsOwnedContent(
        FileType type,
        string name,
        string extension)
    {
        var media = new VaultItem(
            90,
            name,
            type,
            5,
            extension,
            new DateOnly(2026, 9, 1));
        var vault = new FakeVaultApplicationService();
        vault.Folders[0] = [media];
        vault.FileContents[90] = new VaultFileContent(
            90,
            name,
            type,
            extension,
            [1, 2, 3, 4, 5]);
        var playback = new FakeMediaPlaybackService();
        var viewModel = new VaultViewModel(
            vault,
            new FakeFilePickerService(),
            () => { },
            _ => { },
            mediaPlaybackService: playback);
        await viewModel.InitializeAsync();
        viewModel.SelectedItem = Assert.Single(viewModel.Items);

        await viewModel.OpenSelectedCommand.ExecuteAsync();

        VaultDocumentViewModel document = Assert.IsType<VaultDocumentViewModel>(viewModel.ActiveDocument);
        await document.LoadAsync();
        Assert.False(playback.Session.IsDisposed);

        viewModel.LockImmediately("Gesperrt");

        Assert.Null(viewModel.ActiveDocument);
        Assert.Equal(1, playback.Session.DisposeCount);
        Assert.True(playback.Session.ContentIsCleared);
    }

    private static VaultViewModel CreateViewModel(
        FakeVaultApplicationService vault,
        FakeFilePickerService picker) => new(vault, picker, () => { }, _ => { });

    private static VaultItem CreatePlayerItem(FileType type, string name, string extension) => new(
        90,
        name,
        type,
        1024,
        extension,
        new DateOnly(2026, 9, 1));

    private static VaultFileContent CreatePlayerContent(VaultItem item) => new(
        item.Id,
        item.Name,
        item.Type,
        item.Extension,
        item.Type switch
        {
            FileType.text => "Geladener Text"u8.ToArray(),
            FileType.image => OnePixelPng(),
            _ => [1, 2, 3, 4, 5]
        });

    private static TaskCompletionSource<T> NewCompletion<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Task WaitForLoadingToFinishAsync(VaultDocumentViewModel document)
    {
        if (!document.IsLoading)
            return Task.CompletedTask;

        var completion = NewCompletion();
        PropertyChangedEventHandler? handler = null;
        handler = (_, args) =>
        {
            if (args.PropertyName != nameof(VaultDocumentViewModel.IsLoading) || document.IsLoading)
                return;
            document.PropertyChanged -= handler;
            completion.TrySetResult();
        };
        document.PropertyChanged += handler;
        if (!document.IsLoading)
        {
            document.PropertyChanged -= handler;
            completion.TrySetResult();
        }

        return completion.Task;
    }

    private static Task WaitForBusyToFinishAsync(VaultViewModel viewModel)
    {
        if (!viewModel.IsBusy)
            return Task.CompletedTask;

        var completion = NewCompletion();
        PropertyChangedEventHandler? handler = null;
        handler = (_, args) =>
        {
            if (args.PropertyName != nameof(VaultViewModel.IsBusy) || viewModel.IsBusy)
                return;
            viewModel.PropertyChanged -= handler;
            completion.TrySetResult();
        };
        viewModel.PropertyChanged += handler;
        if (!viewModel.IsBusy)
        {
            viewModel.PropertyChanged -= handler;
            completion.TrySetResult();
        }

        return completion.Task;
    }

    private static byte[] OnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAQSURBVBhXY/jPwPCfARkAAB7zAf+x9MCaAAAAAElFTkSuQmCC");
}
