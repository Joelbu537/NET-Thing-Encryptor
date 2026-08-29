using System.Collections.Concurrent;

namespace NET_Thing_Encryptor;

/// <summary>Stores a vault in ordinary platform file-system directories.</summary>
public sealed class FileSystemVaultStorage : IVaultStorage
{
    private readonly string _rootDirectory;
    private readonly string _defaultObjectDirectory;
    private readonly IReadOnlyCollection<string> _legacyDirectories;
    private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _locks = new();
    private string _objectDirectory;

    public FileSystemVaultStorage(
        string rootDirectory,
        string? objectDirectory = null,
        IEnumerable<string>? legacyDirectories = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _defaultObjectDirectory = Path.GetFullPath(objectDirectory ?? rootDirectory);
        _objectDirectory = _defaultObjectDirectory;
        _legacyDirectories = legacyDirectories?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(AppPaths.FileSystemPathComparer)
            .ToArray() ?? [];
    }

    public string RootDirectory => _rootDirectory;
    public string ObjectLocation => _objectDirectory;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_legacyDirectories.Count != 0)
        {
            LegacyDataMigrationResult result = LegacyDataMigrator.MigrateIfNeeded(
                _rootDirectory,
                _legacyDirectories);
            if (result.Status == LegacyDataMigrationStatus.Conflict)
            {
                throw new LegacyDataMigrationConflictException(
                    result.SourceDirectory!,
                    _rootDirectory);
            }
        }

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(_objectDirectory);
        return Task.CompletedTask;
    }

    public string ResolveObjectLocation(string? persistedLocation)
    {
        string resolved = _defaultObjectDirectory;
        if (!string.IsNullOrWhiteSpace(persistedLocation))
        {
            string? candidate = TryGetAbsolutePath(persistedLocation);
            if (candidate is not null &&
                (AppPaths.PathEquals(candidate, _defaultObjectDirectory) ||
                 ContainsVaultObjects(candidate)))
            {
                resolved = _legacyDirectories.Any(path => AppPaths.PathEquals(path, candidate))
                    ? _defaultObjectDirectory
                    : candidate;
            }
        }

        Directory.CreateDirectory(resolved);
        _objectDirectory = resolved;
        return resolved;
    }

    public string SetObjectLocation(string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        string resolved = Path.GetFullPath(location);
        Directory.CreateDirectory(resolved);
        _objectDirectory = resolved;
        return resolved;
    }

    public string GetLocation(ulong id) => id == 0
        ? Path.Combine(_rootDirectory, "0.nte")
        : Path.Combine(_objectDirectory, ThingData.IDToHex(id) + ".nte");

    public bool Exists(ulong id) => File.Exists(GetLocation(id));

    public IReadOnlyCollection<ulong> ListObjectIds()
    {
        if (!Directory.Exists(_objectDirectory))
            return [];

        var result = new List<ulong>();
        foreach (string file in Directory.EnumerateFiles(
                     _objectDirectory,
                     "*.nte",
                     SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.Length == 16 &&
                ulong.TryParse(
                    name,
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out ulong id) &&
                id != 0)
            {
                result.Add(id);
            }
        }

        result.Sort();
        return result;
    }

    public ValueTask<Stream> OpenReadAsync(
        ulong id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = GetLocation(id);
        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return ValueTask.FromResult(stream);
    }

    public async Task WriteAtomicallyAsync(
        ulong id,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        string destinationPath = GetLocation(id);
        string directory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("The storage location has no parent directory.");
        Directory.CreateDirectory(directory);

        SemaphoreSlim fileLock = _locks.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            fileLock.Release();
        }
    }

    public async Task DeleteAsync(ulong id, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim fileLock = _locks.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string path = GetLocation(id);
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path))
                throw new IOException($"The encrypted file could not be deleted: {path}");
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task<IVaultStorageSnapshot> CreateSnapshotAsync(
        IReadOnlyCollection<ulong>? objectIds = null,
        CancellationToken cancellationToken = default)
    {
        string backupDirectory = Path.Combine(
            Path.GetTempPath(),
            "NET Thing Encryptor",
            "StorageSnapshots",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(backupDirectory);

        try
        {
            IReadOnlyCollection<ulong> ids = objectIds is null
                ? ListObjectIds()
                : objectIds.Where(id => id != 0).Distinct().ToArray();
            var entries = new List<FileSystemSnapshot.Entry>(ids.Count + 1);
            await CaptureAsync(0, backupDirectory, entries, cancellationToken).ConfigureAwait(false);
            foreach (ulong id in ids)
                await CaptureAsync(id, backupDirectory, entries, cancellationToken).ConfigureAwait(false);

            return new FileSystemSnapshot(this, backupDirectory, entries, objectIds is null);
        }
        catch
        {
            TryDeleteDirectory(backupDirectory);
            throw;
        }
    }

    public async Task<string?> PreserveDamagedRootAsync(
        string suffix,
        CancellationToken cancellationToken = default)
    {
        if (!Exists(0))
            return null;

        string safeSuffix = string.Concat(suffix.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        string destination = Path.Combine(_rootDirectory, $"0_{safeSuffix}.nte");
        await using Stream source = await OpenReadAsync(0, cancellationToken).ConfigureAwait(false);
        await using var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await source.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        output.Flush(flushToDisk: true);
        return destination;
    }

    private static string? TryGetAbsolutePath(string value)
    {
        try
        {
            return Path.IsPathFullyQualified(value) ? Path.GetFullPath(value) : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool ContainsVaultObjects(string directory)
    {
        return Directory.Exists(directory) &&
               Directory.EnumerateFiles(directory, "*.nte", SearchOption.TopDirectoryOnly)
                   .Select(Path.GetFileNameWithoutExtension)
                   .Any(name => name is { Length: 16 } &&
                       ulong.TryParse(
                           name,
                           System.Globalization.NumberStyles.HexNumber,
                           System.Globalization.CultureInfo.InvariantCulture,
                           out ulong id) &&
                       id != 0);
    }

    private async Task CaptureAsync(
        ulong id,
        string backupDirectory,
        ICollection<FileSystemSnapshot.Entry> entries,
        CancellationToken cancellationToken)
    {
        string source = GetLocation(id);
        bool existed = File.Exists(source);
        string? backupPath = null;
        if (existed)
        {
            backupPath = Path.Combine(backupDirectory, id == 0 ? "root.nte" : $"{ThingData.IDToHex(id)}.nte");
            await CopyFileAsync(source, backupPath, cancellationToken).ConfigureAwait(false);
        }
        entries.Add(new FileSystemSnapshot.Entry(id, existed, backupPath));
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        destination.Flush(flushToDisk: true);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // A stale snapshot is preferable to masking the original storage result.
        }
    }

    private sealed class FileSystemSnapshot(
        FileSystemVaultStorage storage,
        string backupDirectory,
        IReadOnlyCollection<FileSystemSnapshot.Entry> entries,
        bool includesAllObjects) : IVaultStorageSnapshot
    {
        private bool _disposed;

        public sealed record Entry(ulong Id, bool Existed, string? BackupPath);

        public async Task RestoreAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (includesAllObjects)
            {
                foreach (ulong id in storage.ListObjectIds())
                    await storage.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            }

            foreach (Entry entry in entries)
            {
                if (!entry.Existed)
                {
                    await storage.DeleteAsync(entry.Id, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await using var input = new FileStream(
                    entry.BackupPath!,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await storage.WriteAtomicallyAsync(entry.Id, input, cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                TryDeleteDirectory(backupDirectory);
            }
            return ValueTask.CompletedTask;
        }
    }
}
