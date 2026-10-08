namespace BacktestAnalyzer;
public static class EventMarkers
{
 public static void Timeline(ICanvas c,Report r,Palette p,double left,double right,double top,double bottom,double captionY,Func<DateTime,double> position)
 {
  var forced=SeriesAnalysis.Selected(r).LastOrDefault(cycle=>SeriesAnalysis.TestEnd(r,cycle));if(forced==null)return;
  var previous=SeriesAnalysis.Regular(r).Where(q=>q.End<=forced.End).OrderBy(q=>q.End).LastOrDefault();
  var items=new[]{(Key:"A",Time:previous?.End,Label:Localization.English?"Last regular close":"Letzter regulärer Abschluss",Color:p.Positive),(Key:"B",Time:(DateTime?)forced.Start,Label:Localization.English?(r.StrategyMode=="single"?"Final order entry":"Final cycle start"):(r.StrategyMode=="single"?"Beginn letzte Order":"Beginn letzter Zyklus"),Color:p.Dark?"#FFD06A":"#A76A00"),(Key:"C",Time:forced.End,Label:Localization.English?"Forced close":"Zwangsschluss",Color:p.Negative)};
  var visible=items.Where(q=>q.Time.HasValue&&position(q.Time.Value)>=left&&position(q.Time.Value)<=right).ToArray();if(visible.Length==0)return;
  string Label(int i)=>visible[i].Key+" "+visible[i].Label+" · "+visible[i].Time!.Value.ToString("dd.MM.yy HH:mm");
  double size=6,gap=10;while(size>4.5&&visible.Select((q,i)=>c.Measure(Label(i),size)).Sum()+gap*(visible.Length-1)>right-left)size-=.1;
  double cx=left;for(int i=0;i<visible.Length;i++){var item=visible[i];double at=position(item.Time!.Value);double radius=2.6;
   // The triangle TIP retains the exact time coordinate; only the height changes.
   double tip=Math.Min(bottom-3,top+10+i*13);double labelWidth=c.Measure(item.Key,5.5,true);c.DownTriangle(at,tip,radius,item.Color,left,right);c.Text(item.Key,at-radius-labelWidth-2,tip-7,5.5,item.Color,true);
   string label=Label(i);c.Text(label,cx,captionY,size,item.Color);if(item.Key=="C"){string result="("+(Localization.English?(r.StrategyMode=="single"?"Final order net: ":"Final cycle net: "):(r.StrategyMode=="single"?"Netto letzte Order: ":"Netto letzter Zyklus: "))+DisplayFormat.Money(forced.Net,r)+")";double financialSize=Math.Min(4.2,size*.76);c.Text(result,Math.Min(cx,right-c.Measure(result,financialSize)),captionY+size+2,financialSize,item.Color);}
   c.Hover(at-5,tip-9,10,11,label+"\n"+DisplayFormat.Time(item.Time.Value)+(item.Key=="C"?"\nNetto: "+DisplayFormat.Money(forced.Net,r):""));cx+=c.Measure(label,size)+gap;
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
  Timeline(c,r,p,left,right,top,bottom,captionY,X);
  if(events.Count>0){var biggest=events[0];var label=$"Equity-DD Max: {biggest.Percent:N2} %";double lx=Math.Clamp(X(biggest.Trough)-c.Measure(label,5.5)/2,left,right-c.Measure(label,5.5));c.Text(label,lx,captionY-10,5.5,p.Muted);}
 }
}
