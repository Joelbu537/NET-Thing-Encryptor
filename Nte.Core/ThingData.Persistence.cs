using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NET_Thing_Encryptor;

public static partial class ThingData
{
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
        while (tempID == 0 || CurrentStorage.Exists(tempID));
        return tempID;
    }
    public static string GetFilePath(ulong id, bool create = false)
    {
        string location = CurrentStorage.GetLocation(id);
        if (create || CurrentStorage.Exists(id))
            return location;

        throw new FileNotFoundException("File not found.", location);
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

        await using Stream input = await CurrentStorage.OpenReadAsync(id).ConfigureAwait(false);
        using var decrypted = await Decrypt(input).ConfigureAwait(false);
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
        string folderContent = JsonSerializer.Serialize<ThingObject>(
            folder,
            FileSerializerOptions);

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(folderContent));
        await using var encrypted = await Encrypt(input);

        await CurrentStorage.WriteAtomicallyAsync(folder.ID, encrypted).ConfigureAwait(false);
    }

    private static async Task SaveFileCoreAsync(ThingFile file)
    {
        await using MemoryStream plainStream = new MemoryStream();

        await JsonSerializer.SerializeAsync<ThingObject>(
            plainStream,
            file,
            FileSerializerOptions);
        plainStream.Position = 0;

        await using var encrypted = await Encrypt(plainStream);
        await CurrentStorage.WriteAtomicallyAsync(file.ID, encrypted).ConfigureAwait(false);
    }

    public static async Task SaveEncryptedDataAsync(ulong id, Stream plaintext)
    {
        BeginSaving();
        try
        {
            await using var encrypted = await Encrypt(plaintext);
            await CurrentStorage.WriteAtomicallyAsync(id, encrypted).ConfigureAwait(false);
        }
        finally
        {
            EndSaving();
        }
    }

}
