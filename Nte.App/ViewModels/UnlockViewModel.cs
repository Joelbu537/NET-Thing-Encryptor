using Nte.App.Services;

namespace Nte.App.ViewModels;

public sealed class UnlockViewModel : ObservableObject
{
    private readonly IVaultApplicationService _vault;
    private readonly IFilePickerService _filePicker;
    private readonly Func<Task> _onUnlocked;
    private readonly Action<string> _setStatus;
    private string _password = string.Empty;
    private string _passwordConfirmation = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _canImportVault;
    private bool _hasPersistedVault;

    public UnlockViewModel(
        IVaultApplicationService vault,
        IFilePickerService filePicker,
        Func<Task> onUnlocked,
        Action<string> setStatus,
        string initialError = "")
    {
        _vault = vault;
        _filePicker = filePicker;
        _onUnlocked = onUnlocked;
        _setStatus = setStatus;
        _errorMessage = initialError;
        _hasPersistedVault = vault.HasPersistedVault;
        _canImportVault = !_hasPersistedVault;
        UnlockCommand = new AsyncCommand(UnlockAsync, () => !IsBusy);
        ImportVaultCommand = new AsyncCommand(
            ImportVaultAsync,
            () => !IsBusy && CanImportVault);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string PasswordConfirmation
    {
        get => _passwordConfirmation;
        set => SetProperty(ref _passwordConfirmation, value);
    }

    public bool HasPersistedVault
    {
        get => _hasPersistedVault;
        private set
        {
            if (!SetProperty(ref _hasPersistedVault, value))
                return;

            OnPropertyChanged(nameof(IsPasswordConfirmationVisible));
            OnPropertyChanged(nameof(Heading));
            OnPropertyChanged(nameof(InstructionText));
        }
    }

    public bool IsPasswordConfirmationVisible => !HasPersistedVault;

    public string Heading => HasPersistedVault
        ? "Tresor entsperren"
        : "Passwort festlegen";

    public string InstructionText => HasPersistedVault
        ? "Gib das zum Entsperren des Tresors benötigte Passwort ein."
        : "Gib ein Passwort zum Verschlüsseln deines Tresors ein.";

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
            UnlockCommand.NotifyCanExecuteChanged();
            ImportVaultCommand.NotifyCanExecuteChanged();
        }
    }

    public bool CanImportVault
    {
        get => _canImportVault;
        private set
        {
            if (SetProperty(ref _canImportVault, value))
                ImportVaultCommand.NotifyCanExecuteChanged();
        }
    }

    public AsyncCommand UnlockCommand { get; }
    public AsyncCommand ImportVaultCommand { get; }

    private async Task UnlockAsync()
    {
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Bitte ein Passwort eingeben.";
            return;
        }

        if (!HasPersistedVault && string.IsNullOrEmpty(PasswordConfirmation))
        {
            ErrorMessage = "Bitte das Passwort bestätigen.";
            return;
        }

        if (!HasPersistedVault && Password != PasswordConfirmation)
        {
            ErrorMessage = "Die Passwörter stimmen nicht überein.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            if (!await _vault.UnlockAsync(Password))
            {
                ErrorMessage = "Das Passwort ist nicht korrekt.";
                return;
            }

            Password = string.Empty;
            PasswordConfirmation = string.Empty;
            _setStatus(string.Empty);
            await _onUnlocked();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Der Tresor konnte nicht entsperrt werden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportVaultAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            IReadableExternalFile? file = await _filePicker.PickVaultArchiveAsync();
            if (file is null)
                return;

            await using Stream source = await file.OpenReadAsync();
            int count = await _vault.ImportVaultAsync(source);
            HasPersistedVault = true;
            Password = string.Empty;
            PasswordConfirmation = string.Empty;
            CanImportVault = false;
            _setStatus($"Tresorarchiv importiert ({count} Objekte). Jetzt mit dessen Passwort entsperren.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Das Tresorarchiv konnte nicht importiert werden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
