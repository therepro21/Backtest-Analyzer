# MT5-Tester-Cacheimport (Beta 0.3)

Der portable Analyzer liest gespeicherte `.tst`-Backtests lokal. Er startet keine Tests, handelt nicht und installiert keine Erweiterung in MT5. „MT5 finden“ zeigt geöffnete `terminal64.exe`-Instanzen mit Prozess-ID, Fenstertitel, Programmpfad und zugeordnetem Datenordner. `origin.txt` verbindet normale Installationen mit Datenordnern; das über die vorhandene Windows-WMI-Schnittstelle gelesene `/portable`-Startargument unterscheidet portable Instanzen. Unlesbare Startargumente werden als unsichere Zuordnung markiert. Weitere Daten-/Exportordner können manuell ausgewählt werden. Nicht gestartete portable Installationen an beliebigen Orten werden nicht durch eine Suche über alle Laufwerke erkannt.

## Unterstützter Stand

Nur Cacheformat **505** ist freigegeben, experimentell anhand zweier lokaler aktueller MT5-Backtests geprüft. Andere Versionen werden abgewiesen. Die eigenständige C#-Implementierung verwendet keine fremde DLL und benötigt keine separat installierte Excel- oder .NET-Laufzeit.

Die öffentlich einsehbaren Strukturbeschreibungen der [SingleTesterCache-Bibliothek](https://www.mql5.com/en/code/27611) dienten als Recherchequelle. Ihr veröffentlichter Headerstand 502 wurde nicht blind übernommen: Version 505 besitzt hier eine Summary von 1.640 Byte. Unser Reader prüft alle Abschnittsgrößen, vier Datensatzanzahlen, Dateiende, Zeitfolge, endständige Cashflow-Balance und eine Änderung der Datei während des Imports.

| Abschnitt | Größe / Position |
|---|---|
| Header | 1.456 Byte; Version int32 bei 0, Formatkennung UTF16 bei 132 |
| Parameter | `parameters_size` bei 1.448, anschließend UTF16 ab 1.456 |
| Summary | nach Parametern, 1.640 Byte |
| Deals | int32 Anzahl, danach je 256 Byte |
| Orders | int32 Anzahl, danach je 224 Byte (derzeit übersprungen) |
| Positionstatistik | int32 Anzahl, danach je 64 Byte (derzeit nicht zur Zuordnung verwendet) |
| Graphpunkte | int32 Anzahl, danach je 32 Byte |

Deals enthalten ID, Order, Sekundentimestamp, Symbol, Richtung, Volumen, Profit, Kommission, Swap und echte Position-ID bei Offset 248. Eine separate Fee ist in diesem geprüften Layout nicht vorhanden. Die ID-Felder der separaten Positionstatistik sind in den geprüften Dateien null und werden deshalb nicht zur Verknüpfung verwendet.

## Datenquellen strikt trennen

- **Balance:** vollständige Deal-/Kontobuchungen; beim Ergänzen eines HTML/XLSX-Berichts bleibt dessen Balance-History erhalten. Die Anzeige fasst standardmäßig feste Fenster von bis zu 60 Sekunden ab der ersten geänderten Buchung zusammen und zeigt deren Schlussbestand. Rohbuchungen und Kostenberechnungen bleiben erhalten. 0 Sekunden zeigt Rohbuchungen. Eine Kontoglättung ist keine Änderung der finanziellen Originalwerte und kein nachträglicher Nachweis eines EA-Zyklus.
- **Equity und Equity-DD:** originale gespeicherte Graphpunkte, keine künstliche Rekonstruktion aus geschlossenen Trades. DD bezieht sich auf das bisherige Equity-Hoch. Diese Punkte sind keine vollständige Tickhistorie; DD-Zeitpunkte sind Zeitpunkte der gespeicherten Beobachtungen in Tester-/Broker-Zeit.
- **Cache-Balance-Feld:** in den geprüften Dateien teilweise im Widerspruch zum vollständigen Deal-Cashflow. Es wird deshalb nicht als Kontostandskurve verwendet. Für diese Abweichung ist keine belastbare Ursache nachgewiesen.
- **Kontobelastung:** gespeicherter MT5-Graphwert; keine Zusage eines vollständig erfassten Maximums oder synchronisierter Kontosnapshots. Der Originalbericht kann einen anderen aus sämtlichen Testzuständen ermittelten Grenzwert enthalten. Aus gespeicherten Nullwerten folgt nicht, dass Margin tatsächlich null war.
- **Ergänzen eines Berichts:** jeder Deal muss in ID, Zeit, Symbol, Seite, Richtung, Volumen und Netto mit dem Cache übereinstimmen; anderenfalls wird der Cache nicht übernommen. Strategieparameter oder ähnliche Dateinamen allein genügen nicht.

## Haltezeiten

Echte Deal-Position-IDs lösen die bisherige FIFO-/P/L-Zuordnungsunsicherheit in den geprüften Backtests. Zwei Zeitbegriffe bleiben notwendig:

1. **Kalenderzeit:** vollständige Zeit zwischen Einstieg und Vollschluss, einschließlich Wochenenden; geeignet zur Betrachtung der Kapitalbindung.
2. **Ohne Samstag/Sonntag:** exakter Abzug überlappender Wochenendtage in Broker-Zeit, ohne Feiertage oder sonstige Handelspausen zu entfernen.

Für sämtliche 37.921 Positionen des ersten Testfalls stimmt die gesamte Verteilung der zweiten Berechnung exakt mit der anonymen Cache-Positionstatistik überein. Der Durchschnitt ist 3:10:58,56 statt 4:20:59,97 Kalenderzeit; das Maximum 164:27:37 statt 212:27:37. Das ist ein vollständiger Abgleich dieses Testfalls, keine allgemeine Garantie für jede zukünftige MT5-Version. Null-Sekunden-Werte werden nicht stillschweigend entfernt.

## Testprofil

Symbol, Tickmodell, feste Ausführungsverzögerung, Hebel, Bars und Ticks sind in den geprüften Cachefeldern lesbar. Historienqualität wird aus dem Originalbericht übernommen; „reale Ticks“ allein wird nicht in „100 % Qualität“ umgedeutet. Spread und Slippage sind nicht aus belegten Cachefeldern lesbar und werden bei fehlenden Berichtsfeldern als „nicht exportiert“ angezeigt. EA-Parameter wie `InpDeviationPoints` oder `InpMaxSpreadPrice` sind keine Messung realisierter Slippage und keine Tester-Spread-Einstellung.

Quellen: [MT5-Startparameter und Testmodell](https://www.metatrader5.com/en/terminal/help/start_advanced/start), [Dateien und Datenordner](https://www.metatrader5.com/en/terminal/help/start_advanced/structure), [Testerdiagramm](https://www.metatrader5.com/en/terminal/help/algotrading/testing), [Margin-/Equity-Kontobelastung](https://www.metatrader5.com/en/terminal/help/signals/signal_monitoring).

Private Cachedateien, Strategieparameter und Analyseberichte werden nicht im öffentlichen Repository gespeichert.
