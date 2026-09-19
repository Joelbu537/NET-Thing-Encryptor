using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace NET_Thing_Encryptor.Tests;

public sealed class RemoteVaultStorageTests
{
    [Fact]
    public async Task RemoteStorage_AuthenticatesAndRoundTripsEncryptedObjects()
    {
        var handler = new VaultProtocolHandler("server secret");
        using var client = new HttpClient(handler);
        using var storage = new RemoteVaultStorage(
            "https://vault.example.test/base",
            "server secret",
            client);

        await storage.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.False(storage.Exists(0));
        Assert.Empty(storage.ListObjectIds());
        Assert.Equal("https://vault.example.test/base", storage.ObjectLocation);

        const ulong id = 0x1234;
        byte[] encrypted = "encrypted payload"u8.ToArray();
        using var input = new MemoryStream(encrypted, writable: false);
        await storage.WriteAtomicallyAsync(id, input, TestContext.Current.CancellationToken);

        Assert.True(input.CanRead);
        Assert.True(storage.Exists(id));
        Assert.Equal([id], storage.ListObjectIds());
        await using Stream remote = await storage.OpenReadAsync(
            id,
            TestContext.Current.CancellationToken);
        using var copy = new MemoryStream();
        await remote.CopyToAsync(copy, TestContext.Current.CancellationToken);
        Assert.Equal(encrypted, copy.ToArray());

        await storage.DeleteAsync(id, TestContext.Current.CancellationToken);

        Assert.False(storage.Exists(id));
        Assert.Empty(storage.ListObjectIds());
        Assert.All(handler.Requests, request =>
        {
            Assert.StartsWith("/base/api/v1/vault", request.Path, StringComparison.Ordinal);
            Assert.Equal("nte:server secret", request.Credentials);
        });
        Assert.Equal(["r1", "r2"], handler.MutationRevisions);
    }

    [Fact]
    public async Task RemoteStorage_MapsAuthenticationAndConcurrentChangeErrors()
    {
        using var unauthorizedClient = new HttpClient(
            new VaultProtocolHandler("correct password"));
        using var unauthorized = new RemoteVaultStorage(
            "https://vault.example.test",
            "wrong password",
            unauthorizedClient);
        await Assert.ThrowsAsync<RemoteVaultAuthenticationException>(
            () => unauthorized.InitializeAsync(TestContext.Current.CancellationToken));

        var conflictingHandler = new VaultProtocolHandler("secret")
        {
            ConflictOnNextMutation = true
        };
        using var conflictingClient = new HttpClient(conflictingHandler);
        using var conflicting = new RemoteVaultStorage(
            "https://vault.example.test",
            "secret",
            conflictingClient);
        await conflicting.InitializeAsync(TestContext.Current.CancellationToken);
        using var content = new MemoryStream([1, 2, 3], writable: false);

        await Assert.ThrowsAsync<RemoteVaultConflictException>(
            () => conflicting.WriteAtomicallyAsync(
                42,
                content,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoteStorageSnapshot_RestoresEncryptedRootAndObjectSet()
    {
        var handler = new VaultProtocolHandler("secret");
        using var client = new HttpClient(handler);
        using var storage = new RemoteVaultStorage(
            "https://vault.example.test",
            "secret",
            client);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await storage.InitializeAsync(cancellationToken);
        using (var originalRoot = new MemoryStream("encrypted root"u8.ToArray(), writable: false))
            await storage.WriteAtomicallyAsync(0, originalRoot, cancellationToken);

        await using IVaultStorageSnapshot snapshot = await storage.CreateSnapshotAsync(
            null,
            cancellationToken);
        using (var changedRoot = new MemoryStream("changed root"u8.ToArray(), writable: false))
            await storage.WriteAtomicallyAsync(0, changedRoot, cancellationToken);
        using (var addedObject = new MemoryStream("added object"u8.ToArray(), writable: false))
            await storage.WriteAtomicallyAsync(99, addedObject, cancellationToken);

        await snapshot.RestoreAsync(cancellationToken);

        Assert.True(storage.Exists(0));
        Assert.False(storage.Exists(99));
        await using Stream root = await storage.OpenReadAsync(0, cancellationToken);
        using var copy = new MemoryStream();
        await root.CopyToAsync(copy, cancellationToken);
        Assert.Equal("encrypted root"u8.ToArray(), copy.ToArray());
    }

    [Theory]
    [InlineData("vault.example.test", "https://vault.example.test")]
    [InlineData("vault.example.test:8443/base", "https://vault.example.test:8443/base")]
    [InlineData("192.168.178.75", "https://192.168.178.75")]
    [InlineData("https://vault.example.test/", "https://vault.example.test")]
    [InlineData("http://localhost:5248", "http://localhost:5248")]
    [InlineData("http://127.0.0.1:5248", "http://127.0.0.1:5248")]
    public void RemoteStorage_NormalizesServerNamesAndDefaultsToHttps(
        string address,
        string expected)
    {
        using var storage = new RemoteVaultStorage(address, "secret");

        Assert.Equal(expected, storage.ObjectLocation);
    }

    [Theory]
    [InlineData("ftp://vault.example.test")]
    [InlineData("http://192.168.178.75:5248")]
    [InlineData("")]
    public void RemoteStorage_RejectsInvalidOrInsecureRemoteAddresses(string address)
    {
        Assert.Throws<ArgumentException>(() => new RemoteVaultStorage(address, "secret"));
    }

    private sealed class VaultProtocolHandler(string expectedPassword) : HttpMessageHandler
    {
        private readonly Dictionary<ulong, byte[]> _objects = [];
        private string _revision = "r1";

        public List<RequestRecord> Requests { get; } = [];
        public List<string> MutationRevisions { get; } = [];
        public bool ConflictOnNextMutation { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string credentials = DecodeCredentials(request.Headers.Authorization);
            Requests.Add(new RequestRecord(request.RequestUri!.AbsolutePath, credentials));
            if (credentials != $"nte:{expectedPassword}")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = JsonContent.Create(new { error = "unauthorized" })
                };
            }

            string path = request.RequestUri.AbsolutePath;
            if (request.Method == HttpMethod.Get &&
                path.TrimEnd('/').EndsWith("/api/v1/vault", StringComparison.Ordinal))
            {
                return Json(new
                {
                    apiVersion = RemoteVaultStorage.SupportedApiVersion,
                    rootExists = _objects.ContainsKey(0),
                    objectIds = _objects.Keys.Where(id => id != 0).Select(ThingData.IDToHex).ToArray(),
                    revision = _revision
                });
            }

            ulong id = ParseObjectId(path);
            if (request.Method == HttpMethod.Get)
            {
                return _objects.TryGetValue(id, out byte[]? payload)
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(payload)
                    }
                    : new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            string suppliedRevision = request.Headers.IfMatch.Single().Tag.Trim('"');
            MutationRevisions.Add(suppliedRevision);
            if (ConflictOnNextMutation || suppliedRevision != _revision)
            {
                ConflictOnNextMutation = false;
                return Json(new { error = "conflict" }, HttpStatusCode.Conflict);
            }

            if (request.Method == HttpMethod.Put)
            {
                _objects[id] = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            }
            else if (request.Method == HttpMethod.Delete)
            {
                _objects.Remove(id);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }

            _revision = _revision == "r1" ? "r2" : "r3";
            var response = new HttpResponseMessage(HttpStatusCode.NoContent);
            response.Headers.ETag = new EntityTagHeaderValue($"\"{_revision}\"");
            return response;
        }

        private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = JsonContent.Create(value) };

        private static ulong ParseObjectId(string path) =>
            ThingData.HexToID(path[(path.LastIndexOf('/') + 1)..]);

        private static string DecodeCredentials(AuthenticationHeaderValue? authorization)
        {
            if (authorization?.Scheme != "Basic" || authorization.Parameter is null)
                return string.Empty;
            return Encoding.UTF8.GetString(Convert.FromBase64String(authorization.Parameter));
        }
    }

    private sealed record RequestRecord(string Path, string Credentials);
}
