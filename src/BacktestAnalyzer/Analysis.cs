using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace BacktestAnalyzer;

public sealed record Deal(string Id, string Order, string Position, DateTime Time, string Symbol, string Side, string Entry, decimal Volume, decimal Price, decimal Profit, decimal Commission, decimal Swap, decimal Fee, decimal? Balance, string Comment) { public decimal Net => Profit + Commission + Swap + Fee; }
public sealed class Trade
{
    public string Id { get; set; } = ""; public string Symbol { get; set; } = ""; public string Side { get; set; } = "";
    public DateTime Open { get; set; } public DateTime? Close { get; set; } public DateTime? LastExit {get;set;}
    public decimal Volume { get; set; } public decimal Remaining { get; set; } public decimal Net { get; set; }
    public decimal OpenPrice { get; set; } public string Position { get; set; } = "";
    public bool Estimated { get; set; } public string MatchMethod {get;set;}="eindeutige History"; public int Exits { get; set; } public double WeightedSeconds { get; set; }
    public double Seconds => Close.HasValue ? (Close.Value - Open).TotalSeconds : 0;
    public string Duration => Close.HasValue ? Stats.Duration(Seconds) : "offen";
    public string OpenText => Open.ToString("yyyy-MM-dd HH:mm:ss"); public string CloseText => Close?.ToString("yyyy-MM-dd HH:mm:ss") ?? "offen";
    public string Quality => Estimated ? "FIFO-Schätzung" : MatchMethod;
    public bool ModelDependent => Estimated || MatchMethod!="eindeutige History";
    public string Result => Net.ToString("N2", CultureInfo.GetCultureInfo("de-DE"));
}
public sealed class Report
{
    public string Source { get; set; } = ""; public string Hash { get; set; } = ""; public string Platform { get; set; } = "";
    public Dictionary<string, string> Metadata { get; set; } = new(); public List<Deal> Deals { get; set; } = new();
    public List<string> InputParameters { get; set; } = new();
    public List<Trade> Trades { get; set; } = new(); public List<string> Warnings { get; set; } = new(); public decimal InitialDeposit { get; set; }
    public List<(DateTime Time, decimal Balance)> Balances { get; set; } = new();
    public string Strategy => Find("Expertenprogramm", "Expert", "Expert Advisor") is { Length: > 0 } s ? s : Path.GetFileNameWithoutExtension(Source);
    public string Find(params string[] names) { foreach(var n in names) foreach(var p in Metadata) if(Parser.Key(p.Key)==Parser.Key(n)) return p.Value; return ""; }
    public List<Trade> Closed => Trades.Where(t=>t.Close.HasValue).ToList();
}
public static class Parser
{
    public static string Key(string s) => Regex.Replace(s.ToLowerInvariant().Normalize(NormalizationForm.FormD), @"[^a-z0-9]", "");
    public static decimal Number(string s)
    {
        s=s.Trim().Replace("\u00a0", "").Replace("\u202f", "").Replace(" ", "").Replace("'", "").Replace("−", "-");
        if(s.Length==0 || s=="-") return 0;
        decimal factor=1; if(s.EndsWith("K",StringComparison.OrdinalIgnoreCase)){factor=1000;s=s[..^1];} else if(s.EndsWith("M",StringComparison.OrdinalIgnoreCase)){factor=1000000;s=s[..^1];} else if(s.EndsWith("B",StringComparison.OrdinalIgnoreCase)){factor=1000000000;s=s[..^1];}
        if(s.Contains(',')&&s.Contains('.')) { if(s.LastIndexOf(',')>s.LastIndexOf('.')) s=s.Replace(".", "").Replace(',', '.'); else s=s.Replace(",", ""); } else if(s.Contains(',')) s=s.Replace(',', '.');
        return decimal.Parse(s,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent,CultureInfo.InvariantCulture)*factor;
    }
    static bool Date(string s,out DateTime dt)=>DateTime.TryParseExact(s.Trim(),new[]{"yyyy.MM.dd HH:mm:ss","yyyy.MM.dd HH:mm","yyyy-MM-dd HH:mm:ss","yyyy-MM-ddTHH:mm:ss"},CultureInfo.InvariantCulture,DateTimeStyles.None,out dt);
    public static Report Load(string path,bool useProfitMatching=true)
    {
        var bytes=File.ReadAllBytes(path); if(bytes.Length>100_000_000)throw new InvalidDataException("Datei ist größer als 100 MB.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string html;
        if(bytes.Length>1&&bytes[0]==255&&bytes[1]==254)html=Encoding.Unicode.GetString(bytes);
        else if(bytes.Length>1&&bytes[0]==254&&bytes[1]==255)html=Encoding.BigEndianUnicode.GetString(bytes);
        else {try{html=new UTF8Encoding(false,true).GetString(bytes);}catch(DecoderFallbackException){html=Encoding.GetEncoding(1252).GetString(bytes);}}
        var doc=new HtmlDocument();doc.LoadHtml(html);
        var rows=(doc.DocumentNode.SelectNodes("//tr")??new HtmlNodeCollection(null)).Select(r=>r.SelectNodes("./td|./th")?.Select(c=>HtmlEntity.DeEntitize(c.InnerText).Trim()).ToArray()??Array.Empty<string>()).ToList();
        var report=new Report{Source=Path.GetFullPath(path),Hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()};
        foreach(var row in rows)for(int i=0;i<row.Length-1;i++)if(row[i].EndsWith(':'))report.Metadata.TryAdd(row[i].TrimEnd(':'),row[i+1]);
        bool readingInputs=false;
        foreach(var row in rows)
        {
            if(row.Length>1 && new[]{"eingaben","inputs","parameters"}.Contains(Key(row[0])))
            { readingInputs=true; if(row[1].Contains('='))report.InputParameters.Add(row[1]); continue; }
            if(!readingInputs)continue;
            if(row.Length>1 && row[0].Length==0 && row[1].Contains('='))report.InputParameters.Add(row[1]);
            else readingInputs=false;
        }
        if(report.InputParameters.Count>0)
        {
            var inputKey=report.Metadata.Keys.FirstOrDefault(k=>new[]{"eingaben","inputs","parameters"}.Contains(Key(k)));
            if(inputKey is not null)report.Metadata[inputKey]=string.Join(Environment.NewLine,report.InputParameters);
        }
        var title=HtmlEntity.DeEntitize(doc.DocumentNode.SelectSingleNode("//title")?.InnerText??"");if(title.StartsWith("Strategy Tester:"))report.Metadata.TryAdd("Expert",title[16..].Trim());
        foreach(var row in rows.Where(r=>r.Length>=2&&!r.Any(x=>Date(x,out _))))for(int i=0;i<row.Length-1;i++)if(new[]{"Initial deposit","Total net profit","Profit factor","Modeling quality","Total trades"}.Contains(row[i],StringComparer.OrdinalIgnoreCase))report.Metadata.TryAdd(row[i],row[i+1]);
        // Preserve provenance of headline values. These are not an equity time series.
        var deposit=report.Find("Ersteinlage","Ersteinzahlung","Initial Deposit","Anfangseinzahlung","Initial deposit");
        if(deposit.Length>0){var m=Regex.Match(deposit,@"[-+]?\d[\d\s.,]*");if(m.Success)report.InitialDeposit=Number(m.Value);}
        Dictionary<string,int>? header=null; bool mt5=false;
        foreach(var row in rows)
        {
            var k=row.Select(Key).ToArray();
            if(k.Any(x=>x is "direction" or "richtung" or "entry")&&k.Any(x=>x is "deal" or "trade" or "transaktion")) {header=new();for(int i=0;i<k.Length;i++)header.TryAdd(k[i],i);mt5=true;report.Platform="MT5";continue;}
            if(k.Any(x=>x is "order" or "auftrag")&&k.Any(x=>x is "time" or "zeit")&&k.Any(x=>x is "type" or "typ")&&!k.Any(x=>x is "status" or "state")&&!mt5){header=new();for(int i=0;i<k.Length;i++)header.TryAdd(k[i],i);report.Platform="MT4";continue;}
            if(header==null)continue;
            string Get(params string[] aliases){foreach(var alias in aliases)if(header.TryGetValue(Key(alias),out var i)&&i<row.Length)return row[i];return "";}
            if(!Date(Get("Time","Zeit"),out var time))continue;
            try
            {
                var side=Get("Type","Typ").ToLowerInvariant();var id=Get("Deal","Trade","Transaktion","Order","Auftrag");
                var bal=Get("Balance","Kontostand");if(bal.Length>0)report.Balances.Add((time,Number(bal)));
                if(mt5)
                {
                    if(side is not ("buy" or "sell")) { if(side=="balance"&&report.InitialDeposit==0&&report.Deals.Count==0)report.InitialDeposit=Number(Get("Profit","Gewinn"));continue; }
                    var entry=Get("Direction","Richtung","Entry").ToLowerInvariant().Replace(" ", "");
                    if(entry is not ("in" or "out" or "in/out" or "inout" or "outby" or "out/by"))throw new InvalidDataException("Unbekannte Deal-Richtung: "+entry);
                    var d=new Deal(id,Get("Order","Auftrag"),Get("Position","Position ID","PositionId"),time,Get("Symbol"),side,entry,Number(Get("Volume","Volumen")),Number(Get("Price","Preis")),Number(Get("Profit","Gewinn")),Number(Get("Commission","Kommission")),Number(Get("Swap")),Number(Get("Fee","Gebühr","Gebuehr")),bal.Length>0?Number(bal):null,Get("Comment","Kommentar"));
                    if(d.Volume<=0)throw new InvalidDataException("Ungültiges Volumen");report.Deals.Add(d);
                }
                else
                {
                    if(side is "modify" or "delete" or "balance" or "credit")continue;
                    if(side is "buy" or "sell")report.Deals.Add(new Deal(id,id,id,time,Get("Symbol").Length>0?Get("Symbol"):report.Find("Symbol"),side,"in",Number(Get("Size","Volume","Lots","Volumen","Größe")),Number(Get("Price","Preis")),0,0,0,0,bal.Length>0?Number(bal):null,Get("Comment","Kommentar")));
                    else if(side is "close" or "s/l" or "t/p" or "closeby" or "close at stop")
                    {
                        var original=report.Deals.LastOrDefault(x=>x.Id==id&&x.Entry=="in");if(original==null){report.Warnings.Add("MT4-Ausstieg ohne Einstieg: "+id);continue;}
                        report.Deals.Add(new Deal(id,id,id,time,original.Symbol,original.Side=="buy"?"sell":"buy","out",Number(Get("Size","Volume","Lots","Volumen","Größe")),Number(Get("Price","Preis")),Number(Get("Profit","Gewinn")),Number(Get("Commission","Kommission")),Number(Get("Swap")),0,bal.Length>0?Number(bal):null,Get("Comment","Kommentar")));
                    }
                    else if(side.Length>0)report.Warnings.Add("MT4-Ereignis ausgelassen: "+side);
                }
            }
            catch(Exception ex)when(ex is FormatException or OverflowException or InvalidDataException){report.Warnings.Add("Zeile nicht auswertbar ("+time.ToString("s")+"): "+ex.Message);}
        }
        if(report.Deals.Count==0)throw new InvalidDataException("Keine unterstützte Deal-/Trade-Tabelle gefunden. Benötigt wird ein vollständiger MT4/MT5-HTML-Backtestbericht.");
        Reconstruct(report,useProfitMatching);
        if(report.Platform=="MT5"&&report.Deals.Any(d=>d.Position.Length==0))report.Warnings.Insert(0,"MT5-HTML enthält keine Position-IDs. Gleichzeitige Einstiege werden über ein P/L-Konsistenzmodell oder FIFO zugeordnet und entsprechend markiert. Das bleibt modellabhängig; bestätigte Positionszuordnung erfordert DEAL_POSITION_ID.");
        var expected=report.Find("Nettogewinn gesamt","Total Net Profit");if(expected.Length>0){try{var actual=report.Deals.Sum(x=>x.Net);if(Math.Abs(Number(expected)-actual)>.05m)report.Warnings.Add("Nettoergebnis weicht vom MT-Bericht ab. Kosten, ausgelassene Zeilen oder Kontobewegungen prüfen.");}catch(FormatException){}}
        report.Warnings.Add("Haltezeit: Kalenderzeit in Broker-Zeit, Entry-Lot bis Vollschluss. Bei Teilausstiegen zusätzlich volumengewichtete Haltedauer. Keine Zeitzonenumrechnung ohne Broker-Zeitzone.");
        report.Warnings.Add("Balance-Kurve wird aus den exportierten Kontoständen dargestellt. Equity, MAE und MFE werden nicht aus geschlossenen Trades geschätzt; vorhandene MT-Kennzahlen bleiben als Quelle gekennzeichnet.");
        return report;
    }
    public static void Reconstruct(Report r,bool useProfitMatching=true)
    {
        // Infer a stable linear profit factor ONLY from isolated full exits.
        // This is a consistency model, not a replacement for historical symbol specifications.
        var factors=new Dictionary<string,decimal>();var samples=new Dictionary<string,List<decimal>>();var inventory=new List<Deal>();var tainted=new HashSet<string>();
        foreach(var d in r.Deals.OrderBy(x=>x.Time))
        {
            if(d.Entry=="in"){inventory.Add(d);continue;}
            var candidates=inventory.Where(a=>a.Symbol==d.Symbol&&a.Side!=d.Side&&(d.Position.Length==0||a.Position==d.Position)).ToList();
            if(candidates.Count==1&&candidates[0].Volume==d.Volume&&!tainted.Contains(candidates[0].Id)&&d.Entry=="out")
            {
                var a=candidates[0];decimal delta=(d.Price-a.Price)*(a.Side=="buy"?1:-1);
                if(Math.Abs(delta)>.05m&&d.Profit!=0){decimal factor=d.Profit/(delta*d.Volume);if(factor>0){if(!samples.ContainsKey(d.Symbol))samples[d.Symbol]=new();samples[d.Symbol].Add(factor);}}
                inventory.Remove(a);
            }
            else {foreach(var a in candidates)tainted.Add(a.Id);decimal remaining=d.Volume;foreach(var a in candidates){if(remaining<=0)break;inventory.Remove(a);remaining-=a.Volume;}}
        }
        foreach(var group in samples){var values=group.Value.Order().ToArray();if(values.Length<5)continue;decimal median=values[values.Length/2];if(values.Count(v=>Math.Abs(v/median-1)<.002m)>values.Length*.95){decimal rounded=Math.Round(median,3);factors[group.Key]=rounded;}}
        if(useProfitMatching&&factors.Count>0)r.Warnings.Add("P/L-Konsistenzmodell aus isolierten Vollschließungen: "+string.Join(", ",factors.Select(f=>f.Key+" Faktor="+f.Value.ToString(CultureInfo.InvariantCulture)))+". Zuordnungen bleiben modellabhängig (keine bestätigten Position-IDs).");
        var open=new List<Trade>();var seen=new HashSet<string>();
        foreach(var d in r.Deals.OrderBy(x=>x.Time))
        {
            // Deal tickets are unique in MT5; MT4 reuses the order ticket for events.
            if(r.Platform=="MT5"&&!seen.Add(d.Id)){r.Warnings.Add("Doppelter Deal ausgelassen: "+d.Id);continue;}
            decimal volume=d.Volume;
            if(d.Entry!="in")
            {
                var candidates=open.Where(t=>t.Remaining>0&&t.Symbol==d.Symbol&&t.Side!=d.Side&&(d.Position.Length==0||t.Position==d.Position)).ToList();
                if(useProfitMatching&&candidates.Count>1&&d.Entry=="out"&&factors.TryGetValue(d.Symbol,out var factor))
                {
                    var matches=candidates.Where(t=>t.Remaining>=d.Volume&&Math.Abs((d.Price-t.OpenPrice)*(t.Side=="buy"?1:-1)*d.Volume*factor-d.Profit)<=.02m+d.Volume*factor*.001m).ToList();
                    if(matches.Count==1){var match=matches[0];candidates=new(){match};match.MatchMethod="P/L-Konsistenzmodell";}
                }
                bool ambiguous=candidates.Count>1;
                if(ambiguous)foreach(var t in candidates)t.Estimated=true;
                decimal exitVolume=Math.Min(volume,candidates.Sum(x=>x.Remaining));
                foreach(var t in candidates)
                {
                    if(volume<=0)break;decimal used=Math.Min(volume,t.Remaining);decimal denominator=d.Entry is "inout" or "in/out"?exitVolume:d.Volume;
                    decimal pnl=denominator>0?d.Profit*used/denominator:0;decimal costs=(d.Commission+d.Swap+d.Fee)*used/d.Volume;
                    t.Net+=pnl+costs;t.Remaining-=used;t.WeightedSeconds+=(double)used*(d.Time-t.Open).TotalSeconds;t.Exits++;t.LastExit=d.Time;
                    if(t.Remaining==0)t.Close=d.Time;volume-=used;
                }
                if(volume>0&&d.Entry is not ("inout" or "in/out")){r.Warnings.Add("Nicht zugeordnetes Ausstiegsvolumen: Deal "+d.Id+", "+volume.ToString(CultureInfo.InvariantCulture));continue;}
            }
            if(d.Entry=="in"||(d.Entry is "inout" or "in/out"&&volume>0))
            {
                if(r.Platform=="MT4")
                {
                    var continuation=open.Where(t=>t.Remaining==volume&&t.Symbol==d.Symbol&&t.Side==d.Side&&t.OpenPrice==d.Price&&t.LastExit==d.Time).ToList();
                    if(continuation.Count==1){var t=continuation[0];t.Position=d.Position;t.MatchMethod="MT4-Teilclose-Modell";r.Warnings.Add("MT4-Teilschließung: Ticketwechsel "+t.Id+" -> "+d.Id+" anhand Zeit, Preis und Restvolumen rekonstruiert (Modellannahme).");continue;}
                }
                var entry=new Trade{Id=d.Id,Symbol=d.Symbol,Side=d.Side,Open=d.Time,Volume=volume,Remaining=volume,OpenPrice=d.Price,Position=d.Position,Net=(d.Commission+d.Swap+d.Fee)*volume/d.Volume};
                open.Add(entry);r.Trades.Add(entry);
            }
        }
        if(r.Trades.Any(t=>t.Remaining>0))r.Warnings.Add(r.Trades.Count(t=>t.Remaining>0)+" offene Entry-Lots separat ausgewiesen; sie zählen nicht als abgeschlossene Haltezeiten.");
    }
}
public sealed class Stats
{
    public List<Trade> Trades { get; } public int Count=>Trades.Count; public double[] Durations { get; }
    public double Mean=>Count>0?Durations.Average():0; public double Median=>Quantile(.5);public double Min=>Count>0?Durations[0]:0;public double Max=>Count>0?Durations[^1]:0;
    public decimal Net=>Trades.Sum(t=>t.Net); public int Wins=>Trades.Count(t=>t.Net>0);public int Losses=>Trades.Count(t=>t.Net<0);
    public decimal? ProfitFactor=>Losses>0?Trades.Where(t=>t.Net>0).Sum(t=>t.Net)/Math.Abs(Trades.Where(t=>t.Net<0).Sum(t=>t.Net)):null;
    public double Std=>Count>1?Math.Sqrt(Durations.Sum(x=>Math.Pow(x-Mean,2))/(Count-1)):0;
    public double Weighted=>Trades.Sum(t=>(double)t.Volume)>0?Trades.Sum(t=>t.WeightedSeconds)/Trades.Sum(t=>(double)t.Volume):0;
    public Stats(IEnumerable<Trade> trades){Trades=trades.Where(t=>t.Close.HasValue).ToList();Durations=Trades.Select(t=>t.Seconds).Order().ToArray();}
    public double Quantile(double p){if(Count==0)return 0;var z=(Count-1)*p;var lo=(int)Math.Floor(z);var hi=(int)Math.Ceiling(z);return Durations[lo]+(Durations[hi]-Durations[lo])*(z-lo);}
    public static string Duration(double seconds){var t=TimeSpan.FromSeconds(Math.Max(0,seconds));return t.TotalDays>=1?$"{(int)t.TotalDays} d {t.Hours} h {t.Minutes} min":t.TotalHours>=1?$"{(int)t.TotalHours} h {t.Minutes} min {t.Seconds} s":$"{(int)t.TotalMinutes} min {t.Seconds} s";}
    public static readonly double[] Limits={300,900,1800,3600,7200,14400,28800,86400,double.PositiveInfinity};
    public static readonly string[] Bins={"<5m","5-15m","15-30m","30-60m","1-2h","2-4h","4-8h","8-24h",">=24h"};
    public int Bin(Trade t){for(int i=0;i<Limits.Length;i++)if(t.Seconds<Limits[i])return i;return Limits.Length-1;}
}
