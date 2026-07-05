using ImageMagick;
using System.ComponentModel;
using System.Diagnostics;

namespace NET_Thing_Encryptor
{
    public partial class ImageViewForm : Form
    {
        private sealed class ImageCacheEntry(
            CancellationTokenSource cancellation,
            Task<Bitmap?> loadTask)
        {
            public CancellationTokenSource Cancellation { get; } = cancellation;
            public Task<Bitmap?> LoadTask { get; } = loadTask;
        }

        private readonly List<ThingObjectLink> _images = [];
        private readonly Dictionary<int, ImageCacheEntry> _imageCache = [];
        private readonly object _cacheLock = new();
        private readonly SemaphoreSlim _imageLoadGate = new(2, 2);
        private readonly int _previousBufferCount;
        private readonly int _nextBufferCount;

        private int _requestedIndex = -1;
        private long _navigationVersion;
        private bool _isClosing;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Index { get; private set; }

        public ImageViewForm(ThingFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            KeyPreview = true;
            InitializeComponent();

            _previousBufferCount = ThingData.Root?.ImageViewerPreviousBufferCount ?? 1;
            _nextBufferCount = ThingData.Root?.ImageViewerNextBufferCount ?? 2;

            _ = InitializeViewerSafelyAsync(file);
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
            Debug.WriteLine(
                $"Opening ImageViewForm for file {file.Name} (ID {file.ID}) with ParentID {file.ParentID}");

            if (file.Type != FileType.image)
            {
                MessageBox.Show(
                    "The provided file is not an image.",
                    "Aborting",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
                return;
            }

            List<ThingObjectLink> content = await ThingData.LoadFolderContent(file.ParentID);
            if (_isClosing)
                return;

            _images.AddRange(content.Where(x => x.Type == FileType.image));
            int selectedIndex = _images.FindIndex(x => x.ID == file.ID);
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

        private Task NavigateByAsync(int offset)
        {
            int baseIndex = _requestedIndex >= 0 ? _requestedIndex : Index;
            return SwitchImageAsync(baseIndex + offset);
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

            Bitmap? displayImage = await GetDisplayImageAsync(index);
            if (_isClosing || navigationVersion != Volatile.Read(ref _navigationVersion))
            {
                displayImage?.Dispose();
                return;
            }

            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            ReplaceDisplayedImage(displayImage ?? CloneErrorImage());
            Index = index;
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
                        LoadBitmapAsync(imageID, cancellation.Token));
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

        private async Task<Bitmap?> GetDisplayImageAsync(int index)
        {
            ImageCacheEntry? entry;
            lock (_cacheLock)
                _imageCache.TryGetValue(index, out entry);

            if (entry is null)
                return null;

            Bitmap? cachedImage = await entry.LoadTask;
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
                return (Bitmap)cachedImage.Clone();
            }
        }

        private async Task<Bitmap?> LoadBitmapAsync(ulong imageID, CancellationToken cancellationToken)
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
                imageFile.Clear();

                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var image = new MagickImage(imageData);
                    if (image.ColorSpace != ColorSpace.sRGB)
                        image.TransformColorSpace(ColorProfiles.SRGB);

                    using var stream = new MemoryStream();
                    image.Write(stream, MagickFormat.Png32);
                    stream.Position = 0;
                    using var temporaryBitmap = new Bitmap(stream);
                    cancellationToken.ThrowIfCancellationRequested();
                    return (Bitmap)temporaryBitmap.Clone();
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
            return image is null ? null : (Image)image.Clone();
        }

        private void ReplaceDisplayedImage(Image? replacement)
        {
            Image? previous = pictureBox.Image;
            pictureBox.Image = null;
            previous?.Dispose();

            if (_isClosing)
                replacement?.Dispose();
            else
                pictureBox.Image = replacement;
        }

        private static void DisposeCacheEntry(ImageCacheEntry entry)
        {
            entry.Cancellation.Cancel();
            if (entry.LoadTask.IsCompleted)
            {
                if (entry.LoadTask.Status == TaskStatus.RanToCompletion)
                    entry.LoadTask.Result?.Dispose();
                entry.Cancellation.Dispose();
                return;
            }

            _ = entry.LoadTask.ContinueWith(
                task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                        task.Result?.Dispose();
                    entry.Cancellation.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private async void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
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

        private void ImageViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            Interlocked.Increment(ref _navigationVersion);
            ReplaceDisplayedImage(null);

            List<ImageCacheEntry> entries;
            lock (_cacheLock)
            {
                entries = _imageCache.Values.ToList();
                _imageCache.Clear();
            }

            foreach (ImageCacheEntry entry in entries)
                DisposeCacheEntry(entry);

            _images.Clear();
        }

        private void textBoxIndex_Enter(object sender, EventArgs e)
        {
            BeginInvoke(textBoxIndex.SelectAll);
        }
    }
}
