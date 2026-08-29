namespace NET_Thing_Encryptor;

internal sealed class WinFormsVaultNotificationAdapter : IDisposable
{
    private WinFormsVaultNotificationAdapter()
    {
        ThingData.NotificationRaised += OnNotificationRaised;
    }

    public static WinFormsVaultNotificationAdapter Subscribe() => new();

    public void Dispose()
    {
        ThingData.NotificationRaised -= OnNotificationRaised;
    }

    private static void OnNotificationRaised(object? sender, VaultNotificationEventArgs args)
    {
        MessageBoxIcon icon = args.Severity switch
        {
            VaultNotificationSeverity.Information => MessageBoxIcon.Information,
            VaultNotificationSeverity.Warning => MessageBoxIcon.Warning,
            VaultNotificationSeverity.Error => MessageBoxIcon.Error,
            _ => MessageBoxIcon.None
        };
        MessageBox.Show(args.Message, args.Title, MessageBoxButtons.OK, icon);
    }
}
