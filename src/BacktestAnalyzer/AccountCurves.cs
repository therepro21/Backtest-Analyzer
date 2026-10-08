namespace BacktestAnalyzer;

public static class AccountCurves
{
    public static List<(DateTime Time,decimal Balance)> DisplayBalances(Report report)
    {
        var result=new List<(DateTime Time,decimal Balance)>();
        var ordered=report.Balances.OrderBy(x=>x.Time).ToList();var changes=ordered.Where((point,index)=>index==0||point.Balance!=ordered[index-1].Balance).ToList();
        if(report.BalanceWindowSeconds<=0)return changes;
        for(int i=0;i<changes.Count;)
        {
            var first=changes[i];var last=first;int next=i+1;
            while(next<changes.Count&&(changes[next].Time-first.Time).TotalSeconds<=report.BalanceWindowSeconds){last=changes[next];next++;}
            // Preserve the starting capital and use the settled balance at the END of each fixed window.
            if(i==0)result.Add(first);
            if(last.Time!=first.Time||i>0)result.Add(last);i=next;
        }
        return result;
    }
    public static List<(DateTime Time,decimal Equity,decimal Money,decimal Percent)> EquityDrawdowns(Report report)
    {
        var result=new List<(DateTime,decimal,decimal,decimal)>();decimal peak=report.EquityReferencePeak??(report.EquityPoints.Count>0?report.EquityPoints[0].Equity:report.InitialDeposit);
        foreach(var p in AccountHistory.AdjustedEquity(report)){peak=Math.Max(peak,p.Equity);decimal money=peak-p.Equity;result.Add((p.Time,p.Equity,money,peak>0?money/peak*100:0));}return result;
    }
}
