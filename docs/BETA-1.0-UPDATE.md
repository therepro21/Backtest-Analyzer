# Beta 1.0 – Bericht und App

Die portable Windows-App arbeitet lokal. Analysen können als `.bta` gespeichert und mit einer zweiten Analyse oder einem Originalbericht verglichen werden. Das Analyseformat enthält Trades, Kontobuchungen, Equity-Beobachtungen und Einstellungen. Ein PDF allein enthält nicht immer alle Zeitreihen; zum vollständigen Vergleich `.bta` verwenden oder Originalbericht mit passendem Tester-Cache importieren.

## Handelsdauer

Standard: bereinigte Handelsdauer, Samstag/Sonntag geschlossen, pauschale Nachtpause 00:00–01:00 Broker-Zeit. Nur überlappende Zeitanteile werden abgezogen; Wochenenden und Nachtpause werden nicht doppelt abgezogen. Wochenendtage, Anfang und Dauer der Nachtpause sind einstellbar. Kalenderdauer bleibt auswählbar. Feiertage und historische Symbol-Handelszeiten werden nicht automatisch ermittelt. Fehlende Orders sind kein sicherer Beleg einer Marktschließung.

Die Bereinigung gilt für Verteilungen, Quantile, Durchschnitt, Maximum, Volumengewichtung und die Grenze langer Handelszyklen. Ursprüngliche Zeitstempel und Originalkennzahlen bleiben erhalten.

## Ergebnisse und 60 Sekunden

Gewinn-/Verlustdiagramme ordnen vollständige Trade-Nettoergebnisse der Schließzeit zu. Ein festes Buchungsfenster ordnet leicht versetzte Schließungen seinem letzten Zeitpunkt zu; es verändert keine Geldbeträge. Gewinn und Verlust erscheinen als positive Betragsgrößen ab Null, der größere Betrag gefüllt, der kleinere mit gestrichelter Kontur. Netto bleibt Gewinn minus Verlust. Das sind keine gestapelten Balken. Der Testende-Verlust bleibt im tatsächlichen Ergebnis enthalten und wird zusätzlich separat ausgewiesen.

Kontozyklen bleiben vom ersten Einstieg bis zur vollständig geschlossenen Brutto-Position definiert. Eine pauschale Verbindung verschiedener Zyklen mit höchstens 60 Sekunden Abstand ist keine gesicherte EA-Zykluskennung. Der geprüfte Originalbericht enthält auch reguläre negative Kontozyklen; sie werden nicht entfernt oder in positive Ergebnisse umgewandelt.

## PDF und Mouseover

Native Tooltips ohne JavaScript, ohne dynamisches Beschreiben oder Ein-/Ausblenden von Formularfeldern. Dichte PDF-Heatmaps bündeln Informationen je Stundenbereich. Streudiagramme verwenden eine Tooltip-Stichprobe. Die App bietet feinere Trefferbereiche; gespeicherte Tester-Equity bleibt eine Beobachtungsreihe und keine vollständige Tickhistorie. Tooltip-Unterstützung und Einblendverhalten hängen vom PDF-Viewer ab. Performance muss im jeweiligen Acrobat geprüft werden.

PDF-Lesezeichen enthalten Gesamt-/Jahreszuordnung und Seiteninhalt. Zusammengehörige Auswertungen sind möglichst in Dreiergruppen angeordnet. Originaldateien und Originalgrafiken bleiben als PDF-Anhänge vollständig erhalten; sie können private Informationen enthalten.

## Vergleich

Zwei Originalberichte oder `.bta`-Analysen: Kennzahlen, absolute/prozentuale Differenzen, Balance/Equity/Drawdown, Haltezeitverteilungen, Monats-/Jahresergebnisse und abweichende Strategieparameter. Zeitachse wahlweise Kalenderdatum oder seit Testbeginn, Kontokurven wahlweise Originalbetrag oder Startwert 100. Unterschiedliche Währungen werden nicht automatisch umgerechnet. Ohne Equity-Zeitreihe werden keine Equity-Werte erfunden.

Deutsch/English ist auswählbar; importierte Originaltexte behalten ihre Originalsprache und einzelne Übersetzungen befinden sich weiterhin in Prüfung. Englische Stundenbeschriftungen und neu gestaltete Zeitangaben verwenden AM/PM.
