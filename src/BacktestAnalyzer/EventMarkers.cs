namespace BacktestAnalyzer;
public static class EventMarkers
{
 public static void Draw(ICanvas c,Report r,Palette p,DateTime start,DateTime end,double left,double right,double top,double bottom,double? ddTop,double? ddBottom,double captionY)
 {
  double span=Math.Max(1,(end-start).TotalSeconds);double X(DateTime time)=>left+(right-left)*(time-start).TotalSeconds/span;
  var events=EventDetails.Drawdowns(r);var occupied=new List<double>();for(int i=0;i<events.Count;i++){var ev=events[i];double xx=X(ev.Trough);if(xx<left||xx>right)continue;int lane=occupied.Count(v=>Math.Abs(v-xx)<12);occupied.Add(xx);xx=Math.Clamp(xx+lane*10,left+4,right-6);double yy=bottom+1;c.Text("*"+(i+1),xx-3,yy,5.5,p.Negative,true);c.Hover(xx-4,yy,10,7,"*"+(i+1)+"\n"+EventDetails.Describe(r,ev));}
  if(!r.ActualTestEnd.HasValue)SymbolAnalysis.SetEvents(r);DateTime test=r.ActualTestEnd??end;var previous=r.PenultimateClose;
  int index=0;foreach(var item in new[]{(Time:(DateTime?)test,Label:Localization.English?"Test end":"Testende"),(Time:previous,Label:Localization.English?"Previous close":"Vorletzter Schluss")}){if(!item.Time.HasValue||item.Time<start||item.Time>end)continue;double xx=X(item.Time.Value);c.Line(xx,top,xx,bottom,p.Muted,.6);if(ddTop.HasValue)c.Line(xx,ddTop.Value,xx,ddBottom!.Value,p.Muted,.6);var label=item.Label+" "+item.Time.Value.ToString("dd.MM HH:mm");double lx=Math.Clamp(xx-c.Measure(label,5.5)/2,left,right-c.Measure(label,5.5));c.Text(label,lx,top+13+index++*8,5.5,p.Muted);c.Hover(xx-3,top,6,bottom-top,label+"\n"+DisplayFormat.Time(item.Time.Value));}
  if(events.Count>0){var biggest=events[0];var label=$"Equity-DD Max: {biggest.Percent:N2} %";double lx=Math.Clamp(X(biggest.Trough)-c.Measure(label,5.5)/2,left,right-c.Measure(label,5.5));c.Text(label,lx,captionY,5.5,p.Muted);}
 }
}
