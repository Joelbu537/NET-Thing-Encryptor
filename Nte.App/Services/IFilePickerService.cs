namespace Nte.App.Services;

public interface IReadableExternalFile
{
    string Name { get; }
    Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default);
}

public interface IWritableExternalFile
{
    string Name { get; }
    Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default);
}

public interface IWritableExternalFolder
{
    string Name { get; }
    Task<IWritableExternalFile> CreateUniqueFileAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);
    Task<IWritableExternalFolder> CreateUniqueFolderAsync(
        string suggestedFolderName,
        CancellationToken cancellationToken = default);
}

public interface IFilePickerService
{
    Task<IReadOnlyList<IReadableExternalFile>> PickDocumentsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadableExternalFile?> PickVaultArchiveAsync(
        CancellationToken cancellationToken = default);
    Task<IWritableExternalFile?> PickDocumentExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);
    Task<IWritableExternalFolder?> PickExportFolderAsync(
        CancellationToken cancellationToken = default);
    Task<IWritableExternalFile?> PickVaultArchiveExportAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);
}

internal static class ExternalExportNamePolicy
{
    private static readonly HashSet<string> WindowsReservedNames = new(
        [
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        ],
        StringComparer.OrdinalIgnoreCase);

    public static string CreateUnique(
        string suggestedName,
        IEnumerable<string> existingNames,
        bool preserveExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedName);
        ArgumentNullException.ThrowIfNull(existingNames);

        string normalized = Normalize(suggestedName);
        var existing = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(normalized))
            return normalized;

        string extension = string.Empty;
        string baseName = normalized;
        if (preserveExtension)
        {
            int extensionStart = normalized.LastIndexOf('.');
            if (extensionStart > 0)
            {
                baseName = normalized[..extensionStart];
                extension = normalized[extensionStart..];
            }
        }

        int suffix = 2;
        string candidate;
        do
        {
            candidate = $"{baseName} ({suffix}){extension}";
            suffix++;
        }
        while (existing.Contains(candidate));
        return candidate;
    }

    public static string Normalize(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        char[] characters = name
            .Select(character => character < 32 || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*'
                ? '_'
                : character)
            .ToArray();
        string normalized = new string(characters).Trim().TrimEnd('.', ' ');
        if (normalized.Length == 0 || normalized is "." or "..")
            normalized = "Export";

        string stem = normalized.Split('.', 2)[0];
        if (WindowsReservedNames.Contains(stem))
            normalized = $"_{normalized}";
        return normalized;
    }
}
