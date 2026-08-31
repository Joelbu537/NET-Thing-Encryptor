using NET_Thing_Encryptor;

namespace Nte.App.Services;

public sealed record VaultSearchCriteria(
    string Name = "",
    FileType? Type = null,
    string Extension = "",
    long? MinimumSize = null,
    long? MaximumSize = null,
    DateOnly? CreatedFrom = null,
    DateOnly? CreatedTo = null);

public sealed record VaultFolderTarget(ulong Id, string Path);

public sealed record VaultFileContent(
    ulong Id,
    string Name,
    FileType Type,
    string Extension,
    byte[] Content);

public sealed record VaultImageReference(
    ulong Id,
    string Name,
    string Extension);

public sealed record VaultImageSeriesOptions(
    IReadOnlyList<VaultImageReference> Images,
    bool IncludeSelectedImageWhenRandomising,
    int AutoplayIntervalSeconds,
    bool LoopAutoplay);

public sealed record VaultPreferences(
    bool DarkMode,
    int AutoLockMinutes,
    int PreviousImageBufferCount,
    int NextImageBufferCount,
    bool IncludeSelectedImageWhenRandomising,
    int ImageAutoplayIntervalSeconds,
    bool LoopImageAutoplay);
