using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nte.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        GetFrame().Configure(this);
    }

    public void SetShellContent(Control content) => GetFrame().WindowContent = content;

    private WindowFrame GetFrame() =>
        this.FindControl<WindowFrame>("Frame")
        ?? throw new InvalidOperationException("The main window frame is unavailable.");
}
