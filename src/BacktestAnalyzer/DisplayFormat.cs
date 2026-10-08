namespace BacktestAnalyzer;
public static class DisplayFormat
{
 public static string Metadata(string name,string value,Report r)
 {
  string k=Parser.Key(name);
  try {
   if(k.Contains("halte")||k.Contains("holding")){var durationMatch=System.Text.RegularExpressions.Regex.Match(value.Trim(),@"^(\d+):(\d{2}):(\d{2})$");if(durationMatch.Success)return Stats.Duration(double.Parse(durationMatch.Groups[1].Value)*3600+double.Parse(durationMatch.Groups[2].Value)*60+double.Parse(durationMatch.Groups[3].Value));}
   bool coefficient=k.Contains("korrelation")||k.Contains("correlation")||k.Contains("faktor")||k.Contains("factor")||k.Contains("ratio")||k.Contains("score")||k.Contains("ahpr")||k.Contains("ghpr")||k.Contains("lrstandard");
   bool monetary=!coefficient&&(k.Contains("gewinn")||k.Contains("verlust")||k.Contains("profit")||k.Contains("loss")||k.Contains("deposit")||k.Contains("einlage")||k.Contains("ruckgang")||k.Contains("drawdown")||k.Contains("balance")||k.Contains("kontostand")||k.Contains("erwartetesergebnis")||k.Contains("expectedpayoff"));
   var percentage=System.Text.RegularExpressions.Regex.Match(value.Trim(),@"^([-+]?\d[\d .,]*)%$");if(percentage.Success)return Parser.Number(percentage.Groups[1].Value).ToString("N2",Localization.Culture)+" %";
   var compound=System.Text.RegularExpressions.Regex.Match(value.Trim(),@"^([-+]?\d[\d .,]*)(%)?\s*\(([-+]?\d[\d .,]*)(%)?\)$");
   if(compound.Success){decimal first=Parser.Number(compound.Groups[1].Value),second=Parser.Number(compound.Groups[3].Value);bool firstPercent=compound.Groups[2].Success,secondPercent=compound.Groups[4].Success;bool countFirst=k.Contains("folge")&&!k.Contains("anzahl")&&!k.Contains("aufeinander");return (firstPercent?first.ToString("N2",Localization.Culture)+" %":countFirst||(!monetary&&!coefficient)?first.ToString("N0",Localization.Culture):monetary?Money(first,r):first.ToString("N2",Localization.Culture))+" ("+(secondPercent?second.ToString("N2",Localization.Culture)+" %":monetary&&(firstPercent||countFirst)?Money(second,r):second.ToString("N0",Localization.Culture))+")";}
   if(System.Text.RegularExpressions.Regex.IsMatch(value.Trim(),@"^[-+]?\d[\d .,]*$")){
    decimal n=Parser.Number(value);
    if(k.Contains("ticks")||k.Contains("bars")||k.Contains("balken")||k.Contains("anzahl")||k.Contains("totaltrades")||k.Contains("totaldeals"))return n.ToString("N0",Localization.Culture);
    if(monetary)return Money(n,r);
    return n.ToString(coefficient?"N3":"0.####",Localization.Culture);
   }
  }catch(FormatException){}return value;
 }
 public static string Currency(Report r)=>r.Find("Währung","Currency") switch{"USD"=>"$","EUR"=>"€","GBP"=>"£",var s=>s};
 public static string Money(decimal v,Report r)=>v.ToString("N2",System.Globalization.CultureInfo.GetCultureInfo(Localization.English?"en-US":"de-DE"))+" "+Currency(r);
 public static string Hour(int h)=>Localization.English?(h==24?"12 AM (+1 day)":new DateTime(2000,1,1,h,0,0).ToString("h tt",Localization.Culture)):h.ToString("00")+" Uhr";
 public static string Time(DateTime t)=>t.ToString(Localization.English?"dd.MM.yyyy h:mm:ss tt":"dd.MM.yyyy HH:mm:ss",Localization.Culture);
 public static string DurationBasis(Report r)=>r.ExcludeWeekends?"Handelsdauer · geschlossene Wochenendtage und pauschale Nachtpause abgezogen":"Kalenderdauer";
}
