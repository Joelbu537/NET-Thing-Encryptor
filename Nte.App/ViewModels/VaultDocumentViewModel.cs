using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using NET_Thing_Encryptor;
using Nte.App.Services;

namespace Nte.App.ViewModels;

public sealed class VaultDocumentViewModel : ObservableObject, IDisposable
{
    private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _save;
    private readonly Action _close;
    private readonly Action<string> _setStatus;
    private readonly DecodedTextDocument? _textDocument;
    private readonly List<VaultImageReference> _images = [];
    private readonly Func<ulong, CancellationToken, Task<VaultFileContent>>? _loadImage;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Func<byte[], Bitmap?> _decodeImage;
    private readonly bool _includeSelectedImageWhenRandomising;
    private readonly bool _loopAutoplay;
    private readonly TimeSpan _autoplayInterval;
    private IMediaPlaybackSession? _mediaSession;
    private Control? _videoSurface;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private ulong _id;
    private string _name;
    private string _extension;
    private string _text = string.Empty;
    private string _savedText = string.Empty;
    private string _searchText = string.Empty;
    private string _errorMessage = string.Empty;
    private Bitmap? _image;
    private int _imageIndex = -1;
    private bool _isEditing;
    private bool _hasDecodedImage;
    private bool _isBusy;
    private bool _isAutoplayRunning;
    private bool _showDiscardConfirmation;
    private bool _isMediaPlaying;
    private bool _canSeekMedia;
    private bool _isMediaSeeking;
    private bool _isUpdatingMediaState;
    private bool _resumeMediaAfterSeek;
    private bool _mediaStartRequested;
    private double _mediaPositionMilliseconds;
    private double _mediaDurationMilliseconds;
    private readonly bool _showBackButton;
    private bool _disposed;
    private CancellationTokenSource? _saveCancellation;
    private CancellationTokenSource? _autoplayCancellation;

    public VaultDocumentViewModel(
        VaultFileContent file,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> save,
        Action close,
        Action<string> setStatus,
        VaultImageSeriesOptions? imageSeries = null,
        Func<ulong, CancellationToken, Task<VaultFileContent>>? loadImage = null,
        IMediaPlaybackService? mediaPlaybackService = null,
        bool showBackButton = true)
        : this(
            file,
            save,
            close,
            setStatus,
            imageSeries,
            loadImage,
            static (delay, cancellationToken) => Task.Delay(delay, cancellationToken),
            DecodeImage,
            mediaPlaybackService,
            showBackButton)
    {
    }

    internal VaultDocumentViewModel(
        VaultFileContent file,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> save,
        Action close,
        Action<string> setStatus,
        VaultImageSeriesOptions? imageSeries,
        Func<ulong, CancellationToken, Task<VaultFileContent>>? loadImage,
        Func<TimeSpan, CancellationToken, Task> delay,
        Func<byte[], Bitmap?>? decodeImage = null,
        IMediaPlaybackService? mediaPlaybackService = null,
        bool showBackButton = true)
    {
        ArgumentNullException.ThrowIfNull(file);
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _close = close ?? throw new ArgumentNullException(nameof(close));
        _setStatus = setStatus ?? throw new ArgumentNullException(nameof(setStatus));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        _decodeImage = decodeImage ?? DecodeImage;
        _id = file.Id;
        _name = file.Name;
        _extension = file.Extension;
        Type = file.Type;
        _showBackButton = showBackButton;

        if (file.Type == FileType.image && imageSeries is not null && loadImage is not null)
        {
            _images.AddRange(imageSeries.Images
                .Where(image => image.Id != 0)
                .DistinctBy(image => image.Id));
            if (_images.All(image => image.Id != file.Id))
                _images.Insert(0, new VaultImageReference(file.Id, file.Name, file.Extension));
            _imageIndex = _images.FindIndex(image => image.Id == file.Id);
            _loadImage = loadImage;
            _includeSelectedImageWhenRandomising = imageSeries.IncludeSelectedImageWhenRandomising;
            _autoplayInterval = TimeSpan.FromSeconds(Math.Clamp(
                imageSeries.AutoplayIntervalSeconds,
                1,
                ThingRoot.MaximumAutoplayIntervalSeconds));
            _loopAutoplay = imageSeries.LoopAutoplay;
        }
        else
        {
            _autoplayInterval = TimeSpan.FromSeconds(5);
        }

        bool mediaSessionOwnsContent = false;
        try
        {
            if (file.Type == FileType.text)
            {
                _textDocument = TextDocumentCodec.Decode(file.Content);
                _text = _textDocument.Text;
                _savedText = _text;
            }
            else if (file.Type == FileType.image)
            {
                _image = _decodeImage(file.Content);
                _hasDecodedImage = true;
            }
            else if (file.Type is FileType.audio or FileType.video && mediaPlaybackService is not null)
            {
                IMediaPlaybackSession? session = mediaPlaybackService.CreateSession(
                    file.Content,
                    file.Type == FileType.video
                        ? MediaPlaybackKind.Video
                        : MediaPlaybackKind.Audio);
                try
                {
                    _videoSurface = session.Surface;
                    session.StateChanged += MediaSession_StateChanged;
                    session.Failed += MediaSession_Failed;
                    _mediaSession = session;
                    mediaSessionOwnsContent = true;
                    session = null;
                }
                finally
                {
                    session?.Dispose();
                }
            }
            else if (file.Type is FileType.audio or FileType.video)
            {
                ErrorMessage = IsAudio
                    ? "Der Audioplayer ist auf dieser Plattform nicht verfügbar."
                    : "Der Videoplayer ist auf dieser Plattform nicht verfügbar.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Der Inhalt konnte nicht dargestellt werden: {ex.Message}";
        }
        finally
        {
            if (!mediaSessionOwnsContent)
                CryptographicOperations.ZeroMemory(file.Content);
        }

        SaveCommand = new AsyncCommand(SaveAsync, () => IsText && IsDirty && !IsBusy);
        ToggleEditingCommand = new AsyncCommand(ToggleEditingAsync, () => IsText && !IsBusy);
        CloseCommand = new AsyncCommand(RequestCloseAsync, () => !IsBusy);
        DiscardAndCloseCommand = new AsyncCommand(DiscardAndCloseAsync, () => !IsBusy);
        CancelCloseCommand = new AsyncCommand(CancelCloseAsync, () => !IsBusy);
        PreviousImageCommand = new AsyncCommand(
            () => NavigateImageAsync(-1),
            () => IsImageSeries && !IsBusy && _imageIndex > 0);
        NextImageCommand = new AsyncCommand(
            () => NavigateImageAsync(1),
            () => IsImageSeries && !IsBusy && _imageIndex < _images.Count - 1);
        RandomiseImagesCommand = new AsyncCommand(
            RandomiseImagesAsync,
            () => IsImageSeries && !IsBusy);
        ToggleAutoplayCommand = new AsyncCommand(
            ToggleAutoplayAsync,
            CanToggleAutoplay);
        StartMediaCommand = new AsyncCommand(StartMediaAsync, CanStartMedia);
        ToggleMediaPlaybackCommand = new AsyncCommand(
            ToggleMediaPlaybackAsync,
            () => IsMedia && _mediaSession is not null);
        SeekMediaBackwardCommand = new AsyncCommand(
            () => SeekMediaByAsync(TimeSpan.FromSeconds(-10)),
            () => IsMedia && _mediaSession is not null && CanSeekMedia);
        SeekMediaForwardCommand = new AsyncCommand(
            () => SeekMediaByAsync(TimeSpan.FromSeconds(10)),
            () => IsMedia && _mediaSession is not null && CanSeekMedia);
    }

    public ulong Id => _id;
    public string Name => _name;
    public string Extension => _extension;
    public FileType Type { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(Extension)
        ? Name
        : $"{Name}.{Extension.TrimStart('.')}";
    public bool IsText => Type == FileType.text;
    public bool IsImageDocument => Type == FileType.image;
    public bool IsImage => IsImageDocument && _hasDecodedImage;
    public bool IsImageSeries => Type == FileType.image && _loadImage is not null && _images.Count > 1;
    public bool IsVideo => Type == FileType.video;
    public bool IsAudio => Type == FileType.audio;
    public bool IsMedia => Type is FileType.audio or FileType.video;
    public bool IsGeneric => !IsText && !IsImageDocument && !IsMedia;
    public bool ShowBackButton => _showBackButton;
    public string GenericMessage =>
        "Für diesen Dateityp ist keine interne Vorschau verfügbar. Der Inhalt kann sicher exportiert werden.";
    public string ImagePositionText => _images.Count == 0
        ? string.Empty
        : _imageIndex < 0
            ? $"Zufallsfolge bereit · {_images.Count} Bilder"
            : $"{_imageIndex + 1} / {_images.Count}";
    public string AutoplayButtonText => IsAutoplayRunning ? "Autoplay stoppen" : "Autoplay starten";
    public bool HasMediaPlayback => _mediaSession is not null;
    public bool HasVideoSurface => IsVideo && _videoSurface is not null;
    public bool ShowAudioPresentation => IsAudio && HasMediaPlayback;
    public bool ShowMediaPlaceholder => IsMedia && !HasMediaPlayback;
    public string MediaUnavailableMessage => IsAudio
        ? "Der Audioplayer ist auf dieser Plattform nicht verfügbar."
        : "Der Videoplayer ist auf dieser Plattform nicht verfügbar.";
    public Control? VideoSurface => _videoSurface;
    public string MediaPlayPauseSymbol => IsMediaPlaying ? "⏸" : "▶";
    public string MediaPlayPauseToolTip => IsMediaPlaying ? "Pause (Leertaste)" : "Wiedergabe (Leertaste)";
    public double MediaTimelineMaximum => Math.Max(1, MediaDurationMilliseconds);
    public string MediaTimeText =>
        $"{FormatMediaTime(MediaPositionMilliseconds, MediaDurationMilliseconds)} / " +
        FormatMediaTime(MediaDurationMilliseconds, MediaDurationMilliseconds);

    public Bitmap? Image => _image;

    public bool IsMediaPlaying
    {
        get => _isMediaPlaying;
        private set
        {
            if (!SetProperty(ref _isMediaPlaying, value))
                return;
            OnPropertyChanged(nameof(MediaPlayPauseSymbol));
            OnPropertyChanged(nameof(MediaPlayPauseToolTip));
        }
    }

    public bool CanSeekMedia
    {
        get => _canSeekMedia;
        private set
        {
            if (!SetProperty(ref _canSeekMedia, value))
                return;
            SeekMediaBackwardCommand.NotifyCanExecuteChanged();
            SeekMediaForwardCommand.NotifyCanExecuteChanged();
        }
    }

    public double MediaPositionMilliseconds
    {
        get => _mediaPositionMilliseconds;
        set
        {
            double maximum = Math.Max(0, MediaDurationMilliseconds);
            double position = Math.Clamp(value, 0, maximum);
            if (!SetProperty(ref _mediaPositionMilliseconds, position))
                return;
            OnPropertyChanged(nameof(MediaTimeText));
            if (!_isUpdatingMediaState && !_isMediaSeeking &&
                _mediaSession is not null && CanSeekMedia)
            {
                _mediaSession.Seek((long)position);
            }
        }
    }

    public double MediaDurationMilliseconds
    {
        get => _mediaDurationMilliseconds;
        private set
        {
            if (!SetProperty(ref _mediaDurationMilliseconds, Math.Max(0, value)))
                return;
            OnPropertyChanged(nameof(MediaTimelineMaximum));
            OnPropertyChanged(nameof(MediaTimeText));
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value))
                return;
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CharacterCountText));
            UpdateSearchCount();
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
                OnPropertyChanged(nameof(IsReadOnly));
        }
    }

    public bool IsReadOnly => !IsEditing || IsBusy;
    public bool IsDirty => IsText && !string.Equals(Text, _savedText, StringComparison.Ordinal);
    public string EncodingText => _textDocument?.EncodingName ?? string.Empty;
    public string CharacterCountText => $"{Text.Length:N0} Zeichen";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                UpdateSearchCount();
        }
    }

    public string SearchResultText { get; private set; } = string.Empty;

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;
            OnPropertyChanged(nameof(IsReadOnly));
            SaveCommand.NotifyCanExecuteChanged();
            ToggleEditingCommand.NotifyCanExecuteChanged();
            NotifyImageCommands();
        }
    }

    public bool IsAutoplayRunning
    {
        get => _isAutoplayRunning;
        private set
        {
            if (!SetProperty(ref _isAutoplayRunning, value))
                return;
            OnPropertyChanged(nameof(AutoplayButtonText));
            ToggleAutoplayCommand.NotifyCanExecuteChanged();
        }
    }

    public bool ShowDiscardConfirmation
    {
        get => _showDiscardConfirmation;
        private set => SetProperty(ref _showDiscardConfirmation, value);
    }

    public AsyncCommand SaveCommand { get; }
    public AsyncCommand ToggleEditingCommand { get; }
    public AsyncCommand CloseCommand { get; }
    public AsyncCommand DiscardAndCloseCommand { get; }
    public AsyncCommand CancelCloseCommand { get; }
    public AsyncCommand PreviousImageCommand { get; }
    public AsyncCommand NextImageCommand { get; }
    public AsyncCommand RandomiseImagesCommand { get; }
    public AsyncCommand ToggleAutoplayCommand { get; }
    public AsyncCommand StartMediaCommand { get; }
    public AsyncCommand ToggleMediaPlaybackCommand { get; }
    public AsyncCommand SeekMediaBackwardCommand { get; }
    public AsyncCommand SeekMediaForwardCommand { get; }

    public bool HandleBackRequested()
    {
        _ = RequestCloseAsync();
        return true;
    }

    public void ForceClose()
    {
        Dispose();
        _close();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _saveCancellation?.Cancel();
        _lifetimeCancellation.Cancel();
        StopAutoplay();
        Bitmap? image = Image;
        SetImage(null, decoded: false);
        image?.Dispose();
        IMediaPlaybackSession? mediaSession = _mediaSession;
        _mediaSession = null;
        if (mediaSession is not null)
        {
            mediaSession.StateChanged -= MediaSession_StateChanged;
            mediaSession.Failed -= MediaSession_Failed;
            mediaSession.Dispose();
        }
        _videoSurface = null;
        _text = string.Empty;
        _savedText = string.Empty;
    }

    public void BeginMediaSeek()
    {
        if (_mediaSession is null || !CanSeekMedia || _isMediaSeeking)
            return;
        _isMediaSeeking = true;
        _resumeMediaAfterSeek = IsMediaPlaying;
        if (_resumeMediaAfterSeek)
            _mediaSession.Pause();
    }

    public void CompleteMediaSeek()
    {
        if (_mediaSession is null || !_isMediaSeeking)
            return;
        _isMediaSeeking = false;
        _mediaSession.Seek((long)MediaPositionMilliseconds);
        if (_resumeMediaAfterSeek)
            _mediaSession.Play();
        _resumeMediaAfterSeek = false;
    }

    private Task StartMediaAsync()
    {
        if (_mediaSession is null || _mediaStartRequested)
            return Task.CompletedTask;
        _mediaStartRequested = true;
        StartMediaCommand.NotifyCanExecuteChanged();
        _mediaSession.Play();
        return Task.CompletedTask;
    }

    private Task ToggleMediaPlaybackAsync()
    {
        if (_mediaSession is null)
            return Task.CompletedTask;
        _mediaStartRequested = true;
        StartMediaCommand.NotifyCanExecuteChanged();
        if (IsMediaPlaying)
            _mediaSession.Pause();
        else
            _mediaSession.Play();
        return Task.CompletedTask;
    }

    private Task SeekMediaByAsync(TimeSpan offset)
    {
        if (_mediaSession is null || !CanSeekMedia)
            return Task.CompletedTask;
        long target = (long)Math.Clamp(
            MediaPositionMilliseconds + offset.TotalMilliseconds,
            0,
            MediaDurationMilliseconds);
        _mediaSession.Seek(target);
        return Task.CompletedTask;
    }

    private bool CanStartMedia() =>
        IsMedia && _mediaSession is not null && !_mediaStartRequested;

    private void MediaSession_StateChanged(
        object? sender,
        MediaPlaybackStateChangedEventArgs args)
    {
        if (_disposed)
            return;
        IsMediaPlaying = args.IsPlaying;
        CanSeekMedia = args.CanSeek;
        MediaDurationMilliseconds = args.DurationMilliseconds;
        if (!_isMediaSeeking)
        {
            _isUpdatingMediaState = true;
            try
            {
                MediaPositionMilliseconds = args.PositionMilliseconds;
            }
            finally
            {
                _isUpdatingMediaState = false;
            }
        }
        ToggleMediaPlaybackCommand.NotifyCanExecuteChanged();
    }

    private void MediaSession_Failed(object? sender, MediaPlaybackFailedEventArgs args)
    {
        if (!_disposed)
            ErrorMessage = args.Message;
    }

    private static string FormatMediaTime(double milliseconds, double durationMilliseconds)
    {
        TimeSpan value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return durationMilliseconds >= TimeSpan.FromHours(1).TotalMilliseconds
            ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
            : $"{(int)value.TotalMinutes}:{value.Seconds:00}";
    }

    private async Task SaveAsync()
    {
        if (_textDocument is null || !IsDirty)
            return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        using var cancellation = new CancellationTokenSource();
        _saveCancellation = cancellation;
        byte[] content = TextDocumentCodec.Encode(_textDocument, Text);
        try
        {
            await _save(content, cancellation.Token);
            _savedText = Text;
            OnPropertyChanged(nameof(IsDirty));
            SaveCommand.NotifyCanExecuteChanged();
            _setStatus($"„{DisplayName}“ gespeichert.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Der Text konnte nicht gespeichert werden: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_saveCancellation, cancellation))
                _saveCancellation = null;
            CryptographicOperations.ZeroMemory(content);
            IsBusy = false;
        }
    }

    private async Task NavigateImageAsync(int offset)
    {
        StopAutoplay();
        int targetIndex = _imageIndex < 0 && offset > 0
            ? 0
            : _imageIndex + offset;
        await SwitchImageAsync(targetIndex, _lifetimeCancellation.Token);
    }

    private async Task<bool> SwitchImageAsync(int targetIndex, CancellationToken cancellationToken)
    {
        if (_loadImage is null || targetIndex < 0 || targetIndex >= _images.Count)
            return false;

        IsBusy = true;
        ErrorMessage = string.Empty;
        VaultImageReference target = _images[targetIndex];
        Bitmap? replacement = null;
        try
        {
            VaultFileContent file = await _loadImage(target.Id, cancellationToken);
            try
            {
                if (file.Id != target.Id || file.Type != FileType.image)
                    throw new InvalidDataException("Der geladene Eintrag ist nicht das erwartete Bild.");
                replacement = _decodeImage(file.Content);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(file.Content);
            }

            cancellationToken.ThrowIfCancellationRequested();
            Bitmap? previous = Image;
            _id = file.Id;
            _name = file.Name;
            _extension = file.Extension;
            SetImage(replacement, decoded: true);
            replacement = null;
            previous?.Dispose();
            _imageIndex = targetIndex;
            OnPropertyChanged(nameof(Id));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Extension));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ImagePositionText));
            NotifyImageCommands();
            _setStatus($"Bild {_imageIndex + 1} von {_images.Count} angezeigt.");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Das Bild konnte nicht geladen werden: {ex.Message}";
            return false;
        }
        finally
        {
            replacement?.Dispose();
            IsBusy = false;
        }
    }

    private Task RandomiseImagesAsync()
    {
        StopAutoplay();
        int selectedIndex = _images.FindIndex(image => image.Id == Id);
        if (selectedIndex < 0 || _images.Count <= 1)
            return Task.CompletedTask;

        VaultImageReference selected = _images[selectedIndex];
        if (_includeSelectedImageWhenRandomising)
        {
            Shuffle(_images);
            _imageIndex = -1;
        }
        else
        {
            List<VaultImageReference> remaining = _images
                .Where((_, index) => index != selectedIndex)
                .ToList();
            Shuffle(remaining);
            _images.Clear();
            _images.Add(selected);
            _images.AddRange(remaining);
            _imageIndex = 0;
        }

        OnPropertyChanged(nameof(ImagePositionText));
        NotifyImageCommands();
        _setStatus("Zufällige Bildreihenfolge erstellt.");
        return Task.CompletedTask;
    }

    private Task ToggleAutoplayAsync()
    {
        if (IsAutoplayRunning)
        {
            StopAutoplay();
            return Task.CompletedTask;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        _autoplayCancellation = cancellation;
        IsAutoplayRunning = true;
        _ = RunAutoplayAsync(cancellation);
        return Task.CompletedTask;
    }

    private async Task RunAutoplayAsync(CancellationTokenSource cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                await _delay(_autoplayInterval, cancellation.Token);
                int targetIndex = _imageIndex + 1;
                if (targetIndex >= _images.Count)
                {
                    if (!_loopAutoplay)
                        break;
                    targetIndex = 0;
                }
                if (!await SwitchImageAsync(targetIndex, cancellation.Token))
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Autoplay wurde beendet: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_autoplayCancellation, cancellation))
            {
                _autoplayCancellation = null;
                IsAutoplayRunning = false;
            }
            cancellation.Dispose();
        }
    }

    private void StopAutoplay()
    {
        CancellationTokenSource? cancellation = _autoplayCancellation;
        _autoplayCancellation = null;
        cancellation?.Cancel();
        IsAutoplayRunning = false;
    }

    private bool CanToggleAutoplay() =>
        IsImageSeries &&
        (IsAutoplayRunning || (!IsBusy && (_loopAutoplay || _imageIndex < _images.Count - 1)));

    private void NotifyImageCommands()
    {
        PreviousImageCommand.NotifyCanExecuteChanged();
        NextImageCommand.NotifyCanExecuteChanged();
        RandomiseImagesCommand.NotifyCanExecuteChanged();
        ToggleAutoplayCommand.NotifyCanExecuteChanged();
    }

    private static Bitmap DecodeImage(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        return new Bitmap(stream);
    }

    private void SetImage(Bitmap? image, bool decoded)
    {
        _image = image;
        _hasDecodedImage = decoded;
        OnPropertyChanged(nameof(Image));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsImageSeries));
        OnPropertyChanged(nameof(IsGeneric));
    }

    private static void Shuffle<T>(IList<T> items)
    {
        for (int index = items.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Shared.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }

    private Task ToggleEditingAsync()
    {
        IsEditing = !IsEditing;
        return Task.CompletedTask;
    }

    private Task RequestCloseAsync()
    {
        if (IsDirty)
        {
            ShowDiscardConfirmation = true;
            return Task.CompletedTask;
        }
        ForceClose();
        return Task.CompletedTask;
    }

    private Task DiscardAndCloseAsync()
    {
        ShowDiscardConfirmation = false;
        ForceClose();
        return Task.CompletedTask;
    }

    private Task CancelCloseAsync()
    {
        ShowDiscardConfirmation = false;
        return Task.CompletedTask;
    }

    private void UpdateSearchCount()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            SearchResultText = string.Empty;
        }
        else
        {
            int count = 0;
            int index = 0;
            while ((index = Text.IndexOf(SearchText, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += Math.Max(1, SearchText.Length);
            }
            SearchResultText = count == 1 ? "1 Treffer" : $"{count} Treffer";
        }
        OnPropertyChanged(nameof(SearchResultText));
    }
}
