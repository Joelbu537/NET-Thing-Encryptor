using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nte.App.Views;

public sealed partial class LoadingView : UserControl
{
    public LoadingView() => AvaloniaXamlLoader.Load(this);
}
