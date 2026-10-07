# Marktdaten und Vergleichsansichten

Unter **Einstellungen → Marktdaten & Zeit** kann eine laufende MT5-Installation ausgewählt werden. „Verfügbare Kurse finden“ listet die exakten Symbolnamen einschließlich Custom-Symbolen. „Kerzen aus MT5 laden“ liest H1-OHLC im Zeitraum des importierten Berichts; H4 und Tageskerzen werden daraus nach Broker-Zeit gebildet. Der Abruf handelt nicht und verändert keine Kontoeinstellungen.

Custom-Symbole werden ausdrücklich ausgewählt und niemals stillschweigend durch Standard-Symbole ersetzt. Die heute gespeicherte MT5-Historie kann von der zum Testzeitpunkt verwendeten Historie abweichen. Fehlende Daten werden nicht erfunden. Native MT5-Zeitangaben werden direkt auf der Broker-Zeitachse dargestellt; die externe UTC-Verschiebung wird dabei nicht zusätzlich angewendet.

Marktkurs lässt sich in der App und im PDF einschalten. Die Kursachse liegt rechts, Balance/Equity links und Equity-Drawdown darunter. „Marktkurs im Detail“ erlaubt einen Zeitraumvergleich. „Auto“ wählt ein festes Kerzenintervall passend zu Zeitraum und Breite; anschließend bleiben sämtliche Kerzen dieses Intervalls erhalten. Die Darstellung fasst nicht mehr nach Pixelgruppen zusammen. D2/D4 sind definierte Mehrtagesblöcke aus H1. Die einmaligen PDF-Vergleichsseiten wurden entfernt. Details stehen in [der aktuellen Überarbeitung](REPORT-UPDATE-2026-10-07.md).

Für externe Daten ist die Symbolzuordnung editierbar. CSV-Spalten: `Time,Open,High,Low,Close`, Dezimalpunkt, ISO-Datum oder unterstütztes MT5-Datumsformat. Die Quellzeitverschiebung wird beim Import angegeben. Der Rechner aus lokalem Datum/Uhrzeit und Serverdatum/Uhrzeit bestimmt den aktuellen UTC-Abstand. Historische Sommerzeitregeln müssen getrennt gewählt und bestätigt werden; der heutige Abstand beweist keine historische Regel.

Der Dukascopy-Download verwendet H1-Daten und einen lokalen Monatscache. Netzwerkfehler werden angezeigt. Bei der aktuellen Prüfung wurde der Online-Endpunkt durch HTTP 429 bzw. Zeitüberschreitung blockiert; der direkte MT5-Abruf wurde erfolgreich geprüft.

## Portable Veröffentlichung

Die Veröffentlichung enthält neben der .NET-App `mt5_bridge.py` und `runtime/python/`. Für die Bridge wird die offizielle eingebettete Python-3.12-Windows-x64-Laufzeit verwendet, mit `MetaTrader5` und `numpy` unter `Lib/site-packages`; dieser Pfad ist in `python312._pth` eingetragen. Die zugehörigen Lizenzdateien verbleiben im Laufzeitordner bzw. den Paketverzeichnissen. Ohne diese optionale Laufzeit bleiben Berichtsimport und CSV-Marktdaten verfügbar, der direkte MT5-Abruf meldet die fehlende Laufzeit.
