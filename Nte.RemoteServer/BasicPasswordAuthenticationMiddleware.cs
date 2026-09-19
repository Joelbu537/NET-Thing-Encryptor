using System.Security.Cryptography;
using System.Text;

namespace Nte.RemoteServer;

internal sealed class BasicPasswordAuthenticationMiddleware(
    RequestDelegate next,
    byte[] expectedPasswordHash)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!TryReadPassword(context.Request.Headers.Authorization, out string? password) ||
            !PasswordMatches(password))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"NTE Remote Vault\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "The remote access password is invalid."
            });
            return;
        }

        await next(context);
    }

    private bool PasswordMatches(string password)
    {
        byte[] actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        try
        {
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedPasswordHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actualHash);
        }
    }

    private static bool TryReadPassword(string? authorization, out string password)
    {
        password = string.Empty;
        const string prefix = "Basic ";
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            string decoded = Encoding.UTF8.GetString(
                Convert.FromBase64String(authorization[prefix.Length..].Trim()));
            int separator = decoded.IndexOf(':');
            if (separator < 0 || !string.Equals(decoded[..separator], "nte", StringComparison.Ordinal))
                return false;
            password = decoded[(separator + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
