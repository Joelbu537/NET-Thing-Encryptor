using System.Security.Cryptography;

namespace NET_Thing_Encryptor;

public sealed class VaultSession
{
    private byte[]? _encryptionKey;
    private byte[]? _legacyIv;
    private int _saving;

    internal VaultSession()
    {
    }

    public ThingRoot? Root { get; internal set; }
    public int Saving => Volatile.Read(ref _saving);
    public bool IsUnlocked => Volatile.Read(ref _encryptionKey) is not null;

    internal void BeginSaving() => Interlocked.Increment(ref _saving);

    internal void EndSaving() => Interlocked.Decrement(ref _saving);

    internal byte[] GetEncryptionKey()
    {
        byte[]? key = Volatile.Read(ref _encryptionKey);
        return key ?? throw new InvalidOperationException("An encryption key has not been established.");
    }

    internal byte[] GetLegacyIv()
    {
        byte[]? iv = Volatile.Read(ref _legacyIv);
        return iv ?? throw new InvalidOperationException("A legacy IV has not been established.");
    }

    internal void SetEncryptionMaterial(byte[] key, byte[] legacyIv)
    {
        byte[]? oldKey = Interlocked.Exchange(ref _encryptionKey, (byte[])key.Clone());
        byte[]? oldIv = Interlocked.Exchange(ref _legacyIv, (byte[])legacyIv.Clone());
        Zero(oldKey);
        Zero(oldIv);
    }

    public void Lock()
    {
        byte[]? oldKey = Interlocked.Exchange(ref _encryptionKey, null);
        byte[]? oldIv = Interlocked.Exchange(ref _legacyIv, null);
        Zero(oldKey);
        Zero(oldIv);
        if (Root is not null)
            Root.Content = null;
    }

    private static void Zero(byte[]? value)
    {
        if (value is not null)
            CryptographicOperations.ZeroMemory(value);
    }
}
