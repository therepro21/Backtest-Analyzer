namespace BacktestAnalyzer;
public static class SeriesPainter
{
 public static void Draw(ICanvas c,Report report,Palette p,double x,double y,double width,double height,bool curves=false)
 {
  var completed=SeriesAnalysis.Selected(report);var all=SeriesAnalysis.Regular(report);var longs=all.Where(s=>ReportOptions.CycleSeconds(report,s)>report.LongSeriesHours*3600).ToList();
  c.Rect(x,y,width,height,p.Surface);c.Text((curves?"Tageskurven":"Startzeit-Heatmap")+" · Zyklen über "+report.LongSeriesHours+" h ohne Sa/So",x+12,y+10,11,p.Ink,true);
  c.Text($"{all.Count:N0} reguläre Zyklen · {longs.Count:N0} über {report.LongSeriesHours} h · "+(all.Count>0?$"{100d*longs.Count/all.Count:N1} %":"—"),x+12,y+30,8,p.Ink,true);
  var cutoff=report.AxisEnd??report.Deals.Max(d=>d.Time).AddTicks(1);int openCycles=SeriesAnalysis.ForReport(report).Count(c=>c.Start<cutoff&&(!c.End.HasValue||c.End.Value>=cutoff));
  c.Text($"{completed.Count:N0} abgeschlossen · {completed.Count-all.Count} Testende · {openCycles} offen am Abschnittsende · Broker-Zeit",x+12,y+45,7,p.Muted);
  if(all.Count==0)return;
  var counts=new int[7,24];var totals=new int[7,24];int Day(DateTime t)=>((int)t.DayOfWeek+6)%7;
  foreach(var s in all)totals[Day(s.Start),s.Start.Hour]++;foreach(var s in longs)counts[Day(s.Start),s.Start.Hour]++;
  double Value(int d,int h)=>report.SeriesAsPercent?(totals[d,h]>0?100d*counts[d,h]/totals[d,h]:0):counts[d,h];
  double maximum=Math.Max(1,Enumerable.Range(0,7).SelectMany(d=>Enumerable.Range(0,24).Select(h=>Value(d,h))).Max());
  double left=x+40,top=y+70,pw=width-55,ph=height-126;var days=new[]{"Mo","Di","Mi","Do","Fr","Sa","So"};var enabled=ReportOptions.Days(report);
  if(!curves){double cw=pw/24,ch=ph/enabled.Length;
   for(int row=0;row<enabled.Length;row++){int d=enabled[row];c.Text(days[d],x+9,top+ch*row+ch/2-4,8,p.Muted);for(int h=0;h<24;h++){
    double v=Value(d,h),f=v/maximum;string color=totals[d,h]==0?p.Background:v==0?(p.Dark?"#24384D":"#E5EFFA"):f>.75?p.Negative:f>.5?(p.Dark?"#CA9149":"#E49D40"):f>.25?(p.Dark?"#996F43":"#F0BD7A"):(p.Dark?"#5F5444":"#F9DFC0");c.Rect(left+h*cw+1,top+row*ch+1,cw-2,ch-2,color);
    if(cw>17)c.Text(totals[d,h]==0?"—":report.SeriesAsPercent?v.ToString("0")+"%":v.ToString("0"),left+h*cw+3,top+row*ch+ch/2-3,6.5,f>.5?"#15283B":p.Ink);
   }}for(int h=0;h<24;h+=2)c.Text(h.ToString("00"),left+h*cw,top+ph+7,7,p.Muted);
   c.Text(report.SeriesAsPercent?"Anteil langer Zyklen an allen Starts im jeweiligen Feld (%)":"Anzahl langer Zyklen je Wochentag und Startstunde",x+12,y+height-29,7,p.Muted);
   c.Text("Blau: 0 lange · Orange: zunehmend viele / hoher Anteil · —: keine Starts",x+12,y+height-16,6.5,p.Muted);
  }else{
   var axis=ChartAxis.Nice(0,maximum);string[] colors=p.Dark?new[]{"#67B7FF","#FFAC3D","#66D7A9","#CDA0FF","#FF7DA7","#FFE16A","#71DCE8"}:new[]{"#145DA0","#D46B00","#16804A","#8245B5","#BF2362","#9A7B00","#00828F"};
   c.VerticalText(report.SeriesAsPercent?"Anteil (%)":"Anzahl Zyklen",x+6,top+ph/2,7,p.Muted);foreach(var tick in axis.Ticks){double yy=top+ph-ph*tick/axis.High;c.Line(left,yy,left+pw,yy,p.Line);c.Text(ChartAxis.Number(tick)+(report.SeriesAsPercent?" %":""),x+20,yy-4,7,p.Muted);}
   for(int row=0;row<enabled.Length;row++){int d=enabled[row];for(int h=1;h<24;h++)c.Line(left+pw*(h-1)/23,top+ph-ph*Value(d,h-1)/axis.High,left+pw*h/23,top+ph-ph*Value(d,h)/axis.High,colors[d],1.3);for(int mark=0;mark<24;mark+=3)c.Circle(left+pw*mark/23,top+ph-ph*Value(d,mark)/axis.High,2.2,colors[d],d%2==1);c.Text(days[d],left+row*pw/enabled.Length,y+height-16,8,colors[d],true);}
   for(int h=0;h<24;h+=3)c.Text(h.ToString("00")+" Uhr",left+pw*h/23,top+ph+7,7,p.Muted);
  }
 }
}
