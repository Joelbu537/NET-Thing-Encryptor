using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia.Android;
using AvaloniaApplication = Nte.App.App;

namespace Nte.Android;

[Activity(
    Label = "NET Thing Encryptor",
    Theme = "@style/NteTheme.NoActionBar",
    MainLauncher = true,
    Exported = true,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.Orientation |
                           ConfigChanges.ScreenSize |
                           ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetSoftInputMode(SoftInput.AdjustResize);
        Window?.AddFlags(WindowManagerFlags.Secure);
    }

    protected override void OnStart()
    {
        base.OnStart();
        AvaloniaApplication.NotifyForegrounded();
    }

    protected override void OnStop()
    {
        AvaloniaApplication.NotifyBackgrounded();
        base.OnStop();
    }
}
