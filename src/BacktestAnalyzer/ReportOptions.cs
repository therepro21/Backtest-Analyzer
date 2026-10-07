namespace BacktestAnalyzer;
public static class ReportOptions
{
    public static bool DayEnabled(Report r,DayOfWeek day)=>day!=DayOfWeek.Saturday&&day!=DayOfWeek.Sunday||day==DayOfWeek.Saturday&&r.SaturdayTrading||day==DayOfWeek.Sunday&&r.SundayTrading;
    public static int[] Days(Report r)=>Enumerable.Range(0,7).Where(i=>i<5||i==5&&r.SaturdayTrading||i==6&&r.SundayTrading).ToArray();
    public static void DetectWeekends(Report r){r.SaturdayTrading=r.Deals.Any(d=>d.Time.DayOfWeek==DayOfWeek.Saturday);r.SundayTrading=r.Deals.Any(d=>d.Time.DayOfWeek==DayOfWeek.Sunday);}
    public static double CycleSeconds(Report r,TradingSeries c)=>Stats.HoldingSeconds(new Trade{Open=c.Start,Close=c.End},true,r.SaturdayTrading,r.SundayTrading);
    public static List<Report> Scopes(Report r)=>r.ReportScope=="total"?new(){r}:r.ReportScope=="years"?YearAnalysis.Scopes(r).Skip(1).DefaultIfEmpty(r).ToList():YearAnalysis.Scopes(r);
}
