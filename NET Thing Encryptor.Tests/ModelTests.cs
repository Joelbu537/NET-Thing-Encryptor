using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class ModelTests
{
    [Fact]
    public async Task ThingFile_TracksHashAndCanReleaseContent()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        var file = new ThingFile("sample", [1, 2, 3]);

        Assert.Equal("5289df737df57326fcdd22597afb1fac", file.MD5Hash);
        file.Content = [4, 5];
        Assert.Equal(ThingData.GetMD5Hash([4, 5]), file.MD5Hash);

        file.ReleaseContent();
        Assert.Null(file.Content);
        Assert.NotEqual(0UL, file.ID);

        file.Clear();
        Assert.Equal(0UL, file.ID);
        Assert.Throws<ArgumentNullException>(() => file.Content = null);
    }

    [Fact]
    public void Root_ClampsSettingsAndSuppliesDefaults()
    {
        var root = new ThingRoot
        {
            ImageViewerPreviousBufferCount = -10,
            ImageViewerNextBufferCount = 100,
            AutoLockMinutes = -1,
            ImageAutoplayIntervalSeconds = 0
        };

        Assert.Equal(0, root.ImageViewerPreviousBufferCount);
        Assert.Equal(ThingRoot.MaximumImageViewerBufferCount, root.ImageViewerNextBufferCount);
        Assert.Equal(0, root.AutoLockMinutes);
        Assert.Equal(1, root.ImageAutoplayIntervalSeconds);
        Assert.Equal("C:\\", root.ImportLocation);
        Assert.Equal("C:\\", root.ExportLocation);
    }

    [Fact]
    public void RootClone_CopiesMutableCollectionsAndSalt()
    {
        var root = new ThingRoot
        {
            Salt = [1, 2, 3],
            Content = [new ThingObjectLink(1, "one", FileType.text, 3)]
        };

        var clone = Assert.IsType<ThingRoot>(root.Clone());
        Assert.NotSame(root.Salt, clone.Salt);
        Assert.NotSame(root.Content, clone.Content);
        Assert.Equal(root.Salt, clone.Salt);
        Assert.Single(clone.Content!);
    }

    [Fact]
    public void RandomiseOrder_KeepsSelectedImageFirstWhenConfigured()
    {
        List<ThingObjectLink> images =
        [
            new(1, "one", FileType.image, 1),
            new(2, "two", FileType.image, 1),
            new(3, "three", FileType.image, 1),
            new(4, "four", FileType.image, 1)
        ];

        int selectedIndex = ImageViewForm.RandomiseOrder(
            images,
            selectedIndex: 2,
            includeSelectedImage: false,
            new Random(42));

        Assert.Equal(0, selectedIndex);
        Assert.Equal(3UL, images[0].ID);
        Assert.Equal([1UL, 2UL, 3UL, 4UL], images.Select(image => image.ID).Order());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(49)]
    [InlineData(99)]
    public void RandomiseOrder_StartsBeforeFirstImageAndDoesNotSkipAnyImages(int selectedIndex)
    {
        List<ThingObjectLink> images = Enumerable.Range(1, 100)
            .Select(id => new ThingObjectLink((ulong)id, id.ToString(), FileType.image, 1))
            .ToList();
        ThingObjectLink selectedImage = images[selectedIndex];

        int cursor = ImageViewForm.RandomiseOrder(
            images,
            selectedIndex,
            includeSelectedImage: true,
            random: new Random(42));

        Assert.Equal(-1, cursor);
        Assert.Contains(selectedImage, images);

        // The same forward increment is used by manual navigation and autoplay.
        List<ulong> visited = [];
        while (++cursor < images.Count)
            visited.Add(images[cursor].ID);

        Assert.Equal(images.Select(image => image.ID), visited);
        Assert.Equal(Enumerable.Range(1, 100).Select(id => (ulong)id), visited.Order());
    }

    [Fact]
    public void RandomiseOrder_CanPinTheDisplayedImageAfterAnEarlierShuffle()
    {
        List<ThingObjectLink> images = Enumerable.Range(1, 100)
            .Select(id => new ThingObjectLink((ulong)id, id.ToString(), FileType.image, 1))
            .ToList();
        ulong displayedImageId = images[49].ID;
        Assert.Equal(-1, ImageViewForm.RandomiseOrder(images, 49, true, new Random(42)));

        int cursor = ImageViewForm.RandomiseOrder(
            images,
            images.FindIndex(image => image.ID == displayedImageId),
            includeSelectedImage: false,
            random: new Random(17));

        Assert.Equal(0, cursor);
        Assert.Equal(displayedImageId, images[0].ID);
        Assert.Equal(100, images.Select(image => image.ID).Distinct().Count());
    }

    [Fact]
    public void TimelineValueAndEventArgs_AreClamped()
    {
        using var timeline = new VideoTimeline { Value = 2.5 };
        Assert.Equal(1d, timeline.Value);
        timeline.Value = -1;
        Assert.Equal(0d, timeline.Value);
        Assert.Equal(1d, new TimelineSeekEventArgs(5).Position);
        Assert.Equal(0d, new TimelineSeekEventArgs(-5).Position);
    }

    [Fact]
    public void ClearImage_RemovesAndDisposesCurrentImage()
    {
        using var pictureBox = new PictureBox();
        var bitmap = new Bitmap(2, 2);
        pictureBox.Image = bitmap;

        pictureBox.ClearImage();

        Assert.Null(pictureBox.Image);
        Assert.ThrowsAny<Exception>(() => bitmap.GetHbitmap());
    }

    [Fact]
    public void SavingCounter_TracksNestedOperations()
    {
        int initial = ThingData.Saving;
        ThingData.BeginSaving();
        ThingData.BeginSaving();
        try
        {
            Assert.Equal(initial + 2, ThingData.Saving);
        }
        finally
        {
            ThingData.EndSaving();
            ThingData.EndSaving();
        }
        Assert.Equal(initial, ThingData.Saving);
    }
}
