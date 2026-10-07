using System.Security.Cryptography;
using System.Text;

namespace BacktestAnalyzer;

// Independently implemented little-endian reader. Layout research: docs/MT5-CACHE.md.
// Version 505 only: fail closed when section sizes/counts do not match exactly.
public static class TesterCache
{
    static string TestValue(string value)
    {
        var parts=value.Split("||");return parts.Length==4&&parts.All(p=>decimal.TryParse(p,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out _))?parts[0]:value;
    }
    public static Report Load(string path)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();
        var modified=File.GetLastWriteTimeUtc(path);
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);using var reader=new BinaryReader(stream,Encoding.Unicode);
        if(stream.Length<3096)throw new InvalidDataException("Tester-Cache zu kurz.");
        byte[] header=reader.ReadBytes(1456);
        int I(int offset)=>BitConverter.ToInt32(header,offset);
        string S(int offset,int bytes)=>Encoding.Unicode.GetString(header,offset,bytes).TrimEnd('\0');
        if(I(0)!=505||!S(132,64).StartsWith("SingleTestCache"))throw new InvalidDataException("Nicht unterstütztes MT5-Cacheformat. Derzeit wird nur die geprüfte Version 505 unterstützt.");
        int chars=I(1448);if(chars<0||chars>1000000)throw new InvalidDataException("Ungültige Parameterlänge.");
        int n=I(1404),orders=I(1408),positions=I(1412),states=I(1416);
        foreach(int count in new[]{n,orders,positions,states})if(count<0||count>10000000)throw new InvalidDataException("Ungültige Cache-Anzahl.");
        long summary=1456L+chars*2L,dealsOffset=summary+1640+4,ordersCount=dealsOffset+n*256L,positionsCount=ordersCount+4+orders*224L,statesCount=positionsCount+4+positions*64L;
        if(statesCount+4+states*32L!=stream.Length)throw new InvalidDataException("Cache-Abschnittsgrößen passen nicht zum geprüften Format.");
        foreach(var section in new[]{(summary+1640,n),(ordersCount,orders),(positionsCount,positions),(statesCount,states)}){stream.Position=section.Item1;if(reader.ReadInt32()!=section.Item2)throw new InvalidDataException("Cache-Zählerprüfung fehlgeschlagen.");}
        var report=new Report{Source=Path.GetFullPath(path),Platform="MT5",InitialDeposit=(decimal)BitConverter.ToDouble(header,1348),EquitySource=Path.GetFullPath(path)};
        report.Metadata["Expertenprogramm"]=S(432,128);report.Metadata["Symbol"]=S(944,64);report.Metadata["Währung"]=S(1284,64);report.Metadata["Server"]=S(816,128);report.Metadata["Cacheformat"]="505 (experimentell, strukturgeprüft)";
        report.Metadata["Tickmodell"]=I(1012) switch{0=>"Jeder Tick",1=>"1 Minute OHLC",2=>"Nur Eröffnungskurse",3=>"Mathematische Berechnungen",4=>"Jeder Tick anhand realer Ticks",_=>"Unbekannt ("+I(1012)+")"};
        report.Metadata["Verzögerung"]=I(1352)>0?I(1352)+" ms (fest)":I(1352)==0?"Keine Verzögerung":"Unbekannter / zufälliger Modus ("+I(1352)+")";
        report.Metadata["Kontomodell"]=I(1360) switch{0=>"Netting",1=>"Exchange",2=>"Hedging",_=>"Unbekannt"};
        report.Metadata["Hebel"]="1:"+I(1356);stream.Position=summary+40;report.Metadata["Balken"]=reader.ReadInt32().ToString("N0");report.Metadata["Ticks"]=reader.ReadInt32().ToString("N0");
        stream.Position=1456;string parameters=Encoding.Unicode.GetString(reader.ReadBytes(chars*2)).TrimEnd('\0');report.InputParameters=parameters.Split(new[]{"\r\n","\n"},StringSplitOptions.RemoveEmptyEntries).Select(p=>{int split=p.IndexOf('=');return split<0?p:p[..(split+1)]+TestValue(p[(split+1)..]);}).ToList();
        report.AvailableColumns.UnionWith(new[]{"commission","swap","position","balance","equity","depositload"});
        DateTime Date(long seconds){if(seconds<0||seconds>4102444800)throw new InvalidDataException("Ungültiger Cache-Zeitstempel.");return DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime,DateTimeKind.Unspecified);}
        decimal Number(double value){if(!double.IsFinite(value)||Math.Abs(value)>1e20)throw new InvalidDataException("Ungültiger Zahlenwert im Cache.");return (decimal)value;}
        decimal balance=0;stream.Position=dealsOffset;
        for(int index=0;index<n;index++)
        {
            var b=reader.ReadBytes(256);long time=BitConverter.ToInt64(b,16);decimal profit=Number(BitConverter.ToDouble(b,128)),commission=Number(BitConverter.ToDouble(b,136)),swap=Number(BitConverter.ToDouble(b,152));balance+=profit+commission+swap;
            int action=BitConverter.ToInt32(b,224),entry=BitConverter.ToInt32(b,228);report.Balances.Add((Date(time),balance));
            if(action is not (0 or 1))continue;
            if(entry is <0 or >3)throw new InvalidDataException("Unbekannte Deal-Richtung im Cache.");
            decimal contract=Number(BitConverter.ToDouble(b,240)),volume=BitConverter.ToUInt64(b,120)/(contract>0?contract*1000:100000000m);
            if(volume<=0||BitConverter.ToUInt64(b,248)==0)throw new InvalidDataException("Cache-Deal ohne gültiges Volumen oder Position-ID.");
            string symbol=Encoding.Unicode.GetString(b,24,64).TrimEnd('\0'),comment=Encoding.Unicode.GetString(b,160,64).TrimEnd('\0');
            decimal price=Number(BitConverter.ToDouble(b,entry==0?88:96));
            report.Deals.Add(new(BitConverter.ToUInt64(b,0).ToString(),BitConverter.ToUInt64(b,8).ToString(),BitConverter.ToUInt64(b,248).ToString(),Date(time),symbol,action==0?"buy":"sell",new[]{"in","out","inout","outby"}[entry],volume,price,profit,commission,swap,0,balance,comment));
        }
        stream.Position=statesCount+4;DateTime previous=default;
        for(int index=0;index<states;index++)
        {
            var time=Date(reader.ReadInt64());if(time<previous)throw new InvalidDataException("Cache-Kurvenpunkte sind nicht chronologisch.");previous=time;
            report.EquityPoints.Add((time,Number(reader.ReadDouble()),Number(reader.ReadDouble()),Number(reader.ReadDouble())));
        }
        if(report.EquityPoints.Count>0&&Math.Abs(report.EquityPoints[^1].Balance-balance)>.02m)throw new InvalidDataException("Cache-Endbalance stimmt nicht mit den Buchungen überein.");
        stream.Position=0;report.Hash=Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if(File.GetLastWriteTimeUtc(path)!=modified)throw new InvalidDataException("Tester-Cache wurde während des Imports geändert. Nach Testende erneut importieren.");
        report.ImportMilliseconds=timer.Elapsed.TotalMilliseconds;timer.Restart();Parser.Reconstruct(report,false);report.ReconstructionMilliseconds=timer.Elapsed.TotalMilliseconds;
        report.Metadata["Nettogewinn aus Cache-Deals (berechnet)"]=report.Deals.Sum(d=>d.Net).ToString("N2");
        if(report.Balances.Count>0)report.Metadata["Periode"]="M"+I(1008)+" ("+report.Balances[0].Time.ToString("yyyy.MM.dd")+" - "+report.Balances[^1].Time.ToString("yyyy.MM.dd")+")";
        report.Warnings.Add("MT5-Cacheformat 505 experimentell: Abschnittsgrößen, Zähler, Zeitfolge und Endbalance geprüft. Echte Deal-Position-IDs vorhanden. Aufstockungen derselben Position können bei Entry-Lot-Teilausstiegen weiterhin eine Modellzuordnung erfordern.");
        report.Warnings.Add("Equity/Balance/Kontobelastung aus gespeicherten Tester-Kurvenpunkten, keine vollständige Tickhistorie. Zeitangaben sind Tester-/Broker-Zeit ohne bekannte Zeitzone. Einzelne Schlussbuchungen dürfen nicht pauschal geglättet werden.");
        report.Warnings.Add("Separate Fee nicht im geprüften Cachelayout vorhanden. Weitere Kontobewegungsarten müssen für andere Strategien separat geprüft werden.");
        return report;
    }
    public static void Attach(Report report,string cachePath)
    {
        var cache=Load(cachePath);
        IEnumerable<string> Parameters(List<string> inputs)=>inputs.Select(p=>p.Trim()).Where(p=>p!="=").Select(p=>{int split=p.IndexOf('=');if(split<0)return p;string value=TestValue(p[(split+1)..].Trim());if(decimal.TryParse(value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var number))value=number.ToString("G29",System.Globalization.CultureInfo.InvariantCulture);return p[..split].Trim()+"="+value;});
        if(report.InputParameters.Count>0&&cache.InputParameters.Count>0&&!Parameters(report.InputParameters).SequenceEqual(Parameters(cache.InputParameters)))throw new InvalidDataException("Tester-Cache hat andere Strategieparameter als der Bericht.");
        if(cache.Deals.Count!=report.Deals.Count||cache.Deals.Where((d,i)=>d.Id!=report.Deals[i].Id||d.Time!=report.Deals[i].Time||d.Symbol!=report.Deals[i].Symbol||d.Side!=report.Deals[i].Side||d.Entry!=report.Deals[i].Entry||d.Volume!=report.Deals[i].Volume||Math.Abs(d.Net-report.Deals[i].Net)>.02m).Any())throw new InvalidDataException("Tester-Cache passt nicht vollständig zur importierten Deal-History.");
        report.Deals=cache.Deals;report.Trades=cache.Trades;report.EquityPoints=cache.EquityPoints;report.EquitySource=cache.Source;
        foreach(var key in new[]{"Tickmodell","Verzögerung","Cacheformat"})report.Metadata[key]=cache.Metadata[key];
        report.Warnings.RemoveAll(w=>w.Contains("keine Position-IDs")||w.Contains("Equity, MAE und MFE")||w.StartsWith("P/L-Konsistenzmodell")||w.StartsWith("Nicht zugeordnetes Ausstiegsvolumen"));report.Warnings.AddRange(cache.Warnings);
    }
}
