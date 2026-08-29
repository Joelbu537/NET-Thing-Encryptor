namespace NET_Thing_Encryptor;

public static partial class ThingData
{
    /// <summary>
    /// Imports one external document from a caller-owned stream. This is suitable for
    /// Android content-resolver streams as well as ordinary desktop files.
    /// </summary>
    public static async Task<ThingFile> ImportFileAsync(
        Stream source,
        string fileName,
        ulong parentId,
        string? objectName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (parentId == 0)
            throw new ArgumentException("Files cannot be placed directly in the root.", nameof(parentId));

        string extension = Path.GetExtension(fileName).TrimStart('.');
        string name = string.IsNullOrWhiteSpace(objectName)
            ? Path.GetFileNameWithoutExtension(fileName)
            : objectName;
        if (string.IsNullOrWhiteSpace(name))
            name = fileName;

        using var content = new MemoryStream();
        await source.CopyToAsync(content, cancellationToken).ConfigureAwait(false);

        var file = new ThingFile(name, content.ToArray())
        {
            Extension = extension,
            Type = FileCategories.GetFileType(fileName)
        };

        try
        {
            await MoveFileToFolderAsync(file, parentId).ConfigureAwait(false);
            return file;
        }
        catch
        {
            file.ReleaseContent();
            throw;
        }
    }

    /// <summary>Writes the decrypted bytes of a vault file to a caller-owned stream.</summary>
    public static async Task ExportFileAsync(
        ulong id,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ThingFile file = await LoadFileAsync<ThingFile>(id).ConfigureAwait(false)
            ?? throw new FileNotFoundException("The file could not be loaded.");

        try
        {
            byte[] content = file.Content
                ?? throw new InvalidDataException("The stored file has no content.");
            await destination.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            long releasedBytes = file.Content?.LongLength ?? 0;
            file.ReleaseContent();
            MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
        }
    }
}
