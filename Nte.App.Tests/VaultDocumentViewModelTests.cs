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
        var service = new FakeVideoPlaybackService();
        var viewModel = new VaultDocumentViewModel(
            new VaultFileContent(7, "clip", FileType.video, "mp4", content),
            (_, _) => Task.CompletedTask,
            () => { },
            _ => { },
            videoPlaybackService: service);

        Assert.True(viewModel.IsVideo);
        Assert.True(viewModel.HasVideoPlayback);
        Assert.False(viewModel.IsGeneric);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, content);

        await viewModel.StartVideoCommand.ExecuteAsync();
        Assert.Equal(1, service.Session.PlayCount);

        service.Session.Publish(
            isPlaying: true,
            canSeek: true,
            positionMilliseconds: 1_500,
            durationMilliseconds: 10_000);
        Assert.True(viewModel.IsVideoPlaying);
        Assert.True(viewModel.CanSeekVideo);
        Assert.Equal("0:01 / 0:10", viewModel.VideoTimeText);

        await viewModel.SeekVideoForwardCommand.ExecuteAsync();
        Assert.Equal(10_000, service.Session.LastSeekMilliseconds);

        viewModel.BeginVideoSeek();
        viewModel.VideoPositionMilliseconds = 4_000;
        viewModel.CompleteVideoSeek();
        Assert.Equal(4_000, service.Session.LastSeekMilliseconds);
        Assert.Equal(2, service.Session.PlayCount);
        Assert.Equal(1, service.Session.PauseCount);

        service.Session.Publish(
            isPlaying: false,
            canSeek: true,
            positionMilliseconds: 10_000,
            durationMilliseconds: 10_000);
        Assert.False(viewModel.IsVideoPlaying);
        Assert.Equal("0:10 / 0:10", viewModel.VideoTimeText);

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(service.Session.IsDisposed);
        Assert.Equal(1, service.Session.DisposeCount);
        Assert.All(content, value => Assert.Equal((byte)0, value));
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
            videoPlaybackService: new ThrowingVideoPlaybackService());

        Assert.True(viewModel.ShowVideoPlaceholder);
        Assert.Contains("Backend nicht verfügbar", viewModel.ErrorMessage);
        Assert.All(content, value => Assert.Equal((byte)0, value));
    }

    private static byte[] OnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAXSURBVBhXY/jPwPCfoYHhPwMDw38wAABD1Al4TlSdlQAAAABJRU5ErkJggg==");

    private sealed class ThrowingVideoPlaybackService : IVideoPlaybackService
    {
        public IVideoPlaybackSession CreateSession(byte[] decryptedContent) =>
            throw new InvalidOperationException("Backend nicht verfügbar");

        public void Dispose()
        {
        }
    }
}
