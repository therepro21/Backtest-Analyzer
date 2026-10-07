namespace BacktestAnalyzer;
public static class OriginalPainter
{
    public static void Draw(ICanvas c,Report r,Palette p,double x,double y,double width,double height,ChartKind kind)
    {
        bool hour=kind is ChartKind.EntryHour or ChartKind.ResultHour,month=kind is ChartKind.EntryMonth or ChartKind.ResultMonth,result=kind is ChartKind.ResultHour or ChartKind.ResultMonth or ChartKind.ResultWeekday;
        var slots=month?Enumerable.Range(1,12).ToArray():hour?Enumerable.Range(0,24).ToArray():ReportOptions.Days(r);
        int Key(DateTime t)=>month?t.Month:hour?t.Hour:((int)t.DayOfWeek+6)%7;
        var entries=r.Deals.Where(d=>d.Entry is "in" or "inout").GroupBy(d=>d.Order).Select(g=>g.First()).ToList();
        var closing=new Dictionary<DateTime,DateTime>();var times=r.Closed.Select(t=>t.Close!.Value).Distinct().Order().ToArray();for(int first=0;first<times.Length;){int last=first;while(last+1<times.Length&&(times[last+1]-times[first]).TotalSeconds<=r.BalanceWindowSeconds)last++;for(int i=first;i<=last;i++)closing[times[i]]=times[last];first=last+1;}
        var pos=slots.Select(i=>result?(double)r.Closed.Where(t=>Key(closing[t.Close!.Value])==i&&t.Net>0).Sum(t=>t.Net):entries.Count(d=>Key(d.Time)==i)).ToArray();
        var neg=slots.Select(i=>result?(double)r.Closed.Where(t=>Key(closing[t.Close!.Value])==i&&t.Net<0).Sum(t=>t.Net):0).ToArray();
        var axis=ChartAxis.Nice(0,Math.Max(1,Math.Max(pos.Max(),-neg.Min())));
        c.Rect(x,y,width,height,p.Surface);c.Text((result?"Gewinne und Verluste":"Ordereröffnungen")+" nach "+(month?"Monat":hour?"Tageszeit":"Wochentag"),x+12,y+10,12,p.Ink,true);
        c.Text(result?"Trade-Ergebnisse nach Schließzeit (Buchungsfenster) · "+DisplayFormat.Currency(r):"Anzahl unterschiedlicher eröffnender Orders · Broker-Zeit",x+12,y+30,7,p.Muted);
        double left=x+80,right=x+width-20,top=y+58,bottom=y+height-55,w=right-left,h=bottom-top,bw=w/slots.Length;
        c.VerticalText(result?"Gewinn-/Verlustbetrag ("+DisplayFormat.Currency(r)+")":"Anzahl Orders",x+12,(top+bottom)/2,7,p.Muted);double Y(double v)=>bottom-(v-axis.Low)/(axis.High-axis.Low)*h;foreach(var tick in axis.Ticks){double yy=Y(tick);c.Line(left,yy,right,yy,p.Line);c.Text(ChartAxis.Number(tick)+(result?" "+DisplayFormat.Currency(r):""),x+29,yy-4,8,p.Muted);}
        string[] days={"Mo","Di","Mi","Do","Fr","Sa","So"};
        for(int k=0;k<slots.Length;k++){double xx=left+k*bw+2;if(result){double gain=pos[k],loss=-neg[k],big=Math.Max(gain,loss),small=Math.Min(gain,loss);string fg=gain>=loss?p.Negative:p.Positive;c.Rect(xx,Y(big),bw-4,Y(0)-Y(big),gain>=loss?p.Positive:p.Negative);for(double d=0;d<bw-4;d+=5)c.Line(xx+d,Y(small),xx+Math.Min(bw-4,d+3),Y(small),fg,1.2);for(double d=Y(small);d<Y(0);d+=5){c.Line(xx,d,xx,Math.Min(Y(0),d+3),fg,1.2);c.Line(xx+bw-4,d,xx+bw-4,Math.Min(Y(0),d+3),fg,1.2);}c.Hover(xx,top,bw-4,h,"Gewinn: "+DisplayFormat.Money((decimal)gain,r)+"\nVerlust: "+DisplayFormat.Money((decimal)-loss,r)+"\nNetto: "+DisplayFormat.Money((decimal)(gain-loss),r));}else c.Rect(xx,Y(pos[k]),bw-4,Math.Max(0,Y(0)-Y(pos[k])),p.Positive);
        c.Text(month?new DateTime(2000,slots[k],1).ToString("MMM",System.Globalization.CultureInfo.GetCultureInfo("de-AT")):hour?(Localization.English?new DateTime(2000,1,1,slots[k],0,0).ToString("htt",Localization.Culture):slots[k].ToString("00")):days[slots[k]],xx,bottom+9,hour?5.5:8,p.Muted);}
        c.Text("Schließzeit · Blau: Gewinn, Orange: Verlust · Betrag nach oben · Testende enthalten.",x+12,y+height-15,6.5,p.Muted);
    }
}
