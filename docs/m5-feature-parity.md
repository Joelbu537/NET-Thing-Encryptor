# M5: Gemeinsame Alltagsfunktionen

## Ziel und Abgrenzung

M5 erweitert den Avalonia-Client von der in M3 und M4 aufgebauten sicheren
Tresornavigation zu einem alltagstauglichen gemeinsamen Funktionsschnitt für
Windows und Android. Linux und macOS verwenden denselben Desktop-Host und
werden weiterhin durch Cross-Publishes berücksichtigt.

Der Meilenstein umfasst Suche, Objektverwaltung, eine geschützte Inhaltsansicht,
Textbearbeitung und portable Einstellungen. Audio- und Videowiedergabe,
Bildserien sowie Streaming großer Dateien sind bewusst nicht Teil dieses
Schnitts. Für diese Funktionen muss zuerst ein auf Windows und Android
verlässliches Backend mit einem klaren Speicher- und Lebenszyklusmodell gewählt
werden.

## Suche und Objektverwaltung

Die Ordneransicht bietet zwei Suchwege:

- Die lokale Suche filtert den geladenen Ordner sofort nach Name, Erweiterung
  oder sichtbarem Typ.
- Die globale Suche durchläuft den Tresor und kann Name, Dateityp, Erweiterung,
  minimale und maximale Größe sowie einen Erstellungszeitraum kombinieren.
  Treffer zeigen ihren Ordnerpfad an.

Dateien und Ordner lassen sich umbenennen, in einen gewählten Zielordner
verschieben und nach einer Inline-Bestätigung löschen. Verschieben und Löschen
unterstützen Mehrfachauswahl. Das rekursive Löschen eines Ordners verwendet die
bestehende transaktionale Core-Operation; Zyklen, Selbstverschiebungen und
Namenskonflikte werden weiterhin im Core abgewiesen.

Ein Mehrfachbefehl ruft pro ausgewähltem Objekt eine eigene transaktionale
Operation auf. Scheitert ein späteres Objekt, bleiben vorher erfolgreich
bearbeitete Objekte bestehen. Eine atomare Transaktion über die gesamte Auswahl
ist eine spätere Erweiterung.

## Inhaltsansicht und Textbearbeitung

`VaultDocumentViewModel` übernimmt entschlüsselte Inhalte nur für die Dauer der
Ansicht. Beim Sperren oder Schließen wird eine Bildinstanz freigegeben, ein
laufender Speichervorgang abgebrochen und der übergebene Bytepuffer genullt.
.NET-Zeichenfolgen des Texteditors können wegen ihrer Unveränderlichkeit nicht
gezielt überschrieben werden und werden deshalb beim Schließen nur dereferenziert.

Unterstützte Ansichten:

- Texte öffnen zunächst schreibgeschützt. Bearbeiten, Speichern,
  Rückgängig/Wiederholen, eine Trefferzählung und eine Bestätigung vor dem
  Verwerfen ungespeicherter Änderungen sind verfügbar.
- UTF-8, UTF-16 und UTF-32 mit Byte-Reihenfolge-Markierung werden erkannt und
  beim Speichern mit derselben Kodierung und Markierung geschrieben. Gültiges
  UTF-8 ohne Markierung bleibt UTF-8; für ältere nicht gültige UTF-8-Dateien
  dient Windows-1252 als kompatibler Fallback.
- Bilder werden mit Avalonia/Skia innerhalb der geschützten App-Oberfläche
  dargestellt.
- Audio, Video und unbekannte Dateitypen zeigen eine eindeutige Meldung und
  bleiben über den vorhandenen Export zugänglich.

`ThingData.ReplaceFileContentAsync(...)` ersetzt einen Dateiinhalt innerhalb
einer Core-Mutation. Datei und Elternlink werden gemeinsam gespeichert, die
sichtbare Größe wird aktualisiert und ein Fehler stellt den Storage-Snapshot
wieder her.

## Portable Einstellungen

Der Avalonia-Client liest und schreibt diese bestehenden Root-Einstellungen:

- helles oder dunkles Erscheinungsbild;
- automatische Sperre in Minuten, wobei `0` die Inaktivitätssperre deaktiviert;
- Bildpuffer vor und nach dem aktuellen Bild;
- Zufallsauswahl, Autoplay-Intervall und Wiederholung für eine spätere
  Bildserienansicht.

Darstellung und Sperrzeit werden nach dem Entsperren sowie nach dem Speichern
sofort angewendet. Werte werden sowohl im ViewModel als auch in `ThingRoot`
begrenzt. Desktop-Pfade werden nicht in die gemeinsame Oberfläche übernommen,
weil Android Dokumentanbieter-URIs statt dauerhafter Dateisystempfade verwendet.

## Oberfläche und Lebenszyklus

Die Tresorseite enthält erweiterte Suche, Mehrfachauswahl, Zielordnerauswahl,
Inline-Löschbestätigung und einen Einstellungsbereich. Geöffnete Inhalte ersetzen
die Ordneransicht innerhalb derselben Navigation. System-Zurück schließt zuerst
Inhalt oder Einstellungen, verlässt anschließend Unterordner und sperrt am Root.

Die Inaktivitätssperre kann zur Laufzeit geändert oder deaktiviert werden. Ein
Sperrvorgang bricht die aktive Tresoroperation ab, schließt den dargestellten
Inhalt, leert sichtbare Listen und verwirft die Sitzungsschlüssel.

## Automatisierte und lokale Abnahme

| Prüfung | lokales M5-Ergebnis |
|---|---|
| `Nte.Core.Tests` | 84 bestanden |
| `Nte.Storage.Tests` | 9 bestanden |
| `Nte.App.Tests` | 34 bestanden |
| Gesamtzahl plattformneutraler Tests | 127 bestanden |
| Core-Release-Build | erfolgreich, 0 Warnungen, 0 Fehler |
| Avalonia-Desktop-Release-Build | erfolgreich, 0 Warnungen, 0 Fehler |
| Windows-Startprobe | erfolgreich, Exitcode 0 |
| sichtbare Windows-Erststartprüfung | erfolgreich; Layout und Unlock-/Archivzustand geprüft |
| Android-Debug-Build | erfolgreich, 0 Warnungen, 0 Fehler |
| Android-Release-Build mit AOT | erfolgreich, 0 Warnungen, 0 Fehler |
| Android API-35-Emulator | APK installiert; Kaltstart erfolgreich |
| Android-Kaltstart | `Status: ok`, 8,153 Sekunden; Prozess und Vordergrund-Activity aktiv |
| Android-Logcat | kein fataler .NET-/Android-Eintrag nach dem Kaltstart |
| Linux-x64-Cross-Publish | erfolgreich |
| macOS-ARM64-Cross-Publish | erfolgreich |

Ein paralleler Android-AOT-Lauf stürzte einmal in der lokalen .NET-Android-
Toolchain beim unveränderten AndroidX-ViewModel-Assembly ab und hinterließ
inkonsistente Zwischenartefakte. Nach `dotnet clean` bestand ein frischer
serieller Release-Build vollständig. Dies wurde nicht durch Abschalten von AOT
oder Linker umgangen.

Die neuen Tests decken den verschlüsselten End-to-End-Ablauf für Lesen,
Inhaltsersatz, Größenaktualisierung, Suche, Umbenennen, Verschieben,
Einstellungen und rekursives Löschen ab. Hinzu kommen Tests für Kodierungstreue,
ungespeicherte Texte, dynamische Inaktivitätssperre sowie Einzel- und
Mehrfachauswahl im Vault-ViewModel.

Die bereits lokal geänderten xUnit-4-Paketversionen der Testprojekte gehören
nicht zu M5. Für Core und Storage wurde die veraltete Parallelisierungsoption
nur während des lokalen Testprozesses durch das xUnit-4-Attribut ersetzt und
anschließend bytegenau wiederhergestellt.

## Bekannte Grenzen und Übergabe

- Audio und Video besitzen noch kein gemeinsames internes Wiedergabe-Backend.
- Die Bildansicht zeigt ein einzelnes Bild. Navigation, Pufferung, Zufallsmodus
  und Autoplay verwenden die gespeicherten Einstellungen noch nicht.
- Der Dokumentexport bleibt auf eine einzelne Datei beschränkt; rekursiver
  Ordner- und Mehrfachexport fehlt.
- Das NTE2-Objektformat sowie Text- und Bildansicht puffern einen vollständigen
  Dateiinhalt. Eine belastbare mobile Großdateigrenze fehlt weiterhin.
- Mehrfachverschieben und Mehrfachlöschen sind pro Objekt transaktional, aber
  nicht als gesamte Auswahl atomar.
- Die Windows-Sichtprüfung endete vor einer Passworteingabe. Der entsperrte
  Funktionsfluss ist durch ViewModel-, XAML-Build- und echte verschlüsselte
  Integrationstests abgedeckt, noch nicht durch eine vollständige manuelle
  Windows-Sitzung.
- Android wurde auf einem API-35-x86_64-Automotive-Emulator installiert und
  kalt gestartet. Eine vollständige Touch-Abnahme auf Telefon oder Tablet sowie
  ein reales Gerät bleiben offen.
- Linux und macOS wurden cross-publiziert, in dieser Windows-Umgebung jedoch
  nicht nativ gestartet.

## M5-Abschlusskriterien

- [x] Lokale und erweiterte globale Dateisuche umgesetzt.
- [x] Umbenennen, Verschieben und rekursives Löschen angebunden.
- [x] Mehrfachauswahl für Verschieben und Löschen umgesetzt.
- [x] Textansicht und Textbearbeitung mit kodierungstreuem Speichern umgesetzt.
- [x] Einzelbildansicht umgesetzt.
- [x] Portable Einstellungen gespeichert und Darstellung/Sperrzeit angewendet.
- [x] Inhaltsersatz im Core transaktional abgesichert.
- [x] Windows- und Android-Debug-/Release-Builds erfolgreich.
- [x] Android-APK auf API 35 installiert und kalt gestartet.
- [x] Linux-x64 und macOS ARM64 weiterhin cross-publizierbar.
- [ ] Audio-/Videowiedergabe mit gemeinsamem Backend umgesetzt.
- [ ] Bildseriennavigation, Pufferung und Autoplay umgesetzt.
- [ ] Rekursiver Ordner-/Mehrfachexport umgesetzt.
- [ ] Mobile Großdateistrategie und reales Android-Gerät abgenommen.
