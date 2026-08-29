using System.IO.Compression;
using System.Reflection;
using System.Text;
using NET_Thing_Encryptor;

namespace Nte.Storage.Tests;

public sealed class VaultStorageTests : IDisposable
{
    private const string Password = "M2 transfer password";
    private static readonly PropertyInfo RootProperty = typeof(ThingData).GetProperty(
        nameof(ThingData.Root),
        BindingFlags.Public | BindingFlags.Static)!;
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        "Nte.Storage.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task StreamTransfer_AcceptsNonSeekableStreams_AndPreservesContent()
    {
        FileSystemVaultStorage storage = await CreateVaultAsync();
        ThingFolder folder = await GetOnlyRootFolderAsync();
        byte[] expected = Encoding.UTF8.GetBytes("content supplied through an Android-like stream");

        using var source = new NonSeekableReadStream(new MemoryStream(expected, writable: false));
        ThingFile imported = await ThingData.ImportFileAsync(
            source,
            "notes.txt",
            folder.ID,
            cancellationToken: TestContext.Current.CancellationToken);
        imported.ReleaseContent();

        using var exportedBytes = new MemoryStream();
        await using (var destination = new NonSeekableWriteStream(exportedBytes))
            await ThingData.ExportFileAsync(
                imported.ID,
                destination,
                TestContext.Current.CancellationToken);

        Assert.Equal(expected, exportedBytes.ToArray());
        Assert.True(storage.Exists(imported.ID));
    }

    [Fact]
    public async Task Archive_RoundTrip_PreservesEncryptedObjects_AndRemapsLocalPath()
    {
        FileSystemVaultStorage sourceStorage = await CreateVaultAsync(includeFile: true);
        var service = new VaultArchiveService();
        using var archiveBytes = new MemoryStream();

        VaultArchiveExportResult export = await service.ExportCurrentAsync(
            archiveBytes,
            TestContext.Current.CancellationToken);
        Dictionary<ulong, byte[]> originalObjects = await ReadRawObjectsAsync(sourceStorage);

        string destinationRoot = Path.Combine(_testDirectory, "destination", "root");
        string destinationObjects = Path.Combine(_testDirectory, "destination", "objects");
        var destinationStorage = new FileSystemVaultStorage(destinationRoot, destinationObjects);
        archiveBytes.Position = 0;
        using var nonSeekableArchive = new NonSeekableReadStream(archiveBytes);
        VaultArchiveImportResult import = await service.ImportAsync(
            nonSeekableArchive,
            destinationStorage,
            TestContext.Current.CancellationToken);

        Assert.Equal(export.ObjectCount, import.ObjectCount);
        Assert.Equal(originalObjects.Keys.Order(), destinationStorage.ListObjectIds().Order());
        foreach ((ulong id, byte[] expected) in originalObjects)
        {
            await using Stream importedObject = await destinationStorage.OpenReadAsync(
                id,
                TestContext.Current.CancellationToken);
            using var copy = new MemoryStream();
            await importedObject.CopyToAsync(copy, TestContext.Current.CancellationToken);
            Assert.Equal(expected, copy.ToArray());
        }

        ResetSession(destinationStorage);
        Assert.True(await ThingData.LoadMainData());
        Assert.Equal(Path.GetFullPath(destinationObjects), ThingData.Root!.SaveLocation);
        Assert.True(await ThingData.AttemptDecrypt(Password));
        ThingFolder folder = await GetOnlyRootFolderAsync();
        ThingObjectLink fileLink = Assert.Single(folder.Content);
        using var plaintext = new MemoryStream();
        await ThingData.ExportFileAsync(
            fileLink.ID,
            plaintext,
            TestContext.Current.CancellationToken);
        Assert.Equal("portable payload", Encoding.UTF8.GetString(plaintext.ToArray()));
    }

    [Fact]
    public async Task Archive_ExistingDestination_OverwritesNothing()
    {
        FileSystemVaultStorage sourceStorage = await CreateVaultAsync(includeFile: true);
        var service = new VaultArchiveService();
        using var archive = new MemoryStream();
        await service.ExportAsync(sourceStorage, archive, TestContext.Current.CancellationToken);

        var destination = new FileSystemVaultStorage(Path.Combine(_testDirectory, "conflict"));
        await destination.InitializeAsync(TestContext.Current.CancellationToken);
        byte[] sentinel = Encoding.UTF8.GetBytes("existing vault root");
        using (var root = new MemoryStream(sentinel, writable: false))
            await destination.WriteAtomicallyAsync(
                0,
                root,
                TestContext.Current.CancellationToken);

        archive.Position = 0;
        await Assert.ThrowsAsync<VaultStorageConflictException>(
            () => service.ImportAsync(
                archive,
                destination,
                TestContext.Current.CancellationToken));

        await using Stream unchanged = await destination.OpenReadAsync(
            0,
            TestContext.Current.CancellationToken);
        using var copy = new MemoryStream();
        await unchanged.CopyToAsync(copy, TestContext.Current.CancellationToken);
        Assert.Equal(sentinel, copy.ToArray());
        Assert.Empty(destination.ListObjectIds());
    }

    [Fact]
    public async Task Archive_TamperedPayload_IsRejectedBeforeWriting()
    {
        FileSystemVaultStorage sourceStorage = await CreateVaultAsync(includeFile: true);
        var service = new VaultArchiveService();
        using var validArchive = new MemoryStream();
        await service.ExportAsync(
            sourceStorage,
            validArchive,
            TestContext.Current.CancellationToken);
        using MemoryStream tamperedArchive = TamperFirstObject(validArchive.ToArray());

        var destination = new FileSystemVaultStorage(Path.Combine(_testDirectory, "tampered"));
        await Assert.ThrowsAsync<VaultArchiveException>(
            () => service.ImportAsync(
                tamperedArchive,
                destination,
                TestContext.Current.CancellationToken));
        Assert.False(destination.Exists(0));
        Assert.Empty(destination.ListObjectIds());
    }

    [Fact]
    public async Task Archive_WriteFailure_RestoresEmptyDestination()
    {
        FileSystemVaultStorage sourceStorage = await CreateVaultAsync(includeFile: true);
        var service = new VaultArchiveService();
        using var archive = new MemoryStream();
        await service.ExportAsync(sourceStorage, archive, TestContext.Current.CancellationToken);
        archive.Position = 0;

        var physicalDestination = new FileSystemVaultStorage(Path.Combine(_testDirectory, "rollback"));
        var faultingDestination = new FaultingVaultStorage(physicalDestination, failOnId: 0);
        await Assert.ThrowsAsync<IOException>(
            () => service.ImportAsync(
                archive,
                faultingDestination,
                TestContext.Current.CancellationToken));

        Assert.False(physicalDestination.Exists(0));
        Assert.Empty(physicalDestination.ListObjectIds());
    }

    [Fact]
    public async Task ResolveObjectLocation_UnavailableForeignPath_UsesLocalDefault()
    {
        string local = Path.Combine(_testDirectory, "local");
        var storage = new FileSystemVaultStorage(local);
        await storage.InitializeAsync(TestContext.Current.CancellationToken);

        string mapped = storage.ResolveObjectLocation(@"Z:\Missing\Vault\Objects");

        Assert.Equal(Path.GetFullPath(local), mapped);
    }

    private async Task<FileSystemVaultStorage> CreateVaultAsync(bool includeFile = false)
    {
        string rootDirectory = Path.Combine(_testDirectory, "source", "root");
        string objectDirectory = Path.Combine(_testDirectory, "source", "objects");
        var storage = new FileSystemVaultStorage(rootDirectory, objectDirectory);
        await storage.InitializeAsync();
        ResetSession(storage);

        var root = new ThingRoot
        {
            Salt = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray(),
            SaveLocation = objectDirectory,
            Content = []
        };
        SetRoot(root);
        Assert.True(await ThingData.AttemptDecrypt(Password));
        ThingFolder folder = new ThingFolder("Documents").AddToRoot();
        await ThingData.SaveFileAsync(folder);
        await ThingData.SaveRootAsync();

        if (includeFile)
        {
            using var payload = new NonSeekableReadStream(
                new MemoryStream(Encoding.UTF8.GetBytes("portable payload"), writable: false));
            ThingFile file = await ThingData.ImportFileAsync(payload, "portable.txt", folder.ID);
            file.ReleaseContent();
        }

        return storage;
    }

    private static async Task<ThingFolder> GetOnlyRootFolderAsync()
    {
        ThingObjectLink folderLink = Assert.Single(ThingData.Root!.Content!);
        return await ThingData.LoadFileAsync<ThingFolder>(folderLink.ID)
            ?? throw new InvalidDataException("Folder missing.");
    }

    private static async Task<Dictionary<ulong, byte[]>> ReadRawObjectsAsync(IVaultStorage storage)
    {
        var result = new Dictionary<ulong, byte[]>();
        foreach (ulong id in storage.ListObjectIds())
        {
            await using Stream input = await storage.OpenReadAsync(id);
            using var output = new MemoryStream();
            await input.CopyToAsync(output);
            result[id] = output.ToArray();
        }
        return result;
    }

    private static MemoryStream TamperFirstObject(byte[] archiveBytes)
    {
        var output = new MemoryStream();
        using (var sourceBytes = new MemoryStream(archiveBytes, writable: false))
        using (var source = new ZipArchive(sourceBytes, ZipArchiveMode.Read))
        using (var destination = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            bool tampered = false;
            foreach (ZipArchiveEntry entry in source.Entries)
            {
                ZipArchiveEntry copy = destination.CreateEntry(entry.FullName, CompressionLevel.NoCompression);
                using Stream input = entry.Open();
                using Stream target = copy.Open();
                if (!tampered && entry.FullName.StartsWith("objects/", StringComparison.Ordinal))
                {
                    using var bytes = new MemoryStream();
                    input.CopyTo(bytes);
                    byte[] payload = bytes.ToArray();
                    payload[^1] ^= 0x01;
                    target.Write(payload);
                    tampered = true;
                }
                else
                {
                    input.CopyTo(target);
                }
            }
            Assert.True(tampered);
        }
        output.Position = 0;
        return output;
    }

    private static void ResetSession(IVaultStorage storage)
    {
        ThingData.LockSession();
        SetRoot(null);
        ThingData.ConfigureStorage(storage);
    }

    private static void SetRoot(ThingRoot? root) => RootProperty.SetValue(null, root);

    public void Dispose()
    {
        ThingData.LockSession();
        SetRoot(null);
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }
        catch
        {
            // Test cleanup must not hide the assertion result.
        }
    }

    private sealed class NonSeekableReadStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class NonSeekableWriteStream(Stream inner) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default) => inner.WriteAsync(buffer, cancellationToken);
        protected override void Dispose(bool disposing) => base.Dispose(disposing);
    }

    private sealed class FaultingVaultStorage(IVaultStorage inner, ulong failOnId) : IVaultStorage
    {
        public string ObjectLocation => inner.ObjectLocation;
        public Task InitializeAsync(CancellationToken cancellationToken = default) =>
            inner.InitializeAsync(cancellationToken);
        public string ResolveObjectLocation(string? persistedLocation) =>
            inner.ResolveObjectLocation(persistedLocation);
        public string SetObjectLocation(string location) => inner.SetObjectLocation(location);
        public string GetLocation(ulong id) => inner.GetLocation(id);
        public bool Exists(ulong id) => inner.Exists(id);
        public IReadOnlyCollection<ulong> ListObjectIds() => inner.ListObjectIds();
        public ValueTask<Stream> OpenReadAsync(ulong id, CancellationToken cancellationToken = default) =>
            inner.OpenReadAsync(id, cancellationToken);
        public Task WriteAtomicallyAsync(
            ulong id,
            Stream content,
            CancellationToken cancellationToken = default) => id == failOnId
                ? Task.FromException(new IOException("Injected storage failure."))
                : inner.WriteAtomicallyAsync(id, content, cancellationToken);
        public Task DeleteAsync(ulong id, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(id, cancellationToken);
        public Task<IVaultStorageSnapshot> CreateSnapshotAsync(
            IReadOnlyCollection<ulong>? objectIds = null,
            CancellationToken cancellationToken = default) =>
            inner.CreateSnapshotAsync(objectIds, cancellationToken);
        public Task<string?> PreserveDamagedRootAsync(
            string suffix,
            CancellationToken cancellationToken = default) =>
            inner.PreserveDamagedRootAsync(suffix, cancellationToken);
    }
}
