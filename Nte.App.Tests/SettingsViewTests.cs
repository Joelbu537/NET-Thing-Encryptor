using System.Xml.Linq;

namespace Nte.App.Tests;

public sealed class SettingsViewTests
{
    [Fact]
    public void VaultExportArea_ShowsDeterminateProgressAndPercentage()
    {
        XDocument document = XDocument.Load(FindSettingsViewPath());
        XNamespace avalonia = "https://github.com/avaloniaui";

        XElement progressBar = document
            .Descendants(avalonia + "ProgressBar")
            .Single(element =>
                element.Attribute("Value")?.Value == "{Binding VaultExportProgress}");
        XElement progressArea = progressBar.Parent
            ?? throw new InvalidDataException("Der Export-Fortschrittsbereich fehlt.");

        Assert.Equal("0", progressBar.Attribute("Minimum")?.Value);
        Assert.Equal("100", progressBar.Attribute("Maximum")?.Value);
        Assert.Equal(
            "{Binding IsVaultExportProgressVisible}",
            progressArea.Attribute("IsVisible")?.Value);
        Assert.Contains(
            progressArea.Descendants(avalonia + "TextBlock"),
            element => element.Attribute("Text")?.Value ==
                "{Binding VaultExportProgressPercentText}");
    }

    [Fact]
    public void SaveButton_IsDefaultActionForKeyboardInput()
    {
        XDocument document = XDocument.Load(FindSettingsViewPath());
        XNamespace avalonia = "https://github.com/avaloniaui";

        XElement saveButton = document
            .Descendants(avalonia + "Button")
            .Single(element =>
                element.Attribute("Command")?.Value == "{Binding SavePreferencesCommand}");

        Assert.Equal("True", saveButton.Attribute("IsDefault")?.Value);
    }

    private static string FindSettingsViewPath()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "Nte.App", "Views", "SettingsView.axaml");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            "SettingsView.axaml wurde ausgehend vom Testverzeichnis nicht gefunden.");
    }
}
