namespace NET_Thing_Encryptor.Tests;

public sealed class LegacyDataMigrationTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(
        Path.GetTempPath(),
        "NETThingEncryptor.MigrationTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void MigrateIfNeeded_CopiesCompleteTreeAndPreservesSource()
    {
        string source = CreateDirectory("portable-data");
        string target = Path.Combine(_testRoot, "local-app-data", "Data");
        WriteFile(source, "0.nte", "root");
        WriteFile(source, Path.Combine("nested", "0001.nte"), "payload");

        LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(target, [source]);

        Assert.Equal(LegacyDataMigrationStatus.Migrated, result.Status);
        Assert.True(AppPaths.PathEquals(source, result.SourceDirectory!));
        Assert.Equal("root", File.ReadAllText(Path.Combine(target, "0.nte")));
        Assert.Equal("payload", File.ReadAllText(Path.Combine(target, "nested", "0001.nte")));
        Assert.Equal("root", File.ReadAllText(Path.Combine(source, "0.nte")));
    }

    [Fact]
    public void MigrateIfNeeded_AcceptsAnEmptyTargetDirectory()
    {
        string source = CreateDirectory("portable-data");
        string target = CreateDirectory(Path.Combine("local-app-data", "Data"));
        WriteFile(source, "0.nte", "root");

        LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(target, [source]);

        Assert.Equal(LegacyDataMigrationStatus.Migrated, result.Status);
        Assert.Equal("root", File.ReadAllText(Path.Combine(target, "0.nte")));
    }

    [Fact]
    public void MigrateIfNeeded_RemovesAStaleIncompleteStagingDirectory()
    {
        string source = CreateDirectory("portable-data");
        string target = Path.Combine(_testRoot, "local-app-data", "Data");
        string stale = CreateDirectory(Path.Combine("local-app-data", ".Data.migration-stale"));
        WriteFile(source, "0.nte", "root");
        WriteFile(stale, "partial.nte", "incomplete");

        LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(target, [source]);

        Assert.Equal(LegacyDataMigrationStatus.Migrated, result.Status);
        Assert.False(Directory.Exists(stale));
        Assert.Equal("root", File.ReadAllText(Path.Combine(target, "0.nte")));
    }

    [Fact]
    public void MigrateIfNeeded_DoesNothingWhenTargetRootExists()
    {
        string source = CreateDirectory("portable-data");
        string target = CreateDirectory(Path.Combine("local-app-data", "Data"));
        WriteFile(source, "0.nte", "legacy");
        WriteFile(target, "0.nte", "current");

        LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(target, [source]);

        Assert.Equal(LegacyDataMigrationStatus.NotRequired, result.Status);
        Assert.Equal("current", File.ReadAllText(Path.Combine(target, "0.nte")));
    }

    [Fact]
    public void MigrateIfNeeded_ReportsConflictWithoutChangingEitherDirectory()
    {
        string source = CreateDirectory("portable-data");
        string target = CreateDirectory(Path.Combine("local-app-data", "Data"));
        WriteFile(source, "0.nte", "legacy");
        WriteFile(target, "existing.nte", "current");

        LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(target, [source]);

        Assert.Equal(LegacyDataMigrationStatus.Conflict, result.Status);
        Assert.True(AppPaths.PathEquals(source, result.SourceDirectory!));
        Assert.False(File.Exists(Path.Combine(target, "0.nte")));
        Assert.Equal("current", File.ReadAllText(Path.Combine(target, "existing.nte")));
        Assert.Equal("legacy", File.ReadAllText(Path.Combine(source, "0.nte")));
    }

    [Fact]
    public void MigrateIfNeeded_IgnoresCandidatesWithoutARootFile()
    {
        string source = CreateDirectory("portable-data");
        string target = Path.Combine(_testRoot, "local-app-data", "Data");
        WriteFile(source, "other.nte", "payload");

        LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(target, [source]);

        Assert.Equal(LegacyDataMigrationStatus.SourceNotFound, result.Status);
        Assert.False(Directory.Exists(target));
    }

    private string CreateDirectory(string relativePath)
    {
        string directory = Path.Combine(_testRoot, relativePath);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, recursive: true);
        }
        catch (IOException)
        {
            // A failed assertion should stay the primary failure if Windows retains a file briefly.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
