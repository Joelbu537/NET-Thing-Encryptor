using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NET_Thing_Encryptor;
public static partial class ThingData
{
    private const string Magic = "NET Thing Encryptor";
    private static readonly byte[] EncryptedFileHeader = "NTE2"u8.ToArray();
    private const int NonceSize = 12;
    private const int AuthenticationTagSize = 16;
    private static readonly SemaphoreSlim MutationLock = new(1, 1);
    private static readonly JsonSerializerOptions FileSerializerOptions = new()
    {
        WriteIndented = true
    };
    private static readonly VaultSession Session = new();
    private static IVaultStorage? _storage;

    public static event EventHandler<VaultNotificationEventArgs>? NotificationRaised;

    public static VaultSession CurrentSession => Session;
    public static IVaultStorage CurrentStorage => _storage
        ?? throw new InvalidOperationException(
            "Vault storage has not been configured. Call ThingData.ConfigureStorage first.");
    public static ThingRoot? Root
    {
        get => Session.Root;
        private set => Session.Root = value;
    }
    public static int Saving => Session.Saving;
    public static bool IsSessionUnlocked => Session.IsUnlocked;

    public static void BeginSaving() => Session.BeginSaving();
    public static void EndSaving() => Session.EndSaving();
    public static void LockSession() => Session.Lock();

    public static void ConfigureStorage(IVaultStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    internal static async Task<T> RunStorageExclusiveAsync<T>(
        Func<IVaultStorage, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        BeginSaving();
        bool lockTaken = false;
        try
        {
            await MutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            return await operation(CurrentStorage).ConfigureAwait(false);
        }
        finally
        {
            if (lockTaken)
                MutationLock.Release();
            EndSaving();
        }
    }

    public static async Task<MemoryStream> Encrypt(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        byte[] key = Session.GetEncryptionKey();
        using var plaintext = new MemoryStream();
        await input.CopyToAsync(plaintext);

        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[checked((int)plaintext.Length)];
        byte[] authenticationTag = new byte[AuthenticationTagSize];

        using (var aes = new AesGcm(key, AuthenticationTagSize))
        {
            aes.Encrypt(
                nonce,
                plaintext.GetBuffer().AsSpan(0, checked((int)plaintext.Length)),
                ciphertext,
                authenticationTag,
                EncryptedFileHeader);
        }

        var output = new MemoryStream(
            EncryptedFileHeader.Length + NonceSize + AuthenticationTagSize + ciphertext.Length);
        await output.WriteAsync(EncryptedFileHeader);
        await output.WriteAsync(nonce);
        await output.WriteAsync(authenticationTag);
        await output.WriteAsync(ciphertext);
        output.Position = 0;
        return output;
    }

    public static async Task<MemoryStream> Decrypt(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await Decrypt(input, Session.GetEncryptionKey(), Session.GetLegacyIv());
    }

    private static async Task<MemoryStream> Decrypt(Stream input, byte[] key, byte[] legacyIv)
    {
        byte[] encryptedBytes = await ReadStreamExactlyAsync(input);

        if (encryptedBytes.AsSpan().StartsWith(EncryptedFileHeader))
        {
            int payloadOffset = EncryptedFileHeader.Length + NonceSize + AuthenticationTagSize;
            if (encryptedBytes.Length < payloadOffset)
                throw new CryptographicException("The encrypted data is incomplete.");

            ReadOnlySpan<byte> nonce = encryptedBytes.AsSpan(EncryptedFileHeader.Length, NonceSize);
            ReadOnlySpan<byte> authenticationTag =
                encryptedBytes.AsSpan(EncryptedFileHeader.Length + NonceSize, AuthenticationTagSize);
            ReadOnlySpan<byte> ciphertext = encryptedBytes.AsSpan(payloadOffset);
            byte[] plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(key, AuthenticationTagSize))
            {
                aes.Decrypt(nonce, ciphertext, authenticationTag, plaintext, EncryptedFileHeader);
            }

            return new MemoryStream(plaintext, writable: false);
        }

        // Compatibility with files written before the authenticated NTE2 format.
        using var legacyAes = Aes.Create();
        legacyAes.Key = key;
        legacyAes.IV = legacyIv;

        var output = new MemoryStream();
        using var encryptedInput = new MemoryStream(encryptedBytes, writable: false);
        using var cryptoStream =
            new CryptoStream(encryptedInput, legacyAes.CreateDecryptor(), CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(output);
        output.Position = 0;
        return output;
    }

    private static async Task<byte[]> ReadStreamExactlyAsync(Stream input)
    {
        if (input.CanSeek)
        {
            long remainingLength = input.Length - input.Position;
            if (remainingLength < 0 || remainingLength > int.MaxValue)
                throw new IOException("The input stream is too large.");

            byte[] result = GC.AllocateUninitializedArray<byte>((int)remainingLength);
            await input.ReadExactlyAsync(result);
            return result;
        }

        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    public static async Task<string> Encrypt(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentNullException(nameof(input));

        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        using var encrypted = await Encrypt(inputStream);
        return Convert.ToBase64String(((MemoryStream)encrypted).ToArray());
    }

    public static async Task<string> Decrypt(string base64Input)
    {
        if (string.IsNullOrEmpty(base64Input))
            throw new ArgumentNullException(nameof(base64Input));

        byte[] encryptedBytes = Convert.FromBase64String(base64Input);
        using var inputStream = new MemoryStream(encryptedBytes);
        using var decrypted = await Decrypt(inputStream);
        return Encoding.UTF8.GetString(((MemoryStream)decrypted).ToArray());
    }

    public static string GetMD5Hash(byte[] input)
    {
        using MD5 md5 = MD5.Create();

        byte[] hashBytes = md5.ComputeHash(input);

        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
    public static async Task<bool> AttemptDecrypt(string password)
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] keyMaterial = Rfc2898DeriveBytes.Pbkdf2(
                password,
                Root.Salt,
                iterations: 10000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 48
                );

        byte[] candidateKey = keyMaterial[..32];
        byte[] candidateLegacyIv = keyMaterial[32..];

        try
        {
            if (!string.IsNullOrEmpty(Root.ContentEncrypted))
            {
                byte[] encryptedBytes = Convert.FromBase64String(Root.ContentEncrypted);
                using var inputStream = new MemoryStream(encryptedBytes, writable: false);
                using var decrypted = await Decrypt(inputStream, candidateKey, candidateLegacyIv);
                string temp = Encoding.UTF8.GetString(decrypted.ToArray());

                if (temp.StartsWith(Magic)) //PWD korrekt
                {
                    string subtemp = temp[Magic.Length..];
                    Root.Content = JsonSerializer.Deserialize<List<ThingObjectLink>>(subtemp) ?? [];
                    Session.SetEncryptionMaterial(candidateKey, candidateLegacyIv);
                    Debug.WriteLine("Password correct");
                    return true;
                }
            }
            else
            {
                Session.SetEncryptionMaterial(candidateKey, candidateLegacyIv);
                Debug.WriteLine("No main file found, assuming first run or no password set.");
                return true;
            }
        }
        catch (CryptographicException)
        {
            Debug.WriteLine("Password incorrect, decryption FAILED.");
        }
        catch (FormatException)
        {
            Debug.WriteLine("Root content is not valid encrypted data.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyMaterial);
            CryptographicOperations.ZeroMemory(candidateKey);
            CryptographicOperations.ZeroMemory(candidateLegacyIv);
        }

        Debug.WriteLine("Password incorrect.");
        return false;
    }

    public static async Task<bool> LoadMainData()
    {
        IVaultStorage storage = CurrentStorage;
        try
        {
            await storage.InitializeAsync().ConfigureAwait(false);
            if (storage.Exists(0))
            {
                Debug.WriteLine("Main file found, attempting to load.");
                await using Stream input = await storage.OpenReadAsync(0).ConfigureAwait(false);
                ThingRoot? root = await JsonSerializer.DeserializeAsync<ThingRoot>(input)
                    .ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(root, nameof(root));

                root.Content = [];
                root.SaveLocation = storage.ResolveObjectLocation(root.SaveLocation);
                Root = root;
                Debug.WriteLine("Main file loaded successfully.");
            }
            else
            {
                Debug.WriteLine("Main data file not found.");
                byte[] salt = new byte[32];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
                Root = new ThingRoot();
                Root.Salt = salt;
                Root.SaveLocation = storage.ObjectLocation;
                Debug.WriteLine("New Root created in memory");
                Notify(
                    "New folder structure created",
                    "The main data file was not found.\n" +
                    "If this is your first time running this program, you can ignore this message.\n" +
                    "This can be caused by deleting/moving Application Files or changing the Save Location.",
                    VaultNotificationSeverity.Information);
            }
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentNullException)
        {
            string? backupPath = await storage.PreserveDamagedRootAsync(
                $"damaged_{DateTime.Now:yyyyMMdd_HHmmss}").ConfigureAwait(false);
            Debug.WriteLine($"{ex.GetType().Name} occurred while loading the root file.");
            Notify(
                "File Corrupted",
                backupPath is null
                    ? "The main data file is corrupted or damaged and could not be preserved.\n" +
                      "Please restore from a backup or recreate the file."
                    : $"The main data file is corrupted or damaged. A backup has been created at {backupPath}.\n" +
                "Please restore from a backup or recreate the file.",
                VaultNotificationSeverity.Error);
            return false;
        }
        catch (LegacyDataMigrationConflictException ex)
        {
            Debug.WriteLine(ex.Message);
            Notify(
                "Data Migration Conflict",
                $"Existing application data prevented an automatic migration.\n\n" +
                $"Old data: {ex.SourceDirectory}\n" +
                $"Target: {ex.TargetDirectory}\n\n" +
                "No data was changed. Move or back up the conflicting target files, then start the application again.",
                VaultNotificationSeverity.Error);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine("Unauthorized Access Exception occurred, user does not have permission to access the main data file.");
            Notify(
                "Access Denied",
                "You do not have permission to access the main data file. " +
                "Please check your permissions for the selected storage location.",
                VaultNotificationSeverity.Error);
        }
        catch (Exception ex)
        {
            Notify(
                "Error",
                $"An unexpected error occurred while loading the main data: {ex.Message}",
                VaultNotificationSeverity.Error);
            Debug.WriteLine($"");
            throw new Exception($"An error occurred while loading the main data. (Type: {ex.GetType().FullName})", ex);
        }
        return false;
    }

    private static void Notify(
        string title,
        string message,
        VaultNotificationSeverity severity)
    {
        NotificationRaised?.Invoke(null, new VaultNotificationEventArgs(title, message, severity));
    }

}
