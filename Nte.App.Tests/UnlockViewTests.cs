using System.Xml.Linq;

namespace Nte.App.Tests;

public sealed class UnlockViewTests
{
    [Fact]
    public void PasswordInput_RequestsFocusAndFullSelectionSafelyWhenViewLoads()
    {
        string viewPath = FindUnlockViewPath();
        XDocument document = XDocument.Load(viewPath);
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        Assert.Equal("UnlockView_Loaded", document.Root?.Attribute("Loaded")?.Value);
        XElement passwordInput = FindNamedElement(document, avalonia, xaml, "PasswordTextBox");
        Assert.Equal("{Binding Password, Mode=TwoWay}", passwordInput.Attribute("Text")?.Value);

        string codeBehind = File.ReadAllText(Path.ChangeExtension(viewPath, ".axaml.cs"));
        Assert.Contains(
            "PasswordTextBox is not { } passwordTextBox",
            codeBehind,
            StringComparison.Ordinal);
        Assert.Contains("passwordTextBox.Focus();", codeBehind, StringComparison.Ordinal);
        Assert.Contains("passwordTextBox.SelectAll();", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "PasswordTextBox.IsAttachedToVisualTree()",
            codeBehind,
            StringComparison.Ordinal);
    }

    [Fact]
    public void VaultSourceTabs_SeparateLocalAndRemoteAccessAndExposeImportProgress()
    {
        XDocument document = XDocument.Load(FindUnlockViewPath());
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement sourceTabs = FindNamedElement(document, avalonia, xaml, "VaultSourceTabs");
        XElement localTab = FindNamedElement(document, avalonia, xaml, "LocalVaultTab");
        XElement remoteTab = FindNamedElement(document, avalonia, xaml, "RemoteVaultTab");
        XElement accessArea = FindNamedElement(document, avalonia, xaml, "VaultAccessArea");
        XElement importArea = FindNamedElement(document, avalonia, xaml, "VaultImportArea");
        XElement remoteArea = FindNamedElement(document, avalonia, xaml, "RemoteVaultArea");
        XElement remoteButton = FindNamedElement(document, avalonia, xaml, "ConnectRemoteVaultButton");
        XElement unlockButton = FindNamedElement(document, avalonia, xaml, "UnlockVaultButton");
        XElement localButton = FindNamedElement(document, avalonia, xaml, "UseLocalVaultButton");
        XElement importProgress = FindNamedElement(document, avalonia, xaml, "VaultImportProgressBar");
        XElement remoteAddress = remoteArea
            .Descendants(avalonia + "TextBox")
            .Single(element => element.Attribute("Text")?.Value == "{Binding RemoteAddress, Mode=TwoWay}");

        Assert.Equal(
            "{Binding SelectedVaultSourceIndex, Mode=TwoWay}",
            sourceTabs.Attribute("SelectedIndex")?.Value);
        Assert.Equal("Lokal", localTab.Attribute("Header")?.Value);
        Assert.Equal("Remote", remoteTab.Attribute("Header")?.Value);
        Assert.Contains(remoteArea, remoteTab.Descendants());
        Assert.Equal(
            "{Binding IsRemoteConnectionVisible}",
            remoteArea.Attribute("IsVisible")?.Value);
        Assert.Contains(importArea, accessArea.Descendants());
        Assert.Equal("{Binding IsVaultAccessVisible}", accessArea.Attribute("IsVisible")?.Value);
        Assert.Equal(
            "{Binding IsArchiveImportVisible}",
            importArea.Attribute("IsVisible")?.Value);
        Assert.Equal(
            "{Binding ConnectRemoteVaultCommand}",
            remoteButton.Attribute("Command")?.Value);
        Assert.Equal(
            "{Binding IsRemoteConnectionVisible}",
            remoteButton.Attribute("IsDefault")?.Value);
        Assert.Equal(
            "{Binding IsVaultAccessVisible}",
            unlockButton.Attribute("IsDefault")?.Value);
        Assert.Equal(
            "{Binding UseLocalVaultCommand}",
            localButton.Attribute("Command")?.Value);
        Assert.Equal(
            "{Binding VaultImportProgress}",
            importProgress.Attribute("Value")?.Value);
        Assert.Equal(
            "{Binding IsVaultImportProgressIndeterminate}",
            importProgress.Attribute("IsIndeterminate")?.Value);
        Assert.Equal("vault.example.net", remoteAddress.Attribute("PlaceholderText")?.Value);
        Assert.Contains("single-line", remoteAddress.Attribute("Classes")?.Value);
        Assert.Equal("RemoteInput_KeyDown", remoteAddress.Attribute("KeyDown")?.Value);
        Assert.All(
            remoteArea.Descendants(avalonia + "TextBox"),
            element => Assert.Contains("single-line", element.Attribute("Classes")?.Value));
        Assert.Contains(
            remoteArea.Descendants(avalonia + "TextBlock"),
            element => element.Attribute("Text")?.Value.Contains(
                "HTTPS wird automatisch verwendet",
                StringComparison.Ordinal) == true);
        Assert.DoesNotContain(
            document.Descendants(avalonia + "TextBlock"),
            element => element.Attribute("Text")?.Value is
                "Tresor öffnen" or
                "Wähle, ob du den Tresor auf diesem Gerät oder auf einem Remote-Server verwenden möchtest.");
    }

    [Fact]
    public void UnlockMenu_StaysAtTopAndUsesAndroidWidthWithoutFocusScrollLoop()
    {
        string viewPath = FindUnlockViewPath();
        XDocument document = XDocument.Load(viewPath);
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement menu = FindNamedElement(document, avalonia, xaml, "UnlockMenu");
        XElement scroller = FindNamedElement(document, avalonia, xaml, "UnlockScrollViewer");
        Assert.Equal("Top", menu.Attribute("VerticalAlignment")?.Value);
        Assert.Null(scroller.Attribute("SizeChanged"));
        Assert.All(
            document.Descendants(avalonia + "TextBox"),
            input => Assert.Null(input.Attribute("GotFocus")));

        string codeBehind = File.ReadAllText(Path.ChangeExtension(viewPath, ".axaml.cs"));
        Assert.Contains("OperatingSystem.IsAndroid()", codeBehind, StringComparison.Ordinal);
        Assert.Contains("UnlockMenu.Margin = new Thickness(0);", codeBehind, StringComparison.Ordinal);
        Assert.Contains("UnlockMenu.BorderThickness = new Thickness(0);", codeBehind, StringComparison.Ordinal);
        Assert.Contains("UnlockMenu.MaxWidth = double.PositiveInfinity;", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("BringIntoView", codeBehind, StringComparison.Ordinal);
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

    private static string FindUnlockViewPath()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "Nte.App", "Views", "UnlockView.axaml");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("UnlockView.axaml wurde ausgehend vom Testverzeichnis nicht gefunden.");
    }
}
