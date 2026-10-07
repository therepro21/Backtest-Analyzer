namespace BacktestAnalyzer;

public static class SeriesPainter
{
    public static void Draw(ICanvas c,Report report,Palette p,double x,double y,double width,double height,bool curves=false)
    {
        var all=SeriesAnalysis.Selected(report);var longOnes=all.Where(s=>s.WeekdaySeconds>report.LongSeriesHours*3600).ToList();
        c.Rect(x,y,width,height,p.Surface);c.Text((curves?"Tageskurven":"Startzeit-Heatmap")+" · Serien > "+report.LongSeriesHours+" h ohne Sa/So",x+12,y+10,12,p.Ink,true);
        c.Text("Konto-Zyklus: erster Einstieg bis alle Positionen geschlossen · Broker-Zeit",x+12,y+30,8,p.Muted);
        if(all.Count==0){c.Text("Keine abgeschlossenen Konto-Zyklen vorhanden.",x+15,y+65,10,p.Muted);return;}
        var counts=new int[7,24];var totals=new int[7,24];int Day(DateTime t)=>((int)t.DayOfWeek+6)%7;
        foreach(var s in all)totals[Day(s.Start),s.Start.Hour]++;foreach(var s in longOnes)counts[Day(s.Start),s.Start.Hour]++;
        double Value(int d,int h)=>report.SeriesAsPercent?(totals[d,h]>0?100d*counts[d,h]/totals[d,h]:0):counts[d,h];
        double maximum=Math.Max(1,Enumerable.Range(0,7).SelectMany(d=>Enumerable.Range(0,24).Select(h=>Value(d,h))).Max());
        double left=x+48,top=y+65,plotWidth=width-65,plotHeight=height-120;var days=new[]{"Mo","Di","Mi","Do","Fr","Sa","So"};
        if(!curves)
        {
            double cellW=plotWidth/24,cellH=plotHeight/7;
            for(int day=0;day<7;day++){c.Text(days[day],x+12,top+cellH*day+cellH/2-5,9,p.Muted);for(int hour=0;hour<24;hour++){
                double v=Value(day,hour);string color=totals[day,hour]==0?p.Background:v==0?p.Surface:v/maximum>.66?p.Negative:v/maximum>.33?(p.Dark?"#AC783E":"#EDBB77"):(p.Dark?"#4D5E78":"#DCE8F5");c.Rect(left+hour*cellW+1,top+day*cellH+1,cellW-2,cellH-2,color);
                if(cellW>18&&totals[day,hour]>0)c.Text(report.SeriesAsPercent?v.ToString("0")+"%":v.ToString("0"),left+hour*cellW+3,top+day*cellH+cellH/2-4,7,v/maximum>.66?"#12283D":p.Ink);
            }}
            for(int hour=0;hour<24;hour+=2)c.Text(hour.ToString("00"),left+hour*cellW,top+plotHeight+8,8,p.Muted);
        }
        else
        {
            string[] colors={p.Positive,p.Negative,"#4481AB","#8B6C4A","#7598B3","#AC783E",p.Muted};
            for(int i=0;i<=4;i++){double yy=top+plotHeight-plotHeight*i/4;c.Line(left,yy,left+plotWidth,yy,p.Line);c.Text((maximum*i/4).ToString("0.#")+(report.SeriesAsPercent?"%":""),x+5,yy-4,8,p.Muted);}
            for(int day=0;day<7;day++){for(int h=1;h<24;h++)c.Line(left+plotWidth*(h-1)/23,top+plotHeight-plotHeight*Value(day,h-1)/maximum,left+plotWidth*h/23,top+plotHeight-plotHeight*Value(day,h)/maximum,colors[day],1.3);c.Text(days[day],left+day*plotWidth/7,y+height-25,9,colors[day],true);}
            for(int hour=0;hour<24;hour+=3)c.Text(hour.ToString("00"),left+plotWidth*hour/23,top+plotHeight+8,8,p.Muted);
        }
        c.Text($"{longOnes.Count:N0} lange / {all.Count:N0} abgeschlossene Serien · "+(report.SeriesAsPercent?"Anteil innerhalb desselben Startzeitfensters":"Anzahl langer Serien"),x+12,y+height-45,8,p.Muted);
    }
}
