# M8 – Nach-Cutover-Härtung

## Ziel und Abgrenzung

M8 schließt nach dem Avalonia-Cutover die ohne neues Tresorformat oder native Medienlaufzeit lösbaren M5-Lücken. Der gemeinsame Client erhält Bildserien sowie Mehrfach- und Ordnerexport. Gleichzeitig werden die verbleibenden Medien- und Großdateigrenzen ausdrücklich entschieden, damit sie nicht länger als unbestimmte vermeintliche Paritätsarbeit geführt werden.

M8 ändert weder NTE2, Schlüsselableitung noch Speicherpfade und entfernt keinen WinForms-Code. Der in M7 eingefrorene Rückfallpfad erhält keine der neuen Produktfunktionen.

## Mehrfach- und Ordnerexport

Ein einzelnes Dokument verwendet weiterhin den Speicherdialog mit sichtbarer Überschreibbestätigung. Sobald mehrere Objekte oder ein Ordner ausgewählt sind, wählt der Benutzer stattdessen einen Zielordner:

```mermaid
flowchart LR
    Selection[Dateien und Ordner auswählen]
    Picker[System-Zielordner wählen]
    Walk[Ordner zyklensicher traversieren]
    Names[Portable eindeutige Namen erzeugen]
    Stream[Dateien in Provider-Streams exportieren]
    Selection --> Picker --> Walk --> Names --> Stream
```

Der neue `IWritableExternalFolder`-Vertrag bildet sowohl normale Desktop-Verzeichnisse als auch Android-Dokumentanbieter ab. Er erstellt Dateien und Unterordner, ohne bestehende Namen zu verwenden. Namenskollisionen erhalten einen Zähler vor der Dateiendung, beispielsweise `Bericht (2).pdf`. Steuerzeichen, Pfadtrenner, plattformkritische Zeichen und reservierte Windows-Namen werden in portable Segmente überführt.

Die Tresorstruktur wird rekursiv beibehalten, leere Ordner eingeschlossen. Ein beschädigter Ordnerzyklus bricht den Export ab. Bereits erfolgreich geschriebene externe Dateien können über allgemeine Dokumentanbieter nicht zuverlässig transaktional zurückgerollt werden; ein Fehler wird deshalb ausdrücklich als unvollständiger Export gemeldet. Vorhandene Zieldaten werden nicht als Rollback-Ersatz gelöscht oder überschrieben.

## Bildserien

Beim Öffnen eines Bildes stellt der Client alle Bilder des aktuellen Ordners bereit. Bei globalen Suchtreffern bildet die sichtbare Ergebnismenge die Serie. Die Ansicht unterstützt:

- vorheriges und nächstes Bild;
- Positionsanzeige und aktualisierten Dateinamen;
- zufällige Reihenfolge unter Berücksichtigung der vorhandenen Einstellung;
- Autoplay mit gespeichertem Intervall;
- optionales Wiederholen am Ende der Serie.

Manuelle Navigation oder eine neue Zufallsreihenfolge beendet laufendes Autoplay. Sperren, Schließen und Dispose brechen Timer und Bildladevorgänge ab. Jeder entschlüsselte Eingabepuffer wird nach der Dekodierung genullt, und die vorherige Bitmap wird nach einem erfolgreichen Wechsel freigegeben.

### Bewusste Speicherentscheidung

Der gemeinsame Client hält nur das aktuell dargestellte entschlüsselte Bild. Er lädt keine Klartextbilder im Voraus. Die historischen Vor-/Nachpufferwerte bleiben im Root-Format erhalten, damit der WinForms-Rückfallstand sie weiter lesen kann, werden in Avalonia aber nicht angewendet. Diese Entscheidung begrenzt den mobilen Arbeitssatz und die Lebensdauer entschlüsselter Bilddaten. Eine spätere Cache-Implementierung benötigt zuerst ein gemessenes Speicherbudget auf realer Android-Hardware.

## Verbleibende Produktentscheidungen

| M5-Lücke | M8-Entscheidung |
|---|---|
| Bildseriennavigation, Zufallsmodus und Autoplay | umgesetzt |
| Bildpufferung | bewusst abgelehnt, solange kein gemessenes mobiles Speicherbudget vorliegt |
| Mehrfach- und rekursiver Ordnerexport | umgesetzt; externe Teilergebnisse sind bei Providerfehlern nicht atomar |
| Audio- und Videowiedergabe | für die aktuelle Produktlinie als Export-only akzeptiert; ein späteres Vorhaben muss Windows und Android, Hintergrundwechsel, native Paketgröße, Lizenzierung und Hardwaretests gemeinsam lösen |
| vollständiges Puffern eines NTE2-Objekts | als Formatgrenze akzeptiert; bestehende Tresore erhalten kein neues Größenlimit, Streaming-Verschlüsselung erfordert ein versioniertes Folgeformat |
| atomare Mehrfachmutation | pro Objekt atomar bleibt akzeptiertes Verhalten; UI meldet Teilerfolge bei Fehlern |
| Linux-/macOS-Endnutzerpakete | bleiben wegen der Plattformpriorisierung technische Vorschau und ein eigener Distributionsmeilenstein |

Diese Entscheidungen erfüllen das M7-Kriterium, offene M5-Funktionen umzusetzen oder bewusst anzunehmen. Sie erfüllen nicht die weiterhin offenen Produktions-, Zeit- und Hardware-Gates für das Löschen des WinForms-Rückfallpfads.

## M8-Abnahme

Die Abnahme umfasst:

- Tests für portable und kollisionsfreie Exportnamen;
- rekursiven Export gemischter Mehrfachauswahl einschließlich vorhandener Zielnamen;
- Bildserienübergabe aus der Ordneransicht;
- Navigation, Zufallsreihenfolge, Autoplay-Ende und Freigabe beim Dispose;
- alle gemeinsamen Core-, Storage- und App-Tests;
- Windows-Desktop- und Android-Build sowie Linux-/macOS-Cross-Publish;
- isolierte Windows-Startprobe;
- unveränderten Stand der drei nicht zu M8 gehörenden Testpaketänderungen.

Ein vollständiger Touch- und Speichertest auf realer Android-Hardware sowie native Linux-/macOS-Paketierung bleiben externe Folgegates und werden nicht als lokale M8-Abnahme ausgegeben.

### Lokales Ergebnis vom 31. August 2026

| Prüfung | Ergebnis |
|---|---|
| `Nte.Core.Tests` | 84 bestanden, 0 fehlgeschlagen |
| `Nte.Storage.Tests` | 9 bestanden, 0 fehlgeschlagen |
| `Nte.App.Tests` | 43 bestanden, 0 fehlgeschlagen |
| gemeinsame Tests gesamt | 136 bestanden, 0 fehlgeschlagen |
| Windows-Desktop, Release | erfolgreich, 0 Warnungen, 0 Fehler |
| isolierte Windows-Startprobe | erfolgreich, Exitcode 0 |
| Android Debug | erfolgreich, 0 Warnungen, 0 Fehler; signiertes APK erzeugt |
| Android Release mit AOT | erfolgreich, 0 Warnungen, 0 Fehler; signiertes APK und AAB erzeugt |
| Linux `linux-x64` | Cross-Publish erfolgreich; Laufzeitprobe bleibt Aufgabe des Linux-CI-Jobs |
| macOS `osx-arm64` | Cross-Publish erfolgreich; Laufzeitprüfung bleibt Aufgabe des macOS-CI-Jobs |

Die bereits vor M8 geänderten Projektdateien `Nte.Core.Tests.csproj`, `Nte.Storage.Tests.csproj` und `Nte.App.Tests.csproj` wurden nicht in M8 übernommen oder zurückgesetzt. Für die lokalen Core- und Storage-Läufe wurden lediglich die zugehörigen `AssemblyInfo.cs`-Dateien temporär an den vorhandenen xUnit-4-Arbeitsstand angepasst und anschließend bytegenau wiederhergestellt.
