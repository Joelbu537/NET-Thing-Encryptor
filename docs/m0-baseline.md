# M0 – gesicherte WinForms-Baseline

Stand: 29. August 2026

Produktversion: 3.7.0

Baseline-Tag: `migration-m0-baseline-v3.7.0`

## Zweck und Umfang

M0 friert das beobachtbare Verhalten und das Datenformat der bestehenden WinForms-Anwendung ein, bevor die plattformübergreifende Migration beginnt. Der Stand enthält noch keine Avalonia- oder Android-Implementierung. Änderungen nach M0 müssen entweder kompatibel zu dieser Baseline bleiben oder eine ausdrücklich versionierte Migration erhalten.

Der Quellstand wird auf dem Branch `codex/cross-platform-migration` gesichert. Der Tag bezeichnet die vollständige M0-Baseline einschließlich der zuvor im Arbeitsverzeichnis vorhandenen Version-3.7-Arbeiten und der in M0 hinzugefügten Golden-Fixtures.

## Reproduzierbare Toolchain

| Bestandteil | M0-Wert |
|---|---|
| .NET SDK | `11.0.100-preview.7.26381.103` |
| Zielframework | `net11.0-windows10.0.22000.0` |
| Windows-Mindestversion | Windows 10, Build 19041 |
| Laufzeit/Paket | selbstenthaltend, `win-x64`, Multi-File |
| Inno Setup | 6.7.3 |
| Anwendungsversion | 3.7.0 |
| Test-Stack | Microsoft.NET.Test.Sdk 18.7.0, xUnit 3.2.2 |
| Medien/Bilder | LibVLCSharp 3.10.1, LibVLC Windows 3.0.23.1, Magick.NET 14.16.0 |

Im Repository existiert in M0 keine `global.json`. Für reproduzierbare Builds muss daher das oben genannte SDK ausdrücklich ausgewählt werden. .NET 11 ist zu diesem Stichtag eine Preview und für Produktion noch nicht freigegeben; die Wahl ist Bestandteil der eingefrorenen Baseline, nicht automatisch die Vorgabe für die neue Architektur.

Referenzbefehle:

```powershell
dotnet test ".\NET Thing Encryptor.sln" -c Release
.\build\build-installer.ps1 -Clean -ExpectedVersion 3.7.0
.\build\test-installer.ps1 `
  -InstallerPath ".\artifacts\installer\NET-Thing-Encryptor-Setup-3.7.0.exe" `
  -ExpectedVersion 3.7.0
```

## Verifizierter Ausgangszustand

Die folgenden Prüfungen wurden am M0-Stand lokal unter Windows ausgeführt:

| Prüfung | Ergebnis |
|---|---|
| Release-Testlauf | 90 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Self-contained Publish | `win-x64` erfolgreich; VLC- und ImageMagick-Bestandteile vorhanden |
| Inno-Setup-Build | erfolgreich |
| Statische Installer-Prüfung | Version und SHA-256 erfolgreich geprüft |
| Installergröße | 94.564.547 Bytes |
| Installer-SHA-256 | `5aa7a07d2d3d84cc039c47059cb5abed7fcb7fb97c4ea03125deacd363c58406` |
| Code-Signatur | nicht vorhanden; für einen lokalen Baseline-Build erwartet |

Nicht lokal ausgeführt wurden der Installations-/Deinstallations-Smoke-Test und eine Signaturprüfung. Der Smoke-Test verändert das Benutzerprofil und die Uninstall-Registry und darf nur in einem isolierten CI-Konto oder einer Windows-Test-VM laufen. Die vorhandene CI führt ihn für englische und deutsche Installation aus und prüft, dass Benutzerdaten die Deinstallation überstehen.

## Golden-Fixture `baseline-v3.7`

Das Verzeichnis `NET Thing Encryptor.Tests/Fixtures/baseline-v3.7` enthält ausschließlich synthetische Daten. Das Fixture deckt Root, Ordner, Unicode-Text mit UTF-8-BOM, PNG, WAV, MP4 und ein historisches AES-CBC-Objekt ab. Die absichtlich künstlichen absoluten Windows-Pfade machen die spätere Pfadmigration sichtbar.

Fixture-Passwort: `M0 synthetic baseline password`

| Datei/ID | Inhalt | SHA-256 |
|---|---|---|
| `0.nte` | Root-Metadaten und verschlüsselte Linkliste | `cdac9d4a3dce09b62ea560297e260becda8e60d17e8b9e13b484a3f42fbf33e0` |
| `1000000000000001.nte` | Ordner „M0 Baseline“ | `21c01ba13135a8fa932d7c70b919206f35498a4cb7ef4dfd718891cd6d18be01` |
| `2000000000000001.nte` | Text „Unicode notes“ | `6901dae9002f24b4386c58be9f7be87e3d3a4e79b99c080ef831e6e36fb9f075` |
| `3000000000000001.nte` | synthetisches 1×1-PNG | `bc6779b3c239e86c3dee8f9ee5002fb31147ad426dae033551d00c36cf501e74` |
| `4000000000000001.nte` | synthetischer WAV-Ton | `2388b21a75c489bcd85f0d2544eee758252f540506fea516c0705073aec9f123` |
| `5000000000000001.nte` | synthetischer MP4-Clip | `ed5583f3b5c795275f8e10e21e2958fb21f9dea57de9da8c163cb80b43a638e5` |
| `legacy-cbc.nte` | historischer CBC-Payload | `32ff18954aa0b4a20ffb586681dbf757290f1620e1366692ee892ff0ae5827ee` |

`manifest.sha256` und `BaselineCompatibilityTests` erkennen jede unbeabsichtigte Veränderung. Dieses Verzeichnis darf nicht neu erzeugt oder überschrieben werden. Bei einer absichtlichen Formatänderung wird ein neues, versioniertes Fixture-Verzeichnis hinzugefügt; alte Fixtures bleiben als Lesekompatibilitätsvertrag erhalten.

## Daten- und Sicherheitsvertrag

Die Migration muss folgende Invarianten bewahren:

- `0.nte` ist ein JSON-Dokument mit Salt, Einstellungen, Speicher-/Import-/Exportpfaden und `ContentEncrypted`. Die entschlüsselte Root-Linkliste und damit die sichtbaren Objektnamen dürfen nicht im Klartext der Root-Datei stehen.
- Objektdateien heißen `<16-stellige Hex-ID>.nte`. ID `0` ist für die Root reserviert; neue IDs sind ungleich null und kollisionsfrei.
- Neue verschlüsselte Payloads beginnen mit `NTE2` und verwenden AES-GCM mit 12-Byte-Nonce, 16-Byte-Tag und dem Header als Associated Data. Jeder Schreibvorgang verwendet eine frische Nonce. Manipulierte oder unvollständige Daten werden abgewiesen.
- Der Schlüssel wird mit PBKDF2-HMAC-SHA-256, 10.000 Iterationen und dem Root-Salt abgeleitet. Die 48 Ausgabebytes werden in 32 Byte AES-Schlüssel und 16 Byte historischen IV geteilt.
- AES-CBC ohne `NTE2`-Header bleibt lesbar. Neue Daten werden ausschließlich im authentifizierten `NTE2`-Format geschrieben.
- Datei- und Root-Schreibvorgänge erfolgen über eine temporäre Datei und einen atomaren Move. Temporäre Dateien dürfen nach Erfolg oder Fehler nicht verbleiben.
- Mehrfachschritte wie Verschieben, Umbenennen und rekursives Löschen werden serialisiert und über verschlüsselte Backups zurückgerollt, wenn ein Teilschritt fehlschlägt. Unbeteiligte gesperrte Tresordateien dürfen Operationen nicht verhindern.
- Eine portable Altinstallation wird über ein Staging-Verzeichnis vollständig in das Benutzerprofil kopiert. Die Quelle bleibt erhalten; Konflikte führen zum Abbruch statt zum Überschreiben.
- `SaveLocation`, `ImportLocation` und `ExportLocation` werden aktuell als absolute Windows-Pfade gespeichert. Das ist eine bekannte Portabilitätsgrenze und benötigt vor Android/macOS/Linux eine definierte Pfadstrategie.
- Die Root-Datei liegt im App-Datenverzeichnis, Objektdateien liegen unter `Root.SaveLocation`; ein Tresor kann daher logisch über zwei Orte verteilt sein.
- Verschlüsseln und Entschlüsseln puffert einen Objektinhalt derzeit vollständig im Speicher. Ab 256 MiB warnt die Windows-Oberfläche; dies ist eine bekannte Grenze für Android und ein Kandidat für späteres Streaming.
- Beim Sperren werden Schlüssel und historischer IV aus dem Prozesszustand entfernt und überschrieben; die entschlüsselte Root-Linkliste wird verworfen.

## Verhaltensinventar der Windows-Anwendung

### Start, Sitzung und Navigation

- Pro Benutzer läuft nur eine Instanz; eine zweite Instanz wird über den Mutex abgewiesen.
- Beim ersten Start werden Datenverzeichnis und Root angelegt. Bestehende portable Daten werden nach den oben genannten Konfliktregeln migriert.
- Der Passwortdialog entsperrt den Tresor; ein falsches Passwort verändert die Baseline nicht. Inaktivität sperrt nach der konfigurierten Zeit, wobei `0` die Sperre deaktiviert.
- Root- und Ordnernavigation zeigt Name, Typ, Größe und Änderungsdatum. Namen mit Zahlensegmenten werden natürlich sortiert.
- Lokale Suche filtert die aktuelle Ansicht. Die globale Suche läuft rekursiv und kombiniert Name, Erweiterung, Typ, Größe und Zeitfilter; Ordner erscheinen nicht als Treffer.

### Datei- und Ordneroperationen

- Mehrere Dateien können in einen Ordner importiert werden. Dateien dürfen nicht direkt in der Root liegen. Namenskonflikte erhalten einen eindeutigen Suffix.
- Ordner können auch in der Root erstellt werden. Umbenennen verlangt innerhalb desselben Elternobjekts einen case-insensitiv eindeutigen Namen.
- Mehrfachauswahl kann markiert und gemeinsam verschoben, exportiert oder gelöscht werden. Verschieben verhindert Root-, Selbst- und Zyklusziele.
- Löschen verlangt eine Bestätigung; Ordner werden rekursiv mit allen Nachkommen entfernt. Bei Fehlern bleiben Referenzen und Dateien konsistent.
- Export erhält Inhalt und Erweiterung, exportiert Ordner rekursiv und löst Dateisystemkonflikte mit einem nummerierten Suffix wie `(2)`.
- Erstellte Textdateien und importierte Binärdateien behalten Typ, Größe, Zeitstempel und Elternbezug im verschlüsselten Modell.

### Einstellungen

- Änderbar sind verschlüsselter Speicherort, Standard-Import- und Exportordner, Hell-/Dunkelmodus, automatische Sperrzeit, Bildpuffer, Zufallsreihenfolge, Autoplay-Intervall und Schleifenwiedergabe.
- Beim Wechsel des verschlüsselten Speicherorts werden Objektdateien verschoben und der Root-Eintrag erst nach erfolgreichem Abschluss aktualisiert.
- Werte werden validiert und begrenzt; ungültige oder fehlende ältere Werte erhalten definierte Standardwerte.

### Text- und Notfalleditor

- Text öffnet zunächst schreibgeschützt. Bearbeiten kann eingeschaltet werden; Undo, Redo, Suche/F3, Speichern und eine Rückfrage bei ungespeicherten Änderungen bleiben erhalten.
- UTF-8, UTF-16 LE/BE und UTF-32 LE/BE werden anhand einer BOM erkannt und mit derselben BOM-Konvention gespeichert. Gültiges UTF-8 ohne BOM bleibt UTF-8; sonst dient Windows-1252 als Rückfall.
- Schriftfamilie und Schriftgröße sind einstellbar und werden auf gültige Werte begrenzt.
- Der Notfalleditor zeigt entschlüsseltes internes JSON zunächst schreibgeschützt. Vor einer Änderung wird automatisch ein verschlüsseltes Backup erzeugt. Formatieren, Validieren und Speichern akzeptieren nur gültiges JSON. Ein entschlüsselter Export ist möglich, aber deutlich zu warnen.

### Bild, Audio und Video

- Die Bildansicht navigiert per Schaltfläche, Klick und Tastatur vor/zurück und zeigt Position und Dateiname. Vor-/Nachpuffer, animierte GIFs, ImageMagick-Downscaling, Zufallsreihenfolge, Autoplay und Schleife gehören zum Vertrag.
- Audio und Video werden aus dem entschlüsselten Speicherstrom über LibVLC wiedergegeben. Play/Pause, Zeitleiste, Seek per Tastatur und Statusaktualisierung bleiben erhalten; Audio besitzt eine eigene Metadaten-/Visualanzeige.
- Sekundärfenster geben Bild-, Stream- und native Medienressourcen beim Schließen frei. Der Hauptdialog blockiert das Beenden während aktiver Speichervorgänge; eine automatische Sperre schließt offene Inhaltsfenster.

## Abnahme für die Migrationsschritte

Diese Liste wird nach jedem vertikalen Migrationsschnitt gegen die WinForms-Baseline ausgeführt. Automatisierbare Punkte sollen als Tests umgesetzt werden; visuelle und native Medienpunkte bleiben zusätzlich manuell.

### Gemeinsame Kernlogik

- [ ] Alle alten `baseline-v3.7`-Dateien lassen sich mit dem Fixture-Passwort unverändert lesen.
- [ ] Falsches Passwort, manipuliertes GCM und unvollständige Daten werden abgewiesen.
- [ ] Neu gespeicherte Dateien verwenden `NTE2` und eine frische Nonce.
- [ ] Legacy-CBC bleibt lesbar; das Lesen verändert die Quelldatei nicht.
- [ ] Import, Umbenennen, Verschieben, rekursives Löschen und Export liefern dieselben Modellresultate.
- [ ] Fehler während einer Mutation rollen alle betroffenen Root-, Parent- und Objektänderungen zurück.
- [ ] Globale Suche und natürliche Sortierung liefern dieselben Treffer und Reihenfolgen.

### Windows

- [ ] Start, Passwort, Erstlauf, Einzelinstanz und automatische Sperre funktionieren auf Windows 10 und 11.
- [ ] Alle Hauptoperationen sind mit Maus und Tastatur erreichbar; Fokus, Mehrfachauswahl und Dialogbestätigungen bleiben verständlich.
- [ ] Hell-/Dunkelmodus sowie Text-, Bild-, Audio- und Videoansicht sind funktional und visuell geprüft.
- [ ] Upgrade von 3.7.0 erhält den vorhandenen Tresor und alle Einstellungen.
- [ ] Self-contained Paket, signierter Installer, englischer/deutscher Installations-Smoke-Test und Deinstallation ohne Datenverlust sind grün.

### Android

- [ ] Ein kopierter 3.7.0-Tresor kann über plattformgerechte Datei-/Ordnerauswahl geöffnet oder importiert werden.
- [ ] Pfade werden ohne Windows-Laufwerksannahmen auf App-Sandbox und Storage Access Framework abgebildet.
- [ ] App-Wechsel, Display-Sperre und Timeout entfernen Schlüssel und entschlüsselte Inhalte aus der aktiven Sitzung.
- [ ] System-Zurück, Touch-Auswahl, Freigabe/Export und Berechtigungsablehnung sind definiert und getestet.
- [ ] Große Dateien überschreiten das festgelegte Speicherbudget nicht; Abbruch und niedriger Speicher führen nicht zu beschädigten Daten.
- [ ] Bild, Audio und Video verwenden Android-taugliche Backends und werden auf mindestens einem realen Gerät geprüft.

### Linux und macOS

- [ ] Kern- und Fixture-Tests laufen ohne Windows-Abhängigkeit.
- [ ] Pfade, Dateinamen, case-sensitive Dateisysteme und Dateiauswahl sind abgedeckt.
- [ ] Anwendung startet, Tresoroperationen funktionieren, und mindestens je ein Bild-, Audio- und Videoformat wurde geprüft.
- [ ] Paketformat und Signierungs-/Notarisierungsbedarf sind dokumentiert, auch wenn die Distribution nach Windows und Android folgt.

## Offene Baseline-Risiken

- Der lokale Installer ist unsigniert; eine echte Release-Signatur kann nur mit dem geschützten Zertifikat geprüft werden.
- Der vollständige Installations-/Deinstallations-Smoke-Test wurde lokal nicht ausgeführt und wartet auf CI oder eine isolierte Windows-VM.
- Die 90 automatisierten Tests decken Kernlogik und ausgewählte WinForms-Komponenten ab, aber keine vollständige visuelle Bedienung und keine reale VLC-/ImageMagick-Wiedergabe.
- Absolute Windows-Pfade, vollständige Speicherpufferung und Windows-spezifische Dialoge/Medienpakete sind die größten bekannten Risiken für Android.
- M0 definiert Rückrollen bei abgefangenen Operationsfehlern. Verhalten bei Prozessabbruch, Stromverlust oder vollem Datenträger braucht in der neuen Persistenzschicht eigene Fehler-Injektionstests.
- Linux und macOS besitzen in M0 weder Buildartefakte noch ausgeführte Plattformtests; sie bleiben ausdrücklich im Abnahmeumfang der späteren Meilensteine.

## M0-Abschlusskriterien

- [x] Bestehende Testbaseline erfasst und erfolgreich ausgeführt.
- [x] Synthetisches, personenbezugsfreies Golden-Fixture eingecheckt und mit SHA-256 geschützt.
- [x] Lesekompatibilität für Root, Ordner, Text, Bild, Audio, Video und Legacy-CBC automatisiert geprüft.
- [x] Self-contained Windows-Publish und statischer Installer-Test erfolgreich.
- [x] Datenvertrag, Nutzerverhalten, Plattformabnahme und bekannte Risiken dokumentiert.
- [x] Branch, Baseline-Commits und annotierter Tag erstellt.
- [ ] Installations-Smoke-Test in isolierter Windows-Umgebung bestätigt.
- [ ] Releaseartefakte mit produktivem Zertifikat signiert.

Die letzten beiden Punkte gehören zur Release-Abnahme und blockieren nicht den lokalen M0-Quellcode-Tag. Sie müssen vor einer produktiven 3.7.0-Veröffentlichung beziehungsweise vor dem Abschalten der WinForms-Version erfüllt werden.
