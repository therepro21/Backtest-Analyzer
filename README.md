# Backtest-Analyzer

**Entwurfs- und Beta-Phase.** Backtest-Analyzer ist ein unabhängiges, lokal arbeitendes Analyseprojekt für exportierte MT4- und MT5-Backtestberichte. Schwerpunkt sind nachvollziehbare Haltezeiten, Verteilungen und saubere A4-Berichte. Der aktuelle Stand ist ein Entwicklungsprototyp. Ein lokales, selbständiges Windows-x64-Testpaket wurde gebaut und geprüft; eine stabile Freigabe steht noch aus.

![Logo](assets/logo.png)

Copyright © 2026 Michael P. Thiess. Dieses Projekt steht in keiner Verbindung zu MetaQuotes, MetaTrader, einem Broker oder den Urhebern analysierter Strategien. MetaTrader, MT4 und MT5 dienen nur zur Beschreibung kompatibler Eingabeformate.

## Daten und Diagramme

Jede Kennzahl soll ihre Herkunft anzeigen: **selbst berechnet**, **aus Originalreport übernommen** oder **modellabhängig**. Originalwerte dienen auch zum Abgleich; sie werden nicht stillschweigend durch eine anders definierte Kennzahl ersetzt.

| Auswertung | Datenbasis | Darstellung |
|---|---|---|
| Minimum, Maximum, Mittelwert, Median, Quartile, P90/P95/P99, Streuung | rekonstruierte Ein-/Ausstiege | Kennzahlen, Histogramm, glatte Dichtekurve, Boxplot, kumulative Verteilung |
| Gewinner/Verlierer/Nulltrades, Long/Short | Nettoergebnis und Handelsrichtung | getrennte Verteilungen, Boxplots, Tabellen |
| Ergebnis gegen Haltezeit | pro Trade rekonstruierte Zeit und Ergebnis | Scatterplot, optional Ergebnis je Lot |
| Übernacht, Wochenende, Dauer über Schwellenwert | Kalenderzeiten der Trades | Balken, Anteil, längste Trades |
| Balance und Balance-Drawdown | exportierte Kontostände und Kontobewegungen | Linien, Drawdown-Fläche, lineare/logarithmische Skala |
| Ergebnis, Trefferquote und Häufigkeit nach Stunden/Tagen/Monaten | Zeitstempel und Ergebnis | Balken, Heatmap, Kalender; Einstieg/Ausstieg klar unterscheiden |
| Lotentwicklung und gleichzeitige offene Trades | Ein-/Ausstiegsvolumen | Linie, Expositions-Zeitlinie; Modellzuordnungen kennzeichnen |
| Profitfaktor, Erwartungswert, Gewinn-/Verlustserien, größter/bester Trade | definierte abgeschlossene Trade-Einheiten | Kennzahlen und Tabellen |
| Kommission und Swap, Kostenbelastung und Ergebnis vor/nach ausgewiesenen Kosten | Deal-/Trade-Spalten und gegebenenfalls separate Kontobuchungen | Kostenbrücke, Monatsbalken, kumulative Kostenkurve, Kosten nach Haltedauer |
| Equity-Drawdown, Sharpe, Recovery, AHPR/GHPR, History Quality | Originalkennzahlen | neu gestaltete Kennzahlen mit Quellenlabel |
| MAE/MFE-Rohverlauf, exakte historische Equity-Kurve | zusätzliche Zeitreihen erforderlich | ohne Rohdaten keine eigene numerische Rekonstruktion |

Geplant, aber noch nicht vollständig umgesetzt: geglättete Dichtekurven in der nativen App, Kostenmodule, Expositionsdiagramme, Heatmaps, Nettorenditen aus bereinigten Kontobewegungen, Mehrfachimport und Laufvergleich, vollständiger CSV-Import mit Position-IDs und robustere MT4-Fortsetzungserkennung. Die interaktiven Designentwürfe zeigen die glatten Dichtekurven bereits.

Die überarbeiteten Entwürfe zeigen standardmäßig eine **Häufigkeitskurve**: unten Haltedauer, links Prozent aller ausgewerteten Trades, umschaltbar auf Anzahl. Die Intervalle sind gleich breit und zwischen 5, 15, 30 und 60 Minuten wählbar. Gewinner, Verlierer und Nulltrades beziehen sich auf dieselbe Gesamtzahl. Die Linien verbinden tatsächliche Intervallhäufigkeiten ohne zusätzliche Extremwerte; die Y-Achse skaliert automatisch nach dem sichtbaren Ausschnitt. Beim Vergrößern langer Haltezeiten bleibt die Prozentbasis unverändert. Die Form ergibt sich aus den Daten; eine Badewannenform wird nicht vorausgesetzt. Eine optionale KDE wäre eine separat bezeichnete Dichteansicht, deren Achse nicht mit einfachen Prozentanteilen verwechselt werden darf.

Drawdown wird anhand des bisherigen Höchststands berechnet: Geldbetrag = Höchststand minus aktueller Wert; Prozent = Geldbetrag geteilt durch Höchststand mal 100. Maximaler Geldbetrag und maximaler Prozentwert werden als zwei mögliche unterschiedliche Ereignisse geführt, jeweils mit dem zugehörigen anderen Wert. Aus einer numerischen Balance-Zeitreihe sind vorheriges Hoch, Tiefpunkt und erste beobachtete Erholung ableitbar. Diese Zeitpunkte gelten für die exportierten Beobachtungen. Die geschlossene Trade-History liefert keinen vollständigen Equity- oder Margin-Verlauf während offener Positionen. Equity-Kennzahlen und minimaler Margin-Level können aus dem Originalreport übernommen werden. Eine digitalisierte Rastergrafik wäre nur eine ausdrücklich als ungefähr bezeichnete Schätzung und ersetzt keine Zeitreihe.

GUI, Detailbericht und Quickreport werden unabhängig ausgewählt. **Für die GUI ist Research Studio gewählt.** Detailbericht und Quickreport bleiben offen. Die aktualisierte GUI-Vorschau bietet Übersicht, Haltezeit, Risiko/Drawdown, Kosten, Originalbericht und Datenprüfung. Originalwerte und alle Strategie-Eingabeparameter werden übernommen; die Kontostandskurve zeigt den Rückgang vom bisherigen Hoch als orange Fläche, volle Währungsbeträge und eine adaptive Kalenderachse. Die native Anwendung bildet diese überarbeitete Vorschau noch nicht vollständig ab.

Kommission und Swap können in unterschiedlichen Vorzeichenkonventionen auftreten; Swap kann eine Gutschrift sein. Bei signierten Dealspalten ist Netto gleich realisiertem Preisgewinn plus Kommission plus Swap plus separat ausgewiesenen Gebühren. Bereits im Nettowert enthaltene Beträge dürfen nicht nochmals abgezogen werden. Fehlende Gebührenangaben werden als nicht verfügbar angezeigt. Spread und Slippage sind ohne zusätzliche Referenzdaten keine exakt isolierbaren Kosten.

## Haltezeit und Rekonstruktion

Die Ausgangseinheit ist ein **Entry-Lot**: Einstieg bis zum vollständigen Schließen dieses Einstiegsvolumens. Dies ist bei einfachen Trades identisch mit der Positionshaltedauer. Bei Aufstockungen, Teilverkäufen und Netting muss zusätzlich zwischen Positionsepisode und volumengewichteter Dauer unterschieden werden. Die Benutzeroberfläche und Berichte müssen die verwendete Einheit nennen.

- Kalenderdauer: Schließzeit minus Einstiegszeit, intern in Sekunden.
- Durchschnitt: Summe der Dauern geteilt durch Anzahl abgeschlossener Einheiten.
- Quantile: lineare Interpolation auf den sortierten Dauern, Index `(n - 1) * p`.
- Standardabweichung: Stichprobe mit Nenner `n - 1`.
- Volumengewichtete Dauer: Summe aus geschlossenem Volumen mal Haltedauer jeder Tranche, geteilt durch geschlossenes Volumen. Unterschiedliche Symbole nicht ohne geeignete Gewichtung zusammenfassen.
- Offene Positionen: eigener Abschnitt, nicht als abgeschlossene Trades zählen.
- Kontobewegungen: Einzahlungen/Auszahlungen nicht als Handelsgewinn behandeln.
- Broker-Zeit: keine automatische UTC-Umrechnung oder Handelsstundenberechnung ohne Zeitzone und Handelskalender.

Zuordnung erfolgt aus der History: zuerst explizite Ticket-/Positionsbezüge, anschließend eindeutige zeitliche und mengenmäßige Zuordnung. Bei konkurrierenden Einstiegen kann ein **P/L-Konsistenzmodell** einen Einstieg anhand Preis, Volumen und Bruttoergebnis identifizieren. Der Prototyp schätzt dafür einen stabilen linearen Gewinnfaktor aus isolierten vollständigen Schließungen. Dies ist eine Modellannahme; Währungsumrechnung, Rundung und nichtlineare Instrumente können sie ungültig machen. Fällt der Abgleich nicht eindeutig aus, verwendet der Prototyp FIFO und markiert die betroffenen Trades als Schätzung.

Bei MT4-Teilschließungen kann die Restposition eine neue Ticketnummer erhalten. Ein Zusammenhang aus Zeitpunkt, Einstiegspreis und Restvolumen wird als MT4-Fortsetzungsmodell gekennzeichnet. Ein importierter HTML-Bericht enthält nicht immer den ursprünglichen Eröffnungszeitpunkt einer Fortsetzung.

Der Modus „Nur eindeutige History“ schließt P/L-, FIFO- und MT4-Fortsetzungsmodelle aus. Übereinstimmende Gesamtkennzahlen allein beweisen nicht die Korrektheit jeder einzelnen Zuordnung.

## Designs zur Auswahl

GUI: **Klartext Dashboard**, **Report Assistent**, **Research Studio**.

Detailbericht: **Performance & Risiko**, **Haltezeit Studie**, **Prüfbarer Analysebericht**. Mehrseitig, bevorzugt A4 hoch.

Quick Report: **Überblick kompakt**, **Haltezeit kompakt**, **Datencheck kompakt**. Eine Seite, bevorzugt A4 quer.

Alle Varianten verwenden Logo 2, Blau für positive und Orange für negative Ergebnisse, separate Kennzeichnung für Nulltrades, einen wählbaren Light-/Darkmode, den Programmnamen oben sowie Copyright und GitHub-Link unten. App und Bericht erhalten unabhängig wählbare Themes. Darkmode verändert für bessere Lesbarkeit die Helligkeit der Diagrammfarben.

## Prototyp bauen

Windows und .NET SDK 10 erforderlich. Entwicklungsabhängigkeiten: HtmlAgilityPack 1.13.0 und PDFsharp 6.2.4. Beide sind separat lizenzierte Drittanbieterkomponenten; ihre Lizenztexte müssen mit einer späteren Veröffentlichung ausgeliefert werden.

```powershell
dotnet build src/BacktestAnalyzer/BacktestAnalyzer.csproj -c Release
dotnet publish src/BacktestAnalyzer/BacktestAnalyzer.csproj -c Release -r win-x64 --self-contained true
```

Die geplante selbständige Windows-x64-EXE benötigt keine separate .NET-Installation. Single-File-Veröffentlichungen können native Laufzeitdateien beim Start lokal entpacken; „portable“ bedeutet nicht zwingend, dass keinerlei temporäre Dateien angelegt werden. Die Anwendung braucht keine Broker-Anmeldedaten und keine Verbindung zum Terminal.

CLI des Entwicklungsprototyps:

```powershell
Backtest-Analyzer.exe --export report.html output.pdf
Backtest-Analyzer.exe --export report.html detail.pdf --detail --dark
Backtest-Analyzer.exe --export report.html verified.pdf --confirmed
Backtest-Analyzer.exe --export report.html fifo.pdf --fifo
Backtest-Analyzer.exe --export report.xlsx detail.pdf --detail
Backtest-Analyzer.exe --export-html report.xlsx interactive.html --dark
```

Importprüfungen: `dotnet run --project tests/ImportChecks -c Release`. Die synthetischen Daten prüfen Kodierungen, Streamgrenzen, HTML-Entities, Excel-Zellen, Kosten, Positionsbezüge und Formatgleichheit. Private Backtests gehören nicht in die Tests oder das Repository.

Der Prototyp erzeugt daneben eine JSON-Prüfausgabe. Der Import unterstützt deutsche und englische MT4-HTML- und MT5-HTML/XLSX-Backtestberichte, nicht sämtliche Kontoauszüge oder Optimierungsberichte. XLSX-Unterstützung bezieht sich auf den unveränderten MT5-Export mit `Sheet1`, textuellen Zeitstempeln und dessen Spaltenlayout; beliebig umformatierte Excel-Dateien sind nicht zugesichert. Excel muss nicht installiert sein. Fehler und ausgelassene Ereignisse müssen geprüft werden; der Prototyp ist noch nicht zur unbeaufsichtigten Verarbeitung gedacht.

### Verarbeitung großer Exporte

HTML wird zeilenweise gelesen; nur eine Tabellenzeile wird als HTML geparst. XLSX wird direkt aus dem ZIP-Container mit `XmlReader` gelesen. Die Shared-String-Tabelle bleibt im Speicher, die gesamte Tabelle und XML-Dokumentstruktur nicht. Dateihashes werden aus einem Stream berechnet. Geschlossene Einstiege werden aus dem aktiven Suchinventar entfernt. Bei vielen gleichzeitig offenen Positionen bleibt der Aufwand des Konsistenzmodells von der Größe dieses Inventars abhängig.

XLSX ist die bevorzugte Eingabe, wenn beide Exporte vorliegen: mehrere Kennzahlen haben mehr Nachkommastellen, die Dateien sind kleiner und der gemessene Speicherbedarf war geringer. HTML bleibt unterstützt und kann beim Lesen etwas schneller sein. Die App zeigt Import- und Zuordnungszeit an. Geprüft wurde ein aktueller Build-5833-Export mit mehr als 100.000 Deals; die bisherige pauschale 100-MB-Dateigrenze wurde entfernt. Das ist keine Garantie für beliebig große oder fehlerhafte Dateien.

Die native Haltezeitansicht bietet jetzt gleich breite Intervalle von 5/15/30/60 Minuten, Prozent oder Anzahl sowie Ausschnitte über 6/24 Stunden. Prozentbasis ist stets die gesamte ausgewählte Tradezahl. Die Balance zeigt den Prozent-Drawdown dauerhaft darunter. Für große Kurven werden Extrema vor dem Zeichnen zusammengefasst; Statistiken verwenden sämtliche Punkte. Der Scatterplot zeigt bei großen Dateien eine deterministische Auswahl und ist deshalb keine vollständige Punktwolke. App und exportierter Standardbericht bleiben ein Beta-Prototyp; individuelle Analyseberichte können zusätzliche Auswertungen enthalten.

### Interaktiver Report als offline nutzbare HTML-Datei

Der zusätzliche HTML-Export enthält die gesamte Account-History unabhängig von den Tradefiltern, Logo und Daten direkt in einer Datei. Er benötigt keinen Server oder CDN. Ein Zeitcursor zeigt beobachtete Balance, Drawdown in Kontowährung und Prozent, vorheriges Kontohöchstniveau und offene Buy-/Sell-/Netto-Lots. Navigation ist mit Maus, Touch, Pfeiltasten oder Deal-Tasten möglich; Monatsauswahl, Ctrl+Mausrad-Zoom und Light-/Darkmode sind vorhanden. Alle Dealpunkte bleiben für den Cursor erhalten, die gezeichnete Kurve wird unter Erhalt der Extrema reduziert. Zeitstempel werden als Broker-Kalenderzeit angezeigt, ohne automatische Umrechnung in die Zeitzone des Browsers.

Equity und exakte Anzahl offener Positionen werden nicht erfunden. Eine Equity-Zeitreihe fehlt in den geprüften Standardreports, die Lotinventur ist keine Marginberechnung. Fehlende Dealzeilen, Anfangsbestände oder zusätzliche Kontobewegungen können die Interpretation einschränken. Der Export bildet beobachtete Handelsdeal-Balances ab und ist kein Ersatz für eine komplette Kontobuchungs-/Equity-Zeitreihe.

Das A4-PDF enthält im Detailbericht zusätzlich **Acrobat-Mouseover** auf der Balance-/Drawdownkurve. Transparente Zeitfenster-Widgets zeigen eine DD-Spitzenbeobachtung beziehungsweise den letzten bekannten Stand mit Balance, DD-Geldbetrag/-Prozent und Buy-/Sell-/Netto-Lots. Bei vielen Deals ist dies eine vorbereitete Auswahl je Zeitfenster; der HTML-Cursor hält dagegen alle Beobachtungen bereit. Equity und bestätigte Positionsanzahl bleiben ohne Rohdaten nicht verfügbar.

Die Feldaktionen sind im PDF enthalten. Das schreibgeschützte Detailfeld wird am Bildschirm eingeblendet und beim Drucken ausgeblendet. Ohne ausgeführtes JavaScript bleibt die statische Kurve erhalten; Tooltips sind viewerabhängig. Feldbaum, Appearance-Streams, Scriptwerte und statische Darstellung wurden geprüft. Ein tatsächlicher Desktop-Livetest in Acrobat steht noch aus. Edge unterstützt beispielsweise JavaScript-Formulare nicht. Die PDF-Ansicht in Codex oder im Browser bestätigt deshalb die Acrobat-Funktion nicht. Quellen: [Adobe: PDF-Aktionen](https://helpx.adobe.com/in/acrobat/desktop/edit-documents/apply-pdf-actions/pdf-actions.html), [Adobe: Formularfelder und Tooltips](https://opensource.adobe.com/dc-acrobat-sdk-docs/library/jsdevguide/JS_Dev_AcrobatForms.html), [Adobe: JavaScript API](https://opensource.adobe.com/dc-acrobat-sdk-docs/library/jsapiref/JS_API_AcroJS.html), [Microsoft: Edge-PDF-Funktionen](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-pdf).

### Fehlende Positionsbezüge und zusätzliche Kurvendaten

Standard-Testerberichte können Deal-Ticket und Auftrag enthalten, ohne `DEAL_POSITION_ID` zu exportieren. `DEAL_ORDER` bezeichnet den jeweils ausführenden Auftrag; Öffnungs- und Schließauftrag sind nicht derselbe Schlüssel. Eine belegte Begründung von MetaQuotes für das Weglassen oder einen Schalter zum Einblenden haben wir nicht gefunden. Original-Haltezeiten müssen separat angezeigt und gegen rekonstruierte Dauern geprüft werden. Auch eine später lokal eindeutige Zuordnung kann von früheren Modellentscheidungen abhängen; der Filter ist kein Vollständigkeitsnachweis.

Die belastbare Ergänzung ist ein eigener Deal-Export im getesteten EA, etwa in dessen bestehendem `OnTester()`: `HistorySelect`, `HistoryDealsTotal`, `HistoryDealGetTicket`, anschließend `DEAL_POSITION_ID`, `DEAL_ORDER`, `DEAL_TIME_MSC`, `DEAL_ENTRY`, `DEAL_TYPE`, Symbol, Volumen, Preis und signierte Profit-/Kostenfelder. Reversals und Close-by dürfen nicht als normale Vollschlüsse vereinfacht werden. Der Prototyp importiert einen solchen Zusatzexport derzeit noch nicht.

Für bereits abgeschlossene Tests existieren experimentelle Chartobjekt-/Cache-Ansätze. ObjectsTrade liest Trade-Verbindungslinien, enthält aber keine Commission/Swap und hat eine problematische Tester-Erkennung anhand `Ticket < 100000`. Build-Kompatibilität und vollständige Abdeckung müssen vor Verwendung geprüft werden. Ein normaler Terminal-Historyexport oder Python-Kontozugriff ist kein belegter Zugriff auf eine frühere isolierte Tester-History.

Der Screenshot des Berichtsmenüs mit Open XML und HTML zeigt nicht das Graph-Menü. Nutzerberichte beschreiben zusätzlich **Strategietester → Grafik/Graph → Rechtsklick auf die Balance-/Equitygrafik → CSV speichern**. Die konkrete Verfügbarkeit in Build 5833 ist noch lokal zu prüfen. Eine solche Zeitreihe könnte Equity und Deposit Load ergänzen, liefert aber nicht automatisch Positions-IDs. „Chart öffnen“ öffnet den Instrumentchart.

Bei vollständig geschlossener History ohne Anfangsbestand kann die volumengewichtete Haltedauer unabhängig von der individuellen Paarung aus dem Integral des offenen Volumens bestimmt werden. Das bestätigt weder Positionsmedian noch Extremwerte oder Haltezeitverteilung.

Quellen: [Deal-Eigenschaften](https://www.mql5.com/en/docs/constants/tradingconstants/dealproperties), [OnTester](https://www.mql5.com/en/docs/event_handlers/ontester), [fehlende Position-IDs im Testerbericht](https://www.mql5.com/en/forum/433144), [Graph-CSV und separates Berichtsmenü](https://www.mql5.com/en/forum/462891), [Graph-Dateiformat](https://www.mql5.com/en/forum/432288), [ObjectsTrade](https://www.mql5.com/ru/code/39750), [Microsoft: große SpreadsheetML-Dateien streamen](https://learn.microsoft.com/en-us/office/open-xml/spreadsheet/how-to-parse-and-read-a-large-spreadsheet).

## Vergleichsdateien

- Aktueller MT5-Bericht des Auftraggebers: Build 5833; ausschließlich lokal verarbeitet. Keine Veröffentlichung der Datei, Strategieparameter, Ausgaben oder daraus erzeugter Berichte vorgesehen.
- [MT5-Beispiel mit Commit vom 31. Mai 2024](https://github.com/TimKabue/StrategyTesterReport1/commit/f9a13ca283e7942ab3f010ddbae2c3d18035c5e0): Build 4330, älteres englisches Layout.
- [MT4-Beispiel](https://github.com/danielchkl/Project-Deities/blob/main/StrategyTester.htm): Build 1353, Ereignistabelle mit Teilschließungen.

Fremde Beispieldateien werden nur verlinkt, nicht ungeprüft mit eigener Lizenz weiterveröffentlicht.

## Recherche und Referenzen

- [MT5-Testbericht und Definitionen](https://www.metatrader5.com/en/terminal/help/algotrading/testing_report)
- [MQL5 Deal-Eigenschaften und Position-IDs](https://www.mql5.com/en/docs/constants/tradingconstants/dealproperties)
- [Gewinnberechnung und Kontowährung](https://www.mql5.com/en/docs/trading/ordercalcprofit)
- [QuantAnalyzer](https://strategyquant.com/quantanalyzer/) und [unterstützte Formate](https://strategyquant.com/doc/quantanalyzer/formats-supported-in-quantanalyzer/)
- [MT QuantAnalyzer: Dauer und Gruppierung](https://strategyquant.com/quantanalyzer/mtquantanalyzer/)
- [FX Blue: Auswertung simulierter Trades](https://api.fxblue.com/appstore/u39/mt4-trading-simulator/user-guide)
- [Myfxbook: Duration Analysis](https://www.myfxbook.com/fr/features)
- [MQL5-Forum: fehlende Haltezeitangaben](https://www.mql5.com/en/forum/453457)
- [Forex Factory: MAE und QuantAnalyzer](https://www.forexfactory.com/thread/post/9387894)
- [Reddit: Equity-Drawdown aus geschlossenen Trades](https://www.reddit.com/r/metatrader/comments/1wkc888/can_mt4mt5_ea_statements_hide_the_real_maximum/)
- [Reddit: QuantAnalyzer als Vergleich](https://www.reddit.com/r/metatrader/comments/1sroh8k/which_one_would_you_feel_safe_using/)
- [Microsoft: Single-File-Veröffentlichung](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)

Recherchestand: 5. Oktober 2026. Herstellerdokumentation liefert die technischen Definitionen; Foren und Reddit liefern Hinweise auf praktische Probleme und sind kein Nachweis für die Qualität oder Korrektheit eines Produkts. QuantAnalyzer ist der am besten dokumentierte Vergleichskandidat dieser Recherche. Es wurde kein vollständiger praktischer Vergleichstest aller Produkte durchgeführt.

## Rechtliches und Datenschutz

Bitte [DISCLAIMER.md](DISCLAIMER.md) und [COPYRIGHT.md](COPYRIGHT.md) lesen. Die Software kann Fehler machen und ist kein Entscheidungsersatz für Handel oder Investitionen. Die geplante Anwendung verarbeitet Dateien lokal; die ausdrückliche Aktion „GitHub“ öffnet den Browser. Nutzer müssen selbst sicherstellen, dass sie die erforderlichen Rechte an Eingabedaten und Strategieberichten besitzen.
