using System.Globalization;
using System.Net;
using System.Text;

namespace BacktestAnalyzer;

public sealed class SvgCanvas:ICanvas
{
    public StringBuilder Content {get;}=new();
    public void Hover(double x,double y,double w,double h,string text)=>Content.Append($"<rect x='{N(x)}' y='{N(y)}' width='{N(w)}' height='{N(h)}' fill='transparent'><title>{E(Localization.T(text))}</title></rect>");
    static string N(double number)=>number.ToString("0.###",CultureInfo.InvariantCulture);
    static string E(string value)=>WebUtility.HtmlEncode(value);
    static string C(string color)=>color switch{"#FFFFFF"=>"var(--surface)","#F2F5F9"=>"var(--background)","#173047"=>"var(--ink)","#52677D"=>"var(--muted)","#DCE4ED"=>"var(--line)","#113B65"=>"var(--blue)","#C87500"=>"var(--orange)","#E7EFF7"=>"var(--balancefill)","#FBEBD8"=>"var(--lossfill)","#EDBB77"=>"var(--heatmid)","#DCE8F5"=>"var(--heatlow)",_=>color};
    public void Rect(double x,double y,double width,double height,string fill){if(width>0&&height>0)Content.Append($"<rect x='{N(x)}' y='{N(y)}' width='{N(width)}' height='{N(height)}' fill='{C(fill)}'/>");}
    public void Line(double x1,double y1,double x2,double y2,string color,double width=1)=>Content.Append($"<path d='M{N(x1)} {N(y1)}L{N(x2)} {N(y2)}' fill='none' stroke='{C(color)}' stroke-width='{N(width)}'/>");
    public void Text(string text,double x,double y,double size,string color,bool bold=false)=>Content.Append($"<text x='{N(x)}' y='{N(y+size)}' font-size='{N(size)}' fill='{C(color)}' font-weight='{(bold?600:400)}'>{E(Localization.T(text))}</text>");
    public void VerticalText(string text,double x,double y,double size,string color)=>Content.Append($"<text x='{N(x)}' y='{N(y)}' transform='rotate(-90 {N(x)} {N(y)})' text-anchor='middle' dominant-baseline='middle' font-size='{N(size)}' fill='{C(color)}'>{E(Localization.T(text))}</text>");
    public void Circle(double x,double y,double radius,string color,bool hollow=false)=>Content.Append($"<circle cx='{N(x)}' cy='{N(y)}' r='{N(radius)}' fill='{(hollow?"none":C(color))}' stroke='{C(color)}'/>");
}
public static class HtmlSections
{
    public static string Build(Report source)
    {
        var html=new StringBuilder();string E(string text)=>WebUtility.HtmlEncode(text);
        foreach(var report in ReportOptions.Scopes(source))
        {
            var stats=new Stats(report.Closed,source.ExcludeWeekends);var calendar=new Stats(report.Closed);var weekdays=new Stats(report.Closed,true);
            html.Append("<section class='panel annual'><h2>"+(report.AnalysisYear.HasValue?"Jahresauswertung "+report.AnalysisYear:"Gesamtauswertung")+"</h2><p>Ergebnis nach Deal-Buchungsdatum; Haltezeit und Entry-Lots nach Schlussjahr. Jahreswechsel-Trades behalten ihre gesamte Haltedauer.</p><div class='details'>");
            void Metric(string label,string value)=>html.Append("<div class='metric'><small>"+E(label)+"</small><strong>"+E(value)+"</strong></div>");
            Metric("Abgeschlossene Trades",stats.Count.ToString("N0"));Metric("Gebuchtes Netto",DisplayFormat.Money(report.Deals.Sum(d=>d.Net),report));Metric("Trade-Netto der Schlussjahr-Kohorte",DisplayFormat.Money(stats.Net,report));Metric("Swap / Kommission",DisplayFormat.Money(report.Deals.Sum(d=>d.Swap),report)+" / "+DisplayFormat.Money(report.Deals.Sum(d=>d.Commission),report));
            Metric("Kalender-Haltezeit Ø / Max",Stats.Duration(calendar.Mean)+" / "+Stats.Duration(calendar.Max));Metric("Ohne Sa/So Ø / Max",Stats.Duration(weekdays.Mean)+" / "+Stats.Duration(weekdays.Max));Metric("Mittlere Haltezeit (50 %) / 95 %",Stats.Duration(stats.Median)+" / "+Stats.Duration(stats.Quantile(.95)));Metric("Gewinn / Verlust / Null",$"{stats.Wins} / {stats.Losses} / {stats.Count-stats.Wins-stats.Losses}");
            Metric("Profitfaktor",stats.ProfitFactor?.ToString("N3")??"nicht definiert");Metric("P90 / P99",Stats.Duration(stats.Quantile(.90))+" / "+Stats.Duration(stats.Quantile(.99)));Metric("Standardabweichung",Stats.Duration(stats.Std));Metric("Volumengewichtete Kalenderdauer",Stats.Duration(stats.Weighted));
            var dd=AccountCurves.EquityDrawdowns(report);if(dd.Count>0){var percent=dd.MaxBy(p=>p.Percent);var money=dd.MaxBy(p=>p.Money);Metric("Max. Equity-DD (%)",$"{percent.Percent:N2}% / {percent.Money:N2} · {percent.Time:dd.MM.yyyy HH:mm:ss}");Metric("Max. Equity-DD (Geld)",$"{money.Money:N2} / {money.Percent:N2}% · {money.Time:dd.MM.yyyy HH:mm:ss}");Metric("Max. gespeicherte Kontobelastung",report.EquityPoints.Max(p=>p.DepositLoad).ToString("N2")+"%");}
            var adjusted=SeriesAnalysis.Adjusted(report);if(adjusted.Time.HasValue){Metric("Um letzten Zyklus bereinigt",DisplayFormat.Money(adjusted.Profit,report));Metric("Gesamtgewinn am",adjusted.Time.Value.ToString("dd.MM.yyyy HH:mm:ss"));Metric("Testende-Zyklus separat",DisplayFormat.Money(adjusted.Excluded,report));}
            html.Append("</div><div class='chartgrid'>");
            foreach(var kind in new[]{ChartKind.Balance,ChartKind.Equity,ChartKind.Histogram,ChartKind.Ecdf,ChartKind.Boxplot,ChartKind.Scatter,ChartKind.Monthly,ChartKind.Hourly,ChartKind.Weekday,ChartKind.SeriesHeatmap,ChartKind.SeriesCurve,ChartKind.EntryHour,ChartKind.EntryWeekday,ChartKind.EntryMonth,ChartKind.ResultHour,ChartKind.ResultWeekday,ChartKind.ResultMonth})
            {
                if(kind==ChartKind.Equity&&report.EquityPoints.Count==0)continue;
                var canvas=new SvgCanvas();ChartPainter.Draw(canvas,kind,report,stats.Trades,new Palette(false),0,0,760,350);html.Append("<svg role='img' viewBox='0 0 760 350' xmlns='http://www.w3.org/2000/svg'>"+canvas.Content+"</svg>");
            }
            html.Append("</div><h3>10 ungünstigste Startzeitfenster</h3><table><tr><th>Startzeit</th><th>Lange / Starts</th><th>Anteil</th></tr>");
            foreach(var g in SeriesAnalysis.Regular(report).GroupBy(c=>new{Day=((int)c.Start.DayOfWeek+6)%7,Hour=c.Start.Hour}).Select(g=>new{g.Key.Day,g.Key.Hour,Total=g.Count(),Long=g.Count(c=>c.WeekdaySeconds>report.LongSeriesHours*3600)}).Where(g=>g.Long>0).OrderByDescending(g=>g.Long/(double)g.Total).ThenByDescending(g=>g.Total).Take(10))html.Append($"<tr><td>{new[]{"Montag","Dienstag","Mittwoch","Donnerstag","Freitag","Samstag","Sonntag"}[g.Day]} {g.Hour:00}:00–{g.Hour+1:00}:00</td><td>{g.Long} / {g.Total}</td><td>{100d*g.Long/g.Total:N1} % ({g.Long} von {g.Total})</td></tr>");
            html.Append("</table><p>Sortiert nach Anteil langer regulärer Zyklen. Testende-Zyklen separat; unter 20 Starts kleine Fallzahl.</p>");
            html.Append("<details><summary>Monatsergebnisse und Datenprüfung</summary><table><thead><tr><th>Monat</th><th>Deals</th><th>Netto</th><th>Kommission</th><th>Swap</th></tr></thead><tbody>");
            foreach(var month in report.Deals.GroupBy(d=>d.Time.ToString("yyyy-MM")).OrderBy(g=>g.Key))html.Append($"<tr><td>{month.Key}</td><td>{month.Count():N0}</td><td>{month.Sum(d=>d.Net):N2}</td><td>{month.Sum(d=>d.Commission):N2}</td><td>{month.Sum(d=>d.Swap):N2}</td></tr>");
            html.Append("</tbody></table><p>Separate Gebühren: "+(report.AvailableColumns.Contains("fee")?report.Deals.Sum(d=>d.Fee).ToString("N2"):"nicht exportiert")+". Equity-DD jährlich ab dem übernommenen Anfangswert. Broker-Zeitzone nicht bekannt.</p>");
            foreach(var warning in report.Warnings.Distinct())html.Append("<p class='muted'>"+E(warning)+"</p>");html.Append("</details></section>");
        }
        var original=source.Metadata.Where(m=>!new[]{"eingaben","inputs","parameters"}.Contains(Parser.Key(m.Key))).ToList();int columns=original.Count>40?3:2;
        html.Append("<section class='panel annual'><h2>Originalkennzahlen · Gesamttest</h2><div class='originalgrid' style='grid-template-columns:repeat("+columns+",minmax(0,1fr))'>");
        foreach(var item in original)html.Append("<div><small>"+E(item.Key)+"</small><strong>"+E(item.Value)+"</strong></div>");
        var parameters=source.InputParameters.Where(p=>p.Trim()!="=").ToList();columns=parameters.Count>70?3:2;
        html.Append("</div><h2>Strategieparameter · Gesamttest</h2><div class='originalgrid' style='grid-template-columns:repeat("+columns+",minmax(0,1fr))'>");
        foreach(var parameter in parameters){int split=parameter.IndexOf('=');string label=split>=0?parameter[..split]:parameter,value=split>=0?parameter[(split+1)..]:"";html.Append("<div><small>"+E(label)+"</small><strong>"+E(value)+"</strong></div>");}
        html.Append("</div></section>");return html.ToString();
    }
}
