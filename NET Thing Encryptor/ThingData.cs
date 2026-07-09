using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace NET_Thing_Encryptor;
public static class ThingData
{
    private const string Magic = "NET Thing Encryptor";
    private static readonly byte[] EncryptedFileHeader = "NTE2"u8.ToArray();
    private const int NonceSize = 12;
    private const int AuthenticationTagSize = 16;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim MutationLock = new(1, 1);
    private static readonly JsonSerializerOptions FileSerializerOptions = new()
    {
        WriteIndented = true
    };
    private sealed class MutationBackup(
        string backupDirectory,
        string rootPath,
        string saveLocation)
    {
        public string BackupDirectory { get; } = backupDirectory;
        public string RootPath { get; } = rootPath;
        public string SaveLocation { get; } = saveLocation;
    }

    private static byte[]? EncryptionKey;
    private static byte[]? LegacyIv;
    private static int _saving;
    public static ThingRoot? Root { get; private set; }
    public static int Saving => Volatile.Read(ref _saving);
    public static bool IsSessionUnlocked => Volatile.Read(ref EncryptionKey) is not null;

    public static void BeginSaving() => Interlocked.Increment(ref _saving);
    public static void EndSaving() => Interlocked.Decrement(ref _saving);

    public static void LockSession()
    {
        byte[]? oldKey = Interlocked.Exchange(ref EncryptionKey, null);
        byte[]? oldIv = Interlocked.Exchange(ref LegacyIv, null);
        if (oldKey is not null)
            CryptographicOperations.ZeroMemory(oldKey);
        if (oldIv is not null)
            CryptographicOperations.ZeroMemory(oldIv);
        if (Root is not null)
            Root.Content = null;
    }

    public static async Task<MemoryStream> Encrypt(Stream input)
    {
        byte[] key = GetEncryptionKey();
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
        return await Decrypt(input, GetEncryptionKey(), GetLegacyIv());
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
                    SetEncryptionMaterial(candidateKey, candidateLegacyIv);
                    Debug.WriteLine("Password correct");
                    return true;
                }
            }
            else
            {
                SetEncryptionMaterial(candidateKey, candidateLegacyIv);
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

    private static byte[] GetEncryptionKey()
    {
        byte[]? key = Volatile.Read(ref EncryptionKey);
        return key ?? throw new InvalidOperationException("An encryption key has not been established.");
    }

    private static byte[] GetLegacyIv()
    {
        byte[]? iv = Volatile.Read(ref LegacyIv);
        return iv ?? throw new InvalidOperationException("A legacy IV has not been established.");
    }

    private static void SetEncryptionMaterial(byte[] key, byte[] legacyIv)
    {
        byte[]? oldKey = Interlocked.Exchange(ref EncryptionKey, (byte[])key.Clone());
        byte[]? oldIv = Interlocked.Exchange(ref LegacyIv, (byte[])legacyIv.Clone());
        if (oldKey is not null)
            CryptographicOperations.ZeroMemory(oldKey);
        if (oldIv is not null)
            CryptographicOperations.ZeroMemory(oldIv);
    }
    public static async Task<bool> LoadMainData()
    {
        string dataDirectory = AppPaths.DataDirectory;
        string rootPath = AppPaths.RootFilePath;
        try
        {
            MigrateLegacyDataIfNeeded(dataDirectory);

            if (!Directory.Exists(dataDirectory))
            {
                Debug.WriteLine("\\Data directory not found, creating it.");
                Directory.CreateDirectory(dataDirectory);
            }
            if (File.Exists(rootPath))
            {
                Debug.WriteLine("Main file found, attempting to load.");
                using FileStream fs = File.OpenRead(rootPath);
                ThingRoot? root = await JsonSerializer.DeserializeAsync<ThingRoot>(fs);
                ArgumentNullException.ThrowIfNull(root, nameof(root));

                root.Content = [];
                RebaseLegacySaveLocation(root, dataDirectory);
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
                Root.SaveLocation = dataDirectory;
                Debug.WriteLine("New Root created in memory");
                MessageBox.Show("The main data file was not found.\n" +
                    "If this is your first time running this program, you can ignore this message.\n" +
                    "This can be caused by deleting/moving Application Files or changing the Save Location.",
                    "New folder structure created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentNullException)
        {
            string backupPath = Path.Combine(
                dataDirectory,
                $"0_damaged_{DateTime.Now:yyyyMMdd_HHmmss}.nte");
            Debug.WriteLine($"{ex.GetType().Name} occurred while loading the root file.");
            if (File.Exists(rootPath))
                File.Copy(rootPath, backupPath, overwrite: false);
            MessageBox.Show($"The main data file is corrupted or damaged. A backup has been created at {backupPath}.\n" +
                "Please restore from a backup or recreate the file.",
                "File Corrupted", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine("Unauthorized Access Exception occurred, user does not have permission to access the main data file.");
            MessageBox.Show("You do not have permission to access the main data file. " +
                "Please check your permissions or run the application as an administrator.",
                "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An unexpected error occurred while loading the main data: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Debug.WriteLine($"");
            throw new Exception($"An error occurred while loading the main data. (Type: {ex.GetType().FullName})", ex);
        }
        return false;
    }

    private static void MigrateLegacyDataIfNeeded(string targetDataDirectory)
    {
        if (File.Exists(AppPaths.RootFilePath))
            return;

        foreach (string legacyDataDirectory in AppPaths.LegacyDataDirectories)
        {
            string legacyRootPath = Path.Combine(legacyDataDirectory, "0.nte");
            if (!File.Exists(legacyRootPath))
                continue;

            CopyDirectoryIfMissing(legacyDataDirectory, targetDataDirectory);
            Debug.WriteLine($"Migrated legacy data from {legacyDataDirectory} to {targetDataDirectory}.");
            return;
        }
    }

    private static void CopyDirectoryIfMissing(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            string destinationFile = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            if (!File.Exists(destinationFile))
                File.Copy(sourceFile, destinationFile);
        }
    }

    private static void RebaseLegacySaveLocation(ThingRoot root, string dataDirectory)
    {
        foreach (string legacyDataDirectory in AppPaths.LegacyDataDirectories)
        {
            if (AppPaths.PathEquals(root.SaveLocation, legacyDataDirectory))
            {
                root.SaveLocation = dataDirectory;
                return;
            }
        }
    }
    public static string IDToHex(ulong id)
    {
        return id.ToString("X16");
    }
    public static ulong HexToID(string hex)
    {
        if (ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out ulong id))
        {
            return id;
        }
        throw new FormatException("Invalid hex format for ID.");
    }
    public static ulong GenerateID()
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ulong tempID;
        do
        {
            byte[] buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            tempID = BitConverter.ToUInt64(buffer, 0);
        }
        while (tempID == 0 || File.Exists(Path.Combine(Root.SaveLocation, IDToHex(tempID) + ".nte")));
        return tempID;
    }
    public static string GetFilePath(ulong id, bool create = false)
    {
        if(id == 0)
        {
            return AppPaths.RootFilePath;
        }
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        string path = string.Empty;

        path = Path.GetFullPath(Path.Combine(Root.SaveLocation, IDToHex(id) + ".nte"));
        if (File.Exists(path))
            return path;
        else if (create)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        throw new FileNotFoundException("File not found.", path);
    }
    public static async Task<ThingObject?> LoadFileAsync(ulong id)
    {
        Debug.WriteLine($"Loading file {id} ({IDToHex(id)})");
        if (id == 0)
        {
            Debug.WriteLine("Folder ID is 0, which is forbidden.");
            Debugger.Break();
            throw new ArgumentException("Cannot load root element.", nameof(id));
        }

        string filePath = string.Empty;

        filePath = GetFilePath(id);
        //Debug.WriteLine($"File path resolved to: {filePath}");

        using FileStream fs = File.OpenRead(filePath);
        using var decrypted = await Decrypt(fs).ConfigureAwait(false);
        decrypted.Position = 0;
        bool hasTypeDiscriminator = ContainsTypeDiscriminator(decrypted);
        decrypted.Position = 0;

        ThingObject? obj = hasTypeDiscriminator
            ? await JsonSerializer.DeserializeAsync<ThingObject>(
                decrypted,
                FileSerializerOptions).ConfigureAwait(false)
            : await JsonSerializer.DeserializeAsync<ThingFile>(
                decrypted,
                FileSerializerOptions).ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(obj, nameof(obj));
        obj.ID = id;
        return obj;
    }

    private static bool ContainsTypeDiscriminator(MemoryStream jsonStream)
    {
        ReadOnlySpan<byte> pattern = "\"$type\""u8;
        Span<byte> buffer = stackalloc byte[4096];
        int matched = 0;
        long originalPosition = jsonStream.Position;

        try
        {
            jsonStream.Position = 0;
            int bytesRead;
            while ((bytesRead = jsonStream.Read(buffer)) > 0)
            {
                foreach (byte value in buffer[..bytesRead])
                {
                    if (value == pattern[matched])
                    {
                        matched++;
                        if (matched == pattern.Length)
                            return true;
                    }
                    else
                    {
                        matched = value == pattern[0] ? 1 : 0;
                    }
                }
            }

            return false;
        }
        finally
        {
            jsonStream.Position = originalPosition;
        }
    }
    public static async Task<T?> LoadFileAsync<T>(ulong id) where T : ThingObject
    {
        ThingObject? obj = await LoadFileAsync(id);
        if (obj is T tObj)
        {
            return tObj;
        }
        throw new InvalidCastException($"The object with ID {id} does not match the expected type {typeof(T).Name}, got {obj?.GetType().Name} instead");
    }
    public static async Task SaveFileAsync(ThingObject? obj)
    {
        BeginSaving();
        try
        {
            ArgumentNullException.ThrowIfNull(obj, nameof(obj));
            Debug.WriteLine($"Saving file {obj.Name} - ID {obj.ID} ({IDToHex(obj.ID)})");

            switch (obj)
            {
                case ThingFile file:
                    await SaveFileCoreAsync(file);
                    break;
                case ThingFolder folder:
                    await SaveFolderCoreAsync(folder);
                    break;
                default:
                    throw new ArgumentException("Object must be of type ThingFile or ThingFolder.", nameof(obj));
            }
        }
        finally
        {
            EndSaving();
        }
    }

    private static async Task SaveFolderCoreAsync(ThingFolder folder)
    {
        string folderPath = GetFilePath(folder.ID, true);

        string folderContent = JsonSerializer.Serialize<ThingObject>(
            folder,
            FileSerializerOptions);

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(folderContent));
        await using var encrypted = await Encrypt(input);

        await WriteAtomicallyAsync(folderPath, encrypted);
    }

    private static async Task SaveFileCoreAsync(ThingFile file)
    {
        string filePath = GetFilePath(file.ID, true);

        await using MemoryStream plainStream = new MemoryStream();

        await JsonSerializer.SerializeAsync<ThingObject>(
            plainStream,
            file,
            FileSerializerOptions);
        plainStream.Position = 0;

        await using var encrypted = await Encrypt(plainStream);
        await WriteAtomicallyAsync(filePath, encrypted);
    }

    public static async Task SaveEncryptedDataAsync(ulong id, Stream plaintext)
    {
        BeginSaving();
        try
        {
            string filePath = GetFilePath(id, create: true);
            await using var encrypted = await Encrypt(plaintext);
            await WriteAtomicallyAsync(filePath, encrypted);
        }
        finally
        {
            EndSaving();
        }
    }

    private static async Task WriteAtomicallyAsync(string destinationPath, Stream content)
    {
        string fullPath = Path.GetFullPath(destinationPath);
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The destination has no parent directory.");
        Directory.CreateDirectory(directory);

        SemaphoreSlim fileLock = FileLocks.GetOrAdd(fullPath, static _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync();
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            content.Position = 0;
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await content.CopyToAsync(output);
                await output.FlushAsync();
                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            fileLock.Release();
        }
    }

    private static async Task RunMutationAsync(Func<Task> mutation)
    {
        BeginSaving();
        await MutationLock.WaitAsync();
        MutationBackup? backup = null;
        ThingRoot rootSnapshot = CloneRootForRollback(RequireRoot());
        try
        {
            backup = await CreateMutationBackupAsync();
            await mutation();
        }
        catch
        {
            Root = rootSnapshot;
            if (backup is not null)
                await RestoreMutationBackupAsync(backup);
            throw;
        }
        finally
        {
            if (backup is not null)
                DeleteMutationBackup(backup);
            MutationLock.Release();
            EndSaving();
        }
    }

    private static ThingRoot CloneRootForRollback(ThingRoot root)
    {
        ThingRoot clone = (ThingRoot)root.Clone();
        clone.Content = root.Content?
            .Select(link => new ThingObjectLink(
                link.ID,
                link.Name,
                link.Type,
                link.Size,
                link.PreviewContent is null ? null : (byte[])link.PreviewContent.Clone())
            {
                CreatedAt = link.CreatedAt
            })
            .ToList();
        return clone;
    }

    private static bool ContainsNameConflict(
        IEnumerable<ThingObjectLink> content,
        string name,
        ulong excludedID = 0)
    {
        return content.Any(link =>
            link.ID != excludedID &&
            string.Equals(link.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<MutationBackup> CreateMutationBackupAsync()
    {
        ThingRoot root = RequireRoot();
        string rootPath = GetFilePath(0);
        string saveLocation = Path.GetFullPath(root.SaveLocation);
        string backupDirectory = Path.Combine(
            Path.GetTempPath(),
            "NET Thing Encryptor",
            "MutationBackups",
            Guid.NewGuid().ToString("N"));
        string backupRootDirectory = Path.Combine(backupDirectory, "root");
        string backupObjectsDirectory = Path.Combine(backupDirectory, "objects");
        Directory.CreateDirectory(backupRootDirectory);
        Directory.CreateDirectory(backupObjectsDirectory);

        if (File.Exists(rootPath))
            await CopyFileAsync(rootPath, Path.Combine(backupRootDirectory, "0.nte"));

        if (Directory.Exists(saveLocation))
        {
            foreach (string file in Directory.EnumerateFiles(saveLocation, "*.nte", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(rootPath), StringComparison.OrdinalIgnoreCase))
                    continue;

                await CopyFileAsync(file, Path.Combine(backupObjectsDirectory, Path.GetFileName(file)));
            }
        }

        return new MutationBackup(backupDirectory, rootPath, saveLocation);
    }

    private static async Task RestoreMutationBackupAsync(MutationBackup backup)
    {
        string backupRootPath = Path.Combine(backup.BackupDirectory, "root", "0.nte");
        string backupObjectsDirectory = Path.Combine(backup.BackupDirectory, "objects");

        Directory.CreateDirectory(Path.GetDirectoryName(backup.RootPath)!);
        Directory.CreateDirectory(backup.SaveLocation);

        if (File.Exists(backupRootPath))
            await CopyFileAsync(backupRootPath, backup.RootPath);

        foreach (string file in Directory.EnumerateFiles(backup.SaveLocation, "*.nte", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(backup.RootPath), StringComparison.OrdinalIgnoreCase))
                continue;
            File.Delete(file);
        }

        if (Directory.Exists(backupObjectsDirectory))
        {
            foreach (string file in Directory.EnumerateFiles(backupObjectsDirectory, "*.nte", SearchOption.TopDirectoryOnly))
                await CopyFileAsync(file, Path.Combine(backup.SaveLocation, Path.GetFileName(file)));
        }
    }

    private static async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using FileStream source = new(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using FileStream destination = new(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await source.CopyToAsync(destination);
        await destination.FlushAsync();
        destination.Flush(flushToDisk: true);
    }

    private static void DeleteMutationBackup(MutationBackup backup)
    {
        try
        {
            if (Directory.Exists(backup.BackupDirectory))
                Directory.Delete(backup.BackupDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not delete mutation backup {backup.BackupDirectory}: {ex}");
        }
    }

    public static async Task MoveFileToFolderAsync(ThingFile file, ulong folderID)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (folderID == 0)
            throw new ArgumentException("Files cannot be placed directly in the root.", nameof(folderID));

        await RunMutationAsync(async () =>
        {
            ThingRoot root = RequireRoot();
            ThingFolder folder = await LoadFileAsync<ThingFolder>(folderID)
                ?? throw new FileNotFoundException("The target folder could not be loaded.");

            if (file.ParentID == folderID)
                return;
            if (folder.Content.Any(x => x.ID == file.ID))
                throw new InvalidOperationException("The target folder already contains this file.");
            if (ContainsNameConflict(folder.Content, file.Name, file.ID))
                throw new InvalidOperationException("The target folder already contains an item with this name.");

            ThingObjectLink? link;

            if (file.ParentID == 0)
            {
                link = root.Content?.FirstOrDefault(x => x.ID == file.ID);
                if (link is not null)
                {
                    root.Content!.Remove(link);
                    await SaveRootAsync();
                }
            }
            else
            {
                ThingFolder oldFolder = await LoadFileAsync<ThingFolder>(file.ParentID)
                    ?? throw new FileNotFoundException("The current parent folder could not be loaded.");
                link = oldFolder.Content.FirstOrDefault(x => x.ID == file.ID);
                if (link is null)
                    throw new InvalidDataException("The current parent does not reference this file.");
                oldFolder.Content.Remove(link);
                await SaveFileAsync(oldFolder);
            }

            link ??= new ThingObjectLink(
                file.ID,
                file.Name,
                file.Type,
                file.Content?.LongLength ?? 0);
            link.Name = file.Name;
            link.Type = file.Type;
            link.Size = file.Content?.LongLength ?? link.Size;

            file.ParentID = folder.ID;
            await SaveFileAsync(file);
            folder.Content.Add(link);
            await SaveFileAsync(folder);
        });
    }

    public static async Task MoveFolderToFolderAsync(ulong folderID, ulong parentFolderID)
    {
        if (folderID == 0)
            throw new ArgumentException("The root folder cannot be moved.", nameof(folderID));
        if (folderID == parentFolderID)
            throw new InvalidOperationException("A folder cannot contain itself.");

        await RunMutationAsync(async () =>
        {
            ThingRoot root = RequireRoot();
            ThingFolder folder = await LoadFileAsync<ThingFolder>(folderID)
                ?? throw new FileNotFoundException("The folder could not be loaded.");
            if (folder.ParentID == parentFolderID)
                return;

            ulong ancestorID = parentFolderID;
            while (ancestorID != 0)
            {
                if (ancestorID == folderID)
                    throw new InvalidOperationException("A folder cannot be moved into one of its descendants.");
                ThingFolder ancestor = await LoadFileAsync<ThingFolder>(ancestorID)
                    ?? throw new FileNotFoundException("An ancestor folder could not be loaded.");
                ancestorID = ancestor.ParentID;
            }

            ThingFolder? oldParent = null;
            ThingObjectLink? link;
            if (folder.ParentID == 0)
            {
                link = root.Content?.FirstOrDefault(x => x.ID == folder.ID);
            }
            else
            {
                oldParent = await LoadFileAsync<ThingFolder>(folder.ParentID)
                    ?? throw new FileNotFoundException("The current parent folder could not be loaded.");
                link = oldParent.Content.FirstOrDefault(x => x.ID == folder.ID);
            }

            if (link is null)
                throw new InvalidDataException("The current parent does not reference this folder.");

            ThingFolder? newParent = parentFolderID == 0
                ? null
                : await LoadFileAsync<ThingFolder>(parentFolderID);
            if (parentFolderID != 0 && newParent is null)
                throw new FileNotFoundException("The target parent folder could not be loaded.");
            if (newParent?.Content.Any(x => x.ID == folder.ID) == true ||
                (parentFolderID == 0 && root.Content?.Any(x => x.ID == folder.ID) == true))
                throw new InvalidOperationException("The target parent already contains this folder.");
            if (newParent is not null &&
                ContainsNameConflict(newParent.Content, folder.Name, folder.ID))
                throw new InvalidOperationException("The target parent already contains an item with this name.");
            if (parentFolderID == 0 &&
                ContainsNameConflict(root.Content ?? [], folder.Name, folder.ID))
                throw new InvalidOperationException("The root already contains an item with this name.");

            root.Content?.RemoveAll(x => x.ID == folder.ID);
            oldParent?.Content.RemoveAll(x => x.ID == folder.ID);

            folder.ParentID = parentFolderID;
            await SaveFileAsync(folder);

            if (newParent is null)
            {
                root.Content ??= [];
                root.Content.Add(link);
                await SaveRootAsync();
            }
            else
            {
                newParent.Content.Add(link);
                await SaveFileAsync(newParent);
            }

            if (oldParent is not null)
                await SaveFileAsync(oldParent);
            else if (parentFolderID != 0)
                await SaveRootAsync();
        });
    }

    public static async Task RenameObjectAsync(ulong id, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        if (id == 0)
            throw new ArgumentException("The root object cannot be renamed.", nameof(id));

        await RunMutationAsync(async () =>
        {
            ThingRoot root = RequireRoot();
            ThingObject obj = await LoadFileAsync(id)
                ?? throw new FileNotFoundException("The object could not be loaded.");
            if (obj.ParentID == 0)
            {
                if (ContainsNameConflict(root.Content ?? [], newName, id))
                    throw new InvalidOperationException("The root already contains an item with this name.");
            }
            else
            {
                ThingFolder parentForNameCheck = await LoadFileAsync<ThingFolder>(obj.ParentID)
                    ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                if (ContainsNameConflict(parentForNameCheck.Content, newName, id))
                    throw new InvalidOperationException("The parent folder already contains an item with this name.");
            }

            obj.Name = newName;
            await SaveFileAsync(obj);

            if (obj.ParentID == 0)
            {
                ThingObjectLink link = root.Content?.FirstOrDefault(x => x.ID == id)
                    ?? throw new InvalidDataException("The root does not reference the renamed object.");
                link.Name = newName;
                await SaveRootAsync();
            }
            else
            {
                ThingFolder parent = await LoadFileAsync<ThingFolder>(obj.ParentID)
                    ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                ThingObjectLink link = parent.Content.FirstOrDefault(x => x.ID == id)
                    ?? throw new InvalidDataException(
                        "The parent folder does not reference the renamed object.");
                link.Name = newName;
                await SaveFileAsync(parent);
            }
        });
    }

    public static async Task UpdateObjectSizeAsync(
        ulong id,
        long size,
        ulong? knownParentID = null)
    {
        if (id == 0)
            throw new ArgumentException("The root object has no parent link.", nameof(id));
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        BeginSaving();
        await MutationLock.WaitAsync();
        try
        {
            ThingRoot root = RequireRoot();
            ulong parentID;
            if (knownParentID.HasValue)
            {
                parentID = knownParentID.Value;
            }
            else
            {
                ThingObject obj = await LoadFileAsync(id)
                    ?? throw new FileNotFoundException("The object could not be loaded.");
                parentID = obj.ParentID;
                if (obj is ThingFile file)
                {
                    long releasedBytes = file.Content?.LongLength ?? 0;
                    file.ReleaseContent();
                    MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
                }
            }

            if (parentID == 0)
            {
                ThingObjectLink? link = root.Content?.FirstOrDefault(x => x.ID == id);
                if (link is not null && link.Size != size)
                {
                    link.Size = size;
                    await SaveRootAsync();
                }
            }
            else
            {
                ThingFolder parent = await LoadFileAsync<ThingFolder>(parentID)
                    ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                ThingObjectLink? link = parent.Content.FirstOrDefault(x => x.ID == id);
                if (link is not null && link.Size != size)
                {
                    link.Size = size;
                    await SaveFileAsync(parent);
                }
            }
        }
        finally
        {
            MutationLock.Release();
            EndSaving();
        }
    }

    private static async Task DeleteFileCoreAsync(ulong fileID)
    {
        ThingRoot root = RequireRoot();
        ThingFile file = await LoadFileAsync<ThingFile>(fileID)
            ?? throw new FileNotFoundException("The file could not be loaded.");
        long releasedBytes = file.Content?.LongLength ?? 0;
        file.ReleaseContent();
        MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
        Debug.WriteLine("Attempting to delete file: " + file.Name);

        if (file.ParentID == 0)
        {
            root.Content?.RemoveAll(x => x.ID == file.ID);
            await SaveRootAsync();
        }
        else
        {
            ThingFolder parent = await LoadFileAsync<ThingFolder>(file.ParentID)
                ?? throw new FileNotFoundException("The parent folder could not be loaded.");
            parent.Content.RemoveAll(x => x.ID == file.ID);
            await SaveFileAsync(parent);
        }

        await DeletePersistedFileAsync(GetFilePath(fileID));
    }

    private static async Task DeleteFolderCoreAsync(ulong folderID)
    {
        if (folderID == 0)
            throw new ArgumentException("The root folder cannot be deleted.", nameof(folderID));

        ThingRoot root = RequireRoot();
        ThingFolder folder = await LoadFileAsync<ThingFolder>(folderID)
            ?? throw new FileNotFoundException("The folder could not be loaded.");
        if (folder.Content.Count != 0)
            throw new InvalidOperationException("A non-empty folder cannot be deleted directly.");

        if (folder.ParentID == 0)
        {
            root.Content?.RemoveAll(x => x.ID == folder.ID);
            await SaveRootAsync();
        }
        else
        {
            ThingFolder parent = await LoadFileAsync<ThingFolder>(folder.ParentID)
                ?? throw new FileNotFoundException("The parent folder could not be loaded.");
            parent.Content.RemoveAll(x => x.ID == folder.ID);
            await SaveFileAsync(parent);
        }

        await DeletePersistedFileAsync(GetFilePath(folderID));
    }

    public static async Task DeleteObject(ulong id)
    {
        if (id == 0)
            throw new ArgumentException("The root object cannot be deleted.", nameof(id));

        await RunMutationAsync(() => DeleteObjectCoreAsync(id));
    }

    private static async Task DeleteObjectCoreAsync(ulong id)
    {
        ThingObject obj = await LoadFileAsync(id)
            ?? throw new FileNotFoundException("The object could not be loaded.");

        if (obj is ThingFile file)
        {
            await DeleteFileCoreAsync(file.ID);
            return;
        }

        if (obj is not ThingFolder folder)
            throw new InvalidDataException("The stored object has an unsupported type.");

        foreach (ThingObjectLink link in folder.Content.ToList())
            await DeleteObjectCoreAsync(link.ID);

        await DeleteFolderCoreAsync(folder.ID);
    }

    private static async Task DeletePersistedFileAsync(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);
        SemaphoreSlim fileLock = FileLocks.GetOrAdd(fullPath, static _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync();
        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            if (File.Exists(fullPath))
                throw new IOException($"The encrypted file could not be deleted: {fullPath}");
        }
        finally
        {
            fileLock.Release();
        }
    }
    public static async Task SaveRootAsync()
    {
        BeginSaving();
        try
        {
            Debug.WriteLine("Saving Root to main file");
            ThingRoot root = RequireRoot();
            ThingRoot tempRoot = (ThingRoot)root.Clone();
            tempRoot.ContentEncrypted = await Encrypt(Magic + JsonSerializer.Serialize(root.Content));
            tempRoot.Content = null;
            string rootContent = JsonSerializer.Serialize(tempRoot);
            string rootPath = GetFilePath(0);
            using var content = new MemoryStream(Encoding.UTF8.GetBytes(rootContent), writable: false);
            await WriteAtomicallyAsync(rootPath, content);
        }
        finally
        {
            EndSaving();
        }
    }
    public static ThingFolder AddToRoot(this ThingFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ThingRoot root = RequireRoot();
        root.Content ??= [];
        if (root.Content.Any(x => x.ID == folder.ID))
            throw new InvalidOperationException("The root already contains this folder.");
        folder.ParentID = 0;
        root.Content.Add(new ThingObjectLink(folder.ID, folder.Name, FileType.folder, 0));
        return folder;
    }
    public static async Task<List<ThingObjectLink>> LoadFolderContent(ulong id)
    {
        List<ThingObjectLink> content = [];

        if (id == 0)
        {
            ThingRoot root = RequireRoot();
            foreach (ThingObjectLink link in root.Content ?? [])
            {
                content.Add(link);
            }
            return content;
        }
        else
        {
            ThingObject? file = await LoadFileAsync(id);
            if(file is ThingFolder folder)
            {
                foreach(ThingObjectLink link in folder.Content)
                {
                    content.Add(link);
                }
                return content;
            }
        }
        throw new ArgumentException("The provided ID does not correspond to a folder.", nameof(id));
    }

    private static ThingRoot RequireRoot()
    {
        return Root ?? throw new InvalidOperationException("The root data has not been loaded.");
    }
    public static string Sizeify(this long sizeInBytes)
    {
        string[] sizes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        double len = sizeInBytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
    public static void ClearImage(this PictureBox p)
    {
        Image? image = p.Image;
        p.Image = null;
        image?.Dispose();
    }
}
