# Installer-Spezifikation

## Ziel und Grenzen

Der Installer verteilt den Avalonia-Desktop-Client von NET Thing Encryptor als per-user installierte, selbstenthaltende Windows-x64-Anwendung. Inno Setup bleibt die Installer-Engine. Ein eigener Bootstrapper, automatische Online-Updates, Dateizuordnungen und ein maschinenweiter Installationsmodus gehören derzeit nicht zum Umfang.

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
| Upgrade von WinForms | Gleiche App-ID und gleicher Datenpfad; eindeutig alte VLC-/ImageMagick-Dateien werden entfernt |
| Silent Setup | Keine Interaktion; Rückgabecode ungleich null bei Fehlern |

## Versionierung

Die kanonische Windows-Produktversion steht in `Nte.Desktop/Nte.Desktop.csproj` unter `Version`. `AssemblyVersion` und `FileVersion` enthalten vier numerische Komponenten. Das Buildskript übernimmt Installer- und Dateiversion direkt aus dieser Projektdatei. `Nte.Android/Nte.Android.csproj` muss dieselbe sichtbare `Version` und zusätzlich einen mit jedem Android-Release steigenden ganzzahligen `ApplicationVersion`-Wert enthalten.

Release-Tags verwenden das Format `v<Version>`. Ein Tag, der nicht zur Projektversion passt, wird von der Release-Pipeline abgewiesen.

## Daten- und Sicherheitsregeln

- Anwendungsdateien und Benutzerdaten bleiben strikt getrennt.
- Der Uninstaller enthält bewusst keine `UninstallDelete`-Regel für Benutzerdaten.
- Migration kopiert zunächst in ein eindeutiges Staging-Verzeichnis und verschiebt erst den vollständigen Bestand an den Zielort.
- Die Quelle wird nach erfolgreicher Migration nicht gelöscht.
- Ein nicht leerer Zielordner ohne gültige Root-Datei gilt als Konflikt.
- Windows-Release-Artefakte müssen authenticode-signiert und mit einem RFC-3161-Zeitstempel versehen werden.
- Android-Release-Artefakte müssen mit dem dauerhaft gesicherten Produktionsschlüssel signiert werden.
- Das Release-Setup erhält nach dem Signieren eine SHA-256-Prüfsummendatei.
- AAB und APK erhalten nach der Signaturprüfung jeweils eine SHA-256-Prüfsummendatei.

## Release-Ablauf

1. Alle Änderungen einchecken und CI erfolgreich abschließen.
2. `Version`, `AssemblyVersion` und `FileVersion` in `Nte.Desktop` setzen.
3. Dieselbe `Version` und einen höheren `ApplicationVersion`-Wert in `Nte.Android` setzen.
4. Änderungen auf `master` zusammenführen.
5. Windows- und Android-Signierungs-Secrets im Repository prüfen.
6. Annotierten Tag erstellen und veröffentlichen, beispielsweise `v3.7.0`.
7. Den Workflow „Signed release“ abwarten.
8. Signaturen und SHA-256-Prüfsummen der veröffentlichten Dateien stichprobenartig prüfen.
9. Upgrade von der zuletzt veröffentlichten WinForms- beziehungsweise Avalonia-Version in einer Windows-Test-VM durchführen.

## Manuelle Abnahmeliste

- [ ] Installation unter einem Standardbenutzer auf Windows 10
- [ ] Installation unter einem Standardbenutzer auf Windows 11
- [ ] Start ohne vorinstalliertes .NET Runtime-Paket
- [ ] Automatische Startprobe der installierten Avalonia-Anwendung
- [ ] Bildanzeige für die in M5 unterstützten Formate
- [ ] Upgrade von der zuletzt veröffentlichten WinForms-Version
- [ ] Upgrade von der zuletzt veröffentlichten Avalonia-Version
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
- [ ] Signiertes AAB lässt sich in einen internen Android-Testkanal laden
- [ ] Signierte APK lässt sich auf einem realen Android-12+-Gerät installieren und starten
- [ ] Sperren im Hintergrund und Dokumentanbieter-Import/-Export auf einem realen Gerät geprüft

## Wiederherstellung bei Release-Problemen

Ein fehlerhaftes Release wird nicht durch Überschreiben des bestehenden Tags repariert. Stattdessen wird der Release-Eintrag als problematisch markiert, die Ursache behoben, die Patch-Version erhöht und ein neues signiertes Release erzeugt. Da Windows-Downgrades blockiert sind und Android einen monoton steigenden Versionscode verlangt, muss auch eine aus dem WinForms-Rückfallzweig gebaute Ersatzversion höhere Desktop- und Android-Versionswerte erhalten. Der Rückfall darf niemals ein älteres Datenformat zurückschreiben; vor Aktivierung wird eine Kopie eines produktionsnahen Tresors mit beiden Clients geprüft.
