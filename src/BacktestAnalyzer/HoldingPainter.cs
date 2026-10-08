namespace BacktestAnalyzer;
public static class HoldingPainter
{
    public static void Draw(ICanvas c,ChartKind kind,Report report,List<Trade> trades,Palette p,double x,double y,double width,double height)
    {
        var s=new Stats(trades,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute);c.Rect(x,y,width,height,p.Surface);
        string title=kind==ChartKind.Histogram?"Haltezeiten · Kurzzeit- und Langzeitdetail":kind==ChartKind.Ecdf?"Wie schnell werden Trades geschlossen?":"Haltezeiten · Gewinner und Verlierer";
        c.Text(title,x+12,y+10,12,p.Ink,true);
        c.Text(report.ExcludeWeekends?"Handelsdauer":"Kalenderdauer",x+12,y+29,7,p.Muted);c.Note(DisplayFormat.DurationBasis(report),x+width-30,y+29,p.Muted);
        if(s.Count==0)return;
        double top=y+73,bottom=y+height-61,left=x+62,right=x+width-18;
        if(kind==ChartKind.Histogram)
        {
            // Equal bins inside each separate panel; same full-cohort percentage denominator.
            double split=Math.Min(3600,Math.Max(60,s.Max)),gap=44,panel=(right-left-gap)/2;
            void Panel(double px,double from,double to,double bin,string name)
            {
                var subset=s.Trades.Where(a=>Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)>=from&&(from==0?Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)<=to:Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)>from&&Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)<=to)).ToList();
                int count=Math.Max(1,(int)Math.Ceiling((to-from)/bin));var wins=new int[count];var losses=new int[count];var zeros=new int[count];
                foreach(var a in subset){int i=Math.Min(count-1,(int)((Stats.HoldingSeconds(a,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)-from)/bin));if(a.Net>0)wins[i]++;else if(a.Net<0)losses[i]++;else zeros[i]++;}
                double factor=report.HoldingAsPercent?100d/s.Count:1;var axis=ChartAxis.Nice(0,Math.Max(.001,wins.Concat(losses).Concat(zeros).Max()*factor));
                c.Text(name,px,y+48,9,p.Ink,true);c.VerticalText(report.HoldingAsPercent?"Trades (%)":"Anzahl Trades",px-44,(top+bottom)/2,7,p.Muted);c.Text($"{subset.Count:N0} Trades · {100d*subset.Count/s.Count:N1} % aller Trades",px,bottom+38,7,p.Muted);
                foreach(double tick in axis.Ticks){double yy=bottom-(tick-axis.Low)/(axis.High-axis.Low)*(bottom-top);c.Line(px,yy,px+panel,yy,p.Line,.4);c.RightText(ChartAxis.Number(tick),px-6,yy-4,7,p.Muted);}
                double X(double seconds)=>from==0?px+panel*(seconds-from)/(to-from):px+panel*Math.Log(seconds/from)/Math.Log(to/from);
                void Curve(int[] values,string color){for(int i=0;i<count;i++){double xx=X(Math.Min(to,from+bin*(i+.5))),yy=bottom-values[i]*factor/axis.High*(bottom-top);if(i>0)c.Line(X(Math.Min(to,from+bin*(i-.5))),bottom-values[i-1]*factor/axis.High*(bottom-top),xx,yy,color,1.3);if(values[i]>0)c.Circle(xx,yy,1.4,color);}}
                int step=Math.Max(1,(int)Math.Ceiling(count/36d));for(int i=0;i<count;i+=step){double a=from+bin*i,b=Math.Min(to,from+bin*Math.Min(count,i+step));var group=subset.Where(t=>{double d=Stats.HoldingSeconds(t,report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute);return d>=a&&(b==to?d<=b:d<b);}).ToList();if(group.Count==0)continue;double xx=X(Math.Max(from,a)),next=X(b);c.Hover(xx,top,Math.Max(2,next-xx),bottom-top,ChartAxis.Duration(a)+" – "+ChartAxis.Duration(b)+$"\nTrades: {group.Count:N0} · Gewinner {group.Count(t=>t.Net>0):N0} / Verlierer {group.Count(t=>t.Net<0):N0} / Null {group.Count(t=>t.Net==0):N0}\nAnteil aller Trades: {100d*group.Count/s.Count:N2} %\nNetto: "+DisplayFormat.Money(group.Sum(t=>t.Net),report)+$"\nGehandelte Lots: {group.Sum(t=>t.Volume):N2}");}
                Curve(wins,p.Positive);Curve(losses,p.Negative);if(zeros.Any(n=>n>0))Curve(zeros,p.Muted);
                if(from==0){for(int i=0;i<=2;i++)c.Text(ChartAxis.Duration(from+(to-from)*i/2),px+panel*i/2-(i==2?35:0),bottom+8,7,p.Muted);}else{double last=-1000;foreach(double tick in new[]{3600d,21600,86400,604800,2592000}){if(tick<from||tick>to)continue;double xx=X(tick);if(xx-last<30)continue;c.Text(ChartAxis.Duration(tick),xx,bottom+8,7,p.Muted);last=xx;}}
                c.Note("Intervalle: "+ChartAxis.Duration(bin)+(from>0?" · logarithmische Zeitachse":""),px,bottom+23,p.Muted);
            }
            Panel(left,0,split,Math.Max(60,Math.Min(split,report.HoldingBinMinutes*60)),"Kurzzeit: bis "+ChartAxis.Duration(split));
            Panel(left+panel+gap,split,Math.Max(split+60,report.HoldingMaxHours>0?Math.Min(s.Max,report.HoldingMaxHours*3600):s.Max),3600,"Langzeit: über "+ChartAxis.Duration(split));
            c.Text(report.HoldingAsPercent?"Anteil aller Trades (%) · eigene Y-Skala je Detailansicht":"Anzahl Trades · eigene Y-Skala je Detailansicht",x+12,y+height-12,7,p.Muted);
            c.Text("Gewinn",x+width-140,y+29,7,p.Positive,true);c.Text("Verlust",x+width-70,y+29,7,p.Negative,true);return;
        }
        if(kind==ChartKind.Ecdf)
        {
            // Logarithmic duration preserves visibility from seconds to days; explicitly labelled.
            double max=Math.Max(60,s.Max),denom=Math.Log10(1+max/60);double X(double sec)=>left+(right-left)*sec/max;
            c.VerticalText("Geschlossene Trades (%)",x+12,(top+bottom)/2,7,p.Muted);foreach(double tick in new[]{0d,25,50,75,100}){double yy=bottom-(bottom-top)*tick/100;c.Line(left,yy,right,yy,p.Line,.4);c.RightText(tick.ToString("0")+" %",left-5,yy-4,8,p.Muted);}
            double prevX=left,prevY=bottom;for(int i=0;i<s.Count;i+=Math.Max(1,s.Count/1600)){double xx=X(s.Durations[i]),yy=bottom-(bottom-top)*(i+1)/s.Count;c.Line(prevX,prevY,xx,yy,p.Positive,1.2);prevX=xx;prevY=yy;}c.Line(prevX,prevY,right,top,p.Positive,1.2);
            var ticks=new[]{0d,300,1800,7200,43200,172800,604800,2592000}.Where(v=>v<=max).ToList();double lastX=-1000;foreach(var tick in ticks){double xx=X(tick);if(xx-lastX<43)continue;c.Text(ChartAxis.Duration(tick),xx-5,bottom+8,7,p.Muted);lastX=xx;}
            c.Text("Haltedauer",left,bottom+25,7,p.Muted);
            foreach(double q in new[]{.1,.25,.5,.75,.9,.95,.99}){double d=s.Quantile(q);c.Hover(X(d)-5,top,10,bottom-top,$"{q*100:0} % geschlossen nach {Stats.Duration(d)}\nTrades: {s.Count:N0}\n"+DisplayFormat.DurationBasis(report));}
            var summary=new[]{.5,.9,.95,.99}.Select(q=>$"{q*100:0} % innerhalb von {Stats.Duration(Math.Ceiling(s.Durations[Math.Clamp((int)Math.Ceiling(q*s.Count)-1,0,s.Count-1)]))}").ToArray();double fs=7;while(summary.Sum(v=>c.Measure(v,fs,true))+24>(width-24)&&fs>5)fs-=.1;double sx=x+12;foreach(var value in summary){c.Text(value,sx,y+height-25,fs,p.Ink,true);sx+=c.Measure(value,fs,true)+8;}c.Text("100 % innerhalb von "+Stats.Duration(s.Max),x+12,y+height-12,7,p.Ink,true);return;
        }
        // Clear comparison replaces ambiguous whiskers with explicit duration values.
        var groups=new[]{s,new Stats(s.Trades.Where(a=>a.Net>0),report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute),new Stats(s.Trades.Where(a=>a.Net<0),report.ExcludeWeekends,report.SaturdayTrading,report.SundayTrading,report.NightPauseMinutes,report.NightPauseStartMinute)};
        c.Text("Kennzahl",x+12,y+51,8,p.Muted);c.Text("Gesamt",x+width*.32,y+51,9,p.Ink,true);c.Text("Gewinner",x+width*.55,y+51,9,p.Positive,true);c.Text("Verlierer",x+width*.78,y+51,9,p.Negative,true);
        string[] labels={"Anzahl Trades","Kürzeste Haltezeit","Mittlere Haltezeit (50 %) ","90 % geschlossen nach","Längste Haltezeit"};
        for(int i=0;i<labels.Length;i++){double yy=y+74+i*19;c.Text(labels[i],x+12,yy,8,p.Muted);for(int g=0;g<3;g++){var a=groups[g];string value=a.Count==0?"—":i==0?a.Count.ToString("N0"):Stats.Duration(i==1?a.Min:i==2?a.Median:i==3?a.Quantile(.9):a.Max);c.Text(value,x+width*(g==0?.32:g==1?.55:.78),yy,8,p.Ink,true);if(i==4){var detail=EventDetails.Longest(report,a.Trades,true);c.Hover(x+width*(g==0?.32:g==1?.55:.78),yy,width*.21,18,detail.Period+"\n"+detail.Prices);}}}
    }
}
