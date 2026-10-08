namespace BacktestAnalyzer;
public sealed record MarketSeries(string Symbol,string Source,bool BrokerTime,bool Custom,List<MarketBar> Bars);
public static class SymbolAnalysis
{
 public static List<string> Symbols(Report r)=>r.Deals.Select(d=>d.Symbol).Concat(r.Trades.Select(t=>t.Symbol)).Where(s=>s.Length>0).Distinct().Order().ToList();
 public static Report ForSymbol(Report source,string symbol)
 {
  var times=source.Deals.Select(d=>d.Time).Concat(source.Balances.Select(b=>b.Time)).Concat(source.AccountBookings.Select(b=>b.Time)).ToArray();
  var r=source.Range(source.AxisStart??times.DefaultIfEmpty(DateTime.Today).Min(),source.AxisEnd??times.DefaultIfEmpty(DateTime.Today).Max().AddTicks(1));r.AnalysisYear=source.AnalysisYear;r.AxisStart=source.AxisStart;r.AxisEnd=source.AxisEnd;r.ScopeSymbol=symbol;
  if(symbol.Length==0){if(Symbols(source).Count>1){r.MarketEnabled=false;r.MarketBars=new();}return r;}
  r.Deals=source.Deals.Where(d=>d.Symbol==symbol).ToList();r.Trades=source.Trades.Where(t=>t.Symbol==symbol).ToList();r.FullHistory=(source.FullHistory.Count>0?source.FullHistory:source.Deals).Where(d=>d.Symbol==symbol).ToList();r.Series=null;
  r.Metadata=new(source.Metadata);r.Metadata["Symbol"]=symbol;
  if(Symbols(source).Count>1){r.SymbolAttributed=true;r.EquityPoints=new();r.EquitySource="";r.EquityReferencePeak=null;r.Balances=new();decimal running=source.InitialDeposit;var start=source.AxisStart??source.Deals.Min(d=>d.Time);r.Balances.Add((start,running));foreach(var g in r.Deals.GroupBy(d=>d.Time).OrderBy(g=>g.Key)){running+=g.Sum(d=>d.Net);r.Balances.Add((g.Key,running));}r.Warnings=new(source.Warnings){"Symbolbezogene Ergebnislinie: Anfangskapital plus gebuchte Symbolergebnisse; keine separate Konto-Equity."};}
  r.MarketBrokerSymbol=symbol;
  if(source.SymbolMarkets.TryGetValue(symbol,out var data)){r.MarketSymbol=data.Symbol;r.MarketLoadedSymbol=data.Symbol;r.MarketBars=data.Bars;r.MarketSource=data.Source;r.MarketSourceTimeIsBroker=data.BrokerTime;r.MarketIsCustom=data.Custom;}
  else if(source.MarketBrokerSymbol!=symbol){r.MarketBars=new();r.MarketEnabled=false;}
  SetEvents(r);return r;
 }
 public static void SetEvents(Report r){var history=r.FullHistory.Count>0?r.FullHistory:r.Deals;r.ActualTestEnd=history.Select(d=>d.Time).DefaultIfEmpty().Max();var cycles=SeriesAnalysis.ForReport(r).Where(s=>s.End.HasValue).OrderBy(s=>s.End).ToList();r.PenultimateClose=cycles.Count>1?cycles[^2].End:null;}
 public static List<Report> Scopes(Report r){SetEvents(r);var result=ReportOptions.SingleScopes(ForSymbol(r,""));if(Symbols(r).Count>1)foreach(var symbol in Symbols(r))result.AddRange(ReportOptions.SingleScopes(ForSymbol(r,symbol)));return result;}
}
