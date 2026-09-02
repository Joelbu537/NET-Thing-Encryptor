using System.Xml.Linq;

namespace Nte.App.Tests;

public sealed class UnlockViewTests
{
    [Fact]
    public void PasswordInput_RequestsFocusAndFullSelectionWhenViewLoads()
    {
        string viewPath = FindUnlockViewPath();
        XDocument document = XDocument.Load(viewPath);
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        Assert.Equal("UnlockView_Loaded", document.Root?.Attribute("Loaded")?.Value);
        XElement passwordInput = FindNamedElement(document, avalonia, xaml, "PasswordTextBox");
        Assert.Equal("{Binding Password, Mode=TwoWay}", passwordInput.Attribute("Text")?.Value);

        string codeBehind = File.ReadAllText(Path.ChangeExtension(viewPath, ".axaml.cs"));
        Assert.Contains("PasswordTextBox.Focus();", codeBehind, StringComparison.Ordinal);
        Assert.Contains("PasswordTextBox.SelectAll();", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultSourceAreas_FollowInitialSetupStateAndKeepRemoteActionUnavailable()
    {
        XDocument document = XDocument.Load(FindUnlockViewPath());
        XNamespace avalonia = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement sourceSelection = FindNamedElement(document, avalonia, xaml, "VaultSourceSelection");
        XElement importArea = FindNamedElement(document, avalonia, xaml, "VaultImportArea");
        XElement remoteArea = FindNamedElement(document, avalonia, xaml, "RemoteVaultArea");
        XElement remoteButton = FindNamedElement(document, avalonia, xaml, "ConnectRemoteVaultButton");

        Assert.Equal(
            "{Binding IsVaultSourceSelectionVisible}",
            sourceSelection.Attribute("IsVisible")?.Value);
        Assert.Contains(importArea, sourceSelection.Descendants());
        Assert.Contains(remoteArea, sourceSelection.Descendants());
        Assert.Equal("{Binding CanConnectRemoteVault}", remoteButton.Attribute("IsEnabled")?.Value);
        Assert.Equal(
            "{Binding RemoteVaultConnectionHint}",
            remoteButton.Attribute("ToolTip.Tip")?.Value);
        Assert.Null(remoteButton.Attribute("Command"));
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
