using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Nte.RemoteServer;

const long defaultMaximumObjectBytes = 8L * 1024 * 1024 * 1024;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string dataDirectory = Path.GetFullPath(
    builder.Configuration["NTE_REMOTE_DATA_DIRECTORY"] ??
    Path.Combine(builder.Environment.ContentRootPath, "vault-data"));
string accessPassword = ReadAccessPassword(builder.Configuration);
long maximumObjectBytes = ReadMaximumObjectBytes(builder.Configuration, defaultMaximumObjectBytes);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maximumObjectBytes;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maximumObjectBytes;
});
builder.Services.AddSingleton(new RemoteVaultRepository(dataDirectory));

WebApplication app = builder.Build();
byte[] expectedPasswordHash = SHA256.HashData(Encoding.UTF8.GetBytes(accessPassword));
app.UseMiddleware<BasicPasswordAuthenticationMiddleware>(expectedPasswordHash);

RemoteVaultRepository repository = app.Services.GetRequiredService<RemoteVaultRepository>();
await repository.InitializeAsync(app.Lifetime.ApplicationStopping);

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    apiVersion = 1
}));

app.MapGet("/api/v1/vault", async (
    RemoteVaultRepository store,
    CancellationToken cancellationToken) =>
    Results.Ok(await store.GetMetadataAsync(cancellationToken)));

app.MapGet("/api/v1/vault/objects/{id}", async (
    string id,
    RemoteVaultRepository store,
    CancellationToken cancellationToken) =>
{
    if (!RemoteVaultRepository.TryParseId(id, out ulong objectId))
        return Results.BadRequest(new { error = "The object ID must contain 16 hexadecimal digits." });
    if (!store.Exists(objectId))
        return Results.NotFound(new { error = "The encrypted object does not exist." });

    Stream content = await store.OpenReadAsync(objectId, cancellationToken);
    return Results.Stream(content, "application/octet-stream");
});

app.MapPut("/api/v1/vault/objects/{id}", async (
    string id,
    HttpRequest request,
    HttpResponse response,
    RemoteVaultRepository store,
    CancellationToken cancellationToken) =>
{
    if (!RemoteVaultRepository.TryParseId(id, out ulong objectId))
        return Results.BadRequest(new { error = "The object ID must contain 16 hexadecimal digits." });

    RemoteMutationResult result = await store.WriteAsync(
        objectId,
        request.Body,
        request.Headers.IfMatch,
        cancellationToken);
    response.Headers.ETag = $"\"{result.Revision}\"";
    return result.RevisionMatched
        ? Results.NoContent()
        : Results.Conflict(new { error = "The vault was changed by another client." });
});

app.MapDelete("/api/v1/vault/objects/{id}", async (
    string id,
    HttpRequest request,
    HttpResponse response,
    RemoteVaultRepository store,
    CancellationToken cancellationToken) =>
{
    if (!RemoteVaultRepository.TryParseId(id, out ulong objectId))
        return Results.BadRequest(new { error = "The object ID must contain 16 hexadecimal digits." });

    RemoteMutationResult result = await store.DeleteAsync(
        objectId,
        request.Headers.IfMatch,
        cancellationToken);
    response.Headers.ETag = $"\"{result.Revision}\"";
    return result.RevisionMatched
        ? Results.NoContent()
        : Results.Conflict(new { error = "The vault was changed by another client." });
});

app.MapPost("/api/v1/vault/preserve-root", async (
    PreserveRootRequest request,
    RemoteVaultRepository store,
    CancellationToken cancellationToken) =>
{
    string? location = await store.PreserveDamagedRootAsync(request.Suffix, cancellationToken);
    return Results.Ok(new { location });
});

app.Run();

static string ReadAccessPassword(IConfiguration configuration)
{
    string? passwordFile = configuration["NTE_REMOTE_ACCESS_PASSWORD_FILE"];
    if (!string.IsNullOrWhiteSpace(passwordFile))
    {
        string fullPath = Path.GetFullPath(passwordFile);
        string password = File.ReadAllText(fullPath).TrimEnd('\r', '\n');
        if (!string.IsNullOrEmpty(password))
            return password;
    }

    string? configured = configuration["NTE_REMOTE_ACCESS_PASSWORD"];
    if (!string.IsNullOrEmpty(configured))
        return configured;

    throw new InvalidOperationException(
        "Set NTE_REMOTE_ACCESS_PASSWORD or NTE_REMOTE_ACCESS_PASSWORD_FILE before starting the server.");
}

static long ReadMaximumObjectBytes(IConfiguration configuration, long fallback)
{
    string? configured = configuration["NTE_REMOTE_MAX_OBJECT_BYTES"];
    if (string.IsNullOrWhiteSpace(configured))
        return fallback;
    if (!long.TryParse(configured, out long result) || result <= 0)
        throw new InvalidOperationException("NTE_REMOTE_MAX_OBJECT_BYTES must be a positive integer.");
    return result;
}

internal sealed record PreserveRootRequest(string Suffix);

public partial class Program;
