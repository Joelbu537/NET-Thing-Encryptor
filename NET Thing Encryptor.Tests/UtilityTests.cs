using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class UtilityTests
{
    [Theory]
    [InlineData("photo.JPG", FileType.image)]
    [InlineData("movie.mkv", FileType.video)]
    [InlineData("notes.md", FileType.text)]
    [InlineData("music.FLAC", FileType.audio)]
    [InlineData("archive.zip", FileType.other)]
    [InlineData("README", FileType.other)]
    [InlineData("", FileType.other)]
    [InlineData("   ", FileType.other)]
    public void GetFileType_ClassifiesExtensions(string path, FileType expected)
    {
        Assert.Equal(expected, FileCategories.GetFileType(path));
    }

    [Fact]
    public void GetFileType_ClassifiesEveryConfiguredExtensionCaseInsensitively()
    {
        foreach (FileCategorie category in FileCategories.Categories)
        {
            foreach (string extension in category.Extensions)
            {
                Assert.Equal(category.Type, FileCategories.GetFileType("file" + extension));
                Assert.Equal(category.Type, FileCategories.GetFileType("file" + extension.ToUpperInvariant()));
            }
        }
    }

    [Fact]
    public async Task GetFileType_RecognizesDirectories()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        Assert.Equal(FileType.folder, FileCategories.GetFileType(environment.Directory));
        string misleadingDirectory = Path.Combine(environment.Directory, "looks-like-an-image.jpg");
        System.IO.Directory.CreateDirectory(misleadingDirectory);
        Assert.Equal(FileType.folder, FileCategories.GetFileType(misleadingDirectory));
    }

    [Theory]
    [InlineData(0UL, "0000000000000000")]
    [InlineData(1UL, "0000000000000001")]
    [InlineData(0xABCDEF0123456789UL, "ABCDEF0123456789")]
    [InlineData(ulong.MaxValue, "FFFFFFFFFFFFFFFF")]
    public void IdHexConversion_RoundTrips(ulong id, string expected)
    {
        Assert.Equal(expected, ThingData.IDToHex(id));
        Assert.Equal(id, ThingData.HexToID(expected.ToLowerInvariant()));
    }

    [Fact]
    public void HexToId_RejectsInvalidInput()
    {
        Assert.Throws<FormatException>(() => ThingData.HexToID("not-hex"));
        Assert.Throws<FormatException>(() => ThingData.HexToID("10000000000000000"));
        Assert.Throws<FormatException>(() => ThingData.HexToID(string.Empty));
    }

    [Theory]
    [InlineData(0L, "0 Bytes")]
    [InlineData(1023L, "1023 Bytes")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1073741824L, "1 GB")]
    public void Sizeify_FormatsBinaryUnits(long bytes, string expected)
    {
        string localizedExpected = expected.Replace(
            ".",
            System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        Assert.Equal(localizedExpected, bytes.Sizeify());
    }

    [Fact]
    public void PathEquals_NormalizesPathsAndTrailingSeparators()
    {
        string left = Path.Combine(Path.GetTempPath(), "folder", "child", "..");
        string right = Path.Combine(Path.GetTempPath(), "folder") + Path.DirectorySeparatorChar;
        Assert.True(AppPaths.PathEquals(left, right));
    }

    [Fact]
    public void NaturalComparer_SortsNumericSegmentsNaturally()
    {
        string[] values = ["file10", "file2", "file1"];
        Array.Sort(values, new NaturalStringComparer());
        Assert.Equal(["file1", "file2", "file10"], values);
        Assert.Equal(0, new NaturalStringComparer().Compare(null, string.Empty));
    }
}
