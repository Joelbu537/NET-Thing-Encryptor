using System.Security.Cryptography;
using System.Text;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class CryptographyTests
{
    private const string DefaultPassword = "correct horse battery staple";

    [Fact]
    public async Task EncryptDecrypt_RoundTripsBinaryAndUsesFreshNonces()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        byte[] plaintext = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        await using var first = await ThingData.Encrypt(new MemoryStream(plaintext));
        await using var second = await ThingData.Encrypt(new MemoryStream(plaintext));
        Assert.NotEqual(first.ToArray(), second.ToArray());
        Assert.Equal("NTE2", Encoding.ASCII.GetString(first.ToArray(), 0, 4));

        await using var decrypted = await ThingData.Decrypt(first);
        Assert.Equal(plaintext, decrypted.ToArray());
    }

    [Fact]
    public async Task EncryptDecrypt_HandlesEmptyAndPositionedStreams()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await using MemoryStream emptyEncrypted = await ThingData.Encrypt(new MemoryStream());
        await using MemoryStream emptyDecrypted = await ThingData.Decrypt(emptyEncrypted);
        Assert.Empty(emptyDecrypted.ToArray());

        var positioned = new MemoryStream([99, 98, 1, 2, 3]);
        positioned.Position = 2;
        await using MemoryStream encrypted = await ThingData.Encrypt(positioned);
        await using MemoryStream decrypted = await ThingData.Decrypt(encrypted);
        Assert.Equal([1, 2, 3], decrypted.ToArray());
    }

    [Fact]
    public async Task Decrypt_HandlesNonSeekableStreams()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await using MemoryStream encrypted = await ThingData.Encrypt(new MemoryStream([4, 5, 6]));
        await using var nonSeekable = new NonSeekableReadStream(encrypted.ToArray());
        await using MemoryStream decrypted = await ThingData.Decrypt(nonSeekable);
        Assert.Equal([4, 5, 6], decrypted.ToArray());
    }

    [Fact]
    public async Task StringEncryption_RoundTripsUnicode()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        const string value = "Grüße 🔐 日本語";
        string encrypted = await ThingData.Encrypt(value);
        Assert.NotEqual(value, encrypted);
        Assert.Equal(value, await ThingData.Decrypt(encrypted));
    }

    [Fact]
    public async Task Decrypt_ReadsLegacyCbcPayloads()
    {
        await using var environment = await TestEnvironment.CreateAsync(DefaultPassword);
        byte[] keyMaterial = Rfc2898DeriveBytes.Pbkdf2(
            DefaultPassword,
            environment.Root.Salt,
            10000,
            HashAlgorithmName.SHA256,
            48);
        byte[] plaintext = Encoding.UTF8.GetBytes("legacy data");
        byte[] ciphertext;
        using (Aes aes = Aes.Create())
        {
            aes.Key = keyMaterial[..32];
            aes.IV = keyMaterial[32..];
            using var output = new MemoryStream();
            await using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                await crypto.WriteAsync(plaintext, TestContext.Current.CancellationToken);
            ciphertext = output.ToArray();
        }

        await using MemoryStream decrypted = await ThingData.Decrypt(new MemoryStream(ciphertext));
        Assert.Equal(plaintext, decrypted.ToArray());
        CryptographicOperations.ZeroMemory(keyMaterial);
    }

    [Fact]
    public async Task Decrypt_RejectsTamperedAndTruncatedCiphertext()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await using var encrypted = await ThingData.Encrypt(new MemoryStream([1, 2, 3, 4]));
        byte[] tampered = encrypted.ToArray();
        tampered[^1] ^= 0xFF;

        await Assert.ThrowsAsync<AuthenticationTagMismatchException>(
            () => ThingData.Decrypt(new MemoryStream(tampered)));
        await Assert.ThrowsAsync<CryptographicException>(
            () => ThingData.Decrypt(new MemoryStream("NTE2"u8.ToArray())));
    }

    [Fact]
    public async Task DecryptString_RejectsInvalidBase64AndMalformedLegacyData()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await Assert.ThrowsAsync<FormatException>(() => ThingData.Decrypt("%%%"));
        await Assert.ThrowsAsync<CryptographicException>(
            () => ThingData.Decrypt(new MemoryStream([1, 2, 3, 4, 5])));
    }

    [Fact]
    public async Task StreamEncryption_RejectsNullStreams()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThingData.Encrypt((Stream)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThingData.Decrypt((Stream)null!));
    }

    [Fact]
    public async Task EncryptionRequiresAnUnlockedSession()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingData.LockSession();

        Assert.False(ThingData.IsSessionUnlocked);
        Assert.Null(environment.Root.Content);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ThingData.Encrypt(new MemoryStream([1])));
    }

    [Fact]
    public async Task PasswordVerification_RejectsWrongPasswordAndRestoresContentWithCorrectPassword()
    {
        const string password = "right password";
        await using var environment = await TestEnvironment.CreateAsync(password);
        environment.Root.Content!.Add(new ThingObjectLink(42, "answer", FileType.text, 12));
        await ThingData.SaveRootAsync();
        string encryptedContent = environment.Root.ContentEncrypted;

        ThingData.LockSession();
        Assert.False(await ThingData.AttemptDecrypt("wrong password"));
        Assert.False(ThingData.IsSessionUnlocked);
        Assert.True(await ThingData.AttemptDecrypt(password));
        Assert.True(ThingData.IsSessionUnlocked);
        Assert.Contains(environment.Root.Content!, link => link.ID == 42 && link.Name == "answer");
        Assert.NotEmpty(encryptedContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task StringEncryption_RejectsMissingValues(string? value)
    {
        await using var environment = await TestEnvironment.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThingData.Encrypt(value!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThingData.Decrypt(value!));
    }

    [Fact]
    public async Task PasswordVerification_RejectsMalformedEncryptedRootContent()
    {
        await using var environment = await TestEnvironment.CreateAsync();
        ThingData.LockSession();
        environment.Root.ContentEncrypted = "not base64";

        Assert.False(await ThingData.AttemptDecrypt(DefaultPassword));
        Assert.False(ThingData.IsSessionUnlocked);
    }

    private sealed class NonSeekableReadStream(byte[] content) : MemoryStream(content, writable: false)
    {
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException();
        }
    }
}
