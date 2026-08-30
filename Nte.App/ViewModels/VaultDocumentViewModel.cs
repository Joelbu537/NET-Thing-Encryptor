using System.Security.Cryptography;
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
    private string _text = string.Empty;
    private string _savedText = string.Empty;
    private string _searchText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isEditing;
    private bool _isBusy;
    private bool _showDiscardConfirmation;
    private bool _disposed;
    private CancellationTokenSource? _saveCancellation;

    public VaultDocumentViewModel(
        VaultFileContent file,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> save,
        Action close,
        Action<string> setStatus)
    {
        ArgumentNullException.ThrowIfNull(file);
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _close = close ?? throw new ArgumentNullException(nameof(close));
        _setStatus = setStatus ?? throw new ArgumentNullException(nameof(setStatus));
        Id = file.Id;
        Name = file.Name;
        Extension = file.Extension;
        Type = file.Type;

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
                using var stream = new MemoryStream(file.Content, writable: false);
                Image = new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Der Inhalt konnte nicht dargestellt werden: {ex.Message}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(file.Content);
        }

        SaveCommand = new AsyncCommand(SaveAsync, () => IsText && IsDirty && !IsBusy);
        ToggleEditingCommand = new AsyncCommand(ToggleEditingAsync, () => IsText && !IsBusy);
        CloseCommand = new AsyncCommand(RequestCloseAsync, () => !IsBusy);
        DiscardAndCloseCommand = new AsyncCommand(DiscardAndCloseAsync, () => !IsBusy);
        CancelCloseCommand = new AsyncCommand(CancelCloseAsync, () => !IsBusy);
    }

    public ulong Id { get; }
    public string Name { get; }
    public string Extension { get; }
    public FileType Type { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(Extension)
        ? Name
        : $"{Name}.{Extension.TrimStart('.')}";
    public bool IsText => Type == FileType.text;
    public bool IsImage => Type == FileType.image && Image is not null;
    public bool IsMedia => Type is FileType.audio or FileType.video;
    public bool IsGeneric => !IsText && !IsImage;
    public string GenericMessage => IsMedia
        ? "Audio- und Videowiedergabe benötigt noch ein gemeinsam geprüftes Medien-Backend. Der verschlüsselte Inhalt kann sicher exportiert werden."
        : "Für diesen Dateityp ist keine interne Vorschau verfügbar. Der Inhalt kann sicher exportiert werden.";
    public Bitmap? Image { get; private set; }

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
        _saveCancellation?.Cancel();
        Image?.Dispose();
        Image = null;
        _text = string.Empty;
        _savedText = string.Empty;
        _disposed = true;
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
