namespace BacktestAnalyzer;
public static class EntryPainter
{
 public static void Draw(ICanvas c,Report r,Palette p,double x,double y,double width,double height,int mode)
 {
  c.Rect(x,y,width,height,p.Surface);var trades=r.Closed;var days=ReportOptions.Days(r);string title=mode==0?"Einstiegszeit-Heatmap · Gewinner":mode==1?"Einstiegszeit-Heatmap · Verlierer":"Einstiegszeit-Heatmap · Verlustquote";
  c.Text(title,x+12,y+10,11,p.Ink,true);c.Text("Einzeltrades · Einstiegsstunde in Brokerzeit · Anzahl und Anteil je Zeitfenster",x+12,y+30,6,p.Muted);
  double left=x+42,top=y+56,cw=(width-58)/24,ch=(height-90)/days.Length;
  var counts=trades.GroupBy(t=>(((int)t.Open.DayOfWeek+6)%7,t.Open.Hour)).ToDictionary(g=>g.Key,g=>g.ToList());int max=Math.Max(1,counts.Values.Select(g=>mode==0?g.Count(t=>t.Net>0):g.Count(t=>t.Net<0)).DefaultIfEmpty(1).Max());
  for(int d=0;d<days.Length;d++){c.RightText(new[]{"Mo","Di","Mi","Do","Fr","Sa","So"}[days[d]],left-6,top+d*ch+ch/2-4,7,p.Muted);for(int h=0;h<24;h++){var list=counts.GetValueOrDefault((days[d],h))??new();int n=list.Count(t=>mode==0?t.Net>0:t.Net<0);double fraction=mode==2?(list.Count==0?0:n/(double)list.Count):n/(double)max;string color=fraction>.65?(mode==0?p.Positive:p.Negative):fraction>.25?(p.Dark?"#38546E":"#C7DCEE"):p.Dark?"#23364C":"#EAF1F8";double xx=left+h*cw,yy=top+d*ch;c.Rect(xx,yy,cw-1,ch-1,color);string value=list.Count==0?"—":mode==2?$"{100d*n/list.Count:0}%":n.ToString();c.CenterText(value,xx+cw/2,yy+ch/2-3,Math.Min(6,cw*.35),fraction>.65?p.Surface:p.Ink);c.Hover(xx,yy,cw,ch,new[]{"Mo","Di","Mi","Do","Fr","Sa","So"}[days[d]]+" · "+DisplayFormat.Hour(h)+"–"+DisplayFormat.Hour(h+1)+$"\n{n} / {list.Count} · "+(list.Count==0?"—":$"{100d*n/list.Count:N1} %")+"\nNetto: "+DisplayFormat.Money(list.Sum(t=>t.Net),r));}}
  for(int h=0;h<24;h+=3)c.CenterText(DisplayFormat.Hour(h),left+(h+.5)*cw,top+days.Length*ch+5,Math.Min(5,cw/4),p.Muted);c.CenterText(DisplayFormat.Hour(23),left+23.5*cw,top+days.Length*ch+5,Math.Min(5,cw/4),p.Muted);
 }
}
