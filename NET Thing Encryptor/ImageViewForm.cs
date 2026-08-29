using ImageMagick;
using System.ComponentModel;
using System.Diagnostics;

namespace NET_Thing_Encryptor
{
    public partial class ImageViewForm : Form
    {
        private const long DecodedBitmapBudget = 256L * 1024 * 1024;

        private sealed class ImageCacheEntry(
            CancellationTokenSource cancellation,
            Task<Image?> loadTask)
        {
            public CancellationTokenSource Cancellation { get; } = cancellation;
            public Task<Image?> LoadTask { get; } = loadTask;
        }

        private readonly List<ThingObjectLink> _images = [];
        private readonly Dictionary<int, ImageCacheEntry> _imageCache = [];
        private readonly object _cacheLock = new();
        private readonly SemaphoreSlim _imageLoadGate = new(1, 1);
        private readonly int _previousBufferCount;
        private readonly int _nextBufferCount;
        private readonly Size _maximumDecodedImageSize;
        private readonly System.Windows.Forms.Timer _autoplayTimer = new();
        private readonly bool _loopOnAutoplay;

        private int _requestedIndex = -1;
        private ulong? _displayedImageId;
        private long _navigationVersion;
        private bool _isClosing;
        private bool _autoplayEnabled;
        private bool _autoplayAdvancing;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Index { get; private set; } = -1;

        public ImageViewForm(ThingFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            KeyPreview = true;
            InitializeComponent();
            ImageMemoryManager.Configure();

            _previousBufferCount = ThingData.Root?.ImageViewerPreviousBufferCount ?? 1;
            _nextBufferCount = ThingData.Root?.ImageViewerNextBufferCount ?? 2;
            int autoplayIntervalSeconds = ThingData.Root?.ImageAutoplayIntervalSeconds ?? 5;
            _loopOnAutoplay = ThingData.Root?.LoopOnAutoplay ?? false;
            _autoplayTimer.Interval = checked(autoplayIntervalSeconds * 1000);
            _autoplayTimer.Tick += AutoplayTimer_Tick;
            AppTheme.Apply(contextMenuImage, ThingData.Root?.DarkMode ?? true);
            Rectangle screenBounds = Screen.FromControl(this).Bounds;
            _maximumDecodedImageSize = CalculateMaximumDecodedImageSize(
                screenBounds.Size,
                _previousBufferCount + _nextBufferCount + 2);

            _ = InitializeViewerSafelyAsync(file);
        }

        private static Size CalculateMaximumDecodedImageSize(
            Size screenSize,
            int residentImageCopies)
        {
            int targetWidth = Math.Max(1280, screenSize.Width);
            int targetHeight = Math.Max(720, screenSize.Height);
            long bytesPerImage = DecodedBitmapBudget / Math.Max(1, residentImageCopies);
            double availablePixels = bytesPerImage / 4d;
            double requestedPixels = (double)targetWidth * targetHeight;
            if (requestedPixels <= availablePixels)
                return new Size(targetWidth, targetHeight);

            double scale = Math.Sqrt(availablePixels / requestedPixels);
            return new Size(
                Math.Max(1280, (int)Math.Floor(targetWidth * scale)),
                Math.Max(720, (int)Math.Floor(targetHeight * scale)));
        }

        private async Task InitializeViewerSafelyAsync(ThingFile file)
        {
            try
            {
                await InitializeViewerAsync(file);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not initialize the image viewer: {ex}");
                if (!_isClosing)
                {
                    MessageBox.Show(
                        $"The image viewer could not be initialized: {ex.Message}",
                        "Image viewer error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    Close();
                }
            }
        }

        private async Task InitializeViewerAsync(ThingFile file)
        {
            string fileName = file.Name;
            ulong fileID = file.ID;
            ulong parentID = file.ParentID;
            FileType fileType = file.Type;
            long releasedBytes = file.Content?.LongLength ?? 0;
            file.ReleaseContent();
            MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);

            Debug.WriteLine(
                $"Opening ImageViewForm for file {fileName} (ID {fileID}) with ParentID {parentID}");

            if (fileType != FileType.image)
            {
                MessageBox.Show(
                    "The provided file is not an image.",
                    "Aborting",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
                return;
            }

            List<ThingObjectLink> content = await ThingData.LoadFolderContent(parentID);
            if (_isClosing)
                return;

            _images.AddRange(content.Where(x => x.Type == FileType.image));
            int selectedIndex = _images.FindIndex(x => x.ID == fileID);
            if (selectedIndex < 0)
            {
                MessageBox.Show(
                    "The selected image is no longer part of its parent folder.",
                    "Image not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
                return;
            }

            Debug.WriteLine(
                $"Found {_images.Count} images; buffering {_previousBufferCount} before and {_nextBufferCount} after.");
            await SwitchImageAsync(selectedIndex);
        }

        private async Task NavigateByAsync(int offset)
        {
            int baseIndex = _requestedIndex >= 0 ? _requestedIndex : Index;
            await SwitchImageAsync(baseIndex + offset);
            RestartAutoplayCountdown();
        }

        internal static int RandomiseOrder(
            IList<ThingObjectLink> images,
            int selectedIndex,
            bool includeSelectedImage,
            Random? random = null)
        {
            ArgumentNullException.ThrowIfNull(images);
            if (selectedIndex < 0 || selectedIndex >= images.Count)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            random ??= Random.Shared;
            ThingObjectLink selectedImage = images[selectedIndex];
            if (!includeSelectedImage)
            {
                List<ThingObjectLink> remainingImages = images
                    .Where((_, index) => index != selectedIndex)
                    .ToList();
                Shuffle(remainingImages, random);
                images.Clear();
                images.Add(selectedImage);
                foreach (ThingObjectLink image in remainingImages)
                    images.Add(image);
                return 0;
            }

            Shuffle(images, random);
            // The current bitmap stays visible as a preview, outside the new sequence.
            // Both manual navigation and autoplay must next visit index 0, not follow
            // the displayed image to its random position and skip the preceding images.
            return -1;
        }

        private static void Shuffle(IList<ThingObjectLink> images, Random random)
        {
            for (int index = images.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (images[index], images[swapIndex]) = (images[swapIndex], images[index]);
            }
        }

        private async Task SwitchImageAsync(int index)
        {
            if (_isClosing || index < 0 || index >= _images.Count)
                return;

            _requestedIndex = index;
            long navigationVersion = Interlocked.Increment(ref _navigationVersion);
            textBoxIndex.Text = $"{index + 1}/{_images.Count}";

            MaintainCacheWindow(index);
            if (!IsCacheEntryCompleted(index))
                ShowLoadingImage();

            Image? displayImage = await GetDisplayImageAsync(index);
            if (_isClosing || navigationVersion != Volatile.Read(ref _navigationVersion))
            {
                DisposeImage(displayImage);
                return;
            }

            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            ReplaceDisplayedImage(displayImage ?? CloneErrorImage());
            Index = index;
            _displayedImageId = _images[index].ID;
            textBoxIndex.Text = $"{Index + 1}/{_images.Count}";
            Debug.WriteLine($"Displayed image {Index + 1}/{_images.Count}.");
        }

        private void MaintainCacheWindow(int centerIndex)
        {
            List<int> loadOrder = [centerIndex];
            for (int offset = 1; offset <= _nextBufferCount; offset++)
                loadOrder.Add(centerIndex + offset);
            for (int offset = 1; offset <= _previousBufferCount; offset++)
                loadOrder.Add(centerIndex - offset);

            HashSet<int> desiredIndices = loadOrder
                .Where(index => index >= 0 && index < _images.Count)
                .ToHashSet();
            List<ImageCacheEntry> evictedEntries = [];

            lock (_cacheLock)
            {
                foreach (int index in loadOrder)
                {
                    if (!desiredIndices.Contains(index) || _imageCache.ContainsKey(index))
                        continue;

                    CancellationTokenSource cancellation = new();
                    ulong imageID = _images[index].ID;
                    _imageCache[index] = new ImageCacheEntry(
                        cancellation,
                        LoadImageAsync(imageID, cancellation.Token));
                }

                foreach (int staleIndex in _imageCache.Keys
                             .Where(index => !desiredIndices.Contains(index))
                             .ToList())
                {
                    evictedEntries.Add(_imageCache[staleIndex]);
                    _imageCache.Remove(staleIndex);
                }
            }

            foreach (ImageCacheEntry entry in evictedEntries)
                DisposeCacheEntry(entry);
        }

        private bool IsCacheEntryCompleted(int index)
        {
            lock (_cacheLock)
            {
                return _imageCache.TryGetValue(index, out ImageCacheEntry? entry) &&
                       entry.LoadTask.IsCompleted;
            }
        }

        private async Task<Image?> GetDisplayImageAsync(int index)
        {
            ImageCacheEntry? entry;
            lock (_cacheLock)
                _imageCache.TryGetValue(index, out entry);

            if (entry is null)
                return null;

            Image? cachedImage = await entry.LoadTask;
            if (cachedImage is null)
                return null;

            lock (_cacheLock)
            {
                if (_isClosing ||
                    !_imageCache.TryGetValue(index, out ImageCacheEntry? currentEntry) ||
                    !ReferenceEquals(entry, currentEntry))
                {
                    return null;
                }

                // The cache owns its bitmap. PictureBox receives a separate instance
                // so rapid navigation can never reassign an image that was just disposed.
                return CloneDisplayImage(cachedImage);
            }
        }

        private async Task<Image?> LoadImageAsync(ulong imageID, CancellationToken cancellationToken)
        {
            bool gateEntered = false;
            try
            {
                await _imageLoadGate.WaitAsync(cancellationToken);
                gateEntered = true;

                ThingFile? imageFile = await ThingData.LoadFileAsync<ThingFile>(imageID);
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(imageFile);

                if (imageFile.Content is null ||
                    imageFile.MD5Hash != ThingData.GetMD5Hash(imageFile.Content))
                {
                    throw new FileFormatException("The image is empty or failed its integrity check.");
                }

                byte[] imageData = imageFile.Content;
                string extension = imageFile.Extension;
                imageFile.ReleaseContent();

                return await Task.Run(() =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (IsAnimatedGif(extension, imageData))
                        {
                            MemoryStream gifStream = new(imageData, writable: false);
                            Image gif = Image.FromStream(gifStream);
                            gif.Tag = gifStream;
                            cancellationToken.ThrowIfCancellationRequested();
                            return gif;
                        }

                        using var image = new MagickImage(imageData);
                        if (image.ColorSpace != ColorSpace.sRGB)
                            image.TransformColorSpace(ColorProfiles.SRGB);

                        var targetGeometry = new MagickGeometry(
                            (uint)_maximumDecodedImageSize.Width,
                            (uint)_maximumDecodedImageSize.Height)
                        {
                            Greater = true
                        };
                        image.Resize(targetGeometry);
                        image.Strip();

                        using var stream = new MemoryStream();
                        image.Write(
                            stream,
                            image.HasAlpha ? MagickFormat.Png32 : MagickFormat.Png24);
                        stream.Position = 0;
                        using var temporaryBitmap = new Bitmap(stream);
                        cancellationToken.ThrowIfCancellationRequested();
                        return (Bitmap)temporaryBitmap.Clone();
                    }
                    finally
                    {
                        ImageMemoryManager.Trim();
                        MemoryMaintenance.NotifyLargeBufferReleased(imageData.LongLength);
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load image {imageID}: {ex}");
                return null;
            }
            finally
            {
                if (gateEntered)
                    _imageLoadGate.Release();
            }
        }

        private static bool IsAnimatedGif(string extension, byte[] imageData)
        {
            return string.Equals(extension, "gif", StringComparison.OrdinalIgnoreCase) &&
                   imageData.Length >= 6 &&
                   imageData[0] == 'G' &&
                   imageData[1] == 'I' &&
                   imageData[2] == 'F' &&
                   imageData[3] == '8' &&
                   imageData[5] == 'a';
        }

        private void ShowLoadingImage()
        {
            pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            ReplaceDisplayedImage(CloneImage(pictureBox.InitialImage));
        }

        private Image? CloneErrorImage()
        {
            return CloneImage(pictureBox.ErrorImage);
        }

        private static Image? CloneImage(Image? image)
        {
            return image is null ? null : CloneDisplayImage(image);
        }

        private void ReplaceDisplayedImage(Image? replacement)
        {
            Image? previous = pictureBox.Image;
            pictureBox.Image = null;
            DisposeImage(previous);

            if (_isClosing)
                DisposeImage(replacement);
            else
                pictureBox.Image = replacement;
        }

        private static Image CloneDisplayImage(Image image)
        {
            if (image.Tag is MemoryStream gifStream)
            {
                MemoryStream cloneStream = new(gifStream.ToArray(), writable: false);
                Image clone = Image.FromStream(cloneStream);
                clone.Tag = cloneStream;
                return clone;
            }

            return (Image)image.Clone();
        }

        private static void DisposeImage(Image? image)
        {
            if (image is null)
                return;

            IDisposable? backingStream = image.Tag as IDisposable;
            image.Tag = null;
            image.Dispose();
            backingStream?.Dispose();
        }

        private static void DisposeCacheEntry(ImageCacheEntry entry)
        {
            entry.Cancellation.Cancel();
            if (entry.LoadTask.IsCompleted)
            {
                if (entry.LoadTask.Status == TaskStatus.RanToCompletion)
                    DisposeImage(entry.LoadTask.Result);
                entry.Cancellation.Dispose();
                return;
            }

            _ = entry.LoadTask.ContinueWith(
                task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                        DisposeImage(task.Result);
                    entry.Cancellation.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private async void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            await NavigateByAsync(ClientSize.Width / 2 < e.X ? 1 : -1);
        }

        private void textBoxIndex_Leave(object sender, EventArgs e)
        {
            textBoxIndex.Text = $"{Index + 1}/{_images.Count}";
        }

        private async void textBoxIndex_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            // Suppress Enter immediately so neither the form nor Windows emits a beep
            // while the asynchronous image switch is still running.
            e.Handled = true;
            e.SuppressKeyPress = true;

            string enteredIndex = textBoxIndex.Text.Split('/', 2)[0].Trim();
            if (int.TryParse(enteredIndex, out int requestedIndex) &&
                requestedIndex >= 1 &&
                requestedIndex <= _images.Count)
            {
                await SwitchImageAsync(requestedIndex - 1);
                RestartAutoplayCountdown();
                ActiveControl = null;
            }
            else
            {
                textBoxIndex.Text = $"{Index + 1}/{_images.Count}";
                textBoxIndex.SelectAll();
            }
        }

        private async void ImageViewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Space or Keys.D)
                await NavigateByAsync(1);
            else if (e.KeyCode is Keys.Back or Keys.A)
                await NavigateByAsync(-1);
            else if (e.KeyCode == Keys.Escape)
                Close();
        }

        private void contextMenuImage_Opening(object sender, CancelEventArgs e)
        {
            randomiseOrderToolStripMenuItem.Enabled = _images.Count > 1 && _displayedImageId.HasValue;
            autoplayToolStripMenuItem.Enabled = _images.Count > 1;
            autoplayToolStripMenuItem.Text = _autoplayEnabled
                ? "Stop Autoplay"
                : "Start Autoplay";
        }

        private void randomiseOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isClosing || _images.Count <= 1)
                return;

            // Index can be -1 after randomising, or an unrelated navigation may still
            // be loading. Identify the displayed image independently of the cursor.
            int selectedIndex = _images.FindIndex(image => image.ID == _displayedImageId);
            if (selectedIndex < 0)
                return;

            _autoplayTimer.Stop();
            bool includeSelectedImage = ThingData.Root?.RandomiseSelectedImage ?? false;
            Interlocked.Increment(ref _navigationVersion);
            ClearImageCache();
            Index = RandomiseOrder(
                _images,
                selectedIndex,
                includeSelectedImage);
            _requestedIndex = -1;
            textBoxIndex.Text = $"{Index + 1}/{_images.Count}";
            MaintainCacheWindow(0);
            RestartAutoplayCountdown();
        }

        private void autoplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_autoplayEnabled)
                StopAutoplay();
            else
                StartAutoplay();
        }

        private void StartAutoplay()
        {
            if (_isClosing || _images.Count <= 1 ||
                (!_loopOnAutoplay && Index >= _images.Count - 1))
            {
                StopAutoplay();
                return;
            }

            _autoplayEnabled = true;
            autoplayToolStripMenuItem.Text = "Stop Autoplay";
            _autoplayTimer.Start();
        }

        private void StopAutoplay()
        {
            _autoplayEnabled = false;
            _autoplayTimer.Stop();
            autoplayToolStripMenuItem.Text = "Start Autoplay";
        }

        private void RestartAutoplayCountdown()
        {
            if (!_autoplayEnabled || _isClosing)
                return;

            if (!_loopOnAutoplay && Index >= _images.Count - 1)
            {
                StopAutoplay();
                return;
            }

            _autoplayTimer.Stop();
            _autoplayTimer.Start();
        }

        private async void AutoplayTimer_Tick(object? sender, EventArgs e)
        {
            if (!_autoplayEnabled || _autoplayAdvancing || _isClosing)
                return;

            _autoplayAdvancing = true;
            _autoplayTimer.Stop();
            try
            {
                int currentIndex = _requestedIndex >= 0 ? _requestedIndex : Index;
                int nextIndex = currentIndex + 1;
                if (nextIndex >= _images.Count)
                {
                    if (!_loopOnAutoplay)
                    {
                        StopAutoplay();
                        return;
                    }
                    nextIndex = 0;
                }

                await SwitchImageAsync(nextIndex);
                if (!_loopOnAutoplay && Index >= _images.Count - 1)
                    StopAutoplay();
            }
            finally
            {
                _autoplayAdvancing = false;
                if (_autoplayEnabled && !_isClosing)
                    _autoplayTimer.Start();
            }
        }

        private void ClearImageCache()
        {
            List<ImageCacheEntry> entries;
            lock (_cacheLock)
            {
                entries = _imageCache.Values.ToList();
                _imageCache.Clear();
            }

            foreach (ImageCacheEntry entry in entries)
                DisposeCacheEntry(entry);
        }

        private void ImageViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            StopAutoplay();
            _autoplayTimer.Tick -= AutoplayTimer_Tick;
            _autoplayTimer.Dispose();
            Interlocked.Increment(ref _navigationVersion);
            ReplaceDisplayedImage(null);
            ClearImageCache();

            _images.Clear();
            ImageMemoryManager.Trim();
        }

        private void textBoxIndex_Enter(object sender, EventArgs e)
        {
            BeginInvoke(textBoxIndex.SelectAll);
        }
    }
}
