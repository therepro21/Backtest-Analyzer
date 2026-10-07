using System.Diagnostics;
using System.Text.Json;
namespace BacktestAnalyzer;
public sealed record Mt5Symbol(string Name,bool Custom,string Description){public string Label=>Name+(Custom?" · Custom":"")+" · "+Description;}
public static class Mt5Market
{
    static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true};
    public static string[] Terminals()=>Process.GetProcessesByName("terminal64").Select(p=>{try{return p.MainModule?.FileName;}catch{return null;}}).Where(p=>p!=null).Cast<string>().Distinct().ToArray();
    static async Task<string> Run(params string[] args)
    {
        var python=Path.Combine(AppContext.BaseDirectory,"runtime","python","python.exe");var bridge=Path.Combine(AppContext.BaseDirectory,"mt5_bridge.py");if(!File.Exists(python)||!File.Exists(bridge))throw new FileNotFoundException("Portable MT5 runtime fehlt. Vollständigen App-Ordner verwenden.");
        var info=new ProcessStartInfo(python){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};info.ArgumentList.Add(bridge);foreach(var a in args)info.ArgumentList.Add(a);using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(75));try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw new TimeoutException("MT5-Kursabruf dauerte zu lange.");}var stderr=await error;if(process.ExitCode!=0)throw new InvalidOperationException(stderr.Trim());return await output;
    }
    public static async Task<List<Mt5Symbol>> Symbols(string terminal)=>JsonSerializer.Deserialize<List<Mt5Symbol>>(await Run("symbols",terminal),Json)??new();
    sealed record Result(string Symbol,bool Custom,List<MarketBar> Bars);
    public static async Task Load(Report r,string terminal,string symbol)
    {
        var times=r.Deals.Select(d=>d.Time).Concat(r.Balances.Select(b=>b.Time)).ToList();if(times.Count==0)throw new InvalidOperationException("Kein Zeitraum vorhanden.");long Unix(DateTime t)=>new DateTimeOffset(DateTime.SpecifyKind(t,DateTimeKind.Utc)).ToUnixTimeSeconds();
        var result=JsonSerializer.Deserialize<Result>(await Run("bars",terminal,symbol,Unix(times.Min().AddDays(-1)).ToString(),Unix(times.Max().AddDays(1)).ToString()),Json)??throw new InvalidDataException("MT5-Daten fehlen.");if(result.Symbol!=symbol||result.Bars.Count==0)throw new InvalidDataException("Symbol oder Daten passen nicht.");
        if(result.Bars.Any(b=>b.Open<=0||b.Close<=0||b.Low>Math.Min(b.Open,b.Close)||b.High<Math.Max(b.Open,b.Close)))throw new InvalidDataException("MT5 lieferte inkonsistente OHLC-Werte.");
        r.MarketSymbol=symbol;r.MarketLoadedSymbol=symbol;r.MarketBars=result.Bars.OrderBy(b=>b.Utc).ToList();r.MarketSourceTimeIsBroker=true;r.MarketSource="MT5 "+Path.GetDirectoryName(terminal)!.Split(Path.DirectorySeparatorChar).Last()+" · "+symbol+(result.Custom?" (Custom)":"");r.MarketStatus="";r.MarketEnabled=true;r.MarketTerminalPath=terminal;r.MarketMt5Symbol=symbol;r.MarketIsCustom=result.Custom;
    }
}
