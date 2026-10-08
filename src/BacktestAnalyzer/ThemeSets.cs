using System.Text.Json;
using System.Text.RegularExpressions;
namespace BacktestAnalyzer;
public static class ThemeSets
{
 static readonly string PathName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Backtest-Analyzer","color-sets.json");
 public static Dictionary<string,Dictionary<string,string>> Sets {get;private set;}=Read();public static string Active {get;set;}="Standard";
 static Dictionary<string,Dictionary<string,string>> Read(){try{return JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(File.ReadAllText(PathName))??new();}catch{return new();}}
 public static string Color(bool dark,string key,string fallback)=>Sets.TryGetValue(Active,out var set)&&set.TryGetValue((dark?"Dark.":"Light.")+key,out var value)&&Regex.IsMatch(value,"^#[0-9A-Fa-f]{6}$")?value:fallback;
 public static void Save(string name,Dictionary<string,string> colors){if(string.IsNullOrWhiteSpace(name)||colors.Values.Any(v=>!Regex.IsMatch(v,"^#[0-9A-Fa-f]{6}$")))throw new ArgumentException("Farben: #RRGGBB / Colors: #RRGGBB");Sets[name]=colors;Active=name;Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);File.WriteAllText(PathName,JsonSerializer.Serialize(Sets,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(PathName+".active",name);}
 public static void Activate(string name){Active=Sets.ContainsKey(name)?name:"Standard";Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);File.WriteAllText(PathName+".active",Active);}
 public static void Initialize(){if(File.Exists(PathName+".active"))Active=File.ReadAllText(PathName+".active");}
 public static string Blend(string a,string b,double weight){int Channel(int i)=>(int)Math.Round(Convert.ToInt32(a.Substring(i,2),16)*weight+Convert.ToInt32(b.Substring(i,2),16)*(1-weight));return $"#{Channel(1):X2}{Channel(3):X2}{Channel(5):X2}";}
}
