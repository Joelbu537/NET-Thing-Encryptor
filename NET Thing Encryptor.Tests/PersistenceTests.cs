using System.Text;
using System.Text.Json;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task SaveAndLoadFile_PreservesPolymorphicData()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile file = environment.CreateFile("notes", ".txt", Encoding.UTF8.GetBytes("hello"));

        await ThingData.SaveFileAsync(file);
        ThingFile loaded = (await ThingData.LoadFileAsync<ThingFile>(file.ID))!;

        Assert.Equal(file.ID, loaded.ID);
        Assert.Equal("notes", loaded.Name);
        Assert.Equal(".txt", loaded.Extension);
        Assert.Equal(FileType.text, loaded.Type);
        Assert.Equal("hello", Encoding.UTF8.GetString(loaded.Content!));
        await Assert.ThrowsAsync<InvalidCastException>(() => ThingData.LoadFileAsync<ThingFolder>(file.ID));
    }

    [Fact]
    public async Task SaveAndLoadFolder_PreservesLinks()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFolder folder = environment.CreateRootFolder("documents");
        folder.Content.Add(new ThingObjectLink(123, "child", FileType.other, 99, [8, 9]));

        await ThingData.SaveFileAsync(folder);
        ThingFolder loaded = (await ThingData.LoadFileAsync<ThingFolder>(folder.ID))!;

        ThingObjectLink link = Assert.Single(loaded.Content);
        Assert.Equal(123UL, link.ID);
        Assert.Equal(99, link.Size);
        Assert.Equal([8, 9], link.PreviewContent);
    }

    [Fact]
    public async Task SaveEncryptedData_WritesDecryptablePayloadAtomically()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ulong id = ThingData.GenerateID();
        byte[] payload = Encoding.UTF8.GetBytes("raw payload");

        await ThingData.SaveEncryptedDataAsync(id, new MemoryStream(payload));
        await using FileStream encrypted = File.OpenRead(ThingData.GetFilePath(id));
        await using MemoryStream decrypted = await ThingData.Decrypt(encrypted);

        Assert.Equal(payload, decrypted.ToArray());
        Assert.Empty(System.IO.Directory.GetFiles(environment.Directory, "*.tmp"));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task GetFilePathAndLoad_ValidateArgumentsAndExistence()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ulong missing = 0x1234;

        Assert.EndsWith("0000000000001234.nte", ThingData.GetFilePath(missing, create: true));
        Assert.Throws<FileNotFoundException>(() => ThingData.GetFilePath(missing));
        await Assert.ThrowsAsync<ArgumentException>(() => ThingData.LoadFileAsync(0));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThingData.SaveFileAsync(null));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task GenerateId_ReturnsNonZeroUniqueUnusedIds()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        var ids = Enumerable.Range(0, 100).Select(_ => ThingData.GenerateID()).ToArray();
        Assert.DoesNotContain(0UL, ids);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public async Task SaveFile_OverwritesExistingContentWithoutLeavingTemporaryFiles()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile file = environment.CreateFile("versioned", ".txt", Encoding.UTF8.GetBytes("first"));
        await ThingData.SaveFileAsync(file);

        file.Content = Encoding.UTF8.GetBytes("second");
        await ThingData.SaveFileAsync(file);
        ThingFile loaded = (await ThingData.LoadFileAsync<ThingFile>(file.ID))!;

        Assert.Equal("second", Encoding.UTF8.GetString(loaded.Content!));
        Assert.Empty(System.IO.Directory.GetFiles(environment.Directory, "*.tmp"));
    }

    [Fact]
    public async Task ConcurrentSaves_AlwaysLeaveACompleteAuthenticatedFile()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile first = environment.CreateFile("concurrent", ".bin", Enumerable.Repeat((byte)0x11, 64 * 1024).ToArray());
        var second = new ThingFile("concurrent", Enumerable.Repeat((byte)0x22, 64 * 1024).ToArray())
        {
            ID = first.ID,
            Extension = ".bin",
            Type = FileType.other
        };

        await Task.WhenAll(ThingData.SaveFileAsync(first), ThingData.SaveFileAsync(second));
        ThingFile loaded = (await ThingData.LoadFileAsync<ThingFile>(first.ID))!;
        byte[] loadedContent = Assert.IsType<byte[]>(loaded.Content);

        Assert.True(loadedContent.All(value => value == 0x11) || loadedContent.All(value => value == 0x22));
        Assert.Equal(64 * 1024, loadedContent.Length);
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task LoadFile_ReadsLegacyJsonWithoutTypeDiscriminator()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingFile file = environment.CreateFile("legacy-json", ".txt", [7, 8, 9]);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(file);
        await ThingData.SaveEncryptedDataAsync(file.ID, new MemoryStream(json));

        ThingFile loaded = Assert.IsType<ThingFile>(await ThingData.LoadFileAsync(file.ID));
        Assert.Equal([7, 8, 9], loaded.Content);
        Assert.Equal(file.ID, loaded.ID);
    }

    [Fact]
    public async Task LoadFile_RejectsTamperedAndInvalidJsonWithoutLeakingSavingState()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ulong tamperedId = ThingData.GenerateID();
        await ThingData.SaveEncryptedDataAsync(tamperedId, new MemoryStream("{}"u8.ToArray()));
        string path = ThingData.GetFilePath(tamperedId);
        byte[] bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        bytes[^1] ^= 0x40;
        await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<System.Security.Cryptography.CryptographicException>(
            () => ThingData.LoadFileAsync(tamperedId));

        ulong invalidJsonId = ThingData.GenerateID();
        await ThingData.SaveEncryptedDataAsync(invalidJsonId, new MemoryStream("not json"u8.ToArray()));
        await Assert.ThrowsAsync<JsonException>(() => ThingData.LoadFileAsync(invalidJsonId));
        Assert.Equal(0, ThingData.Saving);
    }

    [Fact]
    public async Task SaveRoot_EncryptsNamesAndCanBeLoadedIntoANewSession()
    {
        await using var environment = await TestEnvironment.CreateAsync("password");
        environment.Root.Content!.Add(new ThingObjectLink(55, "secret-name", FileType.text, 10));
        await ThingData.SaveRootAsync();
        string rootJson = await File.ReadAllTextAsync(AppPaths.RootFilePath, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("secret-name", rootJson);

        ThingData.LockSession();
        TestEnvironment.SetRoot(null);
        Assert.True(await ThingData.LoadMainData());
        Assert.True(await ThingData.AttemptDecrypt("password"));
        Assert.Contains(ThingData.Root!.Content!, link => link.ID == 55 && link.Name == "secret-name");
    }

    [Fact]
    public async Task SaveFile_RejectsUnsupportedObjectsAndAlwaysBalancesSavingCounter()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(
            () => ThingData.SaveFileAsync(new UnsupportedThingObject { ID = ThingData.GenerateID() }));
        Assert.Equal(0, ThingData.Saving);
    }

    private sealed class UnsupportedThingObject : ThingObject;
}
