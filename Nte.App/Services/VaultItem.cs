using NET_Thing_Encryptor;

namespace Nte.App.Services;

public sealed record VaultItem(
    ulong Id,
    string Name,
    FileType Type,
    long Size,
    string Extension,
    DateOnly CreatedAt)
{
    public bool IsFolder => Type == FileType.folder;
}
