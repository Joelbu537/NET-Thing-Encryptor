namespace NET_Thing_Encryptor;

public sealed record GlobalFileSearchCriteria(
    string Name = "",
    FileType? Type = null,
    string Extension = "",
    long? MinimumSize = null,
    long? MaximumSize = null,
    DateOnly? CreatedFrom = null,
    DateOnly? CreatedTo = null);

public sealed record GlobalFileSearchResult(
    ulong ID,
    string Name,
    FileType Type,
    string Extension,
    long Size,
    DateOnly CreatedAt,
    string FolderPath);

public static class GlobalFileSearch
{
    public static async Task<IReadOnlyList<GlobalFileSearchResult>> SearchAsync(
        GlobalFileSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ThingRoot root = ThingData.Root
            ?? throw new InvalidOperationException("The root data has not been loaded.");

        string requestedExtension = NormalizeExtension(criteria.Extension);
        List<GlobalFileSearchResult> results = [];
        HashSet<ulong> visitedFolders = [];
        await SearchFolderAsync(
            root.Content ?? [],
            "Root",
            criteria,
            requestedExtension,
            results,
            visitedFolders,
            cancellationToken);

        return results
            .OrderBy(result => result.Name, new NaturalStringComparer())
            .ThenBy(result => result.FolderPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task SearchFolderAsync(
        IEnumerable<ThingObjectLink> content,
        string folderPath,
        GlobalFileSearchCriteria criteria,
        string requestedExtension,
        List<GlobalFileSearchResult> results,
        HashSet<ulong> visitedFolders,
        CancellationToken cancellationToken)
    {
        foreach (ThingObjectLink link in content)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (link.Type == FileType.folder)
            {
                if (!visitedFolders.Add(link.ID))
                    continue;

                ThingFolder? folder = await ThingData.LoadFileAsync<ThingFolder>(link.ID);
                if (folder is not null)
                {
                    await SearchFolderAsync(
                        folder.Content,
                        folderPath + "/" + link.Name,
                        criteria,
                        requestedExtension,
                        results,
                        visitedFolders,
                        cancellationToken);
                }
                continue;
            }

            if (!MatchesMetadata(link, criteria))
                continue;

            string extension = NormalizeExtension(link.Extension);
            if (!string.IsNullOrEmpty(requestedExtension) && string.IsNullOrEmpty(extension))
            {
                ThingFile? file = await ThingData.LoadFileAsync<ThingFile>(link.ID);
                if (file is not null)
                {
                    extension = NormalizeExtension(file.Extension);
                    long releasedBytes = file.Content?.LongLength ?? 0;
                    file.ReleaseContent();
                    MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
                }
            }

            if (!string.IsNullOrEmpty(requestedExtension) &&
                !string.Equals(extension, requestedExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new GlobalFileSearchResult(
                link.ID,
                link.Name,
                link.Type,
                extension,
                link.Size,
                link.CreatedAt,
                folderPath));
        }
    }

    private static bool MatchesMetadata(
        ThingObjectLink link,
        GlobalFileSearchCriteria criteria)
    {
        return (string.IsNullOrWhiteSpace(criteria.Name) ||
                link.Name.Contains(criteria.Name.Trim(), StringComparison.OrdinalIgnoreCase)) &&
               (!criteria.Type.HasValue || link.Type == criteria.Type.Value) &&
               (!criteria.MinimumSize.HasValue || link.Size >= criteria.MinimumSize.Value) &&
               (!criteria.MaximumSize.HasValue || link.Size <= criteria.MaximumSize.Value) &&
               (!criteria.CreatedFrom.HasValue || link.CreatedAt >= criteria.CreatedFrom.Value) &&
               (!criteria.CreatedTo.HasValue || link.CreatedAt <= criteria.CreatedTo.Value);
    }

    internal static string NormalizeExtension(string? extension) =>
        (extension ?? string.Empty).Trim().TrimStart('.');
}
