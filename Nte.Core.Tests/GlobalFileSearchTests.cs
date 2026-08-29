using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class GlobalFileSearchTests
{
    [Fact]
    public async Task Search_FindsFilesAcrossFoldersWithCombinedFilters()
    {
        await using TestEnvironment environment = await TestEnvironment.CreateAsync();
        ThingFolder documents = environment.CreateRootFolder("Documents");
        await ThingData.SaveFileAsync(documents);

        ThingFolder archive = new("Archive") { ParentID = documents.ID };
        documents.Content.Add(new ThingObjectLink(archive.ID, archive.Name, FileType.folder, 0));
        await ThingData.SaveFileAsync(archive);
        await ThingData.SaveFileAsync(documents);

        ThingFile report = environment.CreateFile(
            "Annual Report",
            ".PDF",
            new byte[2 * 1024 * 1024],
            archive.ID);
        await ThingData.SaveFileAsync(report);
        archive.Content.Add(new ThingObjectLink(
            report.ID,
            report.Name,
            report.Type,
            report.Content!.LongLength,
            extension: report.Extension)
        {
            CreatedAt = new DateOnly(2025, 6, 15)
        });
        await ThingData.SaveFileAsync(archive);
        report.ReleaseContent();

        IReadOnlyList<GlobalFileSearchResult> results = await GlobalFileSearch.SearchAsync(new(
            Name: "report",
            Type: FileType.other,
            Extension: "pdf",
            MinimumSize: 1024 * 1024,
            MaximumSize: 3 * 1024 * 1024,
            CreatedFrom: new DateOnly(2025, 1, 1),
            CreatedTo: new DateOnly(2025, 12, 31)),
            TestContext.Current.CancellationToken);

        GlobalFileSearchResult result = Assert.Single(results);
        Assert.Equal(report.ID, result.ID);
        Assert.Equal("PDF", result.Extension);
        Assert.Equal("Root/Documents/Archive", result.FolderPath);
    }

    [Fact]
    public async Task Search_LoadsLegacyFileMetadataWhenExtensionIsMissingFromLink()
    {
        await using TestEnvironment environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("Legacy");
        await ThingData.SaveFileAsync(folder);
        ThingFile file = environment.CreateFile("manual", ".txt", [1, 2, 3], folder.ID);
        await ThingData.SaveFileAsync(file);
        folder.Content.Add(new ThingObjectLink(file.ID, file.Name, file.Type, 3));
        await ThingData.SaveFileAsync(folder);
        file.ReleaseContent();

        IReadOnlyList<GlobalFileSearchResult> results =
            await GlobalFileSearch.SearchAsync(
                new(Extension: ".TXT"),
                TestContext.Current.CancellationToken);

        GlobalFileSearchResult result = Assert.Single(results);
        Assert.Equal(file.ID, result.ID);
        Assert.Equal("txt", result.Extension);
    }

    [Fact]
    public async Task Search_WithoutFiltersReturnsFilesButNotFolders()
    {
        await using TestEnvironment environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("Media");
        await ThingData.SaveFileAsync(folder);
        ThingFile file = environment.CreateFile("song", "mp3", [1], folder.ID);
        await ThingData.SaveFileAsync(file);
        folder.Content.Add(new ThingObjectLink(
            file.ID, file.Name, file.Type, 1, extension: file.Extension));
        await ThingData.SaveFileAsync(folder);

        IReadOnlyList<GlobalFileSearchResult> results =
            await GlobalFileSearch.SearchAsync(new(), TestContext.Current.CancellationToken);

        Assert.Single(results);
        Assert.All(results, result => Assert.NotEqual(FileType.folder, result.Type));
    }
}
