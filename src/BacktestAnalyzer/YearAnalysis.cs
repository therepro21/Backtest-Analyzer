namespace BacktestAnalyzer;

public static class YearAnalysis
{
    public static List<Report> Scopes(Report source)
    {
        var result=new List<Report>{source};
        var times=source.Balances.Select(x=>x.Time).Concat(source.Deals.Select(x=>x.Time)).ToArray();
        if(times.Length==0)return result;
        int first=times.Min().Year,last=times.Max().Year;
        if(first==last)return result;
        for(int year=first;year<=last;year++)result.Add(ForYear(source,year));
        return result;
    }
    public static Report ForYear(Report source,int year)
    {
        var start=new DateTime(year,1,1);var end=start.AddYears(1);
        var ordered=source.Balances.OrderBy(x=>x.Time).ToList();
        decimal opening=ordered.Where(x=>x.Time<start).Select(x=>x.Balance).DefaultIfEmpty(source.InitialDeposit).Last();
        var inventory=Inventory(source.Deals.Where(d=>d.Time<start));
        var report=new Report{Source=source.Source,Hash=source.Hash,Platform=source.Platform,Metadata=source.Metadata,
            InputParameters=source.InputParameters,AvailableColumns=source.AvailableColumns,Warnings=source.Warnings,
            SaturdayTrading=source.SaturdayTrading,SundayTrading=source.SundayTrading,ReportScope=source.ReportScope,InitialDeposit=opening,AnalysisYear=year,AxisStart=start,AxisEnd=end,OpeningBuyLots=inventory.Buy,OpeningSellLots=inventory.Sell,
            BalanceWindowSeconds=source.BalanceWindowSeconds,
            ExcludeWeekends=source.ExcludeWeekends,
            Series=SeriesAnalysis.ForReport(source),LongSeriesHours=source.LongSeriesHours,SeriesAsPercent=source.SeriesAsPercent,
            OpenAtScopeEnd=source.Trades.Count(t=>t.Open<end&&(!t.Close.HasValue||t.Close.Value>=end)),
            HoldingBinMinutes=source.HoldingBinMinutes,HoldingAsPercent=source.HoldingAsPercent,HoldingMaxHours=source.HoldingMaxHours,
            Deals=source.Deals.Where(d=>d.Time>=start&&d.Time<end).ToList(),
            Trades=source.Trades.Where(t=>t.Close.HasValue&&t.Close.Value>=start&&t.Close.Value<end).ToList(),
            Balances=ordered.Where(x=>x.Time>=start&&x.Time<end).ToList(),EquitySource=source.EquitySource,
            EquityPoints=source.EquityPoints.Where(x=>x.Time>=start&&x.Time<end).ToList()};
        // Keep the preceding account balance, rather than resetting every year to the original deposit.
        if(ordered.Any(x=>x.Time<start))report.Balances.Insert(0,(start,opening));
        var carry=source.EquityPoints.LastOrDefault(x=>x.Time<start);
        if(carry.Time!=default)report.EquityPoints.Insert(0,(start,carry.Balance,carry.Equity,carry.DepositLoad));
        return report;
    }
    public static (decimal Buy,decimal Sell) Inventory(IEnumerable<Deal> deals)
    {
        decimal buy=0,sell=0;
        foreach(var d in deals.OrderBy(x=>x.Time))
        {
            if(d.Entry=="in"){if(d.Side=="buy")buy+=d.Volume;else sell+=d.Volume;}
            else if(d.Entry is "out" or "outby" or "out/by"){if(d.Side=="sell")buy-=d.Volume;else sell-=d.Volume;}
            else if(d.Entry is "inout" or "in/out")
            {if(d.Side=="sell"){var c=Math.Min(buy,d.Volume);buy-=c;sell+=d.Volume-c;}else{var c=Math.Min(sell,d.Volume);sell-=c;buy+=d.Volume-c;}}
        }
        return(buy,sell);
    }
    public static (decimal Money,decimal Percent,DateTime Time,decimal Peak) MaxDrawdown(Report report,bool percentage)
    {
        decimal peak=report.InitialDeposit;var best=(Money:0m,Percent:0m,Time:report.Balances.FirstOrDefault().Time,Peak:peak);
        foreach(var point in report.Balances.OrderBy(x=>x.Time))
        {peak=Math.Max(peak,point.Balance);var money=peak-point.Balance;var percent=peak>0?money/peak*100:0;
            if((percentage?percent:money)>(percentage?best.Percent:best.Money))best=(money,percent,point.Time,peak);}
        return best;
    }
}
