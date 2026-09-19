using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace NET_Thing_Encryptor;

public class RemoteVaultException(string message, Exception? innerException = null)
    : IOException(message, innerException);

public sealed class RemoteVaultAuthenticationException(string message)
    : RemoteVaultException(message);

public sealed class RemoteVaultConflictException(string message)
    : RemoteVaultException(message);

/// <summary>
/// Stores the already encrypted NTE root and object payloads on an NTE remote server.
/// The server access password is used only for HTTP authentication; vault encryption
/// and decryption continue to happen in <see cref="ThingData"/> on the client.
/// </summary>
public sealed class RemoteVaultStorage : IVaultStorage, IDisposable
{
    public const int SupportedApiVersion = 1;
    private const string ApiRoot = "api/v1/vault/";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly AuthenticationHeaderValue _authorization;
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly HashSet<ulong> _objectIds = [];
    private bool _rootExists;
    private bool _initialized;
    private bool _disposed;
    private string? _revision;

    public RemoteVaultStorage(
        string endpoint,
        string accessPassword,
        HttpClient? httpClient = null)
    {
        Endpoint = NormalizeEndpoint(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessPassword);
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100)
        };
        _ownsHttpClient = httpClient is null;
        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"nte:{accessPassword}"));
        _authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public Uri Endpoint { get; }
    public string ObjectLocation => Endpoint.AbsoluteUri.TrimEnd('/');

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            using HttpResponseMessage response = await SendAsync(
                HttpMethod.Get,
                ApiRoot,
                cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            RemoteVaultMetadata metadata = await response.Content.ReadFromJsonAsync<RemoteVaultMetadata>(
                cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? throw new RemoteVaultException("The remote server returned no vault metadata.");
            if (metadata.ApiVersion != SupportedApiVersion)
            {
                throw new RemoteVaultException(
                    $"The remote server uses unsupported API version {metadata.ApiVersion}.");
            }

            var ids = new HashSet<ulong>();
            foreach (string objectId in metadata.ObjectIds ?? [])
            {
                if (!TryParseId(objectId, out ulong id) || id == 0)
                    throw new RemoteVaultException("The remote server returned an invalid object ID.");
                ids.Add(id);
            }

            lock (_stateLock)
            {
                _objectIds.Clear();
                _objectIds.UnionWith(ids);
                _rootExists = metadata.RootExists;
                _revision = metadata.Revision;
                _initialized = true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RemoteVaultException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw CreateConnectionException(ex);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public string ResolveObjectLocation(string? persistedLocation) => ObjectLocation;

    public string SetObjectLocation(string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        Uri requested = NormalizeEndpoint(location);
        if (requested != Endpoint)
        {
            throw new InvalidOperationException(
                "A remote storage endpoint cannot be changed through the local object-location setting.");
        }
        return ObjectLocation;
    }

    public string GetLocation(ulong id) =>
        new Uri(Endpoint, $"{ApiRoot}objects/{ThingData.IDToHex(id)}").AbsoluteUri;

    public bool Exists(ulong id)
    {
        EnsureInitialized();
        lock (_stateLock)
            return id == 0 ? _rootExists : _objectIds.Contains(id);
    }

    public IReadOnlyCollection<ulong> ListObjectIds()
    {
        EnsureInitialized();
        lock (_stateLock)
            return _objectIds.Order().ToArray();
    }

    public async ValueTask<Stream> OpenReadAsync(
        ulong id,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        HttpResponseMessage response = await SendAsync(
            HttpMethod.Get,
            $"{ApiRoot}objects/{ThingData.IDToHex(id)}",
            cancellationToken,
            HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        try
        {
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            return new ResponseOwnedStream(stream, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public async Task WriteAtomicallyAsync(
        ulong id,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        EnsureInitialized();
        using var requestContent = new BorrowedStreamContent(content);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using HttpResponseMessage response = await SendMutationAsync(
            HttpMethod.Put,
            $"{ApiRoot}objects/{ThingData.IDToHex(id)}",
            requestContent,
            cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        UpdateRevision(response);
        lock (_stateLock)
        {
            if (id == 0)
                _rootExists = true;
            else
                _objectIds.Add(id);
        }
    }

    public async Task DeleteAsync(ulong id, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        using HttpResponseMessage response = await SendMutationAsync(
            HttpMethod.Delete,
            $"{ApiRoot}objects/{ThingData.IDToHex(id)}",
            content: null,
            cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        UpdateRevision(response);
        lock (_stateLock)
        {
            if (id == 0)
                _rootExists = false;
            else
                _objectIds.Remove(id);
        }
    }

    public async Task<IVaultStorageSnapshot> CreateSnapshotAsync(
        IReadOnlyCollection<ulong>? objectIds = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        string backupDirectory = Path.Combine(
            Path.GetTempPath(),
            "NET Thing Encryptor",
            "RemoteStorageSnapshots",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(backupDirectory);

        try
        {
            IReadOnlyCollection<ulong> ids = objectIds is null
                ? ListObjectIds()
                : objectIds.Where(id => id != 0).Distinct().ToArray();
            var entries = new List<RemoteSnapshot.Entry>(ids.Count + 1);
            await CaptureAsync(0, backupDirectory, entries, cancellationToken).ConfigureAwait(false);
            foreach (ulong id in ids)
                await CaptureAsync(id, backupDirectory, entries, cancellationToken).ConfigureAwait(false);
            return new RemoteSnapshot(this, backupDirectory, entries, objectIds is null);
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
        EnsureInitialized();
        if (!Exists(0))
            return null;

        using var content = JsonContent.Create(new PreserveRootRequest(suffix));
        using HttpResponseMessage response = await SendAsync(
            HttpMethod.Post,
            $"{ApiRoot}preserve-root",
            cancellationToken,
            content: content).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        PreserveRootResponse result = await response.Content.ReadFromJsonAsync<PreserveRootResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new RemoteVaultException("The remote server returned no backup location.");
        return result.Location;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _initializationLock.Dispose();
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private async Task CaptureAsync(
        ulong id,
        string backupDirectory,
        ICollection<RemoteSnapshot.Entry> entries,
        CancellationToken cancellationToken)
    {
        bool existed = Exists(id);
        string? backupPath = null;
        if (existed)
        {
            backupPath = Path.Combine(
                backupDirectory,
                id == 0 ? "root.nte" : $"{ThingData.IDToHex(id)}.nte");
            await using Stream source = await OpenReadAsync(id, cancellationToken).ConfigureAwait(false);
            await using var destination = new FileStream(
                backupPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }
        entries.Add(new RemoteSnapshot.Entry(id, existed, backupPath));
    }

    private async Task<HttpResponseMessage> SendMutationAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        string? revision;
        lock (_stateLock)
            revision = _revision;
        using var request = CreateRequest(method, relativePath, content);
        if (!string.IsNullOrWhiteSpace(revision))
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{revision}\"");
        return await SendCoreAsync(request, cancellationToken, HttpCompletionOption.ResponseContentRead)
            .ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        CancellationToken cancellationToken,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        HttpContent? content = null)
    {
        using var request = CreateRequest(method, relativePath, content);
        return await SendCoreAsync(request, cancellationToken, completionOption).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string relativePath,
        HttpContent? content)
    {
        var request = new HttpRequestMessage(method, new Uri(Endpoint, relativePath))
        {
            Content = content
        };
        request.Headers.Authorization = _authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        HttpCompletionOption completionOption)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return await _httpClient.SendAsync(request, completionOption, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw CreateConnectionException(ex);
        }
    }

    private RemoteVaultException CreateConnectionException(Exception exception)
    {
        string message = exception switch
        {
            HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError } =>
                $"Der Servername von {Endpoint} konnte nicht aufgelöst werden.",
            HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError } =>
                $"Unter {Endpoint} ist kein Dienst erreichbar. Prüfe Reverse-Proxy, Port und Firewall.",
            HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError } =>
                $"Die sichere HTTPS-Verbindung zu {Endpoint} konnte nicht hergestellt werden. " +
                "Prüfe, ob das TLS-Zertifikat für genau diesen Servernamen beziehungsweise diese IP-Adresse gültig ist.",
            TaskCanceledException =>
                $"Die Verbindung zu {Endpoint} hat das Zeitlimit überschritten.",
            _ => $"Der Remote-Tresor unter {Endpoint} konnte nicht erreicht werden."
        };
        return new RemoteVaultException(message, exception);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string suffix = string.IsNullOrWhiteSpace(detail)
            ? string.Empty
            : $" Server response: {detail.Trim()}";
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                throw new RemoteVaultAuthenticationException(
                    "The remote server rejected the access password.");
            case HttpStatusCode.NotFound:
                throw new FileNotFoundException("The encrypted remote object does not exist.");
            case HttpStatusCode.Conflict:
            case HttpStatusCode.PreconditionFailed:
                throw new RemoteVaultConflictException(
                    "The remote vault was changed by another device. Reconnect before trying again." + suffix);
            default:
                throw new RemoteVaultException(
                    $"The remote server returned HTTP {(int)response.StatusCode}.{suffix}");
        }
    }

    private void UpdateRevision(HttpResponseMessage response)
    {
        string? revision = response.Headers.ETag?.Tag.Trim('"');
        if (string.IsNullOrWhiteSpace(revision))
            return;
        lock (_stateLock)
            _revision = revision;
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_initialized)
            throw new InvalidOperationException("Initialize the remote vault storage before using it.");
    }

    private static Uri NormalizeEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        string value = endpoint.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
            value = $"{Uri.UriSchemeHttps}://{value}";

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException(
                "Gib einen Servernamen oder eine IP-Adresse ein, zum Beispiel vault.example.net.",
                nameof(endpoint));
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
        {
            throw new ArgumentException(
                "Unverschlüsseltes HTTP ist nur für lokale Tests auf diesem Gerät erlaubt. " +
                "Remote-Verbindungen müssen HTTPS verwenden.",
                nameof(endpoint));
        }

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/') + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }

    private static bool TryParseId(string value, out ulong id)
    {
        id = 0;
        return value.Length == 16 && ulong.TryParse(
            value,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out id);
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
            // A stale encrypted snapshot is preferable to hiding the storage result.
        }
    }

    private sealed record RemoteVaultMetadata(
        int ApiVersion,
        bool RootExists,
        string[]? ObjectIds,
        string? Revision);

    private sealed record PreserveRootRequest(string Suffix);
    private sealed record PreserveRootResponse(string Location);

    private sealed class BorrowedStreamContent(Stream source) : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) => source.CopyToAsync(stream);

        protected override bool TryComputeLength(out long length)
        {
            if (source.CanSeek)
            {
                length = source.Length - source.Position;
                return true;
            }
            length = 0;
            return false;
        }
    }

    private sealed class ResponseOwnedStream(Stream inner, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                response.Dispose();
            }
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync().ConfigureAwait(false);
            response.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private sealed class RemoteSnapshot(
        RemoteVaultStorage storage,
        string backupDirectory,
        IReadOnlyCollection<RemoteSnapshot.Entry> entries,
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
                await storage.WriteAtomicallyAsync(entry.Id, input, cancellationToken)
                    .ConfigureAwait(false);
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
