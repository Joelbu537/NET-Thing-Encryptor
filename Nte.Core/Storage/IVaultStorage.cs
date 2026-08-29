namespace NET_Thing_Encryptor;

/// <summary>
/// Provides persistence for the encrypted root document and encrypted vault objects.
/// Implementations may use ordinary files, an Android document provider, or another
/// platform-specific storage mechanism.
/// </summary>
public interface IVaultStorage
{
    /// <summary>A stable, user-facing locator for the current object store.</summary>
    string ObjectLocation { get; }

    /// <summary>Prepares the backing store and performs implementation-specific migration.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a locator persisted by another platform or installation to a usable local
    /// object store. The returned locator is safe to persist in the root document.
    /// </summary>
    string ResolveObjectLocation(string? persistedLocation);

    /// <summary>Applies an explicit, user-selected local object-store locator.</summary>
    string SetObjectLocation(string location);

    /// <summary>Returns a diagnostic locator. It is not required to be a file-system path.</summary>
    string GetLocation(ulong id);

    bool Exists(ulong id);

    IReadOnlyCollection<ulong> ListObjectIds();

    ValueTask<Stream> OpenReadAsync(
        ulong id,
        CancellationToken cancellationToken = default);

    Task WriteAtomicallyAsync(
        ulong id,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ulong id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures the root plus either the selected object IDs or the complete object store.
    /// A null selection means all objects; an empty selection means only the root.
    /// </summary>
    Task<IVaultStorageSnapshot> CreateSnapshotAsync(
        IReadOnlyCollection<ulong>? objectIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Preserves the unreadable root and returns a user-facing locator.</summary>
    Task<string?> PreserveDamagedRootAsync(
        string suffix,
        CancellationToken cancellationToken = default);
}

public interface IVaultStorageSnapshot : IAsyncDisposable
{
    Task RestoreAsync(CancellationToken cancellationToken = default);
}
