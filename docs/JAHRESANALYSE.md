# Gesamt-, Jahres- und Serienauswertung

Der Detail-PDF beginnt mit dem Testprofil und der vollständigen Gesamtauswertung. Bei mehreren Jahren folgen alle Jahresabschnitte in chronologischer Reihenfolge, mit denselben Berechnungs- und Diagrammarten. Gemeinsame Strategieparameter und globale Originalkennzahlen stehen jeweils einmal im Gesamtteil, mit kleinen Light-Bezeichnungen und größeren Semibold-Werten, zwei oder drei Spalten. Lange Inhalte werden umgebrochen und bei Bedarf auf weitere Seiten verteilt. Es wird nichts abgeschnitten, nur leere visuelle `=`-Trennzeilen der Eingaben werden nicht dargestellt.

## Jahresgrenzen

- Zeiträume stammen aus den tatsächlichen Daten, nicht aus Ordnernamen.
- Kontoergebnis, Bruttogewinn, Kommission, Swap und verfügbare Fee werden nach **Deal-Buchungsjahr** ausgewertet. Die Summe der Jahresbuchungen stimmt mit der Gesamtsumme überein.
- Geschlossene Entry-Lots, ihre lebenslangen Ergebnisse, Haltezeitquantile, Stunden- und Wochentagsmuster werden dem **Schlussjahr** zugeordnet. Ein im Vorjahr begonnener Trade behält seine volle Dauer. Sein Lebenszeit-Netto kann vom im Schlussjahr gebuchten Netto abweichen.
- Balance und Inventar werden am Jahreswechsel übernommen. Es gibt keine neue Ersteinzahlung zum 1. Januar.
- Jahres-Equity-DD beginnt am übernommenen Equitywert und verwendet das Equity-Hoch innerhalb dieses Jahres. Der Gesamt-DD bleibt eine separate Gesamtberechnung.
- Jahreskurven haben Monatsgrenzen und Januar–Dezember-Beschriftungen. Bei einem unvollständigen letzten Jahr endet der Verlauf mit den verfügbaren Beobachtungen. Spätere Monatsbalken werden als nicht verfügbar markiert, nicht als getestete Nullmonate behandelt.
- Globale Originalwerte wie Sharpe, Equity-DD und MT5-Haltezeit werden nicht als angebliche jährliche Originalwerte kopiert. Ohne Jahres-Rohdaten fehlt die jeweilige Kennzahl.
- Offene Entry-Lots am Jahresende werden separat gezählt. Sie gehören nicht zur abgeschlossenen Haltezeit-Kohorte.

Tradefilter betreffen Trade-Kennzahlen. Kontostands-/Equity-/Kosten-/Monats- und Serienauswertungen verwenden die vollständige Kontohistory des Zeitabschnitts und sind entsprechend bezeichnet. Das PDF enthält standardmäßig Gesamt- und Jahresabschnitte; der optionale vollständige Trade-Anhang kann sehr umfangreich werden.

## Lange Handelszyklen

Eine Serie ist auf ausdrücklichen Wunsch ein **kontoweiter Handelszyklus vom ersten Einstieg bis wieder sämtliche Positionen geschlossen sind**. Eine Long-/Short-Hedge mit Netto-Lots null beendet keinen Zyklus; entscheidend ist das Bruttoinventar. Diese hergeleiteten Zyklen sind nicht automatisch identisch mit einer EA-internen Zykluskennung. Offene Zyklen werden nicht in die Quote abgeschlossener Zyklen gezählt.

Die Startzeit-Auswertung hat sieben Wochentage und 24 Startstunden in exportierter Broker-Zeit. Standardgrenze ist **mehr als sechs Stunden ohne Samstag/Sonntag**. Feiertage und tägliche Handelsunterbrechungen sind damit nicht entfernt; die Kennzahl wird deshalb nicht pauschal als reine Marktöffnungszeit bezeichnet.

Zwei Ansichten verhindern irreführende Häufigkeitsvergleiche:

1. Anzahl langer abgeschlossener Serien, die im betreffenden Wochentag-/Stundenfeld begonnen haben.
2. Anteil dieser langen Serien an **allen abgeschlossenen Serien mit demselben Startzeitfeld**.

Es gibt eine Heatmap und sieben Tageskurven, einen einstellbaren Dauergrenzwert und Anzahl-/Prozentmodus. Kleine Stichproben können extreme Quoten erzeugen; die Anzahlansicht wird deshalb gemeinsam mit der Prozentansicht im Detailbericht dargestellt. Die Grafik beschreibt historische Daten, keine Ursache oder Prognose.

## Balance-Anzeigefenster

Standard sind feste, ab der ersten geänderten Buchung beginnende Fenster von bis zu 60 Sekunden. Dargestellt wird der letzte gebuchte Bestand jedes Fensters, als Stufenkurve. 0 Sekunden zeigt die Rohbuchungen; 5 und 120 Sekunden sind weitere GUI-Optionen. Die Gruppierung ist eine Anzeigeentscheidung und keine Änderung der Originalbuchungen. Die originale Equityreihe und ihr Drawdown werden nicht geglättet.
