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
    public void Root_ClampsViewerBuffersAndSuppliesDefaults()
    {
        var root = new ThingRoot
        {
            ImageViewerPreviousBufferCount = -10,
            ImageViewerNextBufferCount = 100
        };

        Assert.Equal(0, root.ImageViewerPreviousBufferCount);
        Assert.Equal(ThingRoot.MaximumImageViewerBufferCount, root.ImageViewerNextBufferCount);
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
