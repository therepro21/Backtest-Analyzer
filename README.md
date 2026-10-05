# Backtest-Analyzer

**Entwurfs- und Beta-Phase.** Backtest-Analyzer ist ein unabhängiges, lokal arbeitendes Analyseprojekt für exportierte MT4- und MT5-Backtestberichte. Schwerpunkt sind nachvollziehbare Haltezeiten, Verteilungen und saubere A4-Berichte. Der aktuelle Stand ist ein Entwicklungsprototyp; eine freigegebene portable EXE folgt nach der Abstimmung der Datenbasis und Designs.

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

Für die Dichtekurve wird eine an der Grenze null reflektierte Gauß-KDE vorgeschlagen. Gewinner und Verlierer erhalten zum Vergleich eine gemeinsame Bandbreite und werden jeweils auf 100 % normiert. Die Fläche, nicht die Höhe eines einzelnen Punktes, beschreibt einen Anteil. Histogramm und kumulative empirische Verteilung bleiben daneben verfügbar. Die Glättung verändert keine Kennzahlen.

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
```

Der Prototyp erzeugt daneben eine JSON-Prüfausgabe. Das derzeitige HTML-Importmodell ist auf deutsche und englische Backtestberichte ausgelegt, nicht auf sämtliche denkbaren Kontoauszüge oder Optimierungsberichte. Fehler und ausgelassene Ereignisse müssen geprüft werden; der Prototyp ist noch nicht zur unbeaufsichtigten Verarbeitung gedacht.

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
