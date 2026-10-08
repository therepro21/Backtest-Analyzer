using System.Reflection;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace BacktestAnalyzer;
public sealed class ReportFontResolver:IFontResolver
{
    public FontResolverInfo ResolveTypeface(string familyName,bool isBold,bool isItalic)=>new(isBold?"semibold":"light");
    public byte[] GetFont(string faceName)=>File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),faceName=="semibold"?"seguisb.ttf":"segoeuil.ttf"));
}
public sealed class PdfNotes
{
 public List<string> Texts {get;}=new();public List<(PdfPage Page,int Number,double X,double Y)> Links {get;}=new();
 public int Add(string text,PdfPage page,double x,double y){int i=Texts.IndexOf(text);if(i<0){i=Texts.Count;Texts.Add(text);}Links.Add((page,i+1,x,y));return i+1;}
}
public sealed class PdfCanvas(XGraphics g,PdfDocument doc,PdfPage page,PdfNotes? notes=null):ICanvas
{
    public void RightText(string text,double right,double y,double size,string color,bool bold=false)=>Text(text,right-Measure(text,size,bold),y,size,color,bold);
    public void Note(string text,double x,double y,string color){if(notes!=null){int n=notes.Add(Localization.T(text),page,x,y);Text("*"+n,x,y,4.5,color);}}
    public void Hover(double x,double y,double w,double h,string text)=>NativeHover.Add(doc,page,x,y,w,h,Localization.T(text));
    public double Measure(string text,double size,bool bold=false)=>g.MeasureString(Localization.T(text),new XFont("Backtest UI",size,bold?XFontStyleEx.Bold:XFontStyleEx.Regular)).Width;
    static XColor Color(string s)=>XColor.FromArgb(Convert.ToInt32(s[1..3],16),Convert.ToInt32(s[3..5],16),Convert.ToInt32(s[5..7],16));
    public void Rect(double x,double y,double w,double h,string fill){if(w>0&&h>0)g.DrawRectangle(new XSolidBrush(Color(fill)),x,y,w,h);}
    public void Line(double x1,double y1,double x2,double y2,string color,double width=1)=>g.DrawLine(new XPen(Color(color),width*.65),x1,y1,x2,y2);
    public void Text(string text,double x,double y,double size,string color,bool bold=false)=>g.DrawString(Localization.T(text),new XFont("Backtest UI",size,bold?XFontStyleEx.Bold:XFontStyleEx.Regular),new XSolidBrush(Color(color)),new XPoint(x,y+size));
    public void AngledText(string text,double x,double y,double size,string color,double angle){var state=g.Save();var font=new XFont("Backtest UI",size);double w=g.MeasureString(Localization.T(text),font).Width;g.TranslateTransform(x,y);g.RotateTransform(angle);g.DrawString(Localization.T(text),font,new XSolidBrush(Color(color)),new XPoint(-w/2,size*.35));g.Restore(state);}
    public void VerticalText(string text,double x,double y,double size,string color){var state=g.Save();var font=new XFont("Backtest UI",size);string label=Localization.T(text);double width=g.MeasureString(label,font).Width;g.TranslateTransform(x,y);g.RotateTransform(-90);g.DrawString(label,font,new XSolidBrush(Color(color)),new XPoint(-width/2,size*.35));g.Restore(state);}
    public void Circle(double x,double y,double radius,string color,bool hollow=false){if(hollow)g.DrawEllipse(new XPen(Color(color)),x-radius,y-radius,radius*2,radius*2);else g.DrawEllipse(new XSolidBrush(Color(color)),x-radius,y-radius,radius*2,radius*2);}
}
public static class PdfExport
{
    public const string Repository="https://github.com/therepro21/Backtest-Analyzer";
    public static byte[] Logo=>ReadLogo();
    static byte[] ReadLogo(){using var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png")!;using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
    public static void Save(Report sourceReport,List<Trade> selectedTrades,string path,bool dark,string layout,bool appendix=false)
    {
        if(GlobalFontSettings.FontResolver is not ReportFontResolver)GlobalFontSettings.FontResolver=new ReportFontResolver();
        System.Globalization.CultureInfo.CurrentCulture=Localization.Culture;var report=SymbolAnalysis.ForSymbol(sourceReport,"");var trades=selectedTrades;string scopeTitle="Gesamtauswertung";
        using var doc=new PdfDocument();doc.PageMode=PdfPageMode.UseNone;doc.Info.Title="Backtest-Analyzer - "+report.Strategy;doc.Info.Author="Michael P. Thiess";doc.Info.Subject="Haltezeit- und Backtestanalyse";
        var notes=new PdfNotes();var palette=new Palette(dark);var stats=new Stats(trades,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute);var pages=new List<(PdfPage Page,XGraphics Graphics,PdfCanvas Canvas)>();
        (PdfPage Page,XGraphics G,PdfCanvas C) NewPage(string title,bool landscape=false)
        {
            var page=doc.AddPage();page.Size=PdfSharp.PageSize.A4;if(landscape)page.Orientation=PdfSharp.PageOrientation.Landscape;
            var g=XGraphics.FromPdfPage(page);var c=new PdfCanvas(g,doc,page,notes);pages.Add((page,g,c));double width=page.Width.Point,height=page.Height.Point;
            c.Rect(0,0,width,height,palette.Background);c.Rect(24,18,width-48,67,palette.Surface);
            g.DrawRectangle(new XPen(XColor.FromArgb(17,59,101),.65),24,18,width-48,67);
            c.Rect(32,23,55,55,dark?"#D6EBFF":"#FFFFFF");VectorBrand.Draw(c,32,23,55,dark?"#D6EBFF":"#FFFFFF");
            double brandSize=23;double brandWidth=g.MeasureString("Backtest-Analyzer",new XFont("Backtest UI",brandSize,XFontStyleEx.Bold)).Width;
            c.Text("Backtest-Analyzer",100,38,brandSize,palette.Ink,true);
            string pageLabel=title.Contains("Strategieparameter")||title.Contains("Originalkennzahlen / Gesamttest")?"Settings":report.AnalysisYear.HasValue?"Details "+report.AnalysisYear:"Gesamtübersicht";
            if(pageLabel!="Settings")pageLabel=(report.ScopeSymbol.Length>0?report.ScopeSymbol:SymbolAnalysis.Symbols(sourceReport).Count>1?(Localization.English?"All symbols":"Alle Symbole"):SymbolAnalysis.Symbols(sourceReport).FirstOrDefault()??"")+" · "+(report.AnalysisYear?.ToString()??(Localization.English?"Total":"Gesamt"));
            double labelX=width-36;c.Text(pageLabel,labelX-g.MeasureString(Localization.T(pageLabel),new XFont("Backtest UI",11,XFontStyleEx.Bold)).Width,37,11,palette.Ink,true);
            string section=title.Split('/')[0].Trim();int chars=23;for(int line=0;line<Math.Min(2,(int)Math.Ceiling(section.Length/(double)chars));line++)c.Text(section.Substring(line*chars,Math.Min(chars,section.Length-line*chars)),labelX-g.MeasureString(Localization.T(section.Substring(line*chars,Math.Min(chars,section.Length-line*chars))),new XFont("Backtest UI",9)).Width,54+line*10,9,palette.Muted);
            string chapter=pageLabel;
            var parent=doc.Outlines.Cast<PdfSharp.Pdf.PdfOutline>().FirstOrDefault(o=>o.Title==Localization.T(chapter));parent??=doc.Outlines.Add(Localization.T(chapter),page,false);parent.Outlines.Add(Localization.T(chapter+" · "+title),page,false);
            return(page,g,c);
        }
        void TextLines(PdfCanvas c,string text,double x,double y,double width,int max=20,double size=6)
        {
            if(!notes.Texts.Contains(Localization.T(text)))notes.Texts.Add(Localization.T(text));
        }
        void DataLines(PdfCanvas c,string text,double x,double y,double width,double size=7,double leading=9)
        {
            int max=100,count=0;foreach(var paragraph in text.Split('\n')){string line="";foreach(var word in paragraph.Split(' ')){string candidate=line.Length>0?line+" "+word:word;if(c.Measure(candidate,size)>width&&line.Length>0){if(count>=max)return;c.Text(line,x,y+count++*leading,size,palette.Ink);line=word;}else line=candidate;}if(line.Length>0&&count<max)c.Text(line,x,y+count++*leading,size,palette.Ink);}

        }
        string Short(string t,int len=80)=>t.Length>len?t[..(len-1)]+"…":t;
        void Metric(PdfCanvas c,string label,string value,double x,double y,double width){c.Rect(x,y,width,58,palette.Surface);c.Text(label,x+10,y+5,8,palette.Muted);c.Text(value,x+10,y+18,12,palette.Ink,true);}
        void Right(PdfCanvas c,XGraphics g,string text,double x,double yy,double size=8,bool bold=false){c.Text(text,x-g.MeasureString(Localization.T(text),new XFont("Backtest UI",size,bold?XFontStyleEx.Bold:XFontStyleEx.Regular)).Width,yy,size,palette.Ink,bold);}
        void Headline(PdfCanvas c,double y,double width){double cell=(width-20)/3;Metric(c,"Durchschnitt",Stats.Duration(stats.Mean),24,y,cell);Metric(c,"Mittlere Haltezeit (50 %)",Stats.Duration(stats.Median),34+cell,y,cell);var detail=EventDetails.Longest(report,stats.Trades);Metric(c,"Längste Haltezeit",detail.Duration,44+cell*2,y,cell);DataLines(c,detail.Period+"\n"+detail.Prices,44+cell*2+10,y+33,cell-20,5,6);}
        void Chart(PdfCanvas c,ChartKind kind,double x,double y,double width,double height){ChartPainter.Draw(c,kind,report,stats.Trades,palette,x,y,width,height);}
        if(layout!="quick"&&sourceReport.ReportScope!="years")
        {
            var cover=NewPage(report.IsAccountHistory?"Kontoprofil":"Backtestprofil / Datenqualität");double width=cover.Page.Width.Point;
            cover.C.Text((report.IsAccountHistory?"Konto: ":"EA: ")+Short(report.Strategy,30),30,110,15,palette.Ink,true);
            TextLines(cover.C,DisplayFormat.DurationBasis(report)+". Nachtpause: "+(report.NightPauseStartMinute/60).ToString("00")+":00, "+report.NightPauseMinutes+" min; pauschale Annahme, Feiertage nicht berücksichtigt.",30,145,220,5,10);
            cover.C.Text("Abgeschlossene Trades: "+stats.Count.ToString("N0"),30,210,10,palette.Ink);
            cover.C.Text("Gehandeltes Einstiegsvolumen: "+stats.Trades.Sum(t=>t.Volume).ToString("N2")+" Lots",30,226,9,palette.Ink);
            cover.C.Text("Durchschnittliche Haltezeit: "+Stats.Duration(stats.Mean),30,247,10,palette.Ink);
            cover.C.Text("Mittlere Haltezeit (50 %): "+Stats.Duration(stats.Median),30,263,10,palette.Ink);
            var longestDetail=EventDetails.Longest(report,stats.Trades);cover.C.Text("Längste Haltezeit: "+longestDetail.Duration,30,279,10,palette.Ink);DataLines(cover.C,longestDetail.Period+" "+longestDetail.Prices,30,295,255,5.5,7);
            var coverAdjusted=SeriesAnalysis.Adjusted(report);
            cover.C.Text("Gesamtgewinn: "+AccountHistory.TradingNet(report).ToString("N2")+" "+DisplayFormat.Currency(report),30,307,10,palette.Ink,true);
            if(coverAdjusted.Time.HasValue){cover.C.Text("Um letzten Zyklus bereinigt",30,329,8,palette.Muted);cover.C.Text(coverAdjusted.Profit.ToString("N2")+" "+DisplayFormat.Currency(report),30,341,12,palette.Ink,true);cover.C.Text($"Gesamtgewinn am {DisplayFormat.Time(coverAdjusted.Time.Value)}",30,359,8,palette.Muted);cover.C.Text($"Testende-Zyklus: {coverAdjusted.Excluded:N2} {DisplayFormat.Currency(report)}",30,374,8,palette.Negative);}
            cover.C.Rect(width-290,105,266,290,dark?"#20354F":"#E7F2FF");
            cover.C.Text(report.IsAccountHistory?"KONTOPROFIL":"TESTPROFIL · Original / Cache",width-278,116,11,palette.Ink,true);
            string Value(params string[] aliases){string value=report.Find(aliases);return value.Length>0?value:"nicht exportiert";}
            string IntegerValue(params string[] aliases){string value=Value(aliases);try{return Parser.Number(value).ToString("N0");}catch(FormatException){return value;}}
            var quality=report.IsAccountHistory?new[]{("Plattform",report.Platform),("Kontowährung",DisplayFormat.Currency(report)),("Ersteinzahlung",DisplayFormat.Money(report.InitialDeposit,report)),("Letzter Kontostand",DisplayFormat.Money(report.Balances.LastOrDefault().Balance,report)),("Einzahlungen",DisplayFormat.Money(report.AccountBookings.Where(b=>b.Kind is "initial" or "balance"&&b.Amount>0).Sum(b=>b.Amount),report)),("Auszahlungen",DisplayFormat.Money(report.AccountBookings.Where(b=>b.Kind=="balance"&&b.Amount<0).Sum(b=>b.Amount),report)),("Kredit / Bonus",DisplayFormat.Money(report.AccountBookings.Where(b=>b.Kind is "credit" or "bonus").Sum(b=>b.Amount),report)),("Handelsergebnis",DisplayFormat.Money(AccountHistory.TradingNet(report),report)),("Deals",report.Deals.Count.ToString("N0",Localization.Culture)),("Kontobuchungen",report.AccountBookings.Count.ToString("N0",Localization.Culture))}:new[]{("Symbol",Value("Symbol")),("Zeitraum / Periode",Value("Periode","Period")),("Historienqualität",Value("Qualität der Historie","History Quality","Modeling quality")),("Verwendete Ticks",IntegerValue("Ticks")),("Tickmodell",Value("Tickmodell","Modell","Model")),("Verzögerung",Value("Verzögerung","Delay","Execution delay")),("Tester-Spread",Value("Spread","Tester spread")),("Tester-Slippage",Value("Slippage","Tester slippage")),("Balken / Symbole",IntegerValue("Balken","Bars")+" / "+IntegerValue("Symbole","Symbols")),("Kontowährung / Hebel",Value("Währung","Currency")+" / "+Value("Hebel","Leverage"))};
            for(int qi=0;qi<quality.Length;qi++){var item=quality[qi];double xx=width-278+(qi%2)*124,yy=143+(qi/2)*47;cover.C.Text(item.Item1,xx,yy,7,palette.Muted);var text=item.Item2;var parts=item.Item1=="Zeitraum / Periode"?text.Replace(" ("," | ").Replace(" - "," | bis ").Replace(" -"," | bis ").Replace(")","").Split(" | "):text.Split(' ');string line="";int lineIndex=0;foreach(var word in parts){if(cover.G.MeasureString(line+(line.Length>0?" ":"")+word,new XFont("Backtest UI",8,XFontStyleEx.Bold)).Width>113&&line.Length>0){cover.C.Text(line,xx,yy+12+lineIndex++*10,8,palette.Ink,true);line="";}line+=(line.Length>0?" ":"")+word;}if(line.Length>0)cover.C.Text(line,xx,yy+12+lineIndex*10,8,palette.Ink,true);}
            Chart(cover.C,ChartKind.Balance,24,414,width-48,305);
            TextLines(cover.C,"Balance-Buchungen werden für die Anzeige standardmäßig in festen 60-s-Fenstern zusammengefasst. Equity und Equity-DD bleiben ungeglättet. Rohdaten und Kostenberechnungen bleiben erhalten. Fehlende Testprofilangaben werden nicht aus Qualitätsprozenten erraten.",30,727,width-60,4,9);
        }
        foreach(var currentScope in sourceReport.Deals.Count==0?new List<Report>():ReportOptions.Scopes(sourceReport))
        {
        report=currentScope;scopeTitle=report.AnalysisYear is int year?"Jahresauswertung "+year:"Gesamtauswertung";
        trades=selectedTrades.Where(t=>(!report.AnalysisYear.HasValue||t.Close!.Value.Year==report.AnalysisYear)&& (report.ScopeSymbol.Length==0||t.Symbol==report.ScopeSymbol)).ToList();stats=new Stats(trades,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute);
        string scope=$"n={stats.Count} abgeschlossene Trades; {stats.Trades.Count(t=>!t.ModelDependent)} eindeutige History; {stats.Trades.Count(t=>t.ModelDependent&&!t.Estimated)} Modellzuordnungen; {stats.Trades.Count(t=>t.Estimated)} FIFO-Schätzungen. "+(report.ExcludeWeekends?"Haltezeit ohne Sa/So.":"Kalender-Haltezeit.")+" Broker-Zeit.";
        if(layout=="quick")
        {
            var a=NewPage("Quick Report / A4 Querformat",true);double width=a.Page.Width.Point;Headline(a.C,100,width-48);
            Chart(a.C,ChartKind.Histogram,24,178,(width-64)/2,210);Chart(a.C,ChartKind.Balance,40+(width-64)/2,178,(width-64)/2,210);
            a.C.Rect(24,405,width-48,133,palette.Surface);a.C.Text(Short(report.Strategy)+" | "+report.Platform,38,418,13,palette.Ink,true);
            a.C.Text($"Netto: {DisplayFormat.Money(stats.Net,report)} | Gewinn: {stats.Wins} | Verlust: {stats.Losses} | Null: {stats.Count-stats.Wins-stats.Losses}",38,441,10,palette.Ink);
            a.C.Text($"P90: {Stats.Duration(stats.Quantile(.90))} | P95: {Stats.Duration(stats.Quantile(.95))} | Minimum: {Stats.Duration(stats.Min)}",38,461,10,palette.Ink);
            TextLines(a.C,scope+" "+(stats.Trades.Any(t=>t.Estimated)?"Zuordnung bei parallelen Einstiegen nicht eindeutig; FIFO ist eine Annahme. ":"")+"Equity aus Cache, sofern vorhanden; keine synthetische Equity. Quelle: "+Path.GetFileName(report.Source),38,483,width-345,3,9);
            a.C.Rect(width-294,405,270,133,dark?"#20354F":"#E7F2FF");
            var profile=new[]{("Symbol",report.Find("Symbol")),("Qualität",report.Find("Qualität der Historie","History Quality")),("Ticks",report.Find("Ticks")),("Modell",report.Find("Tickmodell")),("Verzögerung",report.Find("Verzögerung","Delay")),("Spread / Slippage",(report.Find("Spread") is {Length:>0} spread?spread:"nicht exportiert")+" / "+(report.Find("Slippage") is {Length:>0} slippage?slippage:"nicht exportiert"))};double py=412;
            foreach(var item in profile){a.C.Text(item.Item1+": "+(item.Item2.Length>0?item.Item2:"nicht exportiert"),width-284,py,8,palette.Ink);py+=19;}
        }
        else
        {
            var a=NewPage("Detailanalyse / Überblick");double width=a.Page.Width.Point;Headline(a.C,101,width-48);
            a.C.Text((report.IsAccountHistory?"Konto: ":"EA: ")+Short(report.Strategy),30,179,17,palette.Ink,true);TextLines(a.C,scope,30,210,width-60,3);
            Chart(a.C,ChartKind.Balance,24,261,width-48,340);
            a.C.Note("Native PDF-Tooltips ohne JavaScript · Zeitpunkt und Kontodaten je Zeitfenster.",30,620,palette.Muted);


            if(report.EquityPoints.Count>0)
            {
                a=NewPage("Balance / Equity und tatsächlicher Drawdown");Chart(a.C,ChartKind.Equity,24,105,width-48,330);
                var eqdd=AccountCurves.EquityDrawdowns(report);var maxPercent=eqdd.MaxBy(p=>p.Percent);var maxMoney=eqdd.MaxBy(p=>p.Money);
                a.C.Text($"Max. Equity-DD: {maxPercent.Percent:N2}% / {maxPercent.Money:N2} {DisplayFormat.Currency(report)} · {DisplayFormat.Time(maxPercent.Time)}",30,465,11,palette.Ink,true);
                a.C.Text($"Max. Equity-DD in Geld: {maxMoney.Money:N2} {DisplayFormat.Currency(report)} / {maxMoney.Percent:N2}% · {DisplayFormat.Time(maxMoney.Time)}",30,493,11,palette.Ink,true);
                a.C.Text("Max. Margin-Auslastung: "+report.EquityPoints.Max(p=>p.DepositLoad).ToString("N2")+"%",30,526,11,palette.Ink);
                var maxMargin=report.EquityPoints.MaxBy(q=>q.DepositLoad);a.C.Text(DisplayFormat.Time(maxMargin.Time),30,546,8,palette.Muted);DataLines(a.C,Exposure.Text(sourceReport,maxMargin.Time).Replace("\n"," · "),30,559,width-60,7,9);
                var exposures=Exposure.Points(sourceReport).Where(q=>(!report.AxisStart.HasValue||q.Time>=report.AxisStart)&&(!report.AxisEnd.HasValue||q.Time<report.AxisEnd)).ToList();if(exposures.Count>0){var maxPositions=exposures.MaxBy(q=>q.Buy+q.Sell)!;var maxLots=exposures.MaxBy(q=>q.BuyLots+q.SellLots)!;a.C.Text($"Max. offene Positionen: {maxPositions.Buy+maxPositions.Sell} · Buy {maxPositions.Buy} / Sell {maxPositions.Sell}",30,626,9,palette.Ink,true);a.C.Text(DisplayFormat.Time(maxPositions.Time),30,641,7,palette.Muted);a.C.Text($"Max. offene Lots: {maxLots.BuyLots+maxLots.SellLots:N2} · Buy {maxLots.BuyLots:N2} / Sell {maxLots.SellLots:N2}",30,661,9,palette.Ink,true);a.C.Text(DisplayFormat.Time(maxLots.Time),30,676,7,palette.Muted);}
                TextLines(a.C,"Equity-DD = bisheriger Equity-Höchststand minus aktuelle Equity. Balance minus Equity ist der offene Verlust, nicht derselbe Drawdown. Jahres-DD bezieht sich auf den Equity-Höchststand innerhalb des Jahres einschließlich übernommenem Anfangswert. Die Cache-Kurve enthält gespeicherte Beobachtungen, keine vollständige Tickhistorie.",30,706,width-60,5,6);
            }
            if(report.EquityPoints.Count>0&&report.DrawdownEnabled){
                a=NewPage("Drawdownereignisse / Top "+report.DrawdownCount);
                var events=EventDetails.Drawdowns(report);
                for(int batch=0;batch<events.Count;batch+=10){if(batch>0)a=NewPage("Drawdownereignisse / Fortsetzung");double baseY=108;for(int offset=0;offset<Math.Min(10,events.Count-batch);offset++){int ei=batch+offset;double ex=offset<5?30:width/2+6,ey=baseY+(offset%5)*128;var ev=events[ei];a.C.Text("*"+(ei+1)+" Equity-DD · "+ev.Percent.ToString("N2")+" % / "+DisplayFormat.Money(ev.Money,report),ex,ey,10,palette.Ink,true);int lineIndex=0;foreach(var line in EventDetails.Describe(report,ev).Split('\n')){if(a.C.Measure(line,7.8)>width/2-42)throw new InvalidDataException("DD-Zeile überschreitet Spaltenbreite: "+line);a.C.Text(line,ex,ey+18+lineIndex++*13,7.8,palette.Ink);}}}
                a.C.Note("Getrennte Ereignisse · Rang nach prozentualem DD · M1/M5 bezeichnet den letzten abgeschlossenen Kerzenschluss mit seiner Uhrzeit; Ausführung bezeichnet einen exportierten Dealpreis. Kein exakter Tickkurs wird behauptet.",width/2+6,750,palette.Muted);
            }
            a=NewPage("Haltezeiten / Verteilung, Abschluss und Vergleich");Chart(a.C,ChartKind.Histogram,24,103,width-48,230);Chart(a.C,ChartKind.Ecdf,24,345,width-48,225);Chart(a.C,ChartKind.Boxplot,24,582,width-48,182);
            a=NewPage("Zusammenhang / Ergebnis und Zeit");Chart(a.C,ChartKind.Scatter,24,105,width-48,300);Chart(a.C,ChartKind.Monthly,24,423,width-48,285);
            TextLines(a.C,"Zusammenhänge sind beschreibend. Ein höherer Gewinn bei längerer Haltezeit belegt keine Ursache. Bei Teilausstiegen wird das Ergebnis dem Vollschluss des Trades zugeordnet.",30,731,width-60,3);
            a=NewPage("Zeitmuster / Broker-Zeit");Chart(a.C,ChartKind.Hourly,24,105,width-48,275);Chart(a.C,ChartKind.Weekday,24,398,width-48,265);
            a=NewPage("Ordereröffnungen / Stunden, Wochentage und Monate");Chart(a.C,ChartKind.EntryHour,24,103,width-48,213);Chart(a.C,ChartKind.EntryWeekday,24,330,width-48,213);Chart(a.C,ChartKind.EntryMonth,24,557,width-48,213);
            a=NewPage("Gewinne und Verluste / Schließzeit");Chart(a.C,ChartKind.ResultHour,24,103,width-48,213);Chart(a.C,ChartKind.ResultWeekday,24,330,width-48,213);Chart(a.C,ChartKind.ResultMonth,24,557,width-48,213);
            var overnight=stats.Trades.Count(t=>t.Open.Date<t.Close!.Value.Date);var weekends=stats.Trades.Count(t=>Enumerable.Range(0,Math.Min(36600,(t.Close!.Value.Date-t.Open.Date).Days+1)).Any(i=>t.Open.Date.AddDays(i).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
            TextLines(a.C,$"Übernacht-Trades: {overnight}/{stats.Count}. Trades mit berührtem Wochenende: {weekends}/{stats.Count}. Zeiten werden so verarbeitet, wie sie exportiert wurden. UTC-Verschiebung und Handelskalender sind nicht bekannt.",30,773,width-60,1,5);
            if(report.StrategyMode=="single"){a=NewPage("Einzelorder / Einstiegszeiten Gewinner und Verlierer");Chart(a.C,ChartKind.EntryWins,24,103,width-48,213);Chart(a.C,ChartKind.EntryLosses,24,330,width-48,213);Chart(a.C,ChartKind.EntryLossRate,24,557,width-48,213);}
            a=NewPage("Lange Handelszyklen / Start-Wochentag und Uhrzeit");
            bool originalPercent=report.SeriesAsPercent;report.SeriesAsPercent=false;Chart(a.C,ChartKind.SeriesHeatmap,24,103,width-48,215);report.SeriesAsPercent=true;Chart(a.C,ChartKind.SeriesHeatmap,24,330,width-48,215);report.SeriesAsPercent=originalPercent;Chart(a.C,ChartKind.SeriesCurve,24,557,width-48,205);
            TextLines(a.C,"Serie = erster Einstieg bis alle Positionen des Kontos geschlossen sind. Grenzwert: mehr als "+report.LongSeriesHours+" Handelsstunden mit pauschaler Nachtpause. Oben Anzahl, unten Anteil langer Serien an allen in derselben Stunde gestarteten abgeschlossenen Serien. Offene Serien sind rechtszensiert und nicht in der Quote enthalten. Keine Prognose zukünftiger Risiken.",30,775,width-60,1,5);

            var series=SeriesAnalysis.Regular(report);var longs=series.Where(s=>ReportOptions.CycleSeconds(report,s)>report.LongSeriesHours*3600).ToList();
            a=NewPage("Lange Handelszyklen / Top 10 und Datenprüfung");
            a.C.Text("Wochentag",30,110,9,palette.Muted,true);a.C.Text("Stunde",113,110,9,palette.Muted,true);a.C.Text("Lange / Starts",220,110,9,palette.Muted,true);a.C.Text("Anteil > "+report.LongSeriesHours+" h",320,110,9,palette.Muted,true);a.C.Text("> 24 h",452,110,9,palette.Muted,true);
            var ranking=series.GroupBy(c=>new{Day=((int)c.Start.DayOfWeek+6)%7,Hour=c.Start.Hour}).Select(g=>new{g.Key.Day,g.Key.Hour,Total=g.Count(),Over24=g.Count(c=>ReportOptions.CycleSeconds(report,c)>24*3600),Long=g.Count(c=>ReportOptions.CycleSeconds(report,c)>report.LongSeriesHours*3600)}).Where(g=>g.Long>0).OrderByDescending(g=>g.Long/(double)g.Total).ThenByDescending(g=>g.Total).Take(10).ToList();
            int row=0;foreach(var v in ranking){double ry=138+row++*18;a.C.Text(new[]{"Montag","Dienstag","Mittwoch","Donnerstag","Freitag","Samstag","Sonntag"}[v.Day] ,30,ry,9,palette.Ink);a.C.Text(DisplayFormat.Hour(v.Hour)+"–"+DisplayFormat.Hour(v.Hour+1),113,ry,9,palette.Ink);a.C.Text($"{v.Long:N0} / {v.Total:N0}",220,ry,9,palette.Ink,true);a.C.Text($"{100d*v.Long/v.Total:N1} %"+$" ({v.Long} von {v.Total})",320,ry,8,palette.Ink,true);a.C.Text($"{v.Over24} / {v.Total} · {100d*v.Over24/v.Total:N1} %",452,ry,8,palette.Ink);}
            TextLines(a.C,$"Datenbasis: {series.Count:N0} regulär abgeschlossene Zyklen, davon {longs.Count:N0} über {report.LongSeriesHours} Stunden ohne Sa/So. Sortierung nach Anteil langer Zyklen; bei Gleichstand nach Zahl der Starts. Unter 20 Starts: kleine Fallzahl, keine belastbare Risikoprognose. Testende-Zyklen sind separat und ausgeschlossen.",30,325,width-60,3,6);
            double yy=365;
            a.C.Text("Datenprüfung / eigene Berechnungen",30,yy,10,palette.Ink,true);yy+=19;
            var metrics=new[]{("Abgeschlossene Trades",stats.Count.ToString()),("History / Modell / FIFO",$"{stats.Trades.Count(t=>!t.ModelDependent)} / {stats.Trades.Count(t=>t.ModelDependent&&!t.Estimated)} / {stats.Trades.Count(t=>t.Estimated)}"),("Standardabweichung (Stichprobe)",Stats.Duration(stats.Std)),("Volumengewichtete Handelsdauer",Stats.Duration(stats.Weighted)),("Profitfaktor (Trade-Netto)",stats.ProfitFactor?.ToString("N3")??"nicht definiert"),("Offene Positionseinheiten am Abschnittsende",(report.OpenAtScopeEnd??report.Trades.Count(t=>!t.Close.HasValue)).ToString())};
            foreach(var m in metrics){a.C.Text(m.Item1,30,yy,8,palette.Muted);a.C.Text(m.Item2,340,yy,8,palette.Ink);yy+=16;}
            yy+=15;var calendarStats=new Stats(trades);var weekdaysStats=new Stats(trades,true,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute);
            a.C.Text("Kalenderzeit Ø / Max: "+Stats.Duration(calendarStats.Mean)+" / "+Stats.Duration(calendarStats.Max),30,yy,9,palette.Ink);yy+=15;
            a.C.Text("Handelsdauer Ø / Max: "+Stats.Duration(weekdaysStats.Mean)+" / "+Stats.Duration(weekdaysStats.Max),30,yy,9,palette.Ink);yy+=15;
            a.C.Text("Originalangaben aus MetaTrader (nicht neu berechnet)",30,yy,7,palette.Ink,true);yy+=18;
            var original=(report.AnalysisYear.HasValue?new Dictionary<string,string>():report.Metadata).Where(m=>Parser.Key(m.Key).Contains("halte")||Parser.Key(m.Key).Contains("holding")||Parser.Key(m.Key).Contains("equity")||Parser.Key(m.Key).Contains("sharpe")||Parser.Key(m.Key).Contains("quality")||Parser.Key(m.Key).Contains("qualitat")).Take(5);
            foreach(var raw in original){var m=new KeyValuePair<string,string>(raw.Key,DisplayFormat.Metadata(raw.Key,raw.Value,report));a.C.Text(Short(m.Key,48),30,yy,9,palette.Muted);a.C.Text(Short(m.Value,35),330,yy,9,palette.Ink);yy+=15;}
            // Import diagnostics belong exclusively to the final help page.
            a=NewPage("Kontoergebnis / Kosten und Risiko");yy=110;
            string currency=DisplayFormat.Currency(report);
            var ddPercent=YearAnalysis.MaxDrawdown(report,true);var ddMoney=YearAnalysis.MaxDrawdown(report,false);
            var accountMetrics=new[]{
                ("Anfangsbestand",report.InitialDeposit.ToString("N2")+" "+currency),
                ("Letzter exportierter Kontostand",report.Balances.LastOrDefault().Balance.ToString("N2")+" "+currency),
                ("Handelsergebnis einschließlich Kontogebühren",AccountHistory.TradingNet(report).ToString("N2")+" "+currency),
                ("Brutto / Kommission / Swap / Fee",$"{report.Deals.Sum(d=>d.Profit):N2} / {report.Deals.Sum(d=>d.Commission):N2} / {report.Deals.Sum(d=>d.Swap):N2} / "+(report.AvailableColumns.Contains("fee")?report.Deals.Sum(d=>d.Fee).ToString("N2"):"nicht exportiert")),
                ("Rohbuchungs-Balance-DD in Prozent (Prüfwert)",$"{ddPercent.Percent:N2}% / {ddPercent.Money:N2} {currency}"),
                ("Zeitpunkt / Bezugs-Höchststand",$"{DisplayFormat.Time(ddPercent.Time)} / {ddPercent.Peak:N2} {currency}"),
                ("Rohbuchungs-Balance-DD in Geld (Prüfwert)",$"{ddMoney.Money:N2} {currency} / {ddMoney.Percent:N2}%"),
                ("Zeitpunkt / Bezugs-Höchststand",$"{DisplayFormat.Time(ddMoney.Time)} / {ddMoney.Peak:N2} {currency}"),
                ("Trade-Netto der Schlussjahr-Kohorte",stats.Net.ToString("N2")+" "+currency),
                ("Gewinner / Verlierer / Null",$"{stats.Wins:N0} / {stats.Losses:N0} / {stats.Count-stats.Wins-stats.Losses:N0}"),
                ("Trefferquote",stats.Count>0?(100d*stats.Wins/stats.Count).ToString("N2")+"%":"nicht definiert"),
                ("Erwartetes Netto je Trade",stats.Count>0?DisplayFormat.Money(stats.Net/stats.Count,report):"nicht definiert"),
                ("Bestes / schlechtestes Trade",stats.Count>0?DisplayFormat.Money(stats.Trades.Max(t=>t.Net),report)+" / "+DisplayFormat.Money(stats.Trades.Min(t=>t.Net),report):"keine"),
                ("Long / Short Trades",$"{stats.Trades.Count(t=>t.Side=="buy"):N0} / {stats.Trades.Count(t=>t.Side=="sell"):N0}")};
            var adjusted=SeriesAnalysis.Adjusted(report);
            a.C.Text("Gesamtgewinn · tatsächliches Ergebnis",30,yy,9,palette.Muted);a.C.Text(AccountHistory.TradingNet(report).ToString("N2")+" "+currency,30,yy+13,12,palette.Ink,true);yy+=39;
            if(adjusted.Time.HasValue){a.C.Text("Um letzten Zyklus bereinigt",30,yy,9,palette.Muted);a.C.Text($"Gesamtgewinn am {DisplayFormat.Time(adjusted.Time.Value)}: {adjusted.Profit:N2} {currency}",30,yy+13,10,palette.Ink,true);yy+=38;a.C.Text($"Testende-Zyklus separat: {adjusted.Excluded:N2} {currency}",30,yy,9,palette.Negative,true);yy+=18;}else{a.C.Text("Keine erkennbare Testende-Schließung in diesem Abschnitt.",30,yy,8,palette.Muted);yy+=20;}
            var cards=accountMetrics.Where(m=>!m.Item1.StartsWith("Brutto / ")).ToList();cards.InsertRange(3,new[]{("Brutto aus Deals",report.Deals.Sum(d=>d.Profit).ToString("N2")+" "+currency),("Kommission",report.Deals.Sum(d=>d.Commission).ToString("N2")+" "+currency),("Swap",report.Deals.Sum(d=>d.Swap).ToString("N2")+" "+currency),("Separate Gebühren",report.AvailableColumns.Contains("fee")?report.Deals.Sum(d=>d.Fee).ToString("N2")+" "+currency:"nicht exportiert")});
            for(int mi=0;mi<cards.Count;mi++){double xx=24+(mi%2)*(width-36)/2,cy=yy+(mi/2)*53;var m=cards[mi];a.C.Rect(xx,cy,(width-60)/2,45,palette.Surface);a.C.Text(Short(m.Item1,47),xx+8,cy+5,7,palette.Muted);a.C.Text(m.Item2+(!m.Item2.Contains(currency)&&((m.Item1.Contains("Netto")||m.Item1.Contains("schlechtestes")||m.Item1.Contains("Höchststand")))?" "+currency:""),xx+8,cy+20,9,palette.Ink,true);}
            TextLines(a.C,"Originalkennzahlen bleiben separat erhalten. Buchungsjahr und Schlussjahr werden unterschieden; echte Kontobuchungen werden nicht entfernt.",30,yy+Math.Ceiling(cards.Count/2d)*53+10,width-60,3,6);
            a=NewPage("Monatsübersicht / gebuchte Deals");yy=111;
            void MonthHeader(){a.C.Text("Monat",30,yy,8,palette.Muted,true);Right(a.C,a.G,"Deals",190,yy,8,true);Right(a.C,a.G,"Lots (Einstieg)",270,yy,8,true);Right(a.C,a.G,"Netto ("+currency+")",370,yy,8,true);Right(a.C,a.G,"Komm. ("+currency+")",460,yy,8,true);Right(a.C,a.G,"Swap ("+currency+")",565,yy,8,true);yy+=20;}
            MonthHeader();
            foreach(var yearGroup in report.Deals.GroupBy(d=>d.Time.Year).OrderBy(g=>g.Key)){
                if(yy+yearGroup.Select(d=>d.Time.Month).Distinct().Count()*23+26>773){a=NewPage("Monatsübersicht / Fortsetzung");yy=111;MonthHeader();}
                foreach(var month in yearGroup.GroupBy(d=>d.Time.Month).OrderBy(g=>g.Key)){if(yy>756){a=NewPage("Monatsübersicht / Fortsetzung");yy=111;MonthHeader();}var dt=new DateTime(yearGroup.Key,month.Key,1);a.C.Text(dt.ToString("yyyy-MM")+" "+dt.ToString("MMMM",Localization.Culture),30,yy,8,palette.Ink);Right(a.C,a.G,month.Count().ToString("N0"),190,yy);Right(a.C,a.G,month.Where(d=>d.Entry is "in" or "inout").Sum(d=>d.Volume).ToString("N2"),270,yy);Right(a.C,a.G,DisplayFormat.Money(month.Sum(d=>d.Net),report),370,yy);Right(a.C,a.G,DisplayFormat.Money(month.Sum(d=>d.Commission),report),460,yy);Right(a.C,a.G,DisplayFormat.Money(month.Sum(d=>d.Swap),report),565,yy);yy+=23;}
                if(yy>756){a=NewPage("Monatsübersicht / Jahressumme");yy=111;MonthHeader();}a.C.Line(30,yy-4,width-30,yy-4,palette.Positive,.7);a.C.Text("Summe "+yearGroup.Key,30,yy,9,palette.Ink,true);Right(a.C,a.G,yearGroup.Count().ToString("N0"),190,yy,8,true);Right(a.C,a.G,yearGroup.Where(d=>d.Entry is "in" or "inout").Sum(d=>d.Volume).ToString("N2"),270,yy,8,true);Right(a.C,a.G,DisplayFormat.Money(yearGroup.Sum(d=>d.Net),report),370,yy,8,true);Right(a.C,a.G,DisplayFormat.Money(yearGroup.Sum(d=>d.Commission),report),460,yy,8,true);Right(a.C,a.G,DisplayFormat.Money(yearGroup.Sum(d=>d.Swap),report),565,yy,8,true);var annual=YearAnalysis.ForYear(report,yearGroup.Key);var annualDd=AccountCurves.EquityDrawdowns(annual);a.C.Text(annualDd.Count>0?$"(Equity-DD Max: {annualDd.Max(d=>d.Percent):N2} %)":"(Equity-DD nicht verfügbar)",30,yy+15,6,palette.Muted);yy+=56;
            }
            a.C.Note("Geldspalten in "+currency+". Lots zählen eröffnendes Volumen; Schließungen werden nicht doppelt gezählt. Geschlossene Wochentage sind in Tageskategorien ausgeblendet, vorhandene Buchungen bleiben für den Kontoabgleich erhalten.",80,111,palette.Muted);
            var availableKinds=AdvancedPainter.Kinds.Where(k=>AdvancedAnalysis.Panel(report,k).Values.Count>0&&(k!=ChartKind.Correlation||SymbolAnalysis.Symbols(report).Count>1)).ToArray();
            for(int ai=0;ai<availableKinds.Length;){int take=availableKinds.Length-ai==4?2:Math.Min(3,availableKinds.Length-ai);a=NewPage("Erweiterte Analyse / "+AdvancedPainter.Title(availableKinds[ai]));for(int aj=ai;aj<ai+take;aj++)Chart(a.C,availableKinds[aj],24,103+(aj-ai)*224,width-48,212);ai+=take;}
            if(appendix)
            {
                var sorted=stats.Trades.OrderByDescending(t=>t.Seconds).ToList();for(int start=0;start<sorted.Count;start+=25){a=NewPage("Trade-Anhang / nach Haltezeit sortiert");a.C.Text("ID / Symbol",30,103,10,palette.Muted,true);a.C.Text("Einstieg / Vollschluss",150,103,10,palette.Muted,true);a.C.Text("Haltezeit / Netto",340,103,10,palette.Muted,true);yy=132;
                    foreach(var tr in sorted.Skip(start).Take(25)){a.C.Text(Short(tr.Id+" / "+tr.Symbol,22),30,yy,9,palette.Ink);a.C.Text(tr.Open.ToString("yyyy-MM-dd HH:mm:ss"),150,yy,9,palette.Ink);a.C.Text(tr.Close!.Value.ToString("yyyy-MM-dd HH:mm:ss"),150,yy+10,8,palette.Muted);a.C.Text(tr.Duration,340,yy,9,palette.Ink);a.C.Text(DisplayFormat.Money(tr.Net,report)+" / "+tr.Quality,340,yy+10,8,tr.Net>=0?palette.Positive:palette.Negative);yy+=18;}}
            }
        }
        }
        report=sourceReport;scopeTitle=sourceReport.IsAccountHistory?"Kontohistorie / gemeinsame Einstellungen":"Gesamttest / gemeinsame Einstellungen";
        if(sourceReport.IsAccountHistory&&sourceReport.AccountBookings.Count>0&&layout!="quick"){
         var cash=NewPage("Kontobuchungen");double cy=110;cash.C.Text("Einzahlungen",30,cy,9,palette.Muted);cash.C.Text(DisplayFormat.Money(sourceReport.AccountBookings.Where(b=>b.Kind is "balance" or "initial"&&b.Amount>0).Sum(b=>b.Amount),sourceReport),30,cy+14,12,palette.Ink,true);cash.C.Text("Auszahlungen",300,cy,9,palette.Muted);cash.C.Text(DisplayFormat.Money(sourceReport.AccountBookings.Where(b=>b.Kind=="balance"&&b.Amount<0).Sum(b=>b.Amount),sourceReport),300,cy+14,12,palette.Ink,true);cy+=50;
         foreach(var booking in sourceReport.AccountBookings){if(cy>750){cash=NewPage("Kontobuchungen / Fortsetzung");cy=110;}cash.C.Text(DisplayFormat.Time(booking.Time),30,cy,8,palette.Ink);cash.C.Text(Localization.T(AccountHistory.Label(booking)),170,cy,8,palette.Ink);cash.C.RightText(DisplayFormat.Money(booking.Amount,sourceReport),560,cy,9,booking.Amount>=0?palette.Positive:palette.Negative,true);cy+=22;}
        }
        if(layout!="quick"&&sourceReport.InputParameters.Count>0)
        {
            var parameters=sourceReport.InputParameters.Where(p=>p.Trim()!="="&&p.Trim().Length>0).ToList();int columns=parameters.Count>70?3:2;
            int index=0;
            while(index<parameters.Count)
            {
                var a=NewPage("Strategieparameter");double cell=(a.Page.Width.Point-60)/columns;
                a.C.Text("Originalwerte · gelten für den gesamten Test",30,102,10,palette.Muted);
                for(int col=0;col<columns&&index<parameters.Count;col++)
                {
                    double labelSize=parameters.Count>110?5.5:6.5,valueSize=parameters.Count>110?7.8:9;
                    double yy=115;int labelChars=(int)((cell-12)/(labelSize*.5));int valueChars=(int)((cell-12)/(valueSize*.52));
                    while(index<parameters.Count)
                    {
                        string parameter=parameters[index];int split=parameter.IndexOf('=');string label=split>=0?parameter[..split]:parameter,value=split>=0?parameter[(split+1)..]:"";
                        List<string> Wrap(string value,double fs){var lines=new List<string>();string line="";foreach(var word in value.Split(' ')){string candidate=line.Length>0?line+" "+word:word;if(a.C.Measure(candidate,fs)>cell-12&&line.Length>0){lines.Add(line);line=word;}else line=candidate;}lines.Add(line);return lines;}
                        var labelText=Wrap(label,labelSize);var valueText=value.Length>0?Wrap(value,valueSize):new List<string>();int labelLines=labelText.Count,valueLines=valueText.Count;
                        double headingGap=value.Length==0?12:3;double needed=headingGap+labelLines*(labelSize+1.5)+valueLines*(valueSize+1.5)+(value.Length==0?25:0);if(yy+needed>748)break;
                        yy+=headingGap;for(int line=0;line<labelLines;line++)a.C.Text(labelText[line],30+col*cell,yy+line*(labelSize+1.5),labelSize,valueLines==0?palette.Ink:palette.Muted,valueLines==0);
                        yy+=labelLines*(labelSize+1.5);
                        for(int line=0;line<valueLines;line++)a.C.Text(valueText[line],30+col*cell,yy+line*(valueSize+1.5),valueSize,palette.Ink,true);
                        yy+=valueLines*(valueSize+1.5);index++;
                    }
                }
            }
        }
        if(layout!="quick")
        {
            var items=sourceReport.Metadata.Where(m=>!new[]{"eingaben","inputs","parameters"}.Contains(Parser.Key(m.Key))).Select(m=>new KeyValuePair<string,string>(m.Key,DisplayFormat.Metadata(m.Key,m.Value,sourceReport))).ToList();int index=0;
            while(index<items.Count)
            {
                int columns=items.Count>40?3:2;var a=NewPage("Alle Originalkennzahlen / Gesamttest");double cell=(a.Page.Width.Point-60)/columns;
                int lc=(int)((cell-14)/(6.5*.5)),vc=(int)((cell-14)/(10*.52));
                double ItemHeight(KeyValuePair<string,string> m)=>Math.Max(1,Math.Ceiling(m.Key.Length/(double)lc))*7+Math.Max(1,Math.Ceiling(m.Value.Length/(double)vc))*11+7;
                double columnBottom=Math.Min(747,110+items.Sum(ItemHeight)/columns+items.Max(ItemHeight));
                for(int col=0;col<columns&&index<items.Count;col++)
                {
                    double yy=110;int labelChars=(int)((cell-14)/(6.5*.5)),valueChars=(int)((cell-14)/(10*.52));
                    while(index<items.Count)
                    {
                        var item=items[index];int labelLines=Math.Max(1,(int)Math.Ceiling(item.Key.Length/(double)labelChars)),valueLines=Math.Max(1,(int)Math.Ceiling(item.Value.Length/(double)valueChars));
                        if(yy+labelLines*7+valueLines*11+7>columnBottom)break;
                        for(int line=0;line<labelLines;line++)a.C.Text(item.Key.Substring(line*labelChars,Math.Min(labelChars,item.Key.Length-line*labelChars)),30+col*cell,yy+line*7,6.5,palette.Muted);
                        yy+=labelLines*7;
                        for(int line=0;line<valueLines;line++)a.C.Text(item.Value.Substring(line*valueChars,Math.Min(valueChars,item.Value.Length-line*valueChars)),30+col*cell,yy+line*11,10,palette.Ink,true);
                        yy+=valueLines*11+7;index++;
                    }
                }
            }
        }
        var sourceFiles=new List<string>{sourceReport.Source};var folder=Path.GetDirectoryName(sourceReport.Source)!;var stem=Path.GetFileNameWithoutExtension(sourceReport.Source);if(Directory.Exists(folder))sourceFiles.AddRange(Directory.EnumerateFiles(folder,stem+"*",SearchOption.TopDirectoryOnly).Where(f=>new[]{".html",".xlsx",".png"}.Contains(Path.GetExtension(f).ToLowerInvariant())));
        PdfAttachments.Add(doc,sourceFiles);
        {
         var sections=HelpContent.Sections(sourceReport);
         sections.Add((Localization.English?"Embedded originals":"Eingebettete Originaldateien",string.Join(" · ",sourceFiles.Distinct().Where(File.Exists).Select(Path.GetFileName))));
         sections.Add((Localization.English?"Import diagnostics":"Importhinweise",string.Join("\n",sourceReport.Warnings.Distinct())));
         var explanatory=notes.Texts.ToList();for(int n=0;n<explanatory.Count;n++)sections.Add(("*"+(n+1),explanatory[n]));
         var targets=new Dictionary<int,(int Page,double X,double Y)>();var help=NewPage("Hilfe und Erklärung");double cw=(help.Page.Width.Point-72)/2;
         // Exactly one two-column appendix, with measured wrapping and an adaptive small font.
         double font=4.5,leading=5.25;List<(string Title,List<string> Lines)> wrapped=new();
         for(;;){wrapped.Clear();foreach(var section in sections){var lines=new List<string>();foreach(var paragraph in section.Body.Split('\n')){string line="";foreach(var word in paragraph.Split(' ')){var next=line.Length>0?line+" "+word:word;if(help.C.Measure(next,font)>cw&&line.Length>0){lines.Add(line);line=word;}else line=next;}if(line.Length>0)lines.Add(line);}if(lines.Count>0)wrapped.Add((section.Title,lines));}
          double needed=wrapped.Sum(q=>leading*(q.Lines.Count+1)+3);double capacity=2*(765-108);if(needed<=capacity-40)break;font-=.1;leading=font+ .65;if(font<3.5)throw new InvalidDataException("Hilfeseite passt nicht vollständig auf eine Seite.");}
         int column=0;double hy=108,hx=30;foreach(var section in wrapped){double needed=leading*(section.Lines.Count+1)+3;if(hy+needed>765){column++;hy=108;hx=30+column*(cw+12);}if(column>1)throw new InvalidDataException("Hilfeseite überschreitet zwei Spalten.");
          if(section.Title.StartsWith("*")&&int.TryParse(section.Title[1..],out int number))targets[number]=(doc.PageCount,hx,hy);
          help.C.Text(section.Title,hx,hy,font+.25,palette.Ink,true);hy+=leading;foreach(var line in section.Lines){help.C.Text(line,hx,hy,font,palette.Muted);hy+=leading;}hy+=3;
         }
         foreach(var link in notes.Links)if(targets.TryGetValue(link.Number,out var target))link.Page.AddDocumentLink(new PdfRectangle(new XRect(link.X,link.Page.Height.Point-link.Y-9,18,9)),target.Page,new XPoint(target.X,doc.Pages[target.Page-1].Height.Point-target.Y));
        }
        for(int i=0;i<pages.Count;i++)
        {
            var page=pages[i];double height=page.Page.Height.Point,width=page.Page.Width.Point;
            string footer="Backtest-Analyzer Beta 1.0 · © "+DateTime.Now.Year+" Michael P. Thiess · "+Repository.Replace("https://","");
            var footerFont=new XFont("Backtest UI",7.5);double footerWidth=page.Graphics.MeasureString(footer,footerFont).Width;
            page.Canvas.Text(footer,(width-footerWidth)/2,height-36,7.5,palette.Ink);
            string legal="Jegliche Haftung vollständig ausgeschlossen · Keine Gewähr für Vollständigkeit und Richtigkeit · Berichte/Analysen können Fehler enthalten · Keine Verbindung zu MetaQuotes · MetaTrader: Marken/Urheberrechte MetaQuotes Ltd";
            double legalWidth=page.Graphics.MeasureString(legal,new XFont("Backtest UI",4,XFontStyleEx.Bold)).Width;
            page.Canvas.Text(legal,(width-legalWidth)/2,height-22,4,palette.Muted,true);
            page.Canvas.RightText($"{i+1} / {pages.Count}",width-24,height-35.75,7,palette.Muted);
            page.Page.AddWebLink(new PdfRectangle(new XRect((width-footerWidth)/2,25,footerWidth,11)),Repository);page.Graphics.Dispose();
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);doc.Save(path);
    }
}
