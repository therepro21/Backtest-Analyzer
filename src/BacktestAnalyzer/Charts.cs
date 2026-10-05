using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BacktestAnalyzer;
public record Palette(bool Dark)
{
    public string Background=>Dark?"#101B2B":"#F2F5F9";public string Surface=>Dark?"#19273B":"#FFFFFF";
    public string Ink=>Dark?"#EAF1FA":"#173047";public string Muted=>Dark?"#A9B9CE":"#52677D";
    public string Line=>Dark?"#3B4D64":"#DCE4ED";public string Positive=>Dark?"#76B7FF":"#113B65";
    public string Negative=>Dark?"#FFB348":"#C87500";
}
public interface ICanvas
{
    void Rect(double x,double y,double w,double h,string fill);void Line(double x1,double y1,double x2,double y2,string color,double width=1);
    void Text(string text,double x,double y,double size,string color,bool bold=false);void Circle(double x,double y,double radius,string color,bool hollow=false);
}
public enum ChartKind { Histogram, Ecdf, Scatter, Balance, Drawdown, Monthly, Hourly, Weekday, Boxplot }
public static class ChartPainter
{
    public static string Compact(double n)=>Math.Abs(n)>=1e9?(n/1e9).ToString("0.##",CultureInfo.InvariantCulture)+"B":Math.Abs(n)>=1e6?(n/1e6).ToString("0.##",CultureInfo.InvariantCulture)+"M":Math.Abs(n)>=1000?(n/1000).ToString("0.##",CultureInfo.InvariantCulture)+"K":n.ToString("0.##",CultureInfo.InvariantCulture);
    public static void Draw(ICanvas c,ChartKind kind,Report report,List<Trade> trades,Palette p,double x,double y,double width,double height)
    {
        c.Rect(x,y,width,height,p.Surface);var s=new Stats(trades);
        var title=kind switch { ChartKind.Histogram=>"Haltezeitverteilung",ChartKind.Ecdf=>"Geschlossen nach ... (kumulativer Anteil)",ChartKind.Scatter=>"Haltezeit vs. Nettoergebnis",ChartKind.Balance=>"Balance (exportierte Kontostände)",ChartKind.Drawdown=>"Balance-Drawdown (keine Equity-Kurve)",ChartKind.Monthly=>"Nettoergebnis nach Schließmonat",ChartKind.Hourly=>"Nettoergebnis nach Einstiegsstunde",ChartKind.Weekday=>"Nettoergebnis nach Einstiegswochentag",_=>"Haltezeiten: Gewinner / Verlierer"};
        c.Text(title,x+12,y+10,13,p.Ink,true);
        double l=x+58,r=x+width-20,t=y+56,b=y+height-43,w=r-l,h=b-t;
        if(s.Count==0&&kind is not (ChartKind.Balance or ChartKind.Drawdown)){c.Text("Keine abgeschlossenen Trades für diese Auswahl.",x+15,y+60,11,p.Muted);return;}
        void Axes(string yl,string xl,double min,double max){c.Text(yl,x+12,y+31,9,p.Muted);for(int i=0;i<=4;i++){double yy=b-h*i/4;c.Line(l,yy,r,yy,p.Line);c.Text(Compact(min+(max-min)*i/4),x+5,yy-5,9,p.Muted);}c.Line(l,b,r,b,p.Line);c.Text(xl,l,b+27,9,p.Muted);}
        void Legend(){c.Rect(r-178,y+31,8,8,p.Positive);c.Text("Gewinn (+)",r-165,y+29,9,p.Muted);c.Rect(r-83,y+31,8,8,p.Negative);c.Text("Verlust (-)",r-70,y+29,9,p.Muted);}
        if(kind==ChartKind.Histogram)
        {
            var wins=new int[Stats.Bins.Length];var losses=new int[wins.Length];var zeros=new int[wins.Length];foreach(var a in s.Trades){var i=s.Bin(a);if(a.Net>0)wins[i]++;else if(a.Net<0)losses[i]++;else zeros[i]++;}
            double max=Math.Max(1,Enumerable.Range(0,wins.Length).Max(i=>wins[i]+losses[i]+zeros[i]));Axes("Anzahl Entry-Lots","Haltezeit (Kalenderzeit)",0,max);Legend();double step=w/wins.Length;
            for(int i=0;i<wins.Length;i++){double xx=l+i*step+3;double zeroHeight=zeros[i]/max*h,lossHeight=losses[i]/max*h,winHeight=wins[i]/max*h;c.Rect(xx,b-winHeight,step-6,winHeight,p.Positive);c.Rect(xx,b-winHeight-lossHeight,step-6,lossHeight,p.Negative);c.Rect(xx,b-winHeight-lossHeight-zeroHeight,step-6,zeroHeight,p.Muted);c.Text(Stats.Bins[i],xx,b+7,Math.Min(9,step/5),p.Muted);}return;
        }
        if(kind==ChartKind.Boxplot)
        {
            double max=Math.Max(1,s.Max/3600);Axes("Haltezeit (h)","Minimum / Q1 / Median / Q3 / Maximum",0,max);
            var groups=new[]{s.Trades.Where(a=>a.Net>0),s.Trades.Where(a=>a.Net<0)};for(int i=0;i<2;i++){var g=new Stats(groups[i]);double xx=l+w*(i==0?.3:.7);string color=i==0?p.Positive:p.Negative;c.Text((i==0?"Gewinn":"Verlust")+" · n="+g.Count,xx-45,b+7,10,p.Muted);if(g.Count==0)continue;double Y(double sec)=>b-h*(sec/3600)/max;c.Line(xx,Y(g.Min),xx,Y(g.Max),color,2);c.Line(xx-12,Y(g.Min),xx+12,Y(g.Min),color);c.Line(xx-12,Y(g.Max),xx+12,Y(g.Max),color);c.Rect(xx-22,Y(g.Quantile(.75)),44,Math.Max(1,Y(g.Quantile(.25))-Y(g.Quantile(.75))),color);c.Line(xx-22,Y(g.Median),xx+22,Y(g.Median),p.Surface,2);}return;
        }
        if(kind==ChartKind.Ecdf)
        {
            double max=Math.Max(1,s.Max/3600);Axes("Geschlossene Entry-Lots (%)","Haltezeit (h)",0,100);double px=l,py=b;for(int i=0;i<s.Count;i++){double xx=l+w*(s.Durations[i]/3600)/max;double yy=b-h*(i+1)/s.Count;c.Line(px,py,xx,py,p.Positive,1.7);c.Line(xx,py,xx,yy,p.Positive,1.7);px=xx;py=yy;}for(int i=0;i<=4;i++)c.Text(Compact(max*i/4),l+w*i/4-8,b+7,9,p.Muted);return;
        }
        if(kind==ChartKind.Scatter)
        {
            double maxX=Math.Max(.01,s.Max/3600),min=Math.Min(0,(double)s.Trades.Min(a=>a.Net)),max=Math.Max(0,(double)s.Trades.Max(a=>a.Net));if(max==min)max=min+1;double range=max-min;min-=range*.05;max+=range*.05;Axes("Netto (Kontowährung)","Haltezeit (h) · hohle Punkte: FIFO-Schätzung",min,max);Legend();
            foreach(var a in s.Trades)c.Circle(l+w*a.Seconds/3600/maxX,b-h*((double)a.Net-min)/(max-min),2.3,a.Net>=0?p.Positive:p.Negative,a.Estimated);
            for(int i=0;i<=4;i++)c.Text(Compact(maxX*i/4),l+w*i/4-8,b+7,9,p.Muted);return;
        }
        if(kind is ChartKind.Balance or ChartKind.Drawdown)
        {
            var series=report.Balances.OrderBy(a=>a.Time).ToList();if(series.Count<2){c.Text("Keine geeignete Balance-Zeitreihe exportiert.",x+15,y+60,11,p.Muted);return;}
            double peak=(double)series[0].Balance;var points=series.Select(a=>{double val=(double)a.Balance;peak=Math.Max(peak,val);return (a.Time,Value:kind==ChartKind.Drawdown?val-peak:val);}).ToArray();double min=points.Min(a=>a.Value),max=points.Max(a=>a.Value);if(min==max)max=min+1;Axes("Kontowährung",series[0].Time.ToString("yyyy-MM-dd")+"  bis  "+series[^1].Time.ToString("yyyy-MM-dd"),min,max);
            double seconds=Math.Max(1,(points[^1].Time-points[0].Time).TotalSeconds);for(int i=1;i<points.Length;i++)c.Line(l+w*(points[i-1].Time-points[0].Time).TotalSeconds/seconds,b-h*(points[i-1].Value-min)/(max-min),l+w*(points[i].Time-points[0].Time).TotalSeconds/seconds,b-h*(points[i].Value-min)/(max-min),kind==ChartKind.Drawdown?p.Negative:p.Positive,1.5);return;
        }
        List<(string Label,double Value)> categories;
        if(kind==ChartKind.Monthly)categories=s.Trades.GroupBy(a=>a.Close!.Value.ToString("yyyy-MM")).OrderBy(a=>a.Key).Select(a=>(a.Key,(double)a.Sum(z=>z.Net))).ToList();
        else if(kind==ChartKind.Hourly)categories=Enumerable.Range(0,24).Select(i=>(i.ToString("00"),(double)s.Trades.Where(a=>a.Open.Hour==i).Sum(a=>a.Net))).ToList();
        else categories=Enumerable.Range(0,7).Select(i=>(new[]{"Mo","Di","Mi","Do","Fr","Sa","So"}[i],(double)s.Trades.Where(a=>((int)a.Open.DayOfWeek+6)%7==i).Sum(a=>a.Net))).ToList();
        double low=Math.Min(0,categories.Min(a=>a.Value)),high=Math.Max(0,categories.Max(a=>a.Value));if(low==high)high=low+1;Axes("Netto (Kontowährung)",kind==ChartKind.Monthly?"Schließmonat":kind==ChartKind.Hourly?"Einstiegsstunde (Broker-Zeit)":"Einstiegswochentag",low,high);Legend();double bw=w/categories.Count;double baseline=b-h*(0-low)/(high-low);
        for(int i=0;i<categories.Count;i++){double yy=b-h*(categories[i].Value-low)/(high-low);c.Rect(l+i*bw+1,Math.Min(yy,baseline),Math.Max(1,bw-3),Math.Max(1,Math.Abs(yy-baseline)),categories[i].Value>=0?p.Positive:p.Negative);int stride=categories.Count>18?Math.Max(1,categories.Count/6):1;if(i%stride==0)c.Text(categories[i].Label,l+i*bw,b+7,8,p.Muted);}
    }
}
public sealed class WpfCanvas(DrawingContext context):ICanvas
{
    static Brush Brush(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    public void Rect(double x,double y,double w,double h,string fill){if(w>0&&h>0)context.DrawRectangle(Brush(fill),null,new Rect(x,y,w,h));}
    public void Line(double x1,double y1,double x2,double y2,string color,double width=1)=>context.DrawLine(new Pen(Brush(color),width),new Point(x1,y1),new Point(x2,y2));
    public void Text(string text,double x,double y,double size,string color,bool bold=false)=>context.DrawText(new FormattedText(text,CultureInfo.GetCultureInfo("de-DE"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,Brush(color),1),new Point(x,y));
    public void Circle(double x,double y,double radius,string color,bool hollow=false)=>context.DrawEllipse(hollow?null:Brush(color),hollow?new Pen(Brush(color),1):null,new Point(x,y),radius,radius);
}
public sealed class ChartView:FrameworkElement
{
    public required Report Report {get;init;} public required List<Trade> Trades {get;init;}public required Palette Palette {get;init;}public ChartKind Kind {get;init;}
    protected override void OnRender(DrawingContext dc){base.OnRender(dc);if(ActualWidth>140&&ActualHeight>120)ChartPainter.Draw(new WpfCanvas(dc),Kind,Report,Trades,Palette,0,0,ActualWidth,ActualHeight);}
}
