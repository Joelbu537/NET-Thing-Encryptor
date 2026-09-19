using NET_Thing_Encryptor;
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
    private string _remoteAddress = string.Empty;
    private string _remoteAccessPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private string _vaultImportProgressDetail = string.Empty;
    private bool _isBusy;
    private bool _canImportVault;
    private bool _hasPersistedVault;
    private bool _isVaultImportProgressVisible;
    private bool _isVaultImportProgressIndeterminate;
    private double _vaultImportProgress;
    private int _selectedVaultSourceIndex;

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
        _selectedVaultSourceIndex = vault.IsRemoteVault ? 1 : 0;
        UnlockCommand = new AsyncCommand(UnlockAsync, () => !IsBusy);
        ImportVaultCommand = new AsyncCommand(
            ImportVaultAsync,
            () => !IsBusy && CanImportVault);
        ConnectRemoteVaultCommand = new AsyncCommand(
            ConnectRemoteVaultAsync,
            () => !IsBusy &&
                  !string.IsNullOrWhiteSpace(RemoteAddress) &&
                  !string.IsNullOrEmpty(RemoteAccessPassword));
        UseLocalVaultCommand = new AsyncCommand(
            UseLocalVaultAsync,
            () => !IsBusy && IsRemoteVault);
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

    public string RemoteAddress
    {
        get => _remoteAddress;
        set
        {
            if (SetProperty(ref _remoteAddress, value))
                ConnectRemoteVaultCommand.NotifyCanExecuteChanged();
        }
    }

    public string RemoteAccessPassword
    {
        get => _remoteAccessPassword;
        set
        {
            if (SetProperty(ref _remoteAccessPassword, value))
                ConnectRemoteVaultCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasPersistedVault
    {
        get => _hasPersistedVault;
        private set
        {
            if (!SetProperty(ref _hasPersistedVault, value))
                return;

            OnPropertyChanged(nameof(IsPasswordConfirmationVisible));
            OnPropertyChanged(nameof(IsVaultSourceSelectionVisible));
            OnPropertyChanged(nameof(Heading));
            OnPropertyChanged(nameof(InstructionText));
            OnPropertyChanged(nameof(IsArchiveImportVisible));
            OnPropertyChanged(nameof(IsVaultAccessVisible));
        }
    }

    public bool IsPasswordConfirmationVisible => !HasPersistedVault;

    public bool IsVaultSourceSelectionVisible => true;

    public bool IsArchiveImportVisible => !HasPersistedVault;

    public bool IsRemoteVault => _vault.IsRemoteVault;

    public bool IsLocalVault => !IsRemoteVault;

    public bool IsRemoteConnectionVisible =>
        SelectedVaultSourceIndex == 1 && !IsRemoteVault;

    public bool IsVaultAccessVisible =>
        SelectedVaultSourceIndex == (IsRemoteVault ? 1 : 0);

    public int SelectedVaultSourceIndex
    {
        get => _selectedVaultSourceIndex;
        set
        {
            if (!SetProperty(ref _selectedVaultSourceIndex, Math.Clamp(value, 0, 1)))
                return;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(IsRemoteConnectionVisible));
            OnPropertyChanged(nameof(IsVaultAccessVisible));
        }
    }

    public string StorageDescription => IsRemoteVault
        ? $"Aktiver Remote-Tresor: {_vault.StorageLocation}"
        : "Aktiver Tresor: dieses Gerät";

    public string Heading => (HasPersistedVault, IsRemoteVault) switch
    {
        (true, true) => "Remote-Tresor entsperren",
        (false, true) => "Remote-Tresor einrichten",
        (true, false) => "Tresor entsperren",
        _ => "Passwort festlegen"
    };

    public string InstructionText => HasPersistedVault
        ? "Gib das Tresorpasswort ein. Es wird nur auf diesem Gerät zur Entschlüsselung verwendet."
        : "Gib ein Tresorpasswort zur clientseitigen Verschlüsselung ein.";

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
            ConnectRemoteVaultCommand.NotifyCanExecuteChanged();
            UseLocalVaultCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsVaultImportProgressVisible
    {
        get => _isVaultImportProgressVisible;
        private set
        {
            if (!SetProperty(ref _isVaultImportProgressVisible, value))
                return;
            OnPropertyChanged(nameof(ImportVaultButtonText));
        }
    }

    public bool IsVaultImportProgressIndeterminate
    {
        get => _isVaultImportProgressIndeterminate;
        private set
        {
            if (SetProperty(ref _isVaultImportProgressIndeterminate, value))
                OnPropertyChanged(nameof(VaultImportProgressPercentText));
        }
    }

    public double VaultImportProgress
    {
        get => _vaultImportProgress;
        private set
        {
            if (!SetProperty(ref _vaultImportProgress, Math.Clamp(value, 0, 100)))
                return;
            OnPropertyChanged(nameof(VaultImportProgressPercentText));
        }
    }

    public string VaultImportProgressDetail
    {
        get => _vaultImportProgressDetail;
        private set => SetProperty(ref _vaultImportProgressDetail, value);
    }

    public string VaultImportProgressPercentText => IsVaultImportProgressIndeterminate
        ? string.Empty
        : $"{VaultImportProgress:0}%";

    public string ImportVaultButtonText => IsVaultImportProgressVisible
        ? "Tresorarchiv wird importiert …"
        : "Tresorarchiv importieren …";

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
    public AsyncCommand ConnectRemoteVaultCommand { get; }
    public AsyncCommand UseLocalVaultCommand { get; }

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
            BeginVaultImportProgress();
            var progress = new Progress<VaultArchiveImportProgress>(UpdateVaultImportProgress);
            int count = await _vault.ImportVaultAsync(source, progress: progress);
            VaultImportProgress = 100;
            IsVaultImportProgressIndeterminate = false;
            VaultImportProgressDetail = "Import abgeschlossen.";
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
            IsVaultImportProgressVisible = false;
            IsBusy = false;
        }
    }

    private void BeginVaultImportProgress()
    {
        VaultImportProgress = 0;
        IsVaultImportProgressIndeterminate = true;
        VaultImportProgressDetail = "Tresorarchiv wird eingelesen …";
        IsVaultImportProgressVisible = true;
    }

    private void UpdateVaultImportProgress(VaultArchiveImportProgress progress)
    {
        IsVaultImportProgressIndeterminate = progress.IsIndeterminate;
        VaultImportProgress = progress.Percentage;
        VaultImportProgressDetail = progress.Phase switch
        {
            VaultArchiveImportPhase.ReadingArchive => "Tresorarchiv wird eingelesen …",
            VaultArchiveImportPhase.ValidatingArchive => "Tresorarchiv wird auf Integrität geprüft …",
            VaultArchiveImportPhase.WritingObjects when progress.TotalObjects == 1 =>
                "1 Tresorobjekt wird übertragen …",
            VaultArchiveImportPhase.WritingObjects =>
                $"{progress.CompletedObjects} von {progress.TotalObjects} Tresorobjekten übertragen",
            VaultArchiveImportPhase.Completing when progress.CompletedSteps >= progress.TotalSteps =>
                "Import abgeschlossen.",
            _ => "Tresorarchiv wird abgeschlossen …"
        };
    }

    private async Task ConnectRemoteVaultAsync()
    {
        if (string.IsNullOrWhiteSpace(RemoteAddress))
        {
            ErrorMessage = "Bitte einen Servernamen oder eine IP-Adresse eingeben.";
            return;
        }
        if (string.IsNullOrEmpty(RemoteAccessPassword))
        {
            ErrorMessage = "Bitte das Server-Zugangspasswort eingeben.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _vault.ConnectRemoteVaultAsync(RemoteAddress.Trim(), RemoteAccessPassword);
            SelectedVaultSourceIndex = 1;
            HasPersistedVault = _vault.HasPersistedVault;
            CanImportVault = !HasPersistedVault;
            Password = string.Empty;
            PasswordConfirmation = string.Empty;
            RemoteAccessPassword = string.Empty;
            OnPropertyChanged(nameof(IsRemoteVault));
            OnPropertyChanged(nameof(IsLocalVault));
            OnPropertyChanged(nameof(IsRemoteConnectionVisible));
            OnPropertyChanged(nameof(IsVaultAccessVisible));
            OnPropertyChanged(nameof(StorageDescription));
            OnPropertyChanged(nameof(Heading));
            OnPropertyChanged(nameof(InstructionText));
            UseLocalVaultCommand.NotifyCanExecuteChanged();
            _setStatus(HasPersistedVault
                ? "Mit dem Remote-Tresor verbunden. Jetzt mit dessen Tresorpasswort entsperren."
                : "Mit dem leeren Remote-Speicher verbunden. Du kannst einen Tresor anlegen oder ein Archiv importieren.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Die Remote-Verbindung ist fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UseLocalVaultAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _vault.UseLocalVaultAsync();
            HasPersistedVault = _vault.HasPersistedVault;
            CanImportVault = !HasPersistedVault;
            Password = string.Empty;
            PasswordConfirmation = string.Empty;
            OnPropertyChanged(nameof(IsRemoteVault));
            OnPropertyChanged(nameof(IsLocalVault));
            OnPropertyChanged(nameof(IsRemoteConnectionVisible));
            OnPropertyChanged(nameof(IsVaultAccessVisible));
            OnPropertyChanged(nameof(StorageDescription));
            OnPropertyChanged(nameof(Heading));
            OnPropertyChanged(nameof(InstructionText));
            UseLocalVaultCommand.NotifyCanExecuteChanged();
            _setStatus(HasPersistedVault
                ? "Lokaler Tresor ausgewählt. Jetzt mit dessen Tresorpasswort entsperren."
                : "Leerer lokaler Speicher ausgewählt. Du kannst einen Tresor anlegen oder ein Archiv importieren.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Der lokale Tresor konnte nicht ausgewählt werden: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
