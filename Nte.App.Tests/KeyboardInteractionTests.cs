using System.Xml.Linq;

namespace Nte.App.Tests;

public sealed class KeyboardInteractionTests
{
    [Fact]
    public void VaultForms_WireEnterAndAvoidKeyboardResizeScrollLoops()
    {
        string viewPath = FindRepositoryFile("Nte.App", "Views", "VaultView.axaml");
        XDocument document = XDocument.Load(viewPath);
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement actionScroller = FindNamedElement(
            document,
            avalonia,
            xaml,
            "ActionDialogScrollViewer");
        Assert.Equal("Auto", actionScroller.Attribute("VerticalScrollBarVisibility")?.Value);
        Assert.Null(actionScroller.Attribute("SizeChanged"));

        string[] actionInputs = ["CreateFolderNameTextBox", "RenameNameTextBox"];
        foreach (string name in actionInputs)
        {
            XElement input = FindNamedElement(document, avalonia, xaml, name);
            Assert.Equal("ActionTextBox_KeyDown", input.Attribute("KeyDown")?.Value);
            Assert.Null(input.Attribute("GotFocus"));
        }

        string codeBehind = File.ReadAllText(Path.ChangeExtension(viewPath, ".axaml.cs"));
        Assert.DoesNotContain("BringIntoView", codeBehind, StringComparison.Ordinal);

        Assert.All(
            document.Descendants(avalonia + "TextBox")
                .Where(element => element.Attribute("PlaceholderText")?.Value is
                    "Erweiterung" or "Min. Bytes" or "Max. Bytes"),
            element => Assert.Equal("SearchTextBox_KeyDown", element.Attribute("KeyDown")?.Value));
    }

    [Fact]
    public void AndroidActivity_ResizesForTheSoftwareKeyboard()
    {
        string activityPath = FindRepositoryFile("Nte.Android", "MainActivity.cs");
        string source = File.ReadAllText(activityPath);

        Assert.Contains("WindowSoftInputMode = SoftInput.AdjustResize", source, StringComparison.Ordinal);
        Assert.Contains("SetSoftInputMode(SoftInput.AdjustResize)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MobileVault_UsesOneSelectionMenuInsteadOfRowButtons()
    {
        XDocument document = XDocument.Load(
            FindRepositoryFile("Nte.App", "Views", "VaultView.axaml"));
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement selectionMenu = FindNamedElement(
            document,
            avalonia,
            xaml,
            "MobileSelectionActionsButton");
        Assert.Equal("{Binding HasSelection}", selectionMenu.Attribute("IsEnabled")?.Value);
        Assert.Equal("⋮", selectionMenu.Attribute("Content")?.Value);
        Assert.Equal(
            ["Exportieren …", "Umbenennen …", "Verschieben …", "Löschen …"],
            selectionMenu.Descendants(avalonia + "MenuItem")
                .Select(item => item.Attribute("Header")?.Value)
                .ToArray());

        Assert.DoesNotContain(
            document.Descendants(avalonia + "Button"),
            button => button.Attribute("Click")?.Value == "ItemActionsButton_Click" ||
                      button.Attribute("Classes")?.Value?.Contains("row-menu", StringComparison.Ordinal) == true);
    }

    private static XElement FindNamedElement(
        XDocument document,
        XNamespace avalonia,
        XNamespace xaml,
        string name) => document
            .Descendants()
            .Single(element =>
                element.Name.Namespace == avalonia &&
                element.Attribute(xaml + "Name")?.Value == name);

    private static string FindRepositoryFile(params string[] segments)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            string candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(string.Join(Path.DirectorySeparatorChar, segments));
    }
}
