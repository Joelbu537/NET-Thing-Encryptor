using System.Runtime.Versioning;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class CoreBoundaryTests
{
    [Fact]
    public void CoreAssembly_TargetsNet10WithoutWindowsDesktopReferences()
    {
        var assembly = typeof(ThingData).Assembly;
        string? targetFramework = assembly
            .GetCustomAttributes(typeof(TargetFrameworkAttribute), inherit: false)
            .Cast<TargetFrameworkAttribute>()
            .Single()
            .FrameworkName;
        string[] references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework);
        Assert.DoesNotContain("System.Windows.Forms", references);
        Assert.DoesNotContain("System.Drawing.Common", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.Windows", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingRoot_RaisesAPlatformNeutralNotification()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "NETThingEncryptor.CoreBoundaryTests",
            Guid.NewGuid().ToString("N"));
        VaultNotificationEventArgs? notification = null;
        EventHandler<VaultNotificationEventArgs> handler = (_, args) => notification = args;

        ThingData.LockSession();
        TestEnvironment.SetRoot(null);
        AppPaths.DataDirectoryOverride = directory;
        ThingData.NotificationRaised += handler;
        try
        {
            Assert.True(await ThingData.LoadMainData());
            Assert.NotNull(ThingData.CurrentSession.Root);
            Assert.NotNull(notification);
            Assert.Equal(VaultNotificationSeverity.Information, notification.Severity);
        }
        finally
        {
            ThingData.NotificationRaised -= handler;
            ThingData.LockSession();
            TestEnvironment.SetRoot(null);
            AppPaths.DataDirectoryOverride = null;
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
