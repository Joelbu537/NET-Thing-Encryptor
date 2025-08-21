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
                List<ulong>? content = JsonSerializer.Deserialize<List<ulong>>(await Decrypt(root.ContentEncrypted));
                Root = root;
                return true;
            }
            else
            {
                File.Copy(@"/Data/0.nte", @"/Data/0_damaged.nte", true);
                MessageBox.Show("The main data file is corrupted or damaged. A backup has been created at /Data/0_damaged.nte." +
                    "Please restore from a backup or recreate the file.",
                    "File Corrupted", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new InvalidOperationException("The main data file is corrupted or damaged.");
            }
        }
        catch(FileNotFoundException)
        {
            byte[] salt = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            Root = new ThingRoot(salt);
            MessageBox.Show("The main data file was not found, and a new one has been created." +
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
    public static string GetFilePath(ulong id, bool create = false)
    {
        string path = string.Empty;
        if (Root.SaveLocation == null)
        {
            path = Path.GetFullPath(IDToHex(id) + ".nte");
            if (File.Exists(path))
                return path;
            else if (create)
            {
                File.Create(path).Close();
                return path;
            }
        }
        else
        {
            path = Path.GetFullPath(Path.Combine(Root.SaveLocation, IDToHex(id)));
            if (File.Exists(path))
                return path;
            else if (create)
            {
                File.Create(path).Close();
                return path;
            }
        }
        throw new FileNotFoundException("File not found.", path);
    }
    private static async Task<ThingFolder?> GetFolderAsync(ulong folderID)
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
    private static async Task<ThingFile?> GetFileAsync(ulong fileID)
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
    private static async Task SaveFolderAsync(ThingFolder? folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        string folderPath = GetFilePath(folder.ID, true);
        string folderContent = JsonSerializer.Serialize(folder);
        string content = await Encrypt(folderContent);

        using FileStream fs = File.Create(folderPath);
        fs.Write(Encoding.UTF8.GetBytes(content));
    }
    private static async Task SaveFileAsync(ThingFile? file)
    {
        ArgumentNullException.ThrowIfNull(file);

        string filePath = GetFilePath(file.ID, true);
        string fileContent = JsonSerializer.Serialize(file);
        string content = await Encrypt(fileContent);

        using FileStream fs = File.Create(filePath);
        fs.Write(Encoding.UTF8.GetBytes(content));
    }
    public static async Task MoveFileToFolder(ThingFile file, ulong folderID)
    {
        ThingFolder? folder = await GetFolderAsync(folderID);
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));

        ThingFolder? oldFolder = await GetFolderAsync(file.ParentID);
        ArgumentNullException.ThrowIfNull(oldFolder, nameof(oldFolder));
        oldFolder.Content.Remove(file.ID);
        await SaveFolderAsync(oldFolder);

        folder.Content.Add(file.ID);
        await SaveFolderAsync(folder);
        file.ParentID = folder.ID;
        await SaveFileAsync(file);
    }
    public static async Task MoveFolderToFolder(ulong folder, ulong parentFolderID)
    {
        ThingFolder? parentFolder = await GetFolderAsync(parentFolderID);
        ThingFolder? targetFolder = await GetFolderAsync(folder);

        ArgumentNullException.ThrowIfNull(parentFolder, nameof(parentFolder));
        ArgumentNullException.ThrowIfNull(targetFolder, nameof(targetFolder));

        if(targetFolder.ID == 0)
        {
            throw new ArgumentException("Cannot add the root folder to another folder.", nameof(folder));
        }

        ThingFolder? oldFolder = await GetFolderAsync(targetFolder.ParentID);
        ArgumentNullException.ThrowIfNull(oldFolder, nameof(oldFolder));
        oldFolder.Content.Remove(targetFolder.ID);
        await SaveFolderAsync(oldFolder);

        parentFolder.Content.Add(targetFolder.ID);
        await SaveFolderAsync(parentFolder);
        targetFolder.ParentID = parentFolder.ID;
        await SaveFolderAsync(targetFolder);
    }
    public static async Task DeleteFile(ulong fileID)
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ThingFile? file = await GetFileAsync(fileID);
        ArgumentNullException.ThrowIfNull(file, nameof(file));
        string filePath = GetFilePath(fileID);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        if (file.ParentID != 0)
        {
            ThingFolder? folder = await GetFolderAsync(file.ParentID);
            ArgumentNullException.ThrowIfNull(folder, nameof(folder));
            folder.Content.Remove(file.ID);
            await SaveFolderAsync(folder);
        }
    }
    public static async Task DeleteFolder(ulong folderID)
    {
        if(folderID == 0)
        {
            throw new ArgumentException("Cannot delete the root folder.", nameof(folderID));
        }
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ThingFolder? folder = await GetFolderAsync(folderID);
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));
        if (folder.Content.Count != 0)
        {
            throw new InvalidOperationException("Cannot delete a folder that contains files or subfolders.");
        }
        string folderPath = GetFilePath(folderID);
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
        if (folder.ParentID != 0)
        {
            ThingFolder? parentFolder = await GetFolderAsync(folder.ParentID);
            ArgumentNullException.ThrowIfNull(parentFolder, nameof(parentFolder));
            parentFolder.Content.Remove(folder.ID);
            await SaveFolderAsync(parentFolder);
        }
    }
}