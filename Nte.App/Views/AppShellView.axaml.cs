using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nte.App.Views;

public sealed partial class AppShellView : UserControl
{
    public AppShellView() => AvaloniaXamlLoader.Load(this);
}
