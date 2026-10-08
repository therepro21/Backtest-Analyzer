namespace BacktestAnalyzer;

public sealed record TradingSeries(DateTime Start,DateTime? End,decimal Net,int Entries,decimal MaxGrossLots)
{
    public string Position {get;init;}="";public string Symbol {get;init;}="";
    public double CalendarSeconds=>End.HasValue?(End.Value-Start).TotalSeconds:0;
    public double WeekdaySeconds=>Stats.HoldingSeconds(new Trade{Open=Start,Close=End},true,false,false,0);
}
public static class SeriesAnalysis
{
    // One account-wide flat-to-flat cycle, independent of any unavailable EA cycle identifier.
    public static List<TradingSeries> Build(IEnumerable<Deal> history)
    {
        var result=new List<TradingSeries>();decimal buy=0,sell=0,net=0,maxLots=0;int entries=0;DateTime? start=null;
        foreach(var batch in history.OrderBy(d=>d.Time).GroupBy(d=>d.Time))
        {
        foreach(var d in batch)
        {
            bool wasFlat=buy+sell==0;
            if(d.Entry=="in"){if(wasFlat)start=d.Time;if(d.Side=="buy")buy+=d.Volume;else sell+=d.Volume;entries++;}
            else if(d.Entry is "out" or "outby" or "out/by"){if(d.Side=="sell")buy-=d.Volume;else sell-=d.Volume;}
            else if(d.Entry is "inout" or "in/out")
            {if(d.Side=="sell"){var c=Math.Min(buy,d.Volume);buy-=c;sell+=d.Volume-c;}else{var c=Math.Min(sell,d.Volume);sell-=c;buy+=d.Volume-c;}entries++;}
            if(buy<0||sell<0)throw new InvalidDataException("Serienauswertung: unvollständiges oder inkonsistentes Einstiegsvolumen.");
            if(start.HasValue){net+=d.Net;maxLots=Math.Max(maxLots,buy+sell);}
        }
            if(start.HasValue&&buy+sell==0){result.Add(new(start.Value,batch.Key,net,entries,maxLots));start=null;net=0;entries=0;maxLots=0;}
        }
        if(start.HasValue)result.Add(new(start.Value,null,net,entries,maxLots));return result;
    }
    public static List<TradingSeries> Selected(Report report)
    {
        var all=ForReport(report);return all.Where(s=>s.End.HasValue&&(!report.AnalysisYear.HasValue||s.End.Value.Year==report.AnalysisYear)).ToList();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Report,DateTime[]> TestEndTimes=new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Report,HashSet<(string Position,string Symbol,DateTime Time)>> ForcedTrades=new();
    public static bool TestEnd(Report report,TradingSeries cycle)=>cycle.End.HasValue&&(report.StrategyMode=="single"?ForcedTrades.GetValue(report,q=>q.Deals.Where(d=>d.Comment.Contains("end of test",StringComparison.OrdinalIgnoreCase)).Select(d=>(d.Position,d.Symbol,d.Time)).ToHashSet()).Contains((cycle.Position,cycle.Symbol,cycle.End.Value)):TestEndTimes.GetValue(report,r=>r.Deals.Where(d=>d.Comment.Contains("end of test",StringComparison.OrdinalIgnoreCase)).Select(d=>d.Time).Distinct().ToArray()).Any(t=>t>=cycle.Start&&t<=cycle.End));
    public static List<TradingSeries> Regular(Report report)=>Selected(report).Where(c=>!TestEnd(report,c)).ToList();
    public static (DateTime? Time,decimal Profit,decimal Excluded) Adjusted(Report report)
    {
        if(report.StrategyMode=="single")return(null,report.Deals.Sum(d=>d.Net),0);
        var cycles=Selected(report);var end=cycles.LastOrDefault(c=>TestEnd(report,c));if(end==null)return(null,report.Deals.Sum(d=>d.Net),0);
        var previous=cycles.LastOrDefault(c=>c.End<end.Start);if(previous==null)return(null,0,end.Net);
        return(previous.End,report.Deals.Where(d=>d.Time<=previous.End).Sum(d=>d.Net),end.Net);
    }
    public static List<TradingSeries> ForReport(Report report)
    {
        if(report.StrategyMode=="single")return report.Trades.OrderBy(t=>t.Open).Select(t=>new TradingSeries(t.Open,t.Close,t.Net,1,t.Volume){Position=t.Position,Symbol=t.Symbol}).ToList();
        if(report.Series!=null)return report.Series;
        try{return report.Series=Build(report.Deals);}
        catch(InvalidDataException ex){string warning="Serienauswertung nicht verfügbar: "+ex.Message;if(!report.Warnings.Contains(warning))report.Warnings.Add(warning);return report.Series=new();}
    }
}
