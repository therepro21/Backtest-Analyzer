using System.Diagnostics;
using System.Text.Json;
namespace BacktestAnalyzer;
public sealed record Mt5Symbol(string Name,bool Custom,string Description,string CurrencyBase="",string CurrencyProfit="",double ContractSize=0){public string Label=>Name+(Custom?" · Custom":"")+" · "+Description;}
public static class Mt5Market
{
    public static List<Mt5Symbol> Candidates(string original,IEnumerable<Mt5Symbol> all)=>all.Where(s=>s.Name==original||SymbolAliases.Canonical(s.Name)==SymbolAliases.Canonical(original)).OrderByDescending(s=>s.Name==original).ToList();
    static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true};
    public static string[] Terminals()=>Process.GetProcessesByName("terminal64").Select(p=>{try{return p.MainModule?.FileName;}catch{return null;}}).Where(p=>p!=null).Cast<string>().Distinct().ToArray();
    static async Task<string> Run(params string[] args)
    {
        var python=Path.Combine(AppContext.BaseDirectory,"runtime","python","python.exe");var bridge=Path.Combine(AppContext.BaseDirectory,"mt5_bridge.py");if(!File.Exists(python)||!File.Exists(bridge))throw new FileNotFoundException("Portable MT5 runtime fehlt. Vollständigen App-Ordner verwenden.");
        var info=new ProcessStartInfo(python){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};info.ArgumentList.Add(bridge);foreach(var a in args)info.ArgumentList.Add(a);using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(75));try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw new TimeoutException("MT5-Kursabruf dauerte zu lange.");}var stderr=await error;if(process.ExitCode!=0)throw new InvalidOperationException(stderr.Trim());return await output;
    }
    public static async Task<List<Mt5Symbol>> Symbols(string terminal)=>JsonSerializer.Deserialize<List<Mt5Symbol>>(await Run("symbols",terminal),Json)??new();
    sealed record Result(string Symbol,bool Custom,List<MarketBar> Bars);
    public static async Task LoadEventQuotes(Report r,string terminal,string symbol)
    {
        if(r.EquityPoints.Count==0)return;
        var scopes=new[]{r}.Concat(ReportOptions.Scopes(r)).Distinct();
        var times=scopes.SelectMany(s=>EventDetails.Drawdowns(s).SelectMany(e=>new DateTime?[]{e.Start,e.Trough,e.Recovery})).Where(t=>t.HasValue).Select(t=>t!.Value).Distinct().Order().ToArray();
        if(times.Length==0)return;
        var stamps=times.Select(t=>new DateTimeOffset(DateTime.SpecifyKind(t,DateTimeKind.Utc)).ToUnixTimeSeconds());
        var quotes=JsonSerializer.Deserialize<List<EventQuote>>(await Run("quotes",terminal,symbol,JsonSerializer.Serialize(stamps)),Json)??new();
        if(quotes.Any(q=>!times.Contains(q.EventTime)||q.Symbol!=symbol||q.Price<=0||q.Minutes is not (1 or 5)||q.CloseTime>q.EventTime||(q.EventTime-q.CloseTime).TotalMinutes>=q.Minutes))throw new InvalidDataException("Ungültige Ereigniskurse.");
        r.EventQuotes.RemoveAll(q=>q.Symbol==symbol&&times.Contains(q.EventTime));r.EventQuotes.AddRange(quotes);
        if(quotes.Count<times.Length)r.Warnings.Add($"Ereigniskurse: {quotes.Count} von {times.Length} Zeitpunkten mit M1/M5-Schlusskurs verfügbar. Fehlende Kurse bleiben ausdrücklich unverfügbar.");
    }
    public static async Task Load(Report r,string terminal,string symbol)
    {
        var times=r.Deals.Select(d=>d.Time).Concat(r.Balances.Select(b=>b.Time)).ToList();if(times.Count==0)throw new InvalidOperationException("Kein Zeitraum vorhanden.");long Unix(DateTime t)=>new DateTimeOffset(DateTime.SpecifyKind(t,DateTimeKind.Utc)).ToUnixTimeSeconds();
        var result=JsonSerializer.Deserialize<Result>(await Run("bars",terminal,symbol,Unix(times.Min().AddDays(-1)).ToString(),Unix(times.Max().AddDays(1)).ToString()),Json)??throw new InvalidDataException("MT5-Daten fehlen.");if(result.Symbol!=symbol||result.Bars.Count==0)throw new InvalidDataException("Symbol oder Daten passen nicht.");
        if(result.Bars.Any(b=>b.Open<=0||b.Close<=0||b.Low>Math.Min(b.Open,b.Close)||b.High<Math.Max(b.Open,b.Close)))throw new InvalidDataException("MT5 lieferte inkonsistente OHLC-Werte.");
        r.MarketSymbol=symbol;r.MarketLoadedSymbol=symbol;r.MarketBars=result.Bars.OrderBy(b=>b.Utc).ToList();r.MarketSourceTimeIsBroker=true;r.MarketSource="MT5 "+Path.GetDirectoryName(terminal)!.Split(Path.DirectorySeparatorChar).Last()+" · "+symbol+(result.Custom?" (Custom)":"");r.MarketStatus="";r.MarketEnabled=true;r.MarketTerminalPath=terminal;r.MarketMt5Symbol=symbol;r.MarketIsCustom=result.Custom;var original=r.MarketBrokerSymbol.Length>0?r.MarketBrokerSymbol:symbol;r.SymbolMarkets[original]=new(symbol,r.MarketSource,true,result.Custom,r.MarketBars);r.SymbolMappings[original]=symbol;
        await LoadEventQuotes(r,terminal,symbol);
    }
}
