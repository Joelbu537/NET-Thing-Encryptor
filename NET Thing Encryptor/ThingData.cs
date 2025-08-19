using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor;
public static class ThingData
{
    private const string Magic = "NET Thing Encryptor";
    public static byte[]? Key { private get; set; }
    public static byte[]? IV { private get; set; }
    public static ThingRoot Root { get; private set; }

    public static async Task<Stream> Encrypt(Stream input)
    {
        if (Key == null || IV == null)
            throw new InvalidOperationException("Key and IV must be set before encryption.");

        var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;

        var output = new MemoryStream();
        using var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
        await input.CopyToAsync(cryptoStream);
        await cryptoStream.FlushFinalBlockAsync();
        output.Position = 0;
        return output;
    }

    public static async Task<Stream> Decrypt(Stream input)
    {
        if (Key == null || IV == null)
            throw new InvalidOperationException("Key and IV must be set before decryption.");

        var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;

        var output = new MemoryStream();
        using var cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true);
        await cryptoStream.CopyToAsync(output);
        output.Position = 0;
        return output;
    }

    // Convenience: String → String (Base64)
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
    public static string ComputeMD5Hash(byte[] input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(input);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
    public static async Task<bool> AttemptDecrypt(string password)
    {
        byte[] salt = Root.Salt;

        try
        {
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations: 10000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 48
            );

            Key = key[..32];
            IV = key[32..];

            string temp = await Decrypt(Root.ContentEncrypted);

            if(temp.StartsWith(Magic)) //PWD korrekt
            {
                Root.Content = JsonSerializer.Deserialize<List<ulong>>(temp[20..]); // CHECKEN
                return true;
            }

            Key = null;
            IV = null;
            return false;
        }
        catch (Exception)
        {

        }
        return false;
    }
    public async static Task<bool> LoadMainData()
    {
        try
        {
            using FileStream fs = File.OpenRead(@"/Data/0.nte");
            ThingRoot? root = await JsonSerializer.DeserializeAsync<ThingRoot>(fs);

            if (root != null)
            {
                Root = root;
                return true;
            }
        }
        catch(FileNotFoundException)
        {
            MessageBox.Show("The main data file was not found, and a new one is being created." +
                "If this is your first time running this program, you can ignore this message." +
                "This can be caused by deleting/moving Application Files or changing the Save Location.",
                "New folder structure created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("You do not have permission to access the main data file. " +
                "Please check your permissions or run the application as an administrator.",
                "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while loading the main data.", ex);
        }
        return false;
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
        ulong tempID = 0;
        do
        {
            byte[] buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            tempID = BitConverter.ToUInt64(buffer, 0);
        }
        while (tempID != 0 && File.Exists(Path.Combine(Root.SaveLocation, ThingData.IDToHex(tempID))));
        return tempID;

    }
    public static bool VerifyFile(ThingFile file)
    {
        if (ComputeMD5Hash(file.Content) == file.MD5Hash)
        {
            return true;
        }
        return false;
    }
    public static string GetFilePath(ulong id)
    {
        string path = string.Empty;
        if (Root.SaveLocation == null)
        {
            path = Path.GetFullPath(ThingData.IDToHex(id));
            if(File.Exists(path))
                return path;
        }
        else
        {
            path = Path.GetFullPath(Path.Combine(Root.SaveLocation, ThingData.IDToHex(id)));
            if (File.Exists(path))
                return path;
        }
        throw new FileNotFoundException("File not found.", path);
    }
    public static async Task<ThingFolder> GetFolderAsync(ulong folderID)
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        if (folderID == 0)
            throw new ArgumentException("Folder ID cannot be 0.", nameof(folderID));

        try
        {
            string folderPath = GetFilePath(folderID);

            using FileStream fs = File.OpenRead(folderPath);
            ThingFolder? folder = await JsonSerializer.DeserializeAsync<ThingFolder>(await Decrypt(fs));

            ArgumentNullException.ThrowIfNull(folder, nameof(folder));
            return folder;
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("Folder not found.", ThingData.GetFilePath(folderID));
        }
    }
    public static async Task<ThingFile> GetFileAsync(ulong fileID)
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        if (fileID == 0)
            throw new ArgumentException("File ID cannot be 0.", nameof(fileID));
        try
        {
            string filePath = GetFilePath(fileID);
            using FileStream fs = File.OpenRead(filePath);
            ThingFile? file = await JsonSerializer.DeserializeAsync<ThingFile>(await Decrypt(fs));
            ArgumentNullException.ThrowIfNull(file, nameof(file));
            return file;
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("File not found.", ThingData.GetFilePath(fileID));
        }
    }
    public static async Task SaveFolderAsync(ThingFolder? folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        string folderPath = GetFilePath(folder.ID);
        string folderContent = JsonSerializer.Serialize(folder);
        string content = await Encrypt(folderContent);

        using FileStream fs = File.Create(folderPath);
        fs.Write(Encoding.UTF8.GetBytes(content));
    }
    public static async Task SaveFileAsync(ThingFile? file)
    {
        ArgumentNullException.ThrowIfNull(file);

        string filePath = GetFilePath(file.ID);
        string fileContent = JsonSerializer.Serialize(file);
        string content = await Encrypt(fileContent);

        using FileStream fs = File.Create(filePath);
        fs.Write(Encoding.UTF8.GetBytes(content));
    }
    public static async Task AddFileToFolder(ThingFile file, ulong folderID)
    {
        ThingFolder folder = await GetFolderAsync(folderID);

        if (file.ParentID != 0)
        {
            ThingFolder? oldFolder = await GetFolderAsync(file.ParentID);
            oldFolder.Content.Remove(file.ID);
            await SaveFolderAsync(oldFolder);
        }
        folder.Content.Add(file.ID);
        await SaveFolderAsync(folder);
        file.ParentID = folder.ID;

    }
}