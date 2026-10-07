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
    double Measure(string text,double size,bool bold=false)=>text.Length*size*.5;
    void RightText(string text,double right,double y,double size,string color,bool bold=false)=>Text(text,right-Measure(text,size,bold),y,size,color,bold);
    void Hover(double x,double y,double w,double h,string text){}
    void Rect(double x,double y,double w,double h,string fill);void Line(double x1,double y1,double x2,double y2,string color,double width=1);
    void Text(string text,double x,double y,double size,string color,bool bold=false);void VerticalText(string text,double x,double y,double size,string color);void Circle(double x,double y,double radius,string color,bool hollow=false);
}
public enum ChartKind { Histogram, Ecdf, Scatter, Balance, Equity, Drawdown, Monthly, Hourly, Weekday, Boxplot,SeriesHeatmap,SeriesCurve,EntryWins,EntryLosses,EntryLossRate,EntryHour,EntryWeekday,EntryMonth,ResultHour,ResultWeekday,ResultMonth }
public static class ChartPainter
{
    public static string Compact(double n)=>Math.Abs(n)>=1e9?(n/1e9).ToString("0.##",CultureInfo.InvariantCulture)+"B":Math.Abs(n)>=1e6?(n/1e6).ToString("0.##",CultureInfo.InvariantCulture)+"M":Math.Abs(n)>=1000?(n/1000).ToString("0.##",CultureInfo.InvariantCulture)+"K":n.ToString("0.##",CultureInfo.InvariantCulture);
    public static void Draw(ICanvas c,ChartKind kind,Report report,List<Trade> trades,Palette p,double x,double y,double width,double height)
    {
        if(kind is ChartKind.Hourly or ChartKind.Weekday){OriginalPainter.Draw(c,report,p,x,y,width,height,kind==ChartKind.Hourly?ChartKind.ResultHour:ChartKind.ResultWeekday);return;}
        if(kind is ChartKind.EntryHour or ChartKind.EntryWeekday or ChartKind.EntryMonth or ChartKind.ResultHour or ChartKind.ResultWeekday or ChartKind.ResultMonth){OriginalPainter.Draw(c,report,p,x,y,width,height,kind);return;}
        if(kind is ChartKind.Histogram or ChartKind.Ecdf or ChartKind.Boxplot){HoldingPainter.Draw(c,kind,report,trades,p,x,y,width,height);return;}
        if(kind is ChartKind.EntryWins or ChartKind.EntryLosses or ChartKind.EntryLossRate){EntryPainter.Draw(c,report,p,x,y,width,height,kind==ChartKind.EntryWins?0:kind==ChartKind.EntryLosses?1:2);return;}
        if(kind is ChartKind.SeriesHeatmap or ChartKind.SeriesCurve){SeriesPainter.Draw(c,report,p,x,y,width,height,kind==ChartKind.SeriesCurve);return;}
        if(kind is ChartKind.Balance or ChartKind.Equity){AccountPainter.Draw(c,report,p,x,y,width,height,kind==ChartKind.Equity);return;}
        c.Rect(x,y,width,height,p.Surface);var s=new Stats(trades,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute);
        var title=kind switch { ChartKind.Histogram=>"Haltezeitverteilung",ChartKind.Ecdf=>"Geschlossen nach ... (kumulativer Anteil)",ChartKind.Scatter=>"Haltezeit vs. Nettoergebnis",ChartKind.Balance=>"Balance (exportierte Kontostände)",ChartKind.Drawdown=>"Balance-Drawdown (keine Equity-Kurve)",ChartKind.Monthly=>"Monatsergebnis (gebuchte Deals · gesamte Auswahlperiode)",ChartKind.Hourly=>"Zyklusergebnis nach Startstunde",ChartKind.Weekday=>"Zyklusergebnis nach Startwochentag",_=>"Haltezeiten: Gewinner / Verlierer"};
        c.Text(title,x+12,y+10,13,p.Ink,true);
        if(kind is ChartKind.Histogram or ChartKind.Ecdf or ChartKind.Boxplot or ChartKind.Scatter)c.Text(report.ExcludeWeekends?"Bereinigte Handelsdauer":"Kalender-Haltezeit inklusive Wochenenden",x+12,y+height-13,7,p.Muted);
        double l=x+82,r=x+width-20,t=y+56,b=y+height-69,w=r-l,h=b-t;
        if(s.Count==0&&kind is not (ChartKind.Balance or ChartKind.Drawdown)){c.Text("Keine abgeschlossenen Trades für diese Auswahl.",x+15,y+60,11,p.Muted);return;}
        void Axes(string yl,string xl,double min,double max){c.VerticalText(yl,x+13,(t+b)/2,7,p.Muted);var axis=ChartAxis.Nice(min,max);foreach(var value in axis.Ticks){double yy=b-h*(value-min)/(max-min);if(yy<t-1||yy>b+1)continue;c.Line(l,yy,r,yy,p.Line,.45);c.RightText(ChartAxis.Number(value)+(yl.Contains("Netto")||yl.Contains("Kontowährung")?" "+DisplayFormat.Currency(report):""),l-5,yy-4,8,p.Muted);}c.Line(l,b,r,b,p.Line);c.Text(xl,l,b+45,7,p.Muted);}
        void Legend(){c.Rect(r-178,y+31,8,8,p.Positive);c.Text("Gewinn (+)",r-165,y+29,9,p.Muted);c.Rect(r-83,y+31,8,8,p.Negative);c.Text("Verlust (-)",r-70,y+29,9,p.Muted);}
        if(kind==ChartKind.Histogram)
        {
            double bin=report.HoldingBinMinutes*60,limit=report.HoldingMaxHours>0?report.HoldingMaxHours*3600:Math.Max(bin,s.Max+1);int count=Math.Max(1,(int)Math.Ceiling(limit/bin));
            var wins=new int[count];var losses=new int[count];var zeros=new int[count];foreach(var a in s.Trades){int i=(int)(Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)/bin);if(i>=count)continue;if(a.Net>0)wins[i]++;else if(a.Net<0)losses[i]++;else zeros[i]++;}
            double factor=report.HoldingAsPercent?100.0/s.Count:1,max=Math.Max(.01,Enumerable.Range(0,count).Max(i=>Math.Max(wins[i],Math.Max(losses[i],zeros[i])))*factor);max*=1.1;
            Axes(report.HoldingAsPercent?"% aller ausgewählten Entry-Lots":"Anzahl Entry-Lots","Haltedauer (h) · gleiche Intervalle: "+report.HoldingBinMinutes+" min",0,max);Legend();
            void Curve(int[] values,string color){for(int i=1;i<count;i++)c.Line(l+w*(i-.5)/count,b-h*values[i-1]*factor/max,l+w*(i+.5)/count,b-h*values[i]*factor/max,color,.85);if(count==1)c.Circle(l+w/2,b-h*values[0]*factor/max,3,color);}
            Curve(wins,p.Positive);Curve(losses,p.Negative);if(zeros.Any(n=>n>0))Curve(zeros,p.Muted);
            for(int i=0;i<=4;i++)c.Text((limit/3600*i/4).ToString("0.##"),l+w*i/4-8,b+7,9,p.Muted);return;
        }
        if(kind==ChartKind.Boxplot)
        {
            double max=Math.Max(1,s.Max/3600);Axes("Haltezeit (h)","Minimum / Q1 / Median / Q3 / Maximum",0,max);
            var groups=new[]{s.Trades.Where(a=>a.Net>0),s.Trades.Where(a=>a.Net<0)};for(int i=0;i<2;i++){var g=new Stats(groups[i],report.ExcludeWeekends);double xx=l+w*(i==0?.3:.7);string color=i==0?p.Positive:p.Negative;c.Text((i==0?"Gewinn":"Verlust")+" · n="+g.Count,xx-45,b+7,10,p.Muted);if(g.Count==0)continue;double Y(double sec)=>b-h*(sec/3600)/max;c.Line(xx,Y(g.Min),xx,Y(g.Max),color,2);c.Line(xx-12,Y(g.Min),xx+12,Y(g.Min),color);c.Line(xx-12,Y(g.Max),xx+12,Y(g.Max),color);c.Rect(xx-22,Y(g.Quantile(.75)),44,Math.Max(1,Y(g.Quantile(.25))-Y(g.Quantile(.75))),color);c.Line(xx-22,Y(g.Median),xx+22,Y(g.Median),p.Surface,2);}return;
        }
        if(kind==ChartKind.Ecdf)
        {
            double max=Math.Max(1,s.Max/3600);Axes("Geschlossene Entry-Lots (%)","Haltezeit (h)",0,100);double px=l,py=b;for(int i=0;i<s.Count;i+=Math.Max(1,s.Count/1500)){double xx=l+w*(s.Durations[i]/3600)/max;double yy=b-h*(i+1)/s.Count;c.Line(px,py,xx,py,p.Positive,1.7);c.Line(xx,py,xx,yy,p.Positive,1.7);px=xx;py=yy;}for(int i=0;i<=4;i++)c.Text(Compact(max*i/4),l+w*i/4-8,b+7,9,p.Muted);return;
        }
        if(kind==ChartKind.Scatter)
        {
            double maxX=Math.Max(.01,s.Max/3600),min=Math.Min(0,(double)s.Trades.Min(a=>a.Net)),max=Math.Max(0,(double)s.Trades.Max(a=>a.Net));if(max==min)max=min+1;var sa=ChartAxis.Nice(min,max);min=sa.Low;max=sa.High;Axes("Netto (Kontowährung)","Haltezeit (h) · hohle Punkte: FIFO-Schätzung",min,max);Legend();
            int hoverIndex=0;foreach(var a in s.Trades.Where((_,i)=>i%Math.Max(1,s.Count/600)==0)){double xx=l+w*Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)/3600/maxX,yy=b-h*((double)a.Net-min)/(max-min);if(c is not PdfCanvas||hoverIndex++%12==0)c.Hover(xx-3,yy-3,6,6,a.Id+" · "+a.Symbol+"\n"+Stats.Duration(Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute))+"\nNetto: "+DisplayFormat.Money(a.Net,report));c.Circle(l+w*Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)/3600/maxX,b-h*((double)a.Net-min)/(max-min),1.5,a.Net>=0?p.Positive:p.Negative,a.Estimated);}
            for(int i=0;i<=4;i++)c.Text(ChartAxis.Duration(maxX*3600*i/4),l+w*i/4-8,b+7,8,p.Muted);return;
        }
        if(kind is ChartKind.Balance or ChartKind.Drawdown)
        {
            var series=report.Balances.OrderBy(a=>a.Time).ToArray();if(series.Length<2){c.Text("Keine Balance-Zeitreihe exportiert.",x+15,y+60,11,p.Muted);return;}
            var full=new List<(DateTime Time,double Balance,double Percent,double Money)>();double peak=Math.Max((double)report.InitialDeposit,(double)series[0].Balance);
            foreach(var a in series){double val=(double)a.Balance;peak=Math.Max(peak,val);full.Add((a.Time,val,peak>0?(val-peak)/peak*100:0,val-peak));}
            // Render envelope extrema instead of drawing every deal. Calculate DD BEFORE sampling.
            int stride=Math.Max(1,full.Count/Math.Max(100,(int)w));var keep=new SortedSet<int>{0,full.Count-1};
            for(int i=0;i<full.Count;i+=stride){int end=Math.Min(full.Count,i+stride);var idx=Enumerable.Range(i,end-i).ToArray();keep.Add(idx.MinBy(j=>full[j].Balance));keep.Add(idx.MaxBy(j=>full[j].Balance));keep.Add(idx.MinBy(j=>full[j].Percent));}
            var points=keep.Select(i=>full[i]).ToArray();double seconds=Math.Max(1,( (report.AxisEnd??full[^1].Time)-(report.AxisStart??full[0].Time)).TotalSeconds);DateTime axisStart=report.AxisStart??full[0].Time;double X(DateTime dt)=>l+w*(dt-axisStart).TotalSeconds/seconds;
            if(kind==ChartKind.Drawdown){double ddLow=full.Min(a=>a.Money);Axes("Drawdown (Kontowährung)","Broker-Zeit",ddLow,0);for(int i=1;i<points.Length;i++)c.Line(X(points[i-1].Time),b-h*(points[i-1].Money-ddLow)/Math.Max(1,-ddLow),X(points[i].Time),b-h*(points[i].Money-ddLow)/Math.Max(1,-ddLow),p.Negative,1.5);return;}
            l=x+100;w=r-l;double split=t+h*.66;double topH=split-t-15,ddTop=split+12,ddH=b-ddTop;
            double min=Math.Floor(full.Min(a=>a.Balance)/10000)*10000,max=Math.Ceiling(full.Max(a=>a.Balance)/10000)*10000;if(max<=min)max=min+1;
            string currency=report.Find("Währung","Currency");c.Text("Balance / Drawdown (%)",x+12,y+31,9,p.Muted);
            for(int i=0;i<=4;i++){double yy=t+topH-topH*i/4;c.Line(l,yy,r,yy,p.Line,.45);c.Text((min+(max-min)*i/4).ToString("N0")+" "+currency,x+7,yy-5,9,p.Muted);}
            double Y(double balance)=>t+topH-topH*(balance-min)/(max-min);
            double ddScale=Math.Max(10,Math.Ceiling(-full.Min(a=>a.Percent)/10)*10);
            for(int i=1;i<points.Length;i++)
            {
                double start=X(points[i-1].Time),end=X(points[i].Time),span=Math.Max(.01,end-start);
                for(double xx=start;xx<=end;xx+=1){double f=(xx-start)/span,bal=points[i-1].Balance+(points[i].Balance-points[i-1].Balance)*f,dd=points[i-1].Percent+(points[i].Percent-points[i-1].Percent)*f;c.Line(xx,Y(bal),xx,t+topH,p.Dark?"#20354F":"#E7EFF7",1.2);c.Line(xx,ddTop,xx,ddTop-ddH*dd/ddScale,p.Dark?"#493724":"#FBEBD8",1.2);}
            }
            for(int i=1;i<points.Length;i++){c.Line(X(points[i-1].Time),Y(points[i-1].Balance),X(points[i].Time),Y(points[i].Balance),p.Positive,1.4);c.Line(X(points[i-1].Time),ddTop-ddH*points[i-1].Percent/ddScale,X(points[i].Time),ddTop-ddH*points[i].Percent/ddScale,p.Negative,1.2);}
            for(int i=0;i<=3;i++){double yy=ddTop+ddH*i/3;c.Line(l,yy,r,yy,p.Line,.45);c.Text((-ddScale*i/3).ToString("0.0")+"%",x+40,yy-5,9,p.Muted);}
            if(report.AnalysisYear.HasValue){for(int i=0;i<12;i++){var month=axisStart.AddMonths(i);double xx=X(month);c.Line(xx,t,xx,b,p.Line);c.Text(month.ToString("MMM"),xx+2,b+8,8,p.Muted);}}
            else {var month=new DateTime(axisStart.Year,axisStart.Month,1).AddMonths(1);int months=(full[^1].Time.Year-axisStart.Year)*12+full[^1].Time.Month-axisStart.Month+1;for(int i=0;month<=full[^1].Time;month=month.AddMonths(1),i++){double xx=X(month);c.Line(xx,t,xx,b,p.Line);if(i%Math.Max(1,(int)Math.Ceiling(months/8d))==0)c.Text(month.ToString("MM.yy"),xx-12,b+8,8,p.Muted);}}
            return;
        }
        List<(string Label,double Value)> categories;
        if(kind==ChartKind.Monthly){var grouped=report.Deals.GroupBy(a=>a.Time.ToString("yyyy-MM")).ToDictionary(a=>a.Key,a=>(double)a.Sum(z=>z.Net));var start=report.AxisStart??new DateTime(report.Deals.Min(d=>d.Time).Year,report.Deals.Min(d=>d.Time).Month,1);var end=report.AxisEnd??new DateTime(report.Deals.Max(d=>d.Time).Year,report.Deals.Max(d=>d.Time).Month,1).AddMonths(1);categories=new();for(var month=start;month<end;month=month.AddMonths(1))categories.Add((report.AnalysisYear.HasValue?month.ToString("MMM"):month.ToString("yy-MM"),grouped.GetValueOrDefault(month.ToString("yyyy-MM"))));}
        else if(kind==ChartKind.Hourly)categories=Enumerable.Range(0,24).Select(i=>(i.ToString("00"),(double)SeriesAnalysis.Regular(report).Where(a=>a.Start.Hour==i).Sum(a=>a.Net))).ToList();
        else categories=ReportOptions.Days(report).Select(i=>(new[]{"Mo","Di","Mi","Do","Fr","Sa","So"}[i],(double)SeriesAnalysis.Regular(report).Where(a=>((int)a.Start.DayOfWeek+6)%7==i).Sum(a=>a.Net))).ToList();
        double low=Math.Min(0,categories.Min(a=>a.Value)),high=Math.Max(0,categories.Max(a=>a.Value));if(low==high)high=low+1;var nice=ChartAxis.Nice(low,high);low=nice.Low;high=nice.High;Axes("Netto ("+report.Find("Währung","Currency")+")",kind==ChartKind.Monthly?"Buchungsmonat (Broker-Zeit)":kind==ChartKind.Hourly?"Startstunde (Broker-Zeit) · reguläre vollständige Zyklen":"Startwochentag · reguläre vollständige Zyklen",low,high);Legend();double bw=w/categories.Count;double baseline=b-h*(0-low)/(high-low);
        for(int i=0;i<categories.Count;i++){double yy=b-h*(categories[i].Value-low)/(high-low);bool unavailable=kind==ChartKind.Monthly&&report.AnalysisYear.HasValue&&report.AxisStart!.Value.AddMonths(i)>report.Balances.LastOrDefault().Time;if(unavailable)c.Text("n.v.",l+i*bw,baseline-14,8,p.Muted);else c.Rect(l+i*bw+1,Math.Min(yy,baseline),Math.Max(1,bw-3),Math.Max(1,Math.Abs(yy-baseline)),categories[i].Value>=0?p.Positive:p.Negative);int stride=categories.Count>18?Math.Max(1,categories.Count/6):1;if(i%stride==0){if(kind==ChartKind.Monthly){var date=(report.AxisStart??new DateTime(report.Deals.Min(d=>d.Time).Year,report.Deals.Min(d=>d.Time).Month,1)).AddMonths(i);c.Text(date.ToString("yyyy-MM"),l+i*bw,b+7,7,p.Muted);c.Text(date.ToString("MMMM",CultureInfo.GetCultureInfo("de-DE")),l+i*bw,b+19,7,p.Muted);}else c.Text(categories[i].Label,l+i*bw,b+7,8,p.Muted);}}
    }
}
public sealed class WpfCanvas(DrawingContext context,List<(Rect Box,string Text)>? hits=null):ICanvas
{
    public void Hover(double x,double y,double w,double h,string text){hits?.Add((new Rect(x,y,Math.Max(0,w),Math.Max(0,h)),Localization.T(text)));}
    public double Measure(string text,double size,bool bold=false)=>new FormattedText(Localization.T(text),Localization.Culture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,Brush("#000000"),1).Width;
    static Brush Brush(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    public void Rect(double x,double y,double w,double h,string fill){if(w>0&&h>0)context.DrawRectangle(Brush(fill),null,new Rect(x,y,w,h));}
    public void Line(double x1,double y1,double x2,double y2,string color,double width=1)=>context.DrawLine(new Pen(Brush(color),width*.65),new Point(x1,y1),new Point(x2,y2));
    public void Text(string text,double x,double y,double size,string color,bool bold=false)=>context.DrawText(new FormattedText(Localization.T(text),Localization.Culture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,Brush(color),1),new Point(x,y));
    public void VerticalText(string text,double x,double y,double size,string color){var label=new FormattedText(Localization.T(text),Localization.Culture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,Brush(color),1);context.PushTransform(new TranslateTransform(x,y));context.PushTransform(new RotateTransform(-90));context.DrawText(label,new Point(-label.Width/2,-label.Height/2));context.Pop();context.Pop();}
    public void Circle(double x,double y,double radius,string color,bool hollow=false)=>context.DrawEllipse(hollow?null:Brush(color),hollow?new Pen(Brush(color),1):null,new Point(x,y),radius,radius);
}
public sealed class ChartView:FrameworkElement
{
    public required Report Report {get;init;} public required List<Trade> Trades {get;init;}public required Palette Palette {get;init;}public ChartKind Kind {get;init;}
    readonly List<(Rect Box,string Text)> hits=new();string? shown;
    public ChartView(){MouseMove+=(_,e)=>{var at=e.GetPosition(this);string? text=hits.LastOrDefault(h=>h.Box.Contains(at)).Text;if(text!=shown){shown=text;ToolTip=text;System.Windows.Controls.ToolTipService.SetInitialShowDelay(this,80);System.Windows.Controls.ToolTipService.SetShowDuration(this,60000);}};}
    protected override void OnRender(DrawingContext dc){base.OnRender(dc);hits.Clear();if(ActualWidth>140&&ActualHeight>120)ChartPainter.Draw(new WpfCanvas(dc,hits),Kind,Report,Trades,Palette,0,0,ActualWidth,ActualHeight);}
}
