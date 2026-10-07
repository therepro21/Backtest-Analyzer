namespace BacktestAnalyzer;

public sealed record TradingSeries(DateTime Start,DateTime? End,decimal Net,int Entries,decimal MaxGrossLots)
{
    public double CalendarSeconds=>End.HasValue?(End.Value-Start).TotalSeconds:0;
    public double WeekdaySeconds=>Stats.HoldingSeconds(new Trade{Open=Start,Close=End},true);
}
public static class SeriesAnalysis
{
    // One account-wide flat-to-flat cycle, independent of any unavailable EA cycle identifier.
    public static List<TradingSeries> Build(IEnumerable<Deal> history)
    {
        var result=new List<TradingSeries>();decimal buy=0,sell=0,net=0,maxLots=0;int entries=0;DateTime? start=null;
        foreach(var d in history.OrderBy(d=>d.Time))
        {
            bool wasFlat=buy+sell==0;
            if(d.Entry=="in"){if(wasFlat)start=d.Time;if(d.Side=="buy")buy+=d.Volume;else sell+=d.Volume;entries++;}
            else if(d.Entry is "out" or "outby" or "out/by"){if(d.Side=="sell")buy-=d.Volume;else sell-=d.Volume;}
            else if(d.Entry is "inout" or "in/out")
            {if(d.Side=="sell"){var c=Math.Min(buy,d.Volume);buy-=c;sell+=d.Volume-c;}else{var c=Math.Min(sell,d.Volume);sell-=c;buy+=d.Volume-c;}entries++;}
            if(buy<0||sell<0)throw new InvalidDataException("Serienauswertung: unvollständiges oder inkonsistentes Einstiegsvolumen.");
            if(start.HasValue){net+=d.Net;maxLots=Math.Max(maxLots,buy+sell);}
            if(start.HasValue&&buy+sell==0){result.Add(new(start.Value,d.Time,net,entries,maxLots));start=null;net=0;entries=0;maxLots=0;}
        }
        if(start.HasValue)result.Add(new(start.Value,null,net,entries,maxLots));return result;
    }
    public static List<TradingSeries> Selected(Report report)
    {
        var all=ForReport(report);return all.Where(s=>s.End.HasValue&&(!report.AnalysisYear.HasValue||s.End.Value.Year==report.AnalysisYear)).ToList();
    }
    public static List<TradingSeries> ForReport(Report report)
    {
        if(report.Series!=null)return report.Series;
        try{return report.Series=Build(report.Deals);}
        catch(InvalidDataException ex){string warning="Serienauswertung nicht verfügbar: "+ex.Message;if(!report.Warnings.Contains(warning))report.Warnings.Add(warning);return report.Series=new();}
    }
}
