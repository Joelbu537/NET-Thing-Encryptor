using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;

namespace NET_Thing_Encryptor;
public static class ThingData
{
    private const string Magic = "NET Thing Encryptor";
    public static byte[]? Key { get; private set; }
    public static byte[]? IV { get; set; }
    public static ThingRoot? Root { get; private set; }
    public static event EventHandler SaveStatusChanged = delegate { };
    private static int _saving = 0;
    public static int Saving
    {
        get { return _saving; } 
        set { _saving = value; SaveStatusChanged.Invoke(null, EventArgs.Empty); }
    }

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
        await cryptoStream.CopyToAsync(output); /// GRRRR SCHON WIEDER
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
    private static string GetPath(string localPath)
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        if (Root.SaveLocation == null)
        {
            return Path.GetFullPath(localPath);
        }
        else
        {
            return Path.GetFullPath(Path.Combine(Root.SaveLocation, localPath));
        }
    }

    public static async Task<bool> AttemptDecrypt(string password)
    {
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        byte[]? salt = Root.Salt;

        byte[]? key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations: 10000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 48
            );

        Key = key[..32];
        IV = key[32..];

        if (!string.IsNullOrEmpty(Root.ContentEncrypted))
        {
            try
            {
                string? temp = await Decrypt(Root.ContentEncrypted);
                Debug.WriteLine(temp);

                if (temp.StartsWith(Magic)) //PWD korrekt
                {
                    string? subtemp = temp.Substring(Magic.Length);
                    Root.Content = JsonSerializer.Deserialize<List<ThingObjectLink>>(subtemp);
                    Debug.WriteLine("Password correct, main data loaded successfully.");
                    return true;
                }
            }
            catch(CryptographicException)
            {
                Debug.WriteLine("Decryption FAILED.");
            }
        }
        else
        {
            Debug.WriteLine("No content encrypted found, assuming first run or no password set.");
            return true;
        }

        Debug.WriteLine("Password incorrect, resetting Key and IV.");
        Key = null;
        IV = null;
        return false;
    }
    public async static Task<bool> LoadMainData()
    {
    Retry:
        try
        {
            if(!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Data")))
            {
                Debug.WriteLine("\\Data Directory not found, creating it.");
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, @"Data"));
            }
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, @"Data\0.nte")))
            {
                Debug.WriteLine("Main data file found, attempting to load.");
                using FileStream fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, @"Data\0.nte"));
                ThingRoot? root = await JsonSerializer.DeserializeAsync<ThingRoot>(fs);
                ArgumentNullException.ThrowIfNull(root, nameof(root));

                root.Content = new List<ThingObjectLink>();
                Root = root;
                Debug.WriteLine("Root loaded successfully.");
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
                Debug.WriteLine("New Root created in memory");
                MessageBox.Show("The main data file was not found." +
                    "If this is your first time running this program, you can ignore this message." +
                    "This can be caused by deleting/moving Application Files or changing the Save Location.",
                    "New folder structure created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return true;
        }
        catch (JsonException)
        {
            Debug.WriteLine("JSON Exception occurred, attempting to recover.");
            File.Copy(@"/Data/0.nte", @"/Data/0_damaged.nte", true);
            MessageBox.Show("The main data file is corrupted or damaged. A backup has been created at /Data/0_damaged.nte." +
                "Please restore from a backup or recreate the file.",
                "File Corrupted", MessageBoxButtons.OK, MessageBoxIcon.Error);
            goto Retry;
        }
        catch (ArgumentNullException)
        {
            Debug.WriteLine("Argument Null Exception occurred, attempting to recover.");
            File.Copy(@"/Data/0.nte", @"/Data/0_damaged.nte", true);
            MessageBox.Show("The main data file is corrupted or damaged. A backup has been created at /Data/0_damaged.nte." +
                "Please restore from a backup or recreate the file.",
                "File Corrupted", MessageBoxButtons.OK, MessageBoxIcon.Error);
            goto Retry;
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
            Debug.WriteLine($"An unexpected error occurred while loading the main data: {ex.Message}");
            throw new Exception($"An error occurred while loading the main data. (Type: {ex.GetType().FullName})", ex);
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
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ulong tempID = 0;
        do
        {
            byte[] buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            tempID = BitConverter.ToUInt64(buffer, 0);
        }
        while (tempID != 0 && File.Exists((Root.SaveLocation == null) ? ThingData.IDToHex(tempID) : Path.Combine(Root.SaveLocation, ThingData.IDToHex(tempID))));
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
        if(id == 0)
        {
            return Path.Combine(Directory.GetCurrentDirectory() ,"Data\\0.nte");
        }
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        string path = string.Empty;

        path = Path.GetFullPath(Path.Combine(Root.SaveLocation, (IDToHex(id) + ".nte")));
        if (File.Exists(path))
            return path;
        else if (create)
        {
            File.Create(path).Close();
            return path;
        }

        throw new FileNotFoundException("File not found.", path);
    }
    public static async Task<ThingObject?> LoadFileAsync(ulong id)
    {
        Debug.WriteLine($"Loading file with ID: {id}");
        if (id == 0)
            throw new ArgumentException("Cannot load root element.", nameof(id));

        string filePath = string.Empty;
        try
        {
            filePath = GetFilePath(id);
            Debug.WriteLine($"File path resolved to: {filePath}");

            using FileStream fs = File.OpenRead(filePath);
            using var decrypted = await Decrypt(fs);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            ThingObject? obj = await JsonSerializer.DeserializeAsync<ThingObject>(decrypted, options); //// GRRRRRR NEUE INSTANZ :X

            ArgumentNullException.ThrowIfNull(obj, nameof(obj));
            obj.ID = id;
            return obj;
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("File not found.", filePath);
        }
    }
    public static async Task<T?> LoadFileAsync<T>(ulong id) where T : ThingObject
    {
        ThingObject? obj = await LoadFileAsync(id);
        if (obj is T tObj)
        {
            return tObj;
        }
        throw new InvalidCastException($"The object with ID {id} is not of type {typeof(T).Name}.");
    }
    public static async Task SaveFileAsync(ThingObject? obj)
    {
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));
        Debug.WriteLine($"Saving file {obj.Name} with ID {obj.ID} as {IDToHex(obj.ID)}");
        Saving++;

        switch (obj)
        {
            case ThingFile file:
                await SaveFileAsyncLegacy(file);
                break;
            case ThingFolder folder:
                await SaveFolderAsyncLegacy(folder);
                break;
            default:
                throw new ArgumentException("Object must be of type ThingFile or ThingFolder.", nameof(obj));
        }

        Saving--;
    }
    private static async Task SaveFolderAsyncLegacy(ThingFolder? folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        string folderPath = GetFilePath(folder.ID, true);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string folderContent = JsonSerializer.Serialize<ThingObject>(folder, options);

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(folderContent));
        using var encrypted = await Encrypt(input);

        using FileStream fs = File.Create(folderPath);
        await encrypted.CopyToAsync(fs);
    }
    private static async Task SaveFileAsyncLegacy(ThingFile? file)
    {
        ArgumentNullException.ThrowIfNull(file);

        string filePath = GetFilePath(file.ID, true);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string fileContent = JsonSerializer.Serialize<ThingObject>(file, options);

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        using var encrypted = await Encrypt(input);

        using FileStream fs = File.Create(filePath);
        await encrypted.CopyToAsync(fs);
    }
    public static async Task MoveFileToFolderAsync(ThingFile file, ulong folderID)
    {
        Saving++;
        ThingFolder? folder = await LoadFileAsync(folderID) as ThingFolder;
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));
        ThingObjectLink? link;

        if(file.ParentID == 0)
        {
            ArgumentNullException.ThrowIfNull(Root, nameof(Root));
            ArgumentNullException.ThrowIfNull(Root.Content, nameof(Root.Content));
            link = new ThingObjectLink(file.ID, file.Name, file.Type, 0);

            Root.Content.Remove(link);
        }
        else
        {
            ThingFolder? oldFolder = await LoadFileAsync(file.ParentID) as ThingFolder;
            ArgumentNullException.ThrowIfNull(oldFolder, nameof(oldFolder));
            link = oldFolder.Content.FirstOrDefault(x => x.ID == file.ID);
            oldFolder.Content.Remove(link);
            await SaveFileAsync(oldFolder);
        }

        folder.Content.Add(link);
        await SaveFileAsync(folder);
        file.ParentID = folder.ID;
        await SaveFileAsync(file);
        Saving--;
    }
    public static async Task MoveFolderToFolderAsync(ulong folderID, ulong parentFolderID)
    {
        Saving++;
        ThingFolder? parentFolder = await LoadFileAsync(parentFolderID) as ThingFolder;
        ThingFolder? folder = await LoadFileAsync(folderID) as ThingFolder;
        ThingObjectLink? link;

        ArgumentNullException.ThrowIfNull(parentFolder, nameof(parentFolder));
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));

        if(folder.ID == 0)
        {
            throw new ArgumentException("Cannot add the root folder to another folder.", nameof(folder));
        }

        if(folder.ParentID == 0)
        {
            link = Root.Content.FirstOrDefault(x => x.ID == folder.ID);
            Root.Content.Remove(link);
        }
        else
        {
            ThingFolder? oldFolder = await LoadFileAsync(folder.ParentID) as ThingFolder; // GRRRR
            ArgumentNullException.ThrowIfNull(oldFolder, nameof(oldFolder));
            link = oldFolder.Content.FirstOrDefault(x => x.ID == folder.ID);
            oldFolder.Content.Remove(link);
            await SaveFileAsync(oldFolder);
        }


        parentFolder.Content.Add(link);
        await SaveFileAsync(parentFolder);
        folder.ParentID = parentFolder.ID;
        await SaveFileAsync(folder);
        Saving--;
    }
    public static async Task DeleteFile(ulong fileID)
    {
        Saving++;
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ThingFile? file = await LoadFileAsync(fileID) as ThingFile;
        ArgumentNullException.ThrowIfNull(file, nameof(file));
        Debug.WriteLine("Attempting to delete file: " + file.Name);
        string filePath = GetFilePath(fileID);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        ThingFolder? folder = await LoadFileAsync(file.ParentID) as ThingFolder;
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));
        folder.Content.RemoveAll(x => x.ID == file.ID);
        await SaveFileAsync(folder);
        Saving--;
    }
    public static async Task DeleteFolder(ulong folderID)
    {
        Saving++;
        if(folderID == 0)
        {
            throw new ArgumentException("Cannot delete the root folder.", nameof(folderID));
        }
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ThingFolder? folder = await LoadFileAsync(folderID) as ThingFolder;
        ArgumentNullException.ThrowIfNull(folder, nameof(folder));
        Debug.Write("Attempting to delete folder: " + folder.Name + "...  ");
        if (folder.Content.Count != 0)
        {
            throw new InvalidOperationException("Cannot delete a folder that contains files or subfolders.");
        }
        string folderPath = GetFilePath(folderID);
        if (folder.ParentID != 0)
        {
            ThingFolder? parentFolder = await LoadFileAsync(folder.ParentID) as ThingFolder;
            ArgumentNullException.ThrowIfNull(parentFolder, nameof(parentFolder));
            parentFolder.Content.RemoveAll(x => x.ID == folder.ID);
            await SaveFileAsync(parentFolder);
        }
        else
        {
            Root.Content.RemoveAll(x => x.ID == folder.ID);
            await SaveRootAsync();
        }
        if (File.Exists(folderPath))
        {
            File.Delete(folderPath);
        }
        Saving--;
        Debug.WriteLine("Success");
    }
    public static async Task DeleteObject(ulong id)
    {
        Saving++;
        ThingObject? obj = await LoadFileAsync(id);
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));
        await DeleteObject(obj);
        Saving--;
    }
    public static async Task DeleteObject(ThingObject obj)
    {
        Saving++;
        if (obj is ThingFile file)
        {
            await DeleteFile(file.ID);
        }
        else if(obj is ThingFolder folder)
        {
            foreach(ThingObjectLink link in folder.Content.ToList())
            {
                if(link.Type == FileType.folder)
                {
                    await DeleteObject(link.ID);
                }
                else
                {
                    await DeleteFile(link.ID);
                }
            }
            await DeleteFolder(folder.ID);
        }
        else
        {
            throw new ArgumentException("Object must be of type ThingFile or ThingFolder.", nameof(obj));
        }
        Saving--;
    }
    public static async Task SaveRootAsync()
    {
        Saving++;
        Debug.WriteLine("Saving Root data to file.");
        ArgumentNullException.ThrowIfNull(Root, nameof(Root));
        ThingRoot tempRoot = (ThingRoot)Root.Clone();
        tempRoot.ContentEncrypted = await Encrypt(Magic + JsonSerializer.Serialize(Root.Content));
        tempRoot.Content = null;
        string rootContent = JsonSerializer.Serialize(tempRoot);
        string rootPath = GetFilePath(0);
        Debug.WriteLine($"Root path: {rootPath}");
        using FileStream fs = File.Create(rootPath);
        await fs.WriteAsync(Encoding.UTF8.GetBytes(rootContent));
        await fs.FlushAsync();
        fs.Close();
        Saving--;

        // DEBUG
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "debug.txt"), JsonSerializer.Serialize(Root));
    }
    public static ThingFolder AddToRoot(this ThingFolder folder)
    {
        ArgumentNullException.ThrowIfNull(ThingData.Root, "Root cannot be null.");
        ThingData.Root.Content?.Add(new ThingObjectLink(folder.ID, folder.Name, FileType.folder, 0));
        return folder;
    }
    public static async Task<List<ThingObjectLink>> LoadFolderContent(ulong id)
    {
        List<ThingObjectLink> content = new List<ThingObjectLink>();

        if(id == 0)
        {
            foreach (ThingObjectLink link in Root.Content)
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
    public static string Sizeify(this long size_in_bytes)
    {
        string[] sizes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "RB", "QB" };
        double len = size_in_bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return String.Format("{0:0.##} {1}", len, sizes[order]);
    }
    public static void ClearImage(this PictureBox p)
    {
        if (p.Image != null)
        {
            p.Image.Dispose();
            p.Image = null;
        }

    }
}