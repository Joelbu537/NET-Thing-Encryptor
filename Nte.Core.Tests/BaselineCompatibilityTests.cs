using System.Security.Cryptography;
using System.Text;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class BaselineCompatibilityTests
{
    private const string Password = "M0 synthetic baseline password";
    private const ulong FolderId = 0x1000000000000001;

    [Fact]
    public async Task Version37Fixture_LoadsWithoutRewritingTheBaseline()
    {
        string fixtureDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "baseline-v3.7");
        string workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "NETThingEncryptor.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        VerifyFixtureHashes(fixtureDirectory);

        ThingData.LockSession();
        TestEnvironment.SetRoot(null);
        AppPaths.DataDirectoryOverride = workingDirectory;
        try
        {
            foreach (string sourceFile in Directory.EnumerateFiles(fixtureDirectory))
            {
                File.Copy(sourceFile, Path.Combine(workingDirectory, Path.GetFileName(sourceFile)));
            }

            Assert.All(
                Directory.EnumerateFiles(workingDirectory, "*.nte").Where(path => Path.GetFileName(path) != "0.nte"),
                path => Assert.NotEmpty(File.ReadAllBytes(path)));
            Assert.True(await ThingData.LoadMainData());
            Assert.False(await ThingData.AttemptDecrypt("wrong password"));
            Assert.True(await ThingData.AttemptDecrypt(Password));

            ThingObjectLink rootLink = Assert.Single(ThingData.Root!.Content!);
            Assert.Equal(FolderId, rootLink.ID);
            Assert.Equal("M0 Baseline", rootLink.Name);
            Assert.Equal(FileType.folder, rootLink.Type);

            ThingData.Root.SaveLocation = workingDirectory;
            ThingFolder folder = Assert.IsType<ThingFolder>(await ThingData.LoadFileAsync(FolderId));
            Assert.Collection(
                folder.Content.OrderBy(link => link.ID),
                link => AssertLink(link, 0x2000000000000001, "Unicode notes", "txt", FileType.text),
                link => AssertLink(link, 0x3000000000000001, "One pixel", "png", FileType.image),
                link => AssertLink(link, 0x4000000000000001, "Synthetic tone", "wav", FileType.audio),
                link => AssertLink(link, 0x5000000000000001, "Synthetic clip", "mp4", FileType.video));

            ThingFile text = (await ThingData.LoadFileAsync<ThingFile>(0x2000000000000001))!;
            Assert.Equal(Encoding.UTF8.Preamble.ToArray(), text.Content![..Encoding.UTF8.Preamble.Length]);
            Assert.Contains("Grüße 🔐", Encoding.UTF8.GetString(text.Content!, Encoding.UTF8.Preamble.Length,
                text.Content!.Length - Encoding.UTF8.Preamble.Length));

            ThingFile image = (await ThingData.LoadFileAsync<ThingFile>(0x3000000000000001))!;
            Assert.Equal([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], image.Content![..8]);

            ThingFile audio = (await ThingData.LoadFileAsync<ThingFile>(0x4000000000000001))!;
            Assert.Equal("RIFF", Encoding.ASCII.GetString(audio.Content!, 0, 4));
            Assert.Equal("WAVE", Encoding.ASCII.GetString(audio.Content!, 8, 4));

            ThingFile video = (await ThingData.LoadFileAsync<ThingFile>(0x5000000000000001))!;
            Assert.Equal("ftyp", Encoding.ASCII.GetString(video.Content!, 4, 4));

            await using FileStream legacyInput = File.OpenRead(Path.Combine(workingDirectory, "legacy-cbc.nte"));
            await using MemoryStream legacyPlaintext = await ThingData.Decrypt(legacyInput);
            Assert.Equal("M0 legacy CBC fixture", Encoding.UTF8.GetString(legacyPlaintext.ToArray()));

            string storedRoot = await File.ReadAllTextAsync(
                Path.Combine(workingDirectory, "0.nte"),
                TestContext.Current.CancellationToken);
            Assert.DoesNotContain("M0 Baseline", storedRoot);
            Assert.Contains(@"C:\\Synthetic\\NET Thing Encryptor\\Data", storedRoot);
        }
        finally
        {
            ThingData.LockSession();
            TestEnvironment.SetRoot(null);
            AppPaths.DataDirectoryOverride = null;
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private static void VerifyFixtureHashes(string fixtureDirectory)
    {
        string manifestPath = Path.Combine(fixtureDirectory, "manifest.sha256");
        string[] manifestLines = File.ReadAllLines(manifestPath);

        foreach (string line in manifestLines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            int separator = line.IndexOf(" *", StringComparison.Ordinal);
            Assert.True(separator > 0, $"Invalid fixture manifest line: {line}");

            string expectedHash = line[..separator];
            string fileName = line[(separator + 2)..];
            string fixturePath = Path.Combine(fixtureDirectory, fileName);
            Assert.True(File.Exists(fixturePath), $"Missing baseline fixture: {fileName}");

            string actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixturePath)));
            Assert.True(
                string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase),
                $"Baseline fixture hash changed: {fileName}");
        }

        Assert.Equal(
            Directory.EnumerateFiles(fixtureDirectory, "*.nte").Count(),
            manifestLines.Count(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static void AssertLink(
        ThingObjectLink link,
        ulong id,
        string name,
        string extension,
        FileType type)
    {
        Assert.Equal(id, link.ID);
        Assert.Equal(name, link.Name);
        Assert.Equal(extension, link.Extension);
        Assert.Equal(type, link.Type);
        Assert.True(link.Size > 0);
    }
}
