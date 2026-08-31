using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nte.App.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView() => AvaloniaXamlLoader.Load(this);
}
