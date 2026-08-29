using System.Diagnostics;

namespace NET_Thing_Encryptor;

internal enum LegacyDataMigrationStatus
{
    NotRequired,
    SourceNotFound,
    Migrated,
    Conflict
}

internal readonly record struct LegacyDataMigrationResult(
    LegacyDataMigrationStatus Status,
    string? SourceDirectory = null);

internal sealed class LegacyDataMigrationConflictException(
    string sourceDirectory,
    string targetDirectory)
    : IOException(
        $"Legacy data was found at '{sourceDirectory}', but the target directory " +
        $"'{targetDirectory}' already contains files. No data was changed. Resolve the " +
        "conflict manually before starting the application again.")
{
    internal string SourceDirectory { get; } = sourceDirectory;
    internal string TargetDirectory { get; } = targetDirectory;
}

internal static class LegacyDataMigrator
{
    private const string RootFileName = "0.nte";

    internal static LegacyDataMigrationResult MigrateIfNeeded(
        string targetDataDirectory,
        IEnumerable<string> legacyDataDirectories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDataDirectory);
        ArgumentNullException.ThrowIfNull(legacyDataDirectories);

        string targetDirectory = Path.GetFullPath(targetDataDirectory);
        string targetRootPath = Path.Combine(targetDirectory, RootFileName);
        if (File.Exists(targetRootPath))
            return new(LegacyDataMigrationStatus.NotRequired);

        foreach (string candidate in legacyDataDirectories)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            string sourceDirectory = Path.GetFullPath(candidate);
            if (AppPaths.PathEquals(sourceDirectory, targetDirectory))
                continue;

            string sourceRootPath = Path.Combine(sourceDirectory, RootFileName);
            if (!File.Exists(sourceRootPath))
                continue;

            if (Directory.Exists(targetDirectory) &&
                Directory.EnumerateFileSystemEntries(targetDirectory).Any())
            {
                return new(LegacyDataMigrationStatus.Conflict, sourceDirectory);
            }

            CopyAtomically(sourceDirectory, targetDirectory);
            Debug.WriteLine($"Migrated legacy data from {sourceDirectory} to {targetDirectory}.");
            return new(LegacyDataMigrationStatus.Migrated, sourceDirectory);
        }

        return new(LegacyDataMigrationStatus.SourceNotFound);
    }

    private static void CopyAtomically(string sourceDirectory, string targetDirectory)
    {
        string? targetParent = Path.GetDirectoryName(targetDirectory);
        if (string.IsNullOrWhiteSpace(targetParent))
            throw new InvalidOperationException("The target data directory must have a parent directory.");

        Directory.CreateDirectory(targetParent);
        string stagingPrefix = $".{Path.GetFileName(targetDirectory)}.migration-";
        foreach (string staleDirectory in Directory.EnumerateDirectories(
                     targetParent,
                     $"{stagingPrefix}*",
                     SearchOption.TopDirectoryOnly))
        {
            TryDeleteDirectory(staleDirectory);
        }

        string stagingDirectory = Path.Combine(
            targetParent,
            $"{stagingPrefix}{Guid.NewGuid():N}");

        try
        {
            CopyDirectory(sourceDirectory, stagingDirectory);

            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, recursive: false);

            Directory.Move(stagingDirectory, targetDirectory);
        }
        catch
        {
            TryDeleteDirectory(stagingDirectory);
            throw;
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            string destinationFile = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile, overwrite: false);
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Unable to remove migration staging directory {directory}: {ex.Message}");
        }
    }
}
