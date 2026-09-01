# Medienplayer: Video ab Version 4.2.0, Audio ab Version 4.3.0

## Umfang

Version 4.2.0 integriert die Videowiedergabe in den kanonischen Avalonia-Client für Windows und Android. Version 4.3.0 erweitert denselben Wiedergabekern um Audio. Beide verwenden `LibVLCSharp` 3.10.1 und die jeweilige plattformgebundene VideoLAN-Laufzeit. Android bindet `VideoLAN.LibVLC.Android` 3.7.0-beta ein.

Linux und macOS verwenden denselben Desktop-View wie Windows, benötigen jedoch eine kompatible systemweit bereitgestellte LibVLC-Laufzeit. Diese Kombination ist noch nicht praktisch verifiziert und gehört deshalb weiterhin zur technischen Vorschau.

## Plattformarchitektur

Die gemeinsame Anwendung hält Wiedergabestatus, Befehle und Lebenszyklus unabhängig von der Plattform. Das sichtbare Ausgabefenster ist plattformspezifisch:

| Plattform | Videoausgabe |
|---|---|
| Windows | Video über `LibVLCSharp.Avalonia.VideoView`, Audio ohne Bildausgabe; jeweils im eigenen Dokumentfenster |
| Android | Video über eine native, mit Avalonias `NativeControlHost` eingebettete Ansicht; Audio ohne Bildausgabe; `VideoLAN.LibVLC.Android` 3.7.0-beta für `arm64-v8a` und `x86_64` |
| Linux/macOS | Video über Desktop-`VideoView`, Audio ohne Bildausgabe; systemweite LibVLC-Laufzeit erforderlich und noch nicht verifiziert |

Eine Wiedergabesitzung besitzt genau einen LibVLC-Player und genau einen entschlüsselten Eingabestrom. Fenster- beziehungsweise Ansichtswechsel erzeugen keine zweite entschlüsselte Kopie. Audio und Video teilen denselben LibVLC-Dienst; nur Videos fordern eine Plattformoberfläche an. Beim Schließen der Dokumentansicht oder beim Sperren des Tresors wird die Sitzung vollständig beendet und eine eventuell vorhandene native Videoansicht getrennt.

Seit Version 4.3.1 wird das Dokumentfenster beziehungsweise die eingebettete Android-Ansicht bereits vor dem Einlesen des Medieninhalts angezeigt. Ein dokumenteigener Ladezustand bleibt sichtbar, während die Datei entschlüsselt und die Wiedergabesitzung aufgebaut wird. Wird die Ansicht geschlossen oder der Tresor gesperrt, fordert der Dokumentlebenszyklus den Abbruch an; ein bereits zurückgegebener, aber nicht übernommener Klartextpuffer wird überschrieben.

### Entscheidung zur Android-Laufzeit

Die stabile Android-Laufzeit 3.6.5 wurde geprüft, aber nicht übernommen. Der Release-Build meldete damit vier `XA0141`-Warnungen für native Bibliotheken ohne die für Android 16 benötigte Unterstützung von 16-KB-Speicherseiten. `VideoLAN.LibVLC.Android` 3.7.0-beta beseitigt diese Warnungen im aktuellen Build und liefert native Bibliotheken für beide konfigurierten Architekturen `arm64-v8a` und `x86_64`.

Die Beta bleibt ein bewusst akzeptiertes Risiko. Ein warnungsfreier Build belegt weder Decoderstabilität noch fehlerfreien Lebenszyklus auf realen Geräten. Vor jeder Veröffentlichung müssen Audio- und Videowiedergabe, Seek, Hintergrundwechsel, automatische Sperre, wiederholtes Öffnen und Freigabe auf realer Android-Hardware geprüft werden. Die Abnahme soll mindestens ein `arm64-v8a`-Gerät umfassen; Android 16 mit 16-KB-Speicherseiten ist einzubeziehen, sobald entsprechende Hardware verfügbar ist. Emulatorprüfungen ergänzen diesen Test, ersetzen ihn aber nicht.

## Klartext und Speicher

Die verschlüsselte Tresordatei wird in einen seekbaren `MemoryStream` entschlüsselt. LibVLC liest direkt aus diesem Strom; eine entschlüsselte temporäre Datei auf dem Dateisystem wird nicht erzeugt. Dadurch gelten folgende Grenzen:

- Die vollständige entschlüsselte Videodatei bleibt während der Wiedergabesitzung im Arbeitsspeicher. Große Videos erhöhen den Speicherbedarf entsprechend ihrer Dateigröße, zusätzlich zu Decoder- und Bildpuffern.
- Der verwaltete Dateipuffer wird erst nach Stoppen und Freigeben von Player, Medium und Eingabestrom mit Nullen überschrieben. Eine frühere Bereinigung würde laufende oder nachlaufende Lesezugriffe des Decoders beschädigen.
- Native LibVLC- und Decoderpuffer liegen außerhalb der Kontrolle des verwalteten Codes und können nicht verlässlich überschrieben werden. Sie werden durch das Beenden und Freigeben der Wiedergabesitzung an die native Laufzeit zurückgegeben.
- Die automatische Tresorsperre schließt eine offene Wiedergabesitzung. Das gilt für laufende und pausierte Videos sowie für einen Wechsel der Android-App in den Hintergrund.

Diese Speichergrenzen müssen in Sicherheitsbewertung und Supporthinweisen berücksichtigt werden. Das Fehlen einer Klartext-Tempdatei verhindert keine Untersuchung eines laufenden Prozesses oder seines Arbeitsspeichers.

## Bedienung

Der Audio- und Videoplayer bietet sichtbare Schaltflächen für Play/Pause sowie Sprünge um zehn Sekunden und eine seekbare Zeitleiste. Die Zeitposition wird während der Wiedergabe aktualisiert und kann per Zeiger oder Touch geändert werden. Audio zeigt anstelle einer Videooberfläche eine schlichte Medienkarte mit Notensymbol; der Dateiname bleibt in der Dokumentüberschrift sichtbar.

Zusätzlich gelten diese Tastenkürzel im Desktop-Dokumentfenster:

| Eingabe | Aktion |
|---|---|
| Leertaste | Play/Pause |
| Pfeil links oder `J` | zehn Sekunden zurück |
| Pfeil rechts oder `L` | zehn Sekunden vor |

## Lebenszyklus und Fehlerverhalten

Das Öffnen einer Audio- oder Videodatei übernimmt den entschlüsselten Puffer in die Wiedergabesitzung. Schlägt die Initialisierung fehl, wird der Puffer unmittelbar bereinigt und eine verständliche Fehlermeldung angezeigt. Nach erfolgreichem Start gilt diese Reihenfolge beim Schließen:

1. weitere Status- und Zeitereignisse ignorieren;
2. Wiedergabe stoppen und Videoausgabe trennen;
3. Player, Medium, Medieneingabe und Eingabestrom freigeben;
4. verwalteten Klartextpuffer überschreiben;
5. Verweise aus ViewModel und Ansicht entfernen.

Die automatische Sperre und das Schließen des Dokumentfensters verwenden denselben Freigabepfad. Mehrfaches Schließen muss ohne Ausnahme und ohne zweiten Zugriff auf bereits freigegebene native Objekte möglich sein.

## Automatisierte Prüfung

Die gemeinsamen Tests prüfen mindestens:

- Statuswechsel für Play/Pause und Ende der Wiedergabe;
- Begrenzung von Zehn-Sekunden-Sprüngen auf Anfang und bekannte Dauer;
- Formatierung und Aktualisierung der Zeitposition;
- idempotentes Beenden der Sitzung;
- Beenden einer offenen Wiedergabe bei Tresorsperre;
- Bereinigung des verwalteten Puffers nach der Freigabe sowie bei einer fehlgeschlagenen Initialisierung.

Zusätzlich müssen Desktop- und Android-Projekt in Release-Konfiguration gebaut werden. Der Windows-Publish- und Installer-Test prüft, dass Wrapper, native VideoLAN-Laufzeit und Plugins vollständig enthalten sind.

## Manuelle Abnahme

Vor einem Release sind mindestens diese Fälle mit einem kurzen und einem größeren Testvideo zu prüfen:

- Windows 10 und Windows 11: separate Audio- und Videofenster, bei Video Seitenverhältnis und Größenänderung, bei beiden Play/Pause, Zeitleiste, Schaltflächen und alle Tastenkürzel;
- Windows: Fenster während laufender und pausierter Wiedergabe schließen und anschließend dieselbe sowie eine andere Mediendatei erneut öffnen;
- Windows: automatische Sperre auslösen und prüfen, dass Fenster, Wiedergabe und Ton beendet werden;
- Android 12 oder neuer auf realer `arm64-v8a`-Hardware: Audioausgabe, native Videodarstellung, Rotation beziehungsweise Größenänderung, Touchsteuerung, Seek, wiederholtes Öffnen, App-Hintergrund und automatische Sperre;
- `x86_64`-Paketinhalt und Emulatorstart sowie, sobald verfügbar, Android 16 mit 16-KB-Speicherseiten; diese Prüfungen heben das reale Gerätetest-Gate für die Beta-Laufzeit nicht auf;
- mindestens MP4/H.264 sowie ein weiteres im Tresor verwendetes Videoformat mit Tonspur;
- Speicherbeobachtung bei einem größeren Video und Rückgang nach dem Schließen der Sitzung;
- Linux und macOS nur dann als unterstützt ausweisen, wenn Start, Wiedergabe und Paketierung mit der dokumentierten systemweiten LibVLC-Laufzeit erfolgreich geprüft wurden.
