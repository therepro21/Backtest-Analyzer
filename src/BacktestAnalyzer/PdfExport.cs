using System.Reflection;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace BacktestAnalyzer;
public sealed class PdfCanvas(XGraphics g):ICanvas
{
    static XColor Color(string s)=>XColor.FromArgb(Convert.ToInt32(s[1..3],16),Convert.ToInt32(s[3..5],16),Convert.ToInt32(s[5..7],16));
    public void Rect(double x,double y,double w,double h,string fill){if(w>0&&h>0)g.DrawRectangle(new XSolidBrush(Color(fill)),x,y,w,h);}
    public void Line(double x1,double y1,double x2,double y2,string color,double width=1)=>g.DrawLine(new XPen(Color(color),width),x1,y1,x2,y2);
    public void Text(string text,double x,double y,double size,string color,bool bold=false)=>g.DrawString(text,new XFont("Arial",size,bold?XFontStyleEx.Bold:XFontStyleEx.Regular),new XSolidBrush(Color(color)),new XPoint(x,y+size));
    public void Circle(double x,double y,double radius,string color,bool hollow=false){if(hollow)g.DrawEllipse(new XPen(Color(color)),x-radius,y-radius,radius*2,radius*2);else g.DrawEllipse(new XSolidBrush(Color(color)),x-radius,y-radius,radius*2,radius*2);}
}
public static class PdfExport
{
    public const string Repository="https://github.com/therepro21/Backtest-Analyzer";
    public static byte[] Logo=>ReadLogo();
    static byte[] ReadLogo(){using var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png")!;using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
    public static void Save(Report report,List<Trade> trades,string path,bool dark,string layout,bool appendix=false)
    {
        GlobalFontSettings.UseWindowsFontsUnderWindows=true;
        using var doc=new PdfDocument();doc.Info.Title="Backtest-Analyzer - "+report.Strategy;doc.Info.Author="Michael P. Thiess";doc.Info.Subject="Haltezeit- und Backtestanalyse";
        var palette=new Palette(dark);var stats=new Stats(trades);var pages=new List<(PdfPage Page,XGraphics Graphics,PdfCanvas Canvas)>();
        (PdfPage Page,XGraphics G,PdfCanvas C) NewPage(string title,bool landscape=false)
        {
            var page=doc.AddPage();page.Size=PdfSharp.PageSize.A4;if(landscape)page.Orientation=PdfSharp.PageOrientation.Landscape;
            var g=XGraphics.FromPdfPage(page);var c=new PdfCanvas(g);pages.Add((page,g,c));double width=page.Width.Point,height=page.Height.Point;
            c.Rect(0,0,width,height,palette.Background);c.Rect(24,18,width-48,67,palette.Surface);
            using var image=XImage.FromStream(new MemoryStream(Logo));g.DrawImage(image,32,23,55,55);
            c.Text("Backtest-Analyzer",98,25,20,palette.Ink,true);c.Text(title,98,52,11,palette.Muted);
            return(page,g,c);
        }
        void TextLines(PdfCanvas c,string text,double x,double y,double width,int max=20,double size=10)
        {
            var words=text.Split(' ');string line="";int count=0;
            foreach(var word in words){if((line.Length+word.Length)*size*.51>width&&line.Length>0){c.Text(line,x,y+count*(size+5),size,palette.Muted);line="";if(++count>=max)return;}line+=(line.Length>0?" ":"")+word;}
            if(line.Length>0&&count<max)c.Text(line,x,y+count*(size+5),size,palette.Muted);
        }
        string Short(string t,int len=80)=>t.Length>len?t[..(len-1)]+"…":t;
        void Metric(PdfCanvas c,string label,string value,double x,double y,double width){c.Rect(x,y,width,60,palette.Surface);c.Text(label,x+10,y+7,9,palette.Muted);c.Text(value,x+10,y+27,14,palette.Ink,true);}
        void Headline(PdfCanvas c,double y,double width){double cell=(width-20)/3;Metric(c,"Durchschnitt",Stats.Duration(stats.Mean),24,y,cell);Metric(c,"Median",Stats.Duration(stats.Median),34+cell,y,cell);Metric(c,"Maximum",Stats.Duration(stats.Max),44+cell*2,y,cell);}
        void Chart(PdfCanvas c,ChartKind kind,double x,double y,double width,double height)=>ChartPainter.Draw(c,kind,report,stats.Trades,palette,x,y,width,height);
        string scope=$"n={stats.Count} geschlossene Entry-Lots; {stats.Trades.Count(t=>!t.ModelDependent)} eindeutige History; {stats.Trades.Count(t=>t.ModelDependent&&!t.Estimated)} Modellzuordnungen; {stats.Trades.Count(t=>t.Estimated)} FIFO-Schätzungen. Kalenderzeit in Broker-Zeit.";
        if(layout=="quick")
        {
            var a=NewPage("Quick Report / A4 Querformat",true);double width=a.Page.Width.Point;Headline(a.C,100,width-48);
            Chart(a.C,ChartKind.Histogram,24,178,(width-64)/2,210);Chart(a.C,ChartKind.Scatter,40+(width-64)/2,178,(width-64)/2,210);
            a.C.Rect(24,405,width-48,133,palette.Surface);a.C.Text(Short(report.Strategy)+" | "+report.Platform,38,418,13,palette.Ink,true);
            a.C.Text($"Netto: {stats.Net:N2} | Gewinn: {stats.Wins} | Verlust: {stats.Losses} | Null: {stats.Count-stats.Wins-stats.Losses}",38,441,10,palette.Ink);
            a.C.Text($"P90: {Stats.Duration(stats.Quantile(.90))} | P95: {Stats.Duration(stats.Quantile(.95))} | Minimum: {Stats.Duration(stats.Min)}",38,461,10,palette.Ink);
            TextLines(a.C,scope+" "+(stats.Trades.Any(t=>t.Estimated)?"Zuordnung bei parallelen Einstiegen nicht eindeutig; FIFO ist eine Annahme. ":"")+"Equity/MAE/MFE nicht rekonstruiert. Quelle: "+Path.GetFileName(report.Source),38,483,width-78,3,9);
        }
        else
        {
            var a=NewPage("Detailanalyse / Überblick");double width=a.Page.Width.Point;Headline(a.C,101,width-48);
            a.C.Text(Short(report.Strategy),30,179,17,palette.Ink,true);TextLines(a.C,scope,30,210,width-60,3);
            Chart(a.C,ChartKind.Balance,24,261,width-48,235);Chart(a.C,ChartKind.Drawdown,24,514,width-48,235);
            a=NewPage("Haltezeiten / Verteilung und Streuung");Chart(a.C,ChartKind.Histogram,24,105,width-48,235);Chart(a.C,ChartKind.Ecdf,24,355,width-48,230);Chart(a.C,ChartKind.Boxplot,24,600,width-48,180);
            a=NewPage("Zusammenhang / Ergebnis und Zeit");Chart(a.C,ChartKind.Scatter,24,105,width-48,300);Chart(a.C,ChartKind.Monthly,24,423,width-48,285);
            TextLines(a.C,"Zusammenhänge sind beschreibend. Ein höherer Gewinn bei längerer Haltezeit belegt keine Ursache. Bei Teilausstiegen wird das Ergebnis dem Vollschluss des Entry-Lots zugeordnet.",30,731,width-60,3);
            a=NewPage("Zeitmuster / Broker-Zeit");Chart(a.C,ChartKind.Hourly,24,105,width-48,275);Chart(a.C,ChartKind.Weekday,24,398,width-48,265);
            var overnight=stats.Trades.Count(t=>t.Open.Date<t.Close!.Value.Date);var weekends=stats.Trades.Count(t=>Enumerable.Range(0,Math.Min(36600,(t.Close!.Value.Date-t.Open.Date).Days+1)).Any(i=>t.Open.Date.AddDays(i).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
            TextLines(a.C,$"Übernacht-Trades: {overnight}/{stats.Count}. Trades mit berührtem Wochenende: {weekends}/{stats.Count}. Zeiten werden so verarbeitet, wie sie exportiert wurden. UTC-Verschiebung und Handelskalender sind nicht bekannt.",30,687,width-60,5);
            a=NewPage("Datenprüfung / Methodik und Originalkennzahlen");double yy=105;
            a.C.Text("Eigene Berechnungen",30,yy,14,palette.Ink,true);yy+=26;
            var metrics=new[]{("Geschlossene Entry-Lots",stats.Count.ToString()),("History / Modell / FIFO",$"{stats.Trades.Count(t=>!t.ModelDependent)} / {stats.Trades.Count(t=>t.ModelDependent&&!t.Estimated)} / {stats.Trades.Count(t=>t.Estimated)}"),("Standardabweichung (Stichprobe)",Stats.Duration(stats.Std)),("Volumengewichtete Teilausstiegsdauer",Stats.Duration(stats.Weighted)),("Profitfaktor (Trade-Netto)",stats.ProfitFactor?.ToString("N3")??"nicht definiert"),("Offene Entry-Lots (Gesamtdatei)",report.Trades.Count(t=>!t.Close.HasValue).ToString())};
            foreach(var m in metrics){a.C.Text(m.Item1,30,yy,10,palette.Muted);a.C.Text(m.Item2,340,yy,10,palette.Ink);yy+=23;}
            yy+=15;a.C.Text("Originalangaben aus MetaTrader (nicht neu berechnet)",30,yy,12,palette.Ink,true);yy+=25;
            var original=report.Metadata.Where(m=>Parser.Key(m.Key).Contains("halte")||Parser.Key(m.Key).Contains("holding")||Parser.Key(m.Key).Contains("equity")||Parser.Key(m.Key).Contains("sharpe")||Parser.Key(m.Key).Contains("quality")||Parser.Key(m.Key).Contains("qualitat")).Take(9);
            foreach(var m in original){a.C.Text(Short(m.Key,48),30,yy,9,palette.Muted);a.C.Text(Short(m.Value,35),330,yy,9,palette.Ink);yy+=20;}
            yy+=12;a.C.Text("Importhinweise",30,yy,12,palette.Ink,true);yy+=23;
            foreach(var warning in report.Warnings.Distinct().Take(5)){TextLines(a.C,warning,30,yy,width-60,4,9);yy+=Math.Min(4,1+(int)Math.Ceiling(warning.Length*4.6/(width-60)))*14+8;if(yy>746)break;}
            if(appendix)
            {
                var sorted=stats.Trades.OrderByDescending(t=>t.Seconds).ToList();for(int start=0;start<sorted.Count;start+=25){a=NewPage("Trade-Anhang / nach Haltezeit sortiert");a.C.Text("ID / Symbol",30,103,10,palette.Muted,true);a.C.Text("Einstieg / Vollschluss",150,103,10,palette.Muted,true);a.C.Text("Haltezeit / Netto",340,103,10,palette.Muted,true);yy=132;
                    foreach(var tr in sorted.Skip(start).Take(25)){a.C.Text(Short(tr.Id+" / "+tr.Symbol,22),30,yy,9,palette.Ink);a.C.Text(tr.Open.ToString("yyyy-MM-dd HH:mm:ss"),150,yy,9,palette.Ink);a.C.Text(tr.Close!.Value.ToString("yyyy-MM-dd HH:mm:ss"),150,yy+10,8,palette.Muted);a.C.Text(tr.Duration,340,yy,9,palette.Ink);a.C.Text(tr.Net.ToString("N2")+" / "+tr.Quality,340,yy+10,8,tr.Net>=0?palette.Positive:palette.Negative);yy+=25;}}
            }
        }
        for(int i=0;i<pages.Count;i++)
        {
            var page=pages[i];double height=page.Page.Height.Point,width=page.Page.Width.Point;page.Canvas.Text("© "+DateTime.Now.Year+" Michael P. Thiess",24,height-38,9,palette.Muted);page.Canvas.Text(Repository,24,height-23,8,palette.Muted);page.Canvas.Text($"{i+1} / {pages.Count}",width-58,height-30,9,palette.Muted);
            page.Canvas.Text("BETA 0.1 · Fehler möglich · Keine Anlageberatung · Unabhängig von MetaQuotes",24,height-54,8,palette.Muted);
            page.Page.AddWebLink(new PdfRectangle(new XRect(24,12,Math.Min(width-80,300),18)),Repository);page.Graphics.Dispose();
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);doc.Save(path);
    }
}
