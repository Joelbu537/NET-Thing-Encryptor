namespace NET_Thing_Encryptor;

/// <summary>
/// Keeps every encrypted vault object below a platform-provided private application
/// directory. Persisted locators from another installation are deliberately remapped
/// to the local sandbox.
/// </summary>
public sealed class AppSandboxVaultStorage : IVaultStorage
{
    private readonly FileSystemVaultStorage _inner;

    public AppSandboxVaultStorage(string appFilesDirectory, string vaultDirectoryName = "vault")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appFilesDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultDirectoryName);
        if (!Path.IsPathFullyQualified(appFilesDirectory))
            throw new ArgumentException("The application files directory must be absolute.", nameof(appFilesDirectory));
        if (vaultDirectoryName is "." or ".." ||
            Path.IsPathFullyQualified(vaultDirectoryName) ||
            vaultDirectoryName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0 ||
            vaultDirectoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("The vault directory name must be a single relative segment.", nameof(vaultDirectoryName));
        }

        AppFilesDirectory = Path.GetFullPath(appFilesDirectory);
        VaultDirectory = Path.GetFullPath(Path.Combine(AppFilesDirectory, vaultDirectoryName));
        _inner = new FileSystemVaultStorage(VaultDirectory, VaultDirectory);
    }

    public string AppFilesDirectory { get; }
    public string VaultDirectory { get; }
    public string ObjectLocation => _inner.ObjectLocation;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _inner.InitializeAsync(cancellationToken);

    public string ResolveObjectLocation(string? persistedLocation)
    {
        _ = persistedLocation;
        return _inner.ResolveObjectLocation(VaultDirectory);
    }

    public string SetObjectLocation(string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        string candidate = Path.GetFullPath(location);
        if (!AppPaths.PathEquals(candidate, VaultDirectory))
        {
            throw new InvalidOperationException(
                "A private application vault cannot be moved outside its sandbox.");
        }

        return _inner.SetObjectLocation(VaultDirectory);
    }

    public string GetLocation(ulong id) => _inner.GetLocation(id);
    public bool Exists(ulong id) => _inner.Exists(id);
    public IReadOnlyCollection<ulong> ListObjectIds() => _inner.ListObjectIds();

    public ValueTask<Stream> OpenReadAsync(
        ulong id,
        CancellationToken cancellationToken = default) =>
        _inner.OpenReadAsync(id, cancellationToken);

    public Task WriteAtomicallyAsync(
        ulong id,
        Stream content,
        CancellationToken cancellationToken = default) =>
        _inner.WriteAtomicallyAsync(id, content, cancellationToken);

    public Task DeleteAsync(ulong id, CancellationToken cancellationToken = default) =>
        _inner.DeleteAsync(id, cancellationToken);

    public Task<IVaultStorageSnapshot> CreateSnapshotAsync(
        IReadOnlyCollection<ulong>? objectIds = null,
        CancellationToken cancellationToken = default) =>
        _inner.CreateSnapshotAsync(objectIds, cancellationToken);

    public Task<string?> PreserveDamagedRootAsync(
        string suffix,
        CancellationToken cancellationToken = default) =>
        _inner.PreserveDamagedRootAsync(suffix, cancellationToken);
}
