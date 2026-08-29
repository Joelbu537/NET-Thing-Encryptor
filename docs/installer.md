# Installer-Spezifikation

## Ziel und Grenzen

Der Installer verteilt NET Thing Encryptor als per-user installierte, selbstenthaltende Windows-x64-Anwendung. Inno Setup bleibt die Installer-Engine. Ein eigener Bootstrapper, automatische Online-Updates, Dateizuordnungen und ein Maschinen-weiter Installationsmodus gehören derzeit nicht zum Umfang.

## Verhaltensmatrix

| Szenario | Erwartetes Verhalten |
|---|---|
| Neuinstallation | Installation ohne UAC nach `%LOCALAPPDATA%\Programs\NET Thing Encryptor` |
| Gleiches Setup erneut | Reparatur beziehungsweise Ersetzen der Programmdateien; Daten bleiben unverändert |
| Upgrade | Programmdateien werden aktualisiert, Sprache/Verzeichnis/Aufgaben werden übernommen |
| Downgrade | Setup bricht mit einer verständlichen Meldung ab |
| Laufende Anwendung | Setup erkennt den Anwendungs-Mutex und fordert zum Schließen auf |
| Deinstallation | Programmdateien, Uninstaller und Verknüpfungen werden entfernt |
| Benutzerdaten | Werden weder vom Installer noch vom Uninstaller gelöscht |
| Portable Altversion | Der Datenbestand wird beim ersten App-Start atomisch in das Benutzerprofil kopiert |
| Migrationskonflikt | Keine Datei wird überschrieben; der Start wird mit einer Konfliktmeldung beendet |
| Silent Setup | Keine Interaktion; Rückgabecode ungleich null bei Fehlern |

## Versionierung

Die kanonische Produktversion steht in `NET Thing Encryptor.csproj` unter `Version`. `AssemblyVersion` und `FileVersion` enthalten vier numerische Komponenten. Die Anwendung liest ihre sichtbare Version aus `AssemblyInformationalVersion`; das Buildskript übernimmt Installer- und Dateiversion direkt aus der Projektdatei.

Release-Tags verwenden das Format `v<Version>`. Ein Tag, der nicht zur Projektversion passt, wird von der Release-Pipeline abgewiesen.

## Daten- und Sicherheitsregeln

- Anwendungsdateien und Benutzerdaten bleiben strikt getrennt.
- Der Uninstaller enthält bewusst keine `UninstallDelete`-Regel für Benutzerdaten.
- Migration kopiert zunächst in ein eindeutiges Staging-Verzeichnis und verschiebt erst den vollständigen Bestand an den Zielort.
- Die Quelle wird nach erfolgreicher Migration nicht gelöscht.
- Ein nicht leerer Zielordner ohne gültige Root-Datei gilt als Konflikt.
- Release-Artefakte müssen mit SHA-256 signiert und mit einem RFC-3161-Zeitstempel versehen werden.
- Das Release-Setup erhält nach dem Signieren eine SHA-256-Prüfsummendatei.

## Release-Ablauf

1. Alle Änderungen einchecken und CI erfolgreich abschließen.
2. `Version`, `AssemblyVersion` und `FileVersion` in der Projektdatei setzen.
3. Änderungen auf `master` zusammenführen.
4. Signierungs-Secrets im Repository prüfen.
5. Annotierten Tag erstellen und veröffentlichen, beispielsweise `v3.7.0`.
6. Den Workflow „Signed release“ abwarten.
7. Signatur und SHA-256-Prüfsumme des veröffentlichten Setups stichprobenartig prüfen.
8. Upgrade von der zuletzt veröffentlichten Version in einer Windows-Test-VM durchführen.

## Manuelle Abnahmeliste

- [ ] Installation unter einem Standardbenutzer auf Windows 10
- [ ] Installation unter einem Standardbenutzer auf Windows 11
- [ ] Start ohne vorinstalliertes .NET Runtime-Paket
- [ ] Bildanzeige einschließlich ImageMagick-Native-Code
- [ ] Audio- und Videowiedergabe einschließlich VLC-Plugins
- [ ] Upgrade von der zuletzt veröffentlichten Version
- [ ] Upgrade bei zunächst laufender Anwendung
- [ ] Erneutes Ausführen desselben Installers
- [ ] Downgrade wird blockiert
- [ ] Deutsche und englische Oberfläche des Installers
- [ ] Silent Setup und Silent Uninstall liefern Rückgabecode 0
- [ ] Deinstallation entfernt Programmdateien
- [ ] Deinstallation erhält `%LOCALAPPDATA%\NET Thing Encryptor\Data`
- [ ] Migration einer Kopie eines echten portablen Altbestands
- [ ] EXE und Installer besitzen eine gültige, zeitgestempelte Signatur
- [ ] Veröffentlichte SHA-256-Prüfsumme stimmt überein

## Wiederherstellung bei Release-Problemen

Ein fehlerhaftes Release wird nicht durch Überschreiben des bestehenden Tags repariert. Stattdessen wird der Release-Eintrag als problematisch markiert, die Ursache behoben, die Patch-Version erhöht und ein neuer signierter Installer erzeugt. Da Downgrades blockiert sind, muss eine funktional ältere Ersatzversion ebenfalls eine höhere Patch-Version erhalten.
