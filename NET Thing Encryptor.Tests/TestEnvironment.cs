using System.Reflection;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

internal sealed class TestEnvironment : IAsyncDisposable
{
    private static readonly PropertyInfo RootProperty = typeof(ThingData).GetProperty(
        nameof(ThingData.Root), BindingFlags.Public | BindingFlags.Static)!;

    private TestEnvironment(string directory, ThingRoot root)
    {
        Directory = directory;
        Root = root;
    }

    public string Directory { get; }
    public ThingRoot Root { get; }

    public static async Task<TestEnvironment> CreateAsync(string password = "correct horse battery staple")
    {
        ThingData.LockSession();
        SetRoot(null);

        string directory = Path.Combine(Path.GetTempPath(), "NETThingEncryptor.Tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        AppPaths.DataDirectoryOverride = directory;

        var root = new ThingRoot
        {
            Salt = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray(),
            SaveLocation = directory,
            Content = []
        };
        SetRoot(root);
        Assert.True(await ThingData.AttemptDecrypt(password));
        return new TestEnvironment(directory, root);
    }

    public ThingFolder CreateRootFolder(string name)
    {
        var folder = new ThingFolder(name);
        folder.AddToRoot();
        return folder;
    }

    public ThingFile CreateFile(string name, string extension, byte[] content, ulong parentId = 0)
    {
        return new ThingFile(name, content)
        {
            Extension = extension,
            Type = FileCategories.GetFileType(extension),
            ParentID = parentId
        };
    }

    public static void SetRoot(ThingRoot? root) => RootProperty.SetValue(null, root);

    public ValueTask DisposeAsync()
    {
        ThingData.LockSession();
        SetRoot(null);
        AppPaths.DataDirectoryOverride = null;
        try
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // A failed assertion should remain the primary failure if Windows still holds a file briefly.
        }
        return ValueTask.CompletedTask;
    }
}
