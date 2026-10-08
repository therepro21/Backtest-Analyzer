using System.Text.RegularExpressions;
namespace BacktestAnalyzer;
public static class SymbolAliases
{
 public static readonly string[] Instruments=("EURUSD GBPUSD USDJPY USDCHF USDCAD AUDUSD NZDUSD EURGBP EURJPY EURCHF EURAUD EURCAD EURNZD GBPJPY GBPCHF GBPAUD GBPCAD GBPNZD AUDJPY AUDCHF AUDCAD AUDNZD NZDJPY NZDCHF NZDCAD CADJPY CADCHF CHFJPY USDCNH USDSEK USDNOK USDMXN XAUUSD XAUEUR XAUAUD XAUGBP XAGUSD XAGEUR XPTUSD XPDUSD BTCUSD ETHUSD XRPUSD LTCUSD BCHUSD ADAUSD SOLUSD DOTUSD AVAXUSD DASHUSD").Split(' ');
 static readonly Dictionary<string,string> Names=new(StringComparer.OrdinalIgnoreCase){["GOLD"]="XAUUSD",["GOLDOZ"]="XAUUSD",["GOLDEURO"]="XAUEUR",["SILVER"]="XAGUSD",["PLATINUM"]="XPTUSD",["PALLADIUM"]="XPDUSD",["AVXUSD"]="AVAXUSD",["DSHUSD"]="DASHUSD",["LNKUSD"]="LINKUSD"};
 public static string Canonical(string name)
 {
  var s=name.Trim().ToUpperInvariant();s=Regex.Replace(s,@"(?:[ _./#-]*(?:TDS|TICKSTORY|DUKASCOPY|TRUEFX|REALTICKS|TICKS|CUSTOM|99(?:[.,]9)?|100|20\d{2}))+\s*$","");
  s=Regex.Replace(s,@"(?:MICRO|M#|#|-T|-Z)$","");s=Regex.Replace(s,@"[\s_./-]","");
  if(Names.TryGetValue(s,out var alias))return alias;if(Instruments.Contains(s))return s;
  foreach(var suffix in new[]{"M","C","R","Z","PRO","ECN"})if(s.EndsWith(suffix)){var candidate=s[..^suffix.Length];if(Instruments.Contains(candidate))return candidate;if(Names.TryGetValue(candidate,out alias))return alias;}
  return s;
 }

}
