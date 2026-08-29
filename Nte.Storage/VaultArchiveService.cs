using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NET_Thing_Encryptor;

public sealed record VaultArchiveExportResult(int ObjectCount);
public sealed record VaultArchiveImportResult(int ObjectCount);

public sealed class VaultArchiveException(string message, Exception? innerException = null)
    : IOException(message, innerException);

public sealed class VaultStorageConflictException(string message) : IOException(message);

/// <summary>
/// Transfers a complete vault as one .ntevault ZIP stream. Object payloads and the
/// encrypted root content remain in the NTE2 representation; only the persisted local
/// object locator is rewritten during import.
/// </summary>
public sealed class VaultArchiveService
{
    public const string FileExtension = ".ntevault";
    private const string ManifestPath = "manifest.json";
    private const string FormatName = "nte-vault-archive";
    private const int FormatVersion = 1;
    private const long MaximumRootLength = 16L * 1024 * 1024;
    private const int MaximumEntryCount = 100_001;

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Exports the configured ThingData vault while blocking concurrent core mutations.
    /// UI integrations should prefer this method over exporting the adapter directly.
    /// </summary>
    public Task<VaultArchiveExportResult> ExportCurrentAsync(
        Stream destination,
        CancellationToken cancellationToken = default) =>
        ThingData.RunStorageExclusiveAsync(
            storage => ExportAsync(storage, destination, cancellationToken),
            cancellationToken);

    public async Task<VaultArchiveExportResult> ExportAsync(
        IVaultStorage source,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException("The destination stream is not writable.", nameof(destination));

        await source.InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (!source.Exists(0))
            throw new FileNotFoundException("The vault root does not exist.", source.GetLocation(0));

        IReadOnlyCollection<ulong> ids = source.ListObjectIds();
        var manifestEntries = new List<VaultArchiveEntry>(ids.Count + 1);

        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            manifestEntries.Add(await AddStorageEntryAsync(
                archive,
                source,
                0,
                "root/0.nte",
                cancellationToken).ConfigureAwait(false));

            foreach (ulong id in ids.Order())
            {
                manifestEntries.Add(await AddStorageEntryAsync(
                    archive,
                    source,
                    id,
                    $"objects/{ThingData.IDToHex(id)}.nte",
                    cancellationToken).ConfigureAwait(false));
            }

            ZipArchiveEntry manifestEntry = archive.CreateEntry(ManifestPath, CompressionLevel.Optimal);
            await using Stream manifestStream = manifestEntry.Open();
            var manifest = new VaultArchiveManifest(
                FormatName,
                FormatVersion,
                DateTimeOffset.UtcNow,
                manifestEntries);
            await JsonSerializer.SerializeAsync(
                manifestStream,
                manifest,
                ManifestOptions,
                cancellationToken).ConfigureAwait(false);
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        return new VaultArchiveExportResult(ids.Count);
    }

    public async Task<VaultArchiveImportResult> ImportAsync(
        Stream source,
        IVaultStorage destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (!source.CanRead)
            throw new ArgumentException("The source stream is not readable.", nameof(source));

        await destination.InitializeAsync(cancellationToken).ConfigureAwait(false);
        EnsureEmpty(destination);

        string temporaryPath = Path.Combine(
            Path.GetTempPath(),
            $"nte-vault-import-{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var temporaryOutput = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(temporaryOutput, cancellationToken).ConfigureAwait(false);
            }

            await using var archiveInput = new FileStream(
                temporaryPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.RandomAccess);
            using var archive = new ZipArchive(archiveInput, ZipArchiveMode.Read, leaveOpen: false);

            (VaultArchiveManifest manifest, IReadOnlyDictionary<string, ZipArchiveEntry> entries) =
                await ValidateArchiveAsync(archive, cancellationToken).ConfigureAwait(false);
            ZipArchiveEntry rootEntry = entries["root/0.nte"];
            byte[] remappedRoot = await CreateRemappedRootAsync(
                rootEntry,
                destination.ObjectLocation,
                cancellationToken).ConfigureAwait(false);

            EnsureEmpty(destination);
            await using IVaultStorageSnapshot snapshot = await destination.CreateSnapshotAsync(
                null,
                cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (VaultArchiveEntry item in manifest.Entries
                             .Where(entry => entry.Path.StartsWith("objects/", StringComparison.Ordinal))
                             .OrderBy(entry => entry.Path, StringComparer.Ordinal))
                {
                    ulong id = ParseObjectId(item.Path);
                    await using Stream entryStream = entries[item.Path].Open();
                    await destination.WriteAtomicallyAsync(id, entryStream, cancellationToken)
                        .ConfigureAwait(false);
                }

                using var rootStream = new MemoryStream(remappedRoot, writable: false);
                await destination.WriteAtomicallyAsync(0, rootStream, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await snapshot.RestoreAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }

            return new VaultArchiveImportResult(manifest.Entries.Count - 1);
        }
        catch (InvalidDataException ex)
        {
            throw new VaultArchiveException("The vault archive is invalid or damaged.", ex);
        }
        catch (JsonException ex)
        {
            throw new VaultArchiveException("The vault archive manifest is invalid.", ex);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // A stale encrypted transfer file must not mask the import result.
            }
        }
    }

    private static void EnsureEmpty(IVaultStorage storage)
    {
        if (storage.Exists(0) || storage.ListObjectIds().Count != 0)
        {
            throw new VaultStorageConflictException(
                "The destination already contains a vault. No files were overwritten.");
        }
    }

    private static async Task<VaultArchiveEntry> AddStorageEntryAsync(
        ZipArchive archive,
        IVaultStorage storage,
        ulong id,
        string path,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        await using Stream input = await storage.OpenReadAsync(id, cancellationToken).ConfigureAwait(false);
        await using Stream output = entry.Open();
        (long length, string hash) = await CopyAndHashAsync(input, output, cancellationToken)
            .ConfigureAwait(false);
        return new VaultArchiveEntry(path, length, hash);
    }

    private static async Task<(VaultArchiveManifest Manifest, IReadOnlyDictionary<string, ZipArchiveEntry> Entries)>
        ValidateArchiveAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaximumEntryCount + 1)
            throw new VaultArchiveException("The vault archive has an invalid entry count.");

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) || !entries.TryAdd(entry.FullName, entry))
                throw new VaultArchiveException("The vault archive contains a directory or duplicate path.");
        }

        if (!entries.TryGetValue(ManifestPath, out ZipArchiveEntry? manifestEntry))
            throw new VaultArchiveException("The vault archive manifest is missing.");
        if (manifestEntry.Length > MaximumRootLength)
            throw new VaultArchiveException("The vault archive manifest is too large.");

        VaultArchiveManifest? manifest;
        await using (Stream manifestStream = manifestEntry.Open())
        {
            manifest = await JsonSerializer.DeserializeAsync<VaultArchiveManifest>(
                manifestStream,
                ManifestOptions,
                cancellationToken).ConfigureAwait(false);
        }

        if (manifest is null || manifest.Format != FormatName || manifest.Version != FormatVersion)
            throw new VaultArchiveException("The vault archive format or version is not supported.");
        if (manifest.Entries is null || manifest.Entries.Count == 0 ||
            manifest.Entries.Count > MaximumEntryCount)
            throw new VaultArchiveException("The vault archive manifest has an invalid entry count.");

        var expectedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (VaultArchiveEntry item in manifest.Entries)
        {
            if (item is null || string.IsNullOrEmpty(item.Path))
                throw new VaultArchiveException("The vault archive manifest contains an empty entry.");
            if (!expectedPaths.Add(item.Path))
                throw new VaultArchiveException("The vault archive manifest contains duplicate paths.");
            bool isRoot = item.Path == "root/0.nte";
            if (!isRoot && !IsObjectPath(item.Path))
                throw new VaultArchiveException($"The vault archive path is invalid: {item.Path}");
            if (item.Length < 0 || (isRoot && item.Length > MaximumRootLength))
                throw new VaultArchiveException($"The vault archive length is invalid: {item.Path}");
            if (!IsSha256(item.Sha256))
                throw new VaultArchiveException($"The vault archive hash is invalid: {item.Path}");
            if (!entries.TryGetValue(item.Path, out ZipArchiveEntry? archiveEntry) ||
                archiveEntry.Length != item.Length)
                throw new VaultArchiveException($"The vault archive entry is missing or truncated: {item.Path}");

            await using Stream input = archiveEntry.Open();
            string actualHash = Convert.ToHexString(
                await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false));
            if (!string.Equals(actualHash, item.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new VaultArchiveException($"The vault archive hash does not match: {item.Path}");
        }

        if (!expectedPaths.Contains("root/0.nte"))
            throw new VaultArchiveException("The vault archive root is missing.");
        var actualPayloadPaths = entries.Keys
            .Where(path => path != ManifestPath)
            .ToHashSet(StringComparer.Ordinal);
        if (!actualPayloadPaths.SetEquals(expectedPaths))
            throw new VaultArchiveException("The vault archive contains unlisted payloads.");

        return (manifest, entries);
    }

    private static async Task<byte[]> CreateRemappedRootAsync(
        ZipArchiveEntry rootEntry,
        string objectLocation,
        CancellationToken cancellationToken)
    {
        if (rootEntry.Length > MaximumRootLength)
            throw new VaultArchiveException("The vault root is too large.");

        byte[] rootBytes = GC.AllocateUninitializedArray<byte>(checked((int)rootEntry.Length));
        await using (Stream input = rootEntry.Open())
            await input.ReadExactlyAsync(rootBytes, cancellationToken).ConfigureAwait(false);

        try
        {
            ThingRoot metadata = JsonSerializer.Deserialize<ThingRoot>(rootBytes)
                ?? throw new JsonException("The vault root metadata is missing.");
            if (metadata.ID != 0 || metadata.Salt is not { Length: 32 })
                throw new JsonException("The vault root identity or salt is invalid.");
            if (!string.IsNullOrEmpty(metadata.ContentEncrypted))
                _ = Convert.FromBase64String(metadata.ContentEncrypted);

            JsonObject root = JsonNode.Parse(rootBytes)?.AsObject()
                ?? throw new JsonException("The vault root is not a JSON object.");
            string? saveLocationProperty = root
                .Select(property => property.Key)
                .FirstOrDefault(key => string.Equals(
                    key,
                    nameof(ThingRoot.SaveLocation),
                    StringComparison.OrdinalIgnoreCase));
            root[saveLocationProperty ?? nameof(ThingRoot.SaveLocation)] = objectLocation;
            return JsonSerializer.SerializeToUtf8Bytes(root, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            throw new VaultArchiveException("The vault root metadata is invalid.", ex);
        }
    }

    private static bool IsObjectPath(string path)
    {
        const string prefix = "objects/";
        const string suffix = ".nte";
        if (!path.StartsWith(prefix, StringComparison.Ordinal) ||
            !path.EndsWith(suffix, StringComparison.Ordinal) ||
            path.Length != prefix.Length + 16 + suffix.Length)
            return false;

        string hex = path.Substring(prefix.Length, 16);
        return ulong.TryParse(
                   hex,
                   System.Globalization.NumberStyles.HexNumber,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out ulong id) && id != 0;
    }

    private static ulong ParseObjectId(string path) => ThingData.HexToID(path.Substring(8, 16));

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static async Task<(long Length, string Hash)> CopyAndHashAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[81920];
        long length = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            hash.AppendData(buffer, 0, read);
            length = checked(length + read);
        }
        return (length, Convert.ToHexString(hash.GetHashAndReset()));
    }

    private sealed record VaultArchiveManifest(
        string Format,
        int Version,
        DateTimeOffset CreatedUtc,
        List<VaultArchiveEntry> Entries);

    private sealed record VaultArchiveEntry(string Path, long Length, string Sha256);
}
