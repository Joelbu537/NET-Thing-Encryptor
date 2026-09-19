using System.Globalization;
using NET_Thing_Encryptor;

namespace Nte.RemoteServer;

internal sealed record RemoteVaultMetadata(
    int ApiVersion,
    bool RootExists,
    string[] ObjectIds,
    string Revision);

internal sealed record RemoteMutationResult(bool RevisionMatched, string Revision);

internal sealed class RemoteVaultRepository
{
    private readonly FileSystemVaultStorage _storage;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);
    private string _revision = Guid.NewGuid().ToString("N");

    public RemoteVaultRepository(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        _storage = new FileSystemVaultStorage(dataDirectory, dataDirectory);
    }

    public Task InitializeAsync(CancellationToken cancellationToken) =>
        _storage.InitializeAsync(cancellationToken);

    public async Task<RemoteVaultMetadata> GetMetadataAsync(CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return new RemoteVaultMetadata(
                RemoteVaultStorage.SupportedApiVersion,
                _storage.Exists(0),
                _storage.ListObjectIds().Select(ThingData.IDToHex).ToArray(),
                _revision);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public bool Exists(ulong id) => _storage.Exists(id);

    public ValueTask<Stream> OpenReadAsync(ulong id, CancellationToken cancellationToken) =>
        _storage.OpenReadAsync(id, cancellationToken);

    public async Task<RemoteMutationResult> WriteAsync(
        ulong id,
        Stream content,
        string? expectedRevision,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!RevisionMatches(expectedRevision))
                return new RemoteMutationResult(false, _revision);
            await _storage.WriteAtomicallyAsync(id, content, cancellationToken).ConfigureAwait(false);
            _revision = Guid.NewGuid().ToString("N");
            return new RemoteMutationResult(true, _revision);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<RemoteMutationResult> DeleteAsync(
        ulong id,
        string? expectedRevision,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!RevisionMatches(expectedRevision))
                return new RemoteMutationResult(false, _revision);
            await _storage.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            _revision = Guid.NewGuid().ToString("N");
            return new RemoteMutationResult(true, _revision);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public Task<string?> PreserveDamagedRootAsync(
        string suffix,
        CancellationToken cancellationToken) =>
        _storage.PreserveDamagedRootAsync(suffix, cancellationToken);

    public static bool TryParseId(string value, out ulong id)
    {
        id = 0;
        return value.Length == 16 && ulong.TryParse(
            value,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out id);
    }

    private bool RevisionMatches(string? expectedRevision) =>
        string.Equals(
            expectedRevision?.Trim().Trim('"'),
            _revision,
            StringComparison.Ordinal);
}
