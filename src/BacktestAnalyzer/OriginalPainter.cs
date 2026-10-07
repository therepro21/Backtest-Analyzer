namespace BacktestAnalyzer;
public static class OriginalPainter
{
    public static void Draw(ICanvas c,Report r,Palette p,double x,double y,double width,double height,ChartKind kind)
    {
        bool hour=kind is ChartKind.EntryHour or ChartKind.ResultHour,month=kind is ChartKind.EntryMonth or ChartKind.ResultMonth,result=kind is ChartKind.ResultHour or ChartKind.ResultMonth or ChartKind.ResultWeekday;
        var slots=month?Enumerable.Range(1,12).ToArray():hour?Enumerable.Range(0,24).ToArray():ReportOptions.Days(r);
        int Key(DateTime t)=>month?t.Month:hour?t.Hour:((int)t.DayOfWeek+6)%7;
        var entries=r.Deals.Where(d=>d.Entry is "in" or "inout").GroupBy(d=>d.Order).Select(g=>g.First()).ToList();
        var pos=slots.Select(i=>result?(double)r.Closed.Where(t=>Key(t.Open)==i&&t.Net>0).Sum(t=>t.Net):entries.Count(d=>Key(d.Time)==i)).ToArray();
        var neg=slots.Select(i=>result?(double)r.Closed.Where(t=>Key(t.Open)==i&&t.Net<0).Sum(t=>t.Net):0).ToArray();
        var axis=ChartAxis.Nice(Math.Min(0,neg.Min()),Math.Max(1,pos.Max()));
        c.Rect(x,y,width,height,p.Surface);c.Text((result?"Gewinne und Verluste":"Ordereröffnungen")+" nach "+(month?"Monat":hour?"Tageszeit":"Wochentag"),x+12,y+10,12,p.Ink,true);
        c.Text(result?"Trade-Netto nach Einstiegszeit · "+r.Find("Währung","Currency"):"Anzahl unterschiedlicher eröffnender Orders · Broker-Zeit",x+12,y+30,7,p.Muted);
        double left=x+80,right=x+width-20,top=y+58,bottom=y+height-55,w=right-left,h=bottom-top,bw=w/slots.Length;
        c.VerticalText(result?"Netto ("+r.Find("Währung","Currency")+")":"Anzahl Orders",x+12,(top+bottom)/2,7,p.Muted);double Y(double v)=>bottom-(v-axis.Low)/(axis.High-axis.Low)*h;foreach(var tick in axis.Ticks){double yy=Y(tick);c.Line(left,yy,right,yy,p.Line);c.Text(ChartAxis.Number(tick),x+29,yy-4,8,p.Muted);}
        string[] days={"Mo","Di","Mi","Do","Fr","Sa","So"};
        for(int k=0;k<slots.Length;k++){double xx=left+k*bw+2;if(result){c.Rect(xx,Math.Min(Y(pos[k]),Y(0)),bw*.43,Math.Max(0,Math.Abs(Y(pos[k])-Y(0))),p.Positive);c.Rect(xx+bw*.45,Math.Min(Y(neg[k]),Y(0)),bw*.43,Math.Max(0,Math.Abs(Y(neg[k])-Y(0))),p.Negative);}else c.Rect(xx,Y(pos[k]),bw-4,Math.Max(0,Y(0)-Y(pos[k])),p.Positive);
        c.Text(month?new DateTime(2000,slots[k],1).ToString("MMM",System.Globalization.CultureInfo.GetCultureInfo("de-AT")):hour?slots[k].ToString("00"):days[slots[k]],xx,bottom+9,8,p.Muted);}
        c.Text("Originalbezogene Trade-/Orderansicht; Zyklusauswertung steht separat.",x+12,y+height-15,6.5,p.Muted);
    }
}
