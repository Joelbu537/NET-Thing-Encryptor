namespace NET_Thing_Encryptor;

public enum VaultNotificationSeverity
{
    Information,
    Warning,
    Error
}

public sealed class VaultNotificationEventArgs(
    string title,
    string message,
    VaultNotificationSeverity severity) : EventArgs
{
    public string Title { get; } = title;
    public string Message { get; } = message;
    public VaultNotificationSeverity Severity { get; } = severity;
}
