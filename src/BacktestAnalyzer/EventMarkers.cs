namespace BacktestAnalyzer;
public static class EventMarkers
{
 public static void Timeline(ICanvas c,Report r,Palette p,double left,double right,double top,double bottom,double captionY,Func<DateTime,double> position)
 {
  var forced=SeriesAnalysis.Selected(r).LastOrDefault(cycle=>SeriesAnalysis.TestEnd(r,cycle));if(forced==null)return;
  var previous=SeriesAnalysis.Regular(r).Where(q=>q.End<=forced.End).OrderBy(q=>q.End).LastOrDefault();
  var items=new[]{(Time:(DateTime?)forced.Start,Label:r.StrategyMode=="single"?"Beginn letzte Order":"Beginn letzter Zyklus",Color:p.Negative),(Time:previous?.End,Label:"Letzter regulärer Abschluss",Color:p.Positive),(Time:forced.End,Label:"Zwangsschluss am Testende",Color:p.Negative)};
  int row=0;foreach(var item in items){if(!item.Time.HasValue)continue;double at=position(item.Time.Value);if(at<left||at>right)continue;
   c.Line(at,top,at,bottom,item.Color,.9);c.Circle(at,bottom,2,item.Color);string label=Localization.T(item.Label)+": "+item.Time.Value.ToString("dd.MM.yy HH:mm");
   double yy=captionY+row*8;c.Text(label,left,yy,5.8,item.Color,true);c.Line(left+Math.Min(right-left-8,c.Measure(label,5.8,true))+3,yy+3,at,bottom,item.Color,.35);c.Hover(at-3,top,6,bottom-top,label+"\n"+DisplayFormat.Time(item.Time.Value));row++;
  }
 }
 public static void Cash(ICanvas c,Report r,Palette p,DateTime start,DateTime end,double left,double right,double top,double bottom)
 {
  int row=0;foreach(var booking in r.AccountBookings.Where(q=>q.Time>=start&&q.Time<=end)){double at=left+(right-left)*(booking.Time-start).TotalSeconds/Math.Max(1,(end-start).TotalSeconds);string color=booking.Amount>=0?p.Positive:p.Negative;string label=Localization.T(AccountHistory.Label(booking))+" "+DisplayFormat.Money(booking.Amount,r);c.Line(at,top,at,bottom,color,.7);c.Text(label,Math.Clamp(at,left,right-c.Measure(label,6)),top+3+(row++%3)*9,6,color,true);c.Hover(at-3,top,6,bottom-top,label+"\n"+DisplayFormat.Time(booking.Time)+"\n"+booking.Comment);}
 }
 public static void Draw(ICanvas c,Report r,Palette p,DateTime start,DateTime end,double left,double right,double top,double bottom,double? ddTop,double? ddBottom,double captionY)
 {
  double span=Math.Max(1,(end-start).TotalSeconds);double X(DateTime time)=>left+(right-left)*(time-start).TotalSeconds/span;
  var events=EventDetails.Drawdowns(r);var markers=events.Select((ev,i)=>(Event:ev,Rank:i+1,At:X(ev.Trough))).Where(m=>m.At>=left&&m.At<=right).OrderBy(m=>m.At).ToArray();var locations=markers.Select(m=>m.At).ToArray();for(int i=0;i<locations.Length;i++)locations[i]=Math.Max(locations[i],i==0?left+4:locations[i-1]+17);for(int i=locations.Length-1;i>=0;i--)locations[i]=Math.Min(locations[i],i==locations.Length-1?right-10:locations[i+1]-17);for(int i=0;i<markers.Length;i++){var m=markers[i];double xx=locations[i],yy=bottom+1;if(Math.Abs(xx-m.At)>2)c.Line(m.At,bottom,xx,bottom+.8,p.Negative,.3);c.CenterText("*"+m.Rank,xx,yy,5.5,p.Negative,true);c.Hover(xx-7,yy,14,7,"*"+m.Rank+"\n"+EventDetails.Describe(r,m.Event));}
  Cash(c,r,p,start,end,left,right,top,bottom);
  Timeline(c,r,p,left,right,top,bottom,top-27,X);
  if(events.Count>0){var biggest=events[0];var label=$"Equity-DD Max: {biggest.Percent:N2} %";double lx=Math.Clamp(X(biggest.Trough)-c.Measure(label,5.5)/2,left,right-c.Measure(label,5.5));c.Text(label,lx,captionY,5.5,p.Muted);}
 }
}
