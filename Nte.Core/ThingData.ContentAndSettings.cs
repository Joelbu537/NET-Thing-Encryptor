namespace NET_Thing_Encryptor;

public sealed record ThingRootSettings(
    bool DarkMode,
    int AutoLockMinutes,
    int ImageViewerPreviousBufferCount,
    int ImageViewerNextBufferCount,
    bool RandomiseSelectedImage,
    int ImageAutoplayIntervalSeconds,
    bool LoopOnAutoplay);

public static partial class ThingData
{
    public static ThingRootSettings GetRootSettings()
    {
        ThingRoot root = RequireRoot();
        return new ThingRootSettings(
            root.DarkMode,
            root.AutoLockMinutes,
            root.ImageViewerPreviousBufferCount,
            root.ImageViewerNextBufferCount,
            root.RandomiseSelectedImage,
            root.ImageAutoplayIntervalSeconds,
            root.LoopOnAutoplay);
    }

    public static Task UpdateRootSettingsAsync(
        ThingRootSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        return RunMutationAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThingRoot root = RequireRoot();
            root.DarkMode = settings.DarkMode;
            root.AutoLockMinutes = settings.AutoLockMinutes;
            root.ImageViewerPreviousBufferCount = settings.ImageViewerPreviousBufferCount;
            root.ImageViewerNextBufferCount = settings.ImageViewerNextBufferCount;
            root.RandomiseSelectedImage = settings.RandomiseSelectedImage;
            root.ImageAutoplayIntervalSeconds = settings.ImageAutoplayIntervalSeconds;
            root.LoopOnAutoplay = settings.LoopOnAutoplay;
            await SaveRootAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }, () => Task.FromResult<IReadOnlyCollection<ulong>>([0]));
    }

    public static Task ReplaceFileContentAsync(
        ulong id,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        if (id == 0)
            throw new ArgumentException("The root object has no file content.", nameof(id));
        cancellationToken.ThrowIfCancellationRequested();

        return RunMutationAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThingFile file = await LoadFileAsync<ThingFile>(id).ConfigureAwait(false)
                ?? throw new FileNotFoundException("The file could not be loaded.");
            byte[] replacement = content.ToArray();
            try
            {
                file.Content = replacement;
                await SaveFileAsync(file).ConfigureAwait(false);

                ThingObjectLink link;
                if (file.ParentID == 0)
                {
                    ThingRoot root = RequireRoot();
                    link = root.Content?.FirstOrDefault(item => item.ID == id)
                        ?? throw new InvalidDataException("The root does not reference the file.");
                    link.Size = replacement.LongLength;
                    await SaveRootAsync().ConfigureAwait(false);
                }
                else
                {
                    ThingFolder parent = await LoadFileAsync<ThingFolder>(file.ParentID)
                        .ConfigureAwait(false)
                        ?? throw new FileNotFoundException("The parent folder could not be loaded.");
                    link = parent.Content.FirstOrDefault(item => item.ID == id)
                        ?? throw new InvalidDataException("The parent folder does not reference the file.");
                    link.Size = replacement.LongLength;
                    await SaveFileAsync(parent).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                file.ReleaseContent();
                MemoryMaintenance.NotifyLargeBufferReleased(replacement.LongLength);
            }
        }, () => CollectObjectAndParentBackupIdsAsync(id));
    }
}
