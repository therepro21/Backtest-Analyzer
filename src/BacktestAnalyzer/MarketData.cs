using System.Buffers.Binary;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpCompress.Compressors.LZMA;
namespace BacktestAnalyzer;

public sealed record MarketBar(DateTime Utc,decimal Open,decimal High,decimal Low,decimal Close);
public static class MarketData
{
    static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(25)};
    public static readonly string[] Symbols={"XAUUSD","XAGUSD","EURUSD","GBPUSD","USDJPY","USDCHF","AUDUSD","USDCAD","NZDUSD","BTCUSD","ETHUSD"};
    public static string Suggest(string brokerSymbol){var s=brokerSymbol.ToUpperInvariant();if(s.Contains("GOLD")||s.Contains("XAU"))return "XAUUSD";if(s.Contains("SILVER")||s.Contains("XAG"))return "XAGUSD";return Symbols.FirstOrDefault(s.Contains)??"";}
    public static int Offset(Report r,DateTime utc)
    {
        if(r.BrokerTimeRule=="EU")return r.BrokerWinterOffsetMinutes+(TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time").IsDaylightSavingTime(DateTime.SpecifyKind(utc,DateTimeKind.Utc))?60:0);
        if(r.BrokerTimeRule=="US")return r.BrokerWinterOffsetMinutes+(TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").IsDaylightSavingTime(DateTime.SpecifyKind(utc,DateTimeKind.Utc))?60:0);
        return r.BrokerUtcOffsetMinutes;
    }
    public static DateTime BrokerTime(Report r,DateTime utc)=>DateTime.SpecifyKind(utc,DateTimeKind.Unspecified).AddMinutes(r.MarketSourceTimeIsBroker?0:Offset(r,utc));
    public static int CalculateOffset(DateTime localClock,DateTime serverClock,TimeZoneInfo localZone)
    {
        var local=DateTime.SpecifyKind(localClock,DateTimeKind.Unspecified);if(localZone.IsInvalidTime(local)||localZone.IsAmbiguousTime(local))throw new ArgumentException("Lokale Uhrzeit liegt im Sommerzeitwechsel; einen eindeutigen Zeitpunkt verwenden.");
        double diff=(serverClock-TimeZoneInfo.ConvertTimeToUtc(local,localZone)).TotalMinutes;
        var rounded=(int)Math.Round(diff/15)*15;if(Math.Abs(diff-rounded)>2||rounded < -720||rounded>840)throw new ArgumentException("Datum/Uhrzeiten prüfen: UTC-Offset muss zwischen -12 und +14 Stunden liegen.");return rounded;
    }
    sealed class Cached {public List<MarketBar>? Input;public string Key="";public List<MarketBar> Bars=new();}
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Report,Cached> Aggregates=new();
    public static List<MarketBar> Aggregate(Report r)
    {
        var cache=Aggregates.GetOrCreateValue(r);var key=$"{r.MarketInterval}/{r.BrokerTimeRule}/{r.BrokerUtcOffsetMinutes}/{r.BrokerWinterOffsetMinutes}/{r.MarketBars.Count}/{r.MarketSourceTimeIsBroker}";
        if(cache.Input!=r.MarketBars||cache.Key!=key){cache.Input=r.MarketBars;cache.Key=key;cache.Bars=AggregateCore(r);}return cache.Bars;
    }
    static List<MarketBar> AggregateCore(Report r)
    {
        int hours=r.MarketInterval=="H4"?4:r.MarketInterval=="D1"?24:1;
        return r.MarketBars.OrderBy(b=>b.Utc).GroupBy(b=>{var t=BrokerTime(r,b.Utc);return t.Date.AddHours(t.Hour/hours*hours);}).Select(g=>new MarketBar(g.Key,g.First().Open,g.Max(b=>b.High),g.Min(b=>b.Low),g.Last().Close)).ToList();
    }
    public static async Task Load(Report r,IProgress<string>? progress=null,CancellationToken token=default)
    {
        var symbol=r.MarketSymbol.Trim().ToUpperInvariant();if(!Regex.IsMatch(symbol,"^[A-Z0-9]{3,20}$"))throw new ArgumentException("Datenquellen-Symbol muss aus Buchstaben/Ziffern bestehen.");if(!Symbols.Contains(symbol))throw new ArgumentException("Symbol online noch nicht unterstützt. CSV mit Preisen ohne Skalierung importieren.");
        var times=r.Deals.Select(d=>d.Time).Concat(r.Balances.Select(b=>b.Time)).ToList();if(times.Count==0)throw new InvalidOperationException("Kein Testzeitraum vorhanden.");
        var first=times.Min().AddHours(-14);var last=times.Max().AddHours(12);var month=new DateTime(first.Year,first.Month,1);var bars=new List<MarketBar>();var missing=new List<string>();
        var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Backtest-Analyzer","MarketCache",symbol);Directory.CreateDirectory(folder);
        for(;month<=last;month=month.AddMonths(1))
        {
            token.ThrowIfCancellationRequested();progress?.Report(symbol+" · "+month.ToString("yyyy-MM"));var file=Path.Combine(folder,month.ToString("yyyy-MM")+".json");
            if(File.Exists(file)&&month.AddMonths(1)<DateTime.UtcNow.Date){bars.AddRange(JsonSerializer.Deserialize<List<MarketBar>>(await File.ReadAllTextAsync(file,token))??new());continue;}
            var url=$"https://datafeed.dukascopy.com/datafeed/{symbol}/{month.Year}/{month.Month-1:00}/BID_candles_hour_1.bi5";
            using var request=new HttpRequestMessage(HttpMethod.Get,url);request.Headers.UserAgent.ParseAdd("Backtest-Analyzer/1.0");using var response=await Http.SendAsync(request,token);
            if(response.StatusCode==System.Net.HttpStatusCode.NotFound){missing.Add(month.ToString("yyyy-MM"));continue;}
            response.EnsureSuccessStatusCode();var bytes=await response.Content.ReadAsByteArrayAsync(token);var decoded=Decode(bytes,month,Scale(symbol));if(decoded.Count==0){missing.Add(month.ToString("yyyy-MM"));continue;}
            await File.WriteAllTextAsync(file,JsonSerializer.Serialize(decoded),token);bars.AddRange(decoded);await Task.Delay(250,token);
        }
        if(bars.Count==0)throw new InvalidDataException("Keine Kurse verfügbar. Symbol prüfen oder CSV importieren.");
        r.MarketSourceTimeIsBroker=false;r.MarketBars=bars.Where(b=>BrokerTime(r,b.Utc)>=times.Min().AddDays(-1)&&BrokerTime(r,b.Utc)<=times.Max().AddDays(1)).ToList();r.MarketLoadedSymbol=symbol;r.MarketSource="Dukascopy BID · H1 UTC";r.MarketStatus=missing.Count==0?"":"Fehlende Monate: "+string.Join(", ",missing);
    }
    static decimal Scale(string s)=>s is "XAUUSD" or "XAGUSD"?1000:s.EndsWith("JPY")?1000:s is "BTCUSD" or "ETHUSD"?1000:100000;
    public static List<MarketBar> Decode(byte[] bytes,DateTime month,decimal scale)
    {
        if(bytes.Length<13)throw new InvalidDataException("Unvollständiger LZMA-Datenblock.");using var input=new MemoryStream(bytes,13,bytes.Length-13);long size=BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(5,8));using var decoder=LzmaStream.Create(bytes[..5],input,bytes.Length-13,size);using var output=new MemoryStream();decoder.CopyTo(output);var data=output.ToArray();if(data.Length%24!=0)throw new InvalidDataException("Ungültiges OHLC-Datenformat.");var bars=new List<MarketBar>();
        for(int i=0;i<data.Length;i+=24){uint U(int offset)=>BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(i+offset,4));var t=DateTime.SpecifyKind(month,DateTimeKind.Utc).AddSeconds(U(0));decimal o=U(4)/scale,c=U(8)/scale,l=U(12)/scale,h=U(16)/scale;if(t<month||t>=month.AddMonths(1)||o<=0||c<=0||l>Math.Min(o,c)||h<Math.Max(o,c))throw new InvalidDataException("Ungültige Kurswerte.");bars.Add(new(t,o,h,l,c));}return bars;
    }
    public static void ImportCsv(Report r,string file,int sourceUtcOffsetMinutes)
    {
        var result=new List<MarketBar>();int line=0;foreach(var raw in File.ReadLines(file)){line++;var fields=raw.Trim().Trim('\uFEFF').Split(raw.Contains(';')?';':',').Select(s=>s.Trim(' ','"')).ToArray();if(fields.Length<5)continue;bool parsed=DateTime.TryParseExact(fields[0],new[]{"yyyy-MM-dd HH:mm:ss","yyyy.MM.dd HH:mm:ss","yyyy-MM-ddTHH:mm:ss","dd.MM.yyyy HH:mm:ss.fff","dd.MM.yyyy HH:mm:ss"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var t);if(!parsed&&line==1)continue;if(!parsed)throw new InvalidDataException("CSV-Zeit ungültig, Zeile "+line);decimal N(int i)=>decimal.Parse(fields[i],NumberStyles.Float,CultureInfo.InvariantCulture);var b=new MarketBar(DateTime.SpecifyKind(t.AddMinutes(-sourceUtcOffsetMinutes),DateTimeKind.Utc),N(1),N(2),N(3),N(4));if(b.Open<=0||b.Low>Math.Min(b.Open,b.Close)||b.High<Math.Max(b.Open,b.Close))throw new InvalidDataException("OHLC ungültig, Zeile "+line);result.Add(b);}
        if(result.Count==0)throw new InvalidDataException("CSV enthält keine Kurse (Time,Open,High,Low,Close).");r.MarketSourceTimeIsBroker=false;r.MarketBars=result.OrderBy(b=>b.Utc).DistinctBy(b=>b.Utc).ToList();r.MarketLoadedSymbol=r.MarketSymbol;r.MarketSource="CSV: "+Path.GetFileName(file);r.MarketStatus="";
    }
    public static string Description(Report r)=>r.MarketBrokerSymbol+" → "+r.MarketSymbol+" · "+(r.MarketInterval=="D1"&&!Localization.English?"T1":r.MarketInterval)+" · "+r.MarketSource+(r.MarketSourceTimeIsBroker?" · Brokerzeiten direkt":" · UTC"+(r.BrokerUtcOffsetMinutes>=0?"+":"")+(r.BrokerUtcOffsetMinutes/60d).ToString("0.##")+" / "+r.BrokerTimeRule+(r.BrokerTimeConfirmed?"":" (Zeitregel unbestätigt)"));
}
