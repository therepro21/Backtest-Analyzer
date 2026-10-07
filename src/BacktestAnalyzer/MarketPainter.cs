namespace BacktestAnalyzer;
public static class MarketPainter
{
    static string Unit(Report r)=>r.MarketSymbol.Contains("USD")?"$":r.MarketSymbol.EndsWith("JPY")?"¥":r.MarketSymbol.EndsWith("EUR")?"€":r.MarketSymbol.Length>=3?r.MarketSymbol[^3..]:"Preis";
    public static List<MarketBar> Visible(Report r,DateTime start,DateTime end)=>r.MarketEnabled&&r.MarketLoadedSymbol==r.MarketSymbol?MarketData.Aggregate(r).Where(b=>b.Utc>=start&&b.Utc<=end).ToList():new();
    public static void Draw(ICanvas c,Report r,Palette p,DateTime start,DateTime end,double left,double right,double top,double bottom)
    {
        var all=Visible(r,start,end);if(all.Count==0)return;var lo=all.Min(b=>b.Low);var hi=all.Max(b=>b.High);decimal pad=Math.Max(.001m,(hi-lo)*.08m);lo-=pad;hi+=pad;
        double span=Math.Max(1,(end-start).TotalSeconds);double X(DateTime t)=>left+(right-left)*(t-start).TotalSeconds/span;double Y(decimal value)=>bottom-(double)((value-lo)/(hi-lo))*(bottom-top);
        // Dense views retain OHLC envelopes per physical display column, without changing source bars.
        int max=Math.Max(20,(int)((right-left)/2));var groups=all.GroupBy(b=>(int)((b.Utc-start).TotalSeconds/span*max)).ToList();
        foreach(var group in groups){var first=group.First();var last=group.Last();double xx=X(first.Utc),w=Math.Max(.6,Math.Min(8,(right-left)/max*.55));string col=last.Close>=first.Open?(p.Dark?"#4C897E":"#B7D4CA"):(p.Dark?"#986D67":"#E3BBB4");c.Line(xx,Y(group.Max(b=>b.High)),xx,Y(group.Min(b=>b.Low)),col,.8);c.Rect(xx-w/2,Math.Min(Y(first.Open),Y(last.Close)),w,Math.Max(.6,Math.Abs(Y(first.Open)-Y(last.Close))),col);}
        for(int i=0;i<=3;i++){decimal value=lo+(hi-lo)*i/3;c.Text(value.ToString(hi>100?"N0":"N3")+" "+Unit(r),right+5,Y(value)-4,6,p.Muted);}c.VerticalText(r.MarketSymbol+" ("+(r.MarketSymbol.EndsWith("USD")?"$":"Preis")+")",right+43,(top+bottom)/2,6,p.Muted);
    }
    public static string Details(Report r,DateTime start,DateTime end)
    {
        var bars=Visible(r,start,end);if(bars.Count==0)return r.MarketEnabled?"\nMarktkurs: nicht verfügbar":"";
        return "\n"+r.MarketSymbol+" · "+r.MarketInterval+" · "+bars.Count+" Kerzen im Zeitfenster\nO / H / L / C: "+string.Join(" / ",new[]{bars[0].Open,bars.Max(b=>b.High),bars.Min(b=>b.Low),bars[^1].Close}.Select(v=>v.ToString("N3")+" "+Unit(r)))+"\n"+r.MarketSource;
    }
}
