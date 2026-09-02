# Plattformspezifische Oberflächenangleichung vor M9

Diese Änderung verfeinert den nach M8 ausgelieferten Avalonia-Client bis einschließlich Version 4.3.2. Sie ist kein Beginn von M9. Ziel ist eine vertrautere Windows-Bedienung, ohne Android in ein ungeeignetes Desktop-Raster zu zwingen.

## Windows-Vertrag

Die Hauptansicht besteht aus vier kompakten Bereichen:

1. Symbolleiste mit Zurück und Stammverzeichnis links vom aktuellen Tresorpfad sowie Import, neuem Ordner, Aktualisieren, Einstellungen und Sperren
2. Suche
3. Dateiliste mit den Spalten Name, Typ, Größe und Erstellt
4. feste Statuszeile mit Version, relevanten Aktionsmeldungen, sichtbarer Datei- und Ordnerzahl sowie Gesamtgröße

Windows verwendet eine schmale, von der Anwendung gezeichnete Titelleiste mit Verschieben, Größenänderung, Minimieren, Maximieren beziehungsweise Wiederherstellen und Schließen. Linux und macOS behalten ihre nativen Fensterdekorationen. Die frühere breite Produktleiste innerhalb der Inhaltsansicht bleibt entfernt.

Der Stammordner wird als `/` angezeigt; Unterordner bilden einen kompakten Pfad wie `/Dokumente/Bilder`. Ordner und unterstützte Dokumente öffnen ausschließlich per Doppelklick. Ein Rechtsklick auf ein nicht ausgewähltes Element wählt genau dieses Element aus; ein Rechtsklick auf eine bestehende Mehrfachauswahl erhält sie. Das Elementmenü enthält Exportieren, Umbenennen, Verschieben und Löschen. Umbenennen, Verschieben und Löschen öffnen deckende Aktionsdialoge; das dauerhafte Bedienfeld unter der Dateiliste entfällt. Die Desktopzeilen sind kompakt, während Android die größere Touch-Zielfläche beibehält.

Einzeilige Textfelder zentrieren ihren Inhalt vertikal. Eingabe in das Suchfeld kann mit Enter ausgeführt werden. Der Rücksetzknopf liegt innerhalb des Suchfelds. Die Statuszeile zeigt nach dem Entsperren keine redundante Meldung an; spätere Aktions- und Fehlermeldungen können weiterhin dort erscheinen. In Aktionsleisten steht Abbrechen beziehungsweise Schließen links und die bestätigende Aktion rechts.

Der Sperrbildschirm unterscheidet zwischen einem bestehenden Tresor und der Ersteinrichtung. Bei der Ersteinrichtung wird das Passwort zur Bestätigung zweimal verlangt; ein bestehender oder importierter Tresor benötigt nur das Entsperrpasswort. Sobald der Sperrbildschirm sichtbar wird, erhält das erste Passwortfeld den Eingabefokus und markiert einen eventuell vorhandenen Inhalt vollständig. Archivimport und der sichtbar als noch nicht verfügbar gekennzeichnete Einstieg zu einem künftigen Remote-Tresor erscheinen ausschließlich, solange noch kein lokaler Tresor existiert. Die Remote-Schaltfläche besitzt noch keine Verbindungslogik. Eine automatische Sperre nach Inaktivität erzeugt keine zusätzliche Statusmeldung.

Die Einstellungen sind ein eigenes, dem Hauptfenster zugeordnetes modales Fenster. Schließen verwirft den noch nicht gespeicherten Entwurf, „Speichern und anwenden“ persistiert ihn. Der vollständige `.ntevault`-Export befindet sich im Abschnitt „Tresorsicherung“. Der aktive Einstellungsdialog ist zugleich Eigentümer nativer Dateiauswahldialoge und meldet Tastatur- sowie Zeigeraktivität an die automatische Sperre.

Dokumente öffnen auf Desktop-Systemen in einem eigenen, dem Hauptfenster zugeordneten, nicht-modalen Fenster; Android behält die einzelne eingebettete Ansicht und ersetzt sie beim Öffnen eines weiteren Dokuments. Das Desktop-Hauptfenster bleibt bedienbar, mehrere Dokumentfenster können gleichzeitig aktiv sein und das Schließen eines Fensters beeinflusst die übrigen nicht. Sperren oder Beenden schließt dagegen alle offenen Dokumente und gibt ihre entschlüsselten Inhalte frei. Deshalb besitzen Bild- und Medienansichten auf Desktop keinen zusätzlichen Zurück-Knopf, während die eingebettete Android-Ansicht ihn weiterhin anbietet. Bilder werden ohne Scrollcontainer proportional an die tatsächlich verfügbare Fläche angepasst. Bei Bildserien navigiert ein Klick oder Tippen auf die linke beziehungsweise rechte Bildhälfte zum vorherigen beziehungsweise nächsten Bild; die sichtbaren Navigationsknöpfe entfallen. Die Pfeiltasten bleiben als Tastaturalternative erhalten.

Ab Version 4.3.1 erscheint die Dokumentansicht für Text, Bilder, Audio und Video unmittelbar nach der Aktivierung und zeigt während Entschlüsselung und Laden einen dokumenteigenen, unbestimmten Fortschrittszustand. Schließen oder Sperren fordert den Abbruch eines noch laufenden Ladevorgangs an; ein dennoch eintreffender entschlüsselter Puffer wird verworfen und überschrieben.

Videos verwenden ab Version 4.2.0 und Audio ab Version 4.3.0 ein gemeinsames LibVLCSharp-Wiedergabebackend. Auf Desktop-Systemen rendert `LibVLCSharp.Avalonia.VideoView` Videos im eigenen Dokumentfenster; Android bindet dafür die native Videoansicht über einen `NativeControlHost` ein. Audio benötigt keine Videooberfläche. Play/Pause, Sprünge um zehn Sekunden und die Zeitleiste sind für beide Medienarten direkt bedienbar. Leertaste schaltet Play/Pause um, Links/Rechts sowie J/L springen zurück beziehungsweise vor. Entschlüsselte Medien werden nicht in temporäre Klartextdateien geschrieben. Details zu Besitz, Speicherbereinigung und Plattformgrenzen stehen in [video-player.md](video-player.md).

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
- Kontrolle, dass das Passwortfeld beim Erscheinen des Sperrbildschirms unmittelbar Tastatureingaben annimmt und vorhandenen Text vollständig markiert
- Anlegen eines Ordners und Kontrolle des Originalsymbols sowie der Statuszählung
- Öffnen des Ordners per Doppelklick
- Rechtsklick auf eine Zeile, Öffnen des Kontextmenüs und Kontrolle der Auswahlstabilität
- Suche per Enter sowie Rücksetzen über den Knopf im Suchfeld
- Öffnen und Schließen der deckenden Aktionsdialoge
- Öffnen des separaten Einstellungsfensters und Kontrolle des Bereichs „Tresorsicherung“
- Kontrolle der eigenen Windows-Titelleiste einschließlich Verschieben, Größenänderung und Fensterknöpfen
- Öffnen eines Bildes im eigenen Fenster, Größenanpassung sowie Navigation über beide Bildhälften und Pfeiltasten
- Öffnen eines Videos im eigenen Fenster, Start/Pause, Zeitleiste, Zehn-Sekunden-Sprünge und die Tastenkürzel Leertaste, Links/Rechts und J/L
- Öffnen einer Audiodatei im eigenen Fenster mit denselben Wiedergabesteuerungen
- Gleichzeitiges Öffnen mindestens zweier verschiedener Dokumente, weitere Bedienung der Hauptansicht und unabhängiges Schließen der Dokumentfenster
- Kontrolle, dass Bild-, Audio- und Videofenster keinen zusätzlichen Zurück-Knopf zeigen
- Schließen eines Medienfensters ohne Beeinträchtigung weiterer Fenster sowie automatische Tresorsperre mit mehreren geöffneten Dokumenten bei laufender und pausierter Wiedergabe
- Kontrolle der vertikalen Textzentrierung und der Reihenfolge Abbrechen links / Bestätigen rechts
- Kontrolle, dass die Hauptansicht keine zusätzliche breite Produktleiste enthält

Vor einer Windows-Testverteilung muss der Installer neu gebaut werden, damit er nicht mehr den UI-Stand von M8 enthält.
