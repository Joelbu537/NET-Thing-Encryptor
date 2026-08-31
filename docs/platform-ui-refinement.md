# Plattformspezifische Oberflächenangleichung vor M9

Diese Änderung verfeinert den nach M8 ausgelieferten Avalonia-Client. Sie ist kein Beginn von M9. Ziel ist eine vertrautere Windows-Bedienung, ohne Android in ein ungeeignetes Desktop-Raster zu zwingen.

## Windows-Vertrag

Die Hauptansicht besteht aus vier kompakten Bereichen:

1. Symbolleiste mit Zurück, Stammverzeichnis, Import, neuem Ordner, Aktualisieren, Einstellungen und Sperren
2. aktueller Tresorpfad und Suche
3. Dateiliste mit den Spalten Name, Typ, Größe und Erstellt
4. feste Statuszeile mit Version, letzter Meldung, sichtbarer Datei- und Ordnerzahl sowie Gesamtgröße

Die zusätzliche Produktleiste innerhalb des Fensters entfällt. Der native Fenstertitel identifiziert die Anwendung weiterhin.

Ordner und unterstützte Dokumente öffnen per Doppelklick. Ein Rechtsklick auf ein nicht ausgewähltes Element wählt genau dieses Element aus; ein Rechtsklick auf eine bestehende Mehrfachauswahl erhält sie. Das Elementmenü enthält Öffnen, Exportieren, Umbenennen, Verschieben und Löschen. Umbenennen, Verschieben und Löschen öffnen deckende Aktionsdialoge; das dauerhafte Bedienfeld unter der Dateiliste entfällt.

Die Einstellungen sind ein eigenes, dem Hauptfenster zugeordnetes modales Fenster. Schließen verwirft den noch nicht gespeicherten Entwurf, „Speichern und anwenden“ persistiert ihn. Der vollständige `.ntevault`-Export befindet sich im Abschnitt „Tresorsicherung“. Der aktive Einstellungsdialog ist zugleich Eigentümer nativer Dateiauswahldialoge und meldet Tastatur- sowie Zeigeraktivität an die automatische Sperre.

## Android-Vertrag

Android verwendet eine kompakte Dateizeile mit Name, Typ und Größe. Jede Zeile besitzt einen Drei-Punkte-Knopf mit denselben Auswahlaktionen wie das Windows-Kontextmenü. Die Desktop-Statuszeile und die Detailspalten entfallen auf dem kleinen Formfaktor.

Einstellungen werden auf Android als vollständig deckende Seite innerhalb der App gezeigt. Die darunterliegende Dateiansicht bleibt dadurch weder sichtbar noch bedienbar. Die gemeinsamen Aktionsdialoge verwenden ebenfalls eine vollständig opake, zum aktiven Hell- oder Dunkelmodus passende Karte.

## Symbole

Die sechs Dateitypen verwenden wieder die vorhandenen Originalressourcen aus dem WinForms-Client:

| Typ | Ressource |
|---|---|
| Ordner | `imageres_folder_empty.ico` |
| Bild | `imageres_image_file.ico` |
| Text | `imageres_text_file.ico` |
| Audio | `imageres_audio_file.ico` |
| Video | `imageres_video_file.ico` |
| Sonstige Datei | `imageres_other_file.ico` |

Die Symbolleiste übernimmt ebenfalls die vorhandenen Zurück-, Stammordner-, Import-, Ordner- und Einstellungssymbole. Die Ressourcen liegen als `AvaloniaResource` in `Nte.App/Assets/Legacy`; ein zentraler Konverter lädt und cached pro Dateityp genau ein Bild.

## Prüfung

Automatisierte Mindestprüfung:

```powershell
dotnet test --project .\Nte.App.Tests\Nte.App.Tests.csproj
dotnet build .\Nte.Desktop\Nte.Desktop.csproj -c Release
dotnet build .\Nte.Android\Nte.Android.csproj -c Release
```

Unter .NET 10 mit xUnit 4 muss `global.json` für die Testausführung den Runner `Microsoft.Testing.Platform` wählen.

Die manuelle Windows-Prüfung verwendet einen isolierten Ordner über `NTE_DATA_DIRECTORY` und umfasst:

- Entsperren eines Testtresors
- Anlegen eines Ordners und Kontrolle des Originalsymbols sowie der Statuszählung
- Öffnen des Ordners per Doppelklick
- Rechtsklick auf eine Zeile und Kontrolle der Auswahlstabilität
- Öffnen und Schließen der deckenden Aktionsdialoge
- Öffnen des separaten Einstellungsfensters und Kontrolle des Bereichs „Tresorsicherung“
- Kontrolle, dass die Hauptansicht keine zusätzliche Produktleiste enthält

Vor einer Windows-Testverteilung muss der Installer neu gebaut werden, damit er nicht mehr den UI-Stand von M8 enthält.
