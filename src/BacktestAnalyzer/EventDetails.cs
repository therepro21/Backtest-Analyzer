namespace BacktestAnalyzer;
public sealed record DrawdownEvent(DateTime Start,DateTime Trough,DateTime? Recovery,decimal Peak,decimal Equity,decimal Money,decimal Percent);
public static class EventDetails
{
    public static List<DrawdownEvent> Drawdowns(Report r)
    {
        var points=r.EquityPoints.OrderBy(p=>p.Time).ToList();if(points.Count==0)return new();
        decimal peak=r.EquityReferencePeak??points[0].Equity;DateTime peakTime=points[0].Time;DrawdownEvent? active=null;var events=new List<DrawdownEvent>();
        foreach(var p in points){if(p.Equity>=peak){if(active!=null){events.Add(active with{Recovery=p.Time});active=null;}peak=p.Equity;peakTime=p.Time;}else{decimal money=peak-p.Equity,pct=peak>0?money/peak*100:0;if(active==null||money>active.Money)active=new(peakTime,p.Time,null,peak,p.Equity,money,pct);}}
        if(active!=null)events.Add(active);return events.OrderByDescending(e=>e.Percent).ThenByDescending(e=>e.Money).Take(5).ToList();
    }
    public static string Quote(Report r,DateTime time)
    {
        var bar=r.MarketBars.LastOrDefault(b=>MarketData.BrokerTime(r,b.Utc).AddHours(1)<=time&&time<MarketData.BrokerTime(r,b.Utc).AddHours(2));
        return bar==null?(Localization.English?"market price unavailable":"Marktkurs nicht verfügbar"):(Localization.English?"last completed H1 close":"letzter abgeschlossener H1-Schlusskurs")+" "+bar.Close.ToString("N2",Localization.Culture)+" "+MarketData.PriceCurrency(r)+" · "+DisplayFormat.Time(MarketData.BrokerTime(r,bar.Utc).AddHours(1));
    }
    public static string Describe(Report r,DrawdownEvent e)
    {
        var end=e.Recovery??r.EquityPoints.Max(p=>p.Time);var duration=Stats.HoldingSeconds(new Trade{Open=e.Start,Close=end},r.ExcludeWeekends,r.SaturdayTrading,r.SundayTrading,r.NightPauseMinutes,r.NightPauseStartMinute);
        return $"{e.Percent:N2} % / {DisplayFormat.Money(e.Money,r)} · "+(Localization.English?"Peak":"Höchststand")+": "+DisplayFormat.Money(e.Peak,r)+" → "+DisplayFormat.Money(e.Equity,r)+"\n"+(Localization.English?"Start":"Beginn")+": "+DisplayFormat.Time(e.Start)+" · "+Quote(r,e.Start)+"\n"+(Localization.English?"Trough":"Tiefpunkt")+": "+DisplayFormat.Time(e.Trough)+" · "+Quote(r,e.Trough)+"\n"+(e.Recovery.HasValue?(Localization.English?"Recovered":"Erholt")+": "+DisplayFormat.Time(e.Recovery.Value)+" · "+Quote(r,e.Recovery.Value):(Localization.English?"Not recovered by":"Nicht erholt bis")+" "+DisplayFormat.Time(end))+" · "+(Localization.English?"Trading duration":"Handelsdauer")+": "+Stats.Duration(duration);
    }
    public static (string Duration,string Period,string Prices) Longest(Report r,IEnumerable<Trade>? subset=null,bool forceSingle=false)
    {
        DateTime begin,end;decimal? opening=null,closing=null;string symbol="";double seconds;
        var trades=(subset??r.Closed).Where(t=>t.Close.HasValue).ToList();
        if(r.StrategyMode=="basket"&&!forceSingle){
            var c=SeriesAnalysis.Selected(r).MaxBy(c=>ReportOptions.CycleSeconds(r,c));if(c==null)return("—","","");begin=c.Start;end=c.End!.Value;seconds=ReportOptions.CycleSeconds(r,c);
            var history=r.FullHistory.Count>0?r.FullHistory:r.Deals;var first=history.FirstOrDefault(d=>d.Time==begin&&d.Entry is "in" or "inout");var last=history.LastOrDefault(d=>d.Time==end&&d.Entry is "out" or "outby" or "out/by");
            if(first!=null&&last!=null&&first.Symbol==last.Symbol){opening=first.Price;closing=last.Price;symbol=first.Symbol;}
        }else{var t=trades.MaxBy(t=>Stats.HoldingSeconds(t,r.ExcludeWeekends,r.SaturdayTrading,r.SundayTrading,r.NightPauseMinutes,r.NightPauseStartMinute));if(t==null)return("—","","");begin=t.Open;end=t.Close!.Value;seconds=Stats.HoldingSeconds(t,r.ExcludeWeekends,r.SaturdayTrading,r.SundayTrading,r.NightPauseMinutes,r.NightPauseStartMinute);opening=t.OpenPrice;symbol=t.Symbol;var history=r.FullHistory.Count>0?r.FullHistory:r.Deals;closing=history.LastOrDefault(d=>d.Time==end&&d.Symbol==symbol&&d.Position==t.Position&&d.Entry is "out" or "outby" or "out/by")?.Price;}
        string prices=opening.HasValue&&closing.HasValue?$"({symbol}: {opening.Value:N3} → {closing.Value:N3})": "("+Quote(r,begin)+" → "+Quote(r,end)+")";
        return(Stats.Duration(seconds),DisplayFormat.Time(begin)+" → "+DisplayFormat.Time(end),prices);
    }
}
