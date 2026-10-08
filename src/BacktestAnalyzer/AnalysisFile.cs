using System.IO.Compression;
using System.Text.Json;
namespace BacktestAnalyzer;
public static class AnalysisFile
{
 static readonly JsonSerializerOptions Options=new(){IncludeFields=true};
 public static Report Load(string path,bool profitModel=true){if(!path.EndsWith(".bta",StringComparison.OrdinalIgnoreCase))return Parser.Load(path,profitModel);using var file=File.OpenRead(path);using var zip=new GZipStream(file,CompressionMode.Decompress);var r=JsonSerializer.Deserialize<Report>(zip,Options)??throw new InvalidDataException("Empty analysis");if(r.Deals.Count==0&&r.Trades.Count==0&&r.AccountBookings.Count==0)throw new InvalidDataException("Analysis contains no trades");return r;}
 public static void Save(Report r,string path){using var file=File.Create(path);using var zip=new GZipStream(file,CompressionLevel.Optimal);JsonSerializer.Serialize(zip,r,Options);}
}
