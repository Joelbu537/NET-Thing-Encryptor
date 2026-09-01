using NET_Thing_Encryptor;
using Nte.App.Services;
using Nte.App.Tests.Fakes;
using Nte.App.ViewModels;

namespace Nte.App.Tests;

public sealed class VaultDocumentViewModelTests
{
    [Fact]
    public async Task TextDocument_StartsReadOnlyAndSavesWithOriginalBom()
    {
        byte[]? saved = null;
        string status = string.Empty;
        var file = new VaultFileContent(
            42,
            "note",
            FileType.text,
            "txt",
            [0xEF, 0xBB, 0xBF, .. "before"u8.ToArray()]);
        using var viewModel = new VaultDocumentViewModel(
            file,
            (content, _) =>
            {
                saved = content.ToArray();
                return Task.CompletedTask;
            },
            () => { },
            value => status = value);

        Assert.True(viewModel.IsReadOnly);
        await viewModel.ToggleEditingCommand.ExecuteAsync();
        viewModel.Text = "after";
        await viewModel.SaveCommand.ExecuteAsync();

        Assert.False(viewModel.IsDirty);
        Assert.NotNull(saved);
        Assert.True(saved.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Equal("after", TextDocumentCodec.Decode(saved).Text);
        Assert.Contains("gespeichert", status);
    }

    [Fact]
    public async Task ClosingDirtyText_RequiresExplicitDiscard()
    {
        int closes = 0;
        var file = new VaultFileContent(42, "note", FileType.text, "txt", "before"u8.ToArray());
        var viewModel = new VaultDocumentViewModel(
            file,
            (_, _) => Task.CompletedTask,
            () => closes++,
            _ => { });
        viewModel.Text = "changed";

        await viewModel.CloseCommand.ExecuteAsync();
        Assert.True(viewModel.ShowDiscardConfirmation);
        Assert.Equal(0, closes);

        await viewModel.DiscardAndCloseCommand.ExecuteAsync();
        Assert.Equal(1, closes);
    }

    [Fact]
    public void SearchCountsCaseInsensitiveMatches()
    {
        var file = new VaultFileContent(42, "note", FileType.text, "txt", "One one ONE"u8.ToArray());
        using var viewModel = new VaultDocumentViewModel(
            file,
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { });

        viewModel.SearchText = "one";

        Assert.Equal("3 Treffer", viewModel.SearchResultText);
    }

    [Fact]
    public async Task ImageSeries_NavigatesRandomisesAndStopsAutoplayAtTheEnd()
    {
        byte[] firstBuffer = OnePixelPng();
        byte[] png = OnePixelPng();
        var files = new Dictionary<ulong, VaultFileContent>
        {
            [1] = new(1, "First", FileType.image, "png", png),
            [2] = new(2, "Second", FileType.image, "png", png)
        };
        var series = new VaultImageSeriesOptions(
            [new(1, "First", "png"), new(2, "Second", "png")],
            IncludeSelectedImageWhenRandomising: false,
            AutoplayIntervalSeconds: 1,
            LoopAutoplay: false);
        using var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(1, "First", FileType.image, "png", firstBuffer),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            series,
            (id, _) => Task.FromResult(files[id] with { Content = files[id].Content.ToArray() }),
            (_, _) => Task.CompletedTask,
            _ => null);

        Assert.True(viewModel.IsImageSeries);
        Assert.Equal("1 / 2", viewModel.ImagePositionText);
        Assert.All(firstBuffer, value => Assert.Equal((byte)0, value));

        await viewModel.NextImageCommand.ExecuteAsync();
        Assert.Equal((ulong)2, viewModel.Id);
        Assert.Equal("Second.png", viewModel.DisplayName);
        Assert.Equal("2 / 2", viewModel.ImagePositionText);

        await viewModel.PreviousImageCommand.ExecuteAsync();
        await viewModel.RandomiseImagesCommand.ExecuteAsync();
        Assert.Equal((ulong)1, viewModel.Id);
        Assert.Equal("1 / 2", viewModel.ImagePositionText);

        await viewModel.ToggleAutoplayCommand.ExecuteAsync();
        Assert.Equal((ulong)2, viewModel.Id);
        Assert.False(viewModel.IsAutoplayRunning);
        Assert.Equal("2 / 2", viewModel.ImagePositionText);
    }

    [Fact]
    public async Task DisposingImageSeries_StopsAutoplayAndReleasesCurrentBitmap()
    {
        byte[] png = OnePixelPng();
        var series = new VaultImageSeriesOptions(
            [new(1, "First", "png"), new(2, "Second", "png")],
            IncludeSelectedImageWhenRandomising: false,
            AutoplayIntervalSeconds: 1,
            LoopAutoplay: true);
        var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(1, "First", FileType.image, "png", png),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            series,
            (id, _) => Task.FromResult(new VaultFileContent(id, "Next", FileType.image, "png", OnePixelPng())),
            (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
            _ => null);

        await viewModel.ToggleAutoplayCommand.ExecuteAsync();
        Assert.True(viewModel.IsAutoplayRunning);

        viewModel.Dispose();

        Assert.False(viewModel.IsAutoplayRunning);
        Assert.Null(viewModel.Image);
    }

    [Fact]
    public async Task ImageSeries_FailedNavigationKeepsCurrentDocumentAndAllowsRecovery()
    {
        int decodeCount = 0;
        byte[] brokenBuffer = OnePixelPng();
        var series = new VaultImageSeriesOptions(
            [new(1, "First", "png"), new(2, "Broken", "png")],
            IncludeSelectedImageWhenRandomising: false,
            AutoplayIntervalSeconds: 1,
            LoopAutoplay: false);
        using var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(1, "First", FileType.image, "png", OnePixelPng()),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            series,
            (id, _) => Task.FromResult(new VaultFileContent(id, "Broken", FileType.image, "png", brokenBuffer)),
            (_, _) => Task.CompletedTask,
            _ => ++decodeCount == 1 ? null : throw new InvalidDataException("defekt"));

        await viewModel.NextImageCommand.ExecuteAsync();

        Assert.Equal((ulong)1, viewModel.Id);
        Assert.Equal("First.png", viewModel.DisplayName);
        Assert.True(viewModel.IsImageDocument);
        Assert.True(viewModel.IsImageSeries);
        Assert.Contains("defekt", viewModel.ErrorMessage);
        Assert.All(brokenBuffer, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task VideoSession_RetainsContentUntilDisposeAndMapsPlaybackControls()
    {
        byte[] content = [1, 2, 3, 4, 5];
        var service = new FakeMediaPlaybackService();
        var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(7, "clip", FileType.video, "mp4", content),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: service);

        Assert.True(viewModel.IsVideo);
        Assert.True(viewModel.HasMediaPlayback);
        Assert.True(viewModel.HasVideoSurface);
        Assert.Equal(MediaPlaybackKind.Video, service.LastKind);
        Assert.False(viewModel.IsGeneric);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, content);

        await viewModel.StartMediaCommand.ExecuteAsync();
        Assert.Equal(1, service.Session.PlayCount);

        service.Session.Publish(
            isPlaying: true,
            canSeek: true,
            positionMilliseconds: 1_500,
            durationMilliseconds: 10_000);
        Assert.True(viewModel.IsMediaPlaying);
        Assert.True(viewModel.CanSeekMedia);
        Assert.Equal("0:01 / 0:10", viewModel.MediaTimeText);

        await viewModel.SeekMediaForwardCommand.ExecuteAsync();
        Assert.Equal(10_000, service.Session.LastSeekMilliseconds);

        viewModel.BeginMediaSeek();
        viewModel.MediaPositionMilliseconds = 4_000;
        viewModel.CompleteMediaSeek();
        Assert.Equal(4_000, service.Session.LastSeekMilliseconds);
        Assert.Equal(2, service.Session.PlayCount);
        Assert.Equal(1, service.Session.PauseCount);

        service.Session.Publish(
            isPlaying: false,
            canSeek: true,
            positionMilliseconds: 10_000,
            durationMilliseconds: 10_000);
        Assert.False(viewModel.IsMediaPlaying);
        Assert.Equal("0:10 / 0:10", viewModel.MediaTimeText);

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(service.Session.IsDisposed);
        Assert.Equal(1, service.Session.DisposeCount);
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task AudioSession_UsesSharedPlaybackControlsWithoutVideoSurface()
    {
        byte[] content = [9, 8, 7, 6];
        var service = new FakeMediaPlaybackService();
        var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(8, "song", FileType.audio, "flac", content),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: service);

        Assert.True(viewModel.IsAudio);
        Assert.True(viewModel.IsMedia);
        Assert.False(viewModel.IsGeneric);
        Assert.True(viewModel.HasMediaPlayback);
        Assert.True(viewModel.ShowAudioPresentation);
        Assert.False(viewModel.HasVideoSurface);
        Assert.Null(viewModel.VideoSurface);
        Assert.Equal(MediaPlaybackKind.Audio, service.LastKind);
        Assert.Equal(new byte[] { 9, 8, 7, 6 }, content);

        await viewModel.StartMediaCommand.ExecuteAsync();
        Assert.Equal(1, service.Session.PlayCount);

        service.Session.Publish(
            isPlaying: true,
            canSeek: true,
            positionMilliseconds: 15_000,
            durationMilliseconds: 30_000);
        Assert.True(viewModel.IsMediaPlaying);
        Assert.True(viewModel.CanSeekMedia);
        Assert.Equal("0:15 / 0:30", viewModel.MediaTimeText);

        await viewModel.SeekMediaBackwardCommand.ExecuteAsync();
        Assert.Equal(5_000, service.Session.LastSeekMilliseconds);
        await viewModel.SeekMediaForwardCommand.ExecuteAsync();
        Assert.Equal(25_000, service.Session.LastSeekMilliseconds);

        viewModel.BeginMediaSeek();
        viewModel.MediaPositionMilliseconds = 12_000;
        viewModel.CompleteMediaSeek();
        Assert.Equal(12_000, service.Session.LastSeekMilliseconds);
        Assert.Equal(2, service.Session.PlayCount);
        Assert.Equal(1, service.Session.PauseCount);

        await viewModel.ToggleMediaPlaybackCommand.ExecuteAsync();
        Assert.Equal(2, service.Session.PauseCount);

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.Equal(1, service.Session.DisposeCount);
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    [Theory]
    [InlineData(FileType.audio, "mp3", MediaPlaybackKind.Audio)]
    [InlineData(FileType.video, "mp4", MediaPlaybackKind.Video)]
    public async Task LoadingMedia_RemembersStartRequestUntilSessionIsCreated(
        FileType type,
        string extension,
        MediaPlaybackKind expectedKind)
    {
        var readStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var readCompletion = new TaskCompletionSource<VaultFileContent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int readCalls = 0;
        var playback = new FakeMediaPlaybackService();
        byte[] content = [9, 8, 7, 6, 5];
        var viewModel = VaultDocumentViewModel.CreateLoading(
            new VaultFileReference(8, "medium", type, extension),
            (_, cancellationToken) =>
            {
                readCalls++;
                readStarted.TrySetResult();
                return readCompletion.Task.WaitAsync(cancellationToken);
            },
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: playback);

        Task firstLoad = viewModel.LoadAsync();
        Task secondLoad = viewModel.LoadAsync();
        await readStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(viewModel.IsLoading);
        Assert.True(viewModel.StartMediaCommand.CanExecute(null));
        await viewModel.StartMediaCommand.ExecuteAsync();
        Assert.Null(playback.LastKind);

        readCompletion.SetResult(new VaultFileContent(
            8,
            "medium",
            type,
            extension,
            content));
        await Task.WhenAll(firstLoad, secondLoad).WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, readCalls);
        Assert.False(viewModel.IsLoading);
        Assert.True(viewModel.IsContentReady);
        Assert.Equal(expectedKind, playback.LastKind);
        Assert.Equal(1, playback.Session.PlayCount);

        viewModel.Dispose();
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task DisposeDuringBlockedVideoSessionCreation_DiscardsSessionAndClearsContent()
    {
        byte[] content = [7, 6, 5, 4, 3, 2, 1];
        var playback = new BlockingMediaPlaybackService();
        var viewModel = VaultDocumentViewModel.CreateLoading(
            new VaultFileReference(17, "race", FileType.video, "mp4"),
            (_, _) => Task.FromResult(new VaultFileContent(
                17,
                "race",
                FileType.video,
                "mp4",
                content)),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: playback);
        await viewModel.StartMediaCommand.ExecuteAsync();

        Task loading = viewModel.LoadAsync();
        await playback.SessionCreationStarted.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            await Task.Run(
                    viewModel.Dispose,
                    TestContext.Current.CancellationToken)
                .WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            playback.ReleaseSessionCreation();
        }

        await loading.WaitAsync(TestContext.Current.CancellationToken);

        FakeMediaPlaybackSession session = Assert.IsType<FakeMediaPlaybackSession>(
            playback.CreatedSession);
        Assert.Equal(1, session.DisposeCount);
        Assert.True(session.ContentIsCleared);
        Assert.Equal(0, session.PlayCount);
        Assert.False(viewModel.HasMediaPlayback);
        Assert.Null(viewModel.VideoSurface);
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    [Theory]
    [InlineData(FileType.image, "png")]
    [InlineData(FileType.audio, "flac")]
    [InlineData(FileType.video, "mp4")]
    public void SeparateMediaWindow_HidesBackButtonWhileInlineMediaKeepsIt(
        FileType type,
        string extension)
    {
        var service = new FakeMediaPlaybackService();
        using var windowDocument = new VaultDocumentViewModel(
            new VaultFileContent(8, "media", type, extension, [1]),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: service,
            showBackButton: false);

        Assert.False(windowDocument.ShowBackButton);

        using var inlineDocument = new VaultDocumentViewModel(
            new VaultFileContent(9, "media", type, extension, [2]),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: new FakeMediaPlaybackService());

        Assert.True(inlineDocument.ShowBackButton);
    }

    [Fact]
    public void VideoSessionCreationFailure_ZeroesContentAndKeepsFallbackVisible()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(7, "broken", FileType.video, "mp4", content),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: new ThrowingMediaPlaybackService());

        Assert.True(viewModel.ShowMediaPlaceholder);
        Assert.Contains("Backend nicht verfügbar", viewModel.ErrorMessage);
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public void AudioSessionCreationFailure_ZeroesContentAndKeepsFallbackVisible()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(8, "broken", FileType.audio, "flac", content),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            mediaPlaybackService: new ThrowingMediaPlaybackService());

        Assert.True(viewModel.IsAudio);
        Assert.True(viewModel.ShowMediaPlaceholder);
        Assert.Contains("Backend nicht verfügbar", viewModel.ErrorMessage);
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    private static byte[] OnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAQSURBVBhXY/jPwPCfARkAAB7zAf+x9MCaAAAAAElFTkSuQmCC");

    private sealed class ThrowingMediaPlaybackService : IMediaPlaybackService
    {
        public IMediaPlaybackSession CreateSession(
            byte[] decryptedContent,
            MediaPlaybackKind kind) =>
            throw new InvalidOperationException("Backend nicht verfügbar");

        public void Dispose()
        {
        }
    }

    private sealed class BlockingMediaPlaybackService : IMediaPlaybackService
    {
        private readonly TaskCompletionSource _sessionCreationStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseSessionCreation = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SessionCreationStarted => _sessionCreationStarted.Task;
        public IMediaPlaybackSession? CreatedSession { get; private set; }

        public IMediaPlaybackSession CreateSession(
            byte[] decryptedContent,
            MediaPlaybackKind kind)
        {
            _sessionCreationStarted.TrySetResult();
            _releaseSessionCreation.Task.GetAwaiter().GetResult();
            CreatedSession = new FakeMediaPlaybackSession(decryptedContent, kind);
            return CreatedSession;
        }

        public void ReleaseSessionCreation() => _releaseSessionCreation.TrySetResult();

        public void Dispose()
        {
        }
    }
}
