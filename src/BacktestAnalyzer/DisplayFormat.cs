namespace BacktestAnalyzer;
public static class DisplayFormat
{
 public static string Currency(Report r)=>r.Find("Währung","Currency") switch{"USD"=>"$","EUR"=>"€","GBP"=>"£",var s=>s};
 public static string Money(decimal v,Report r)=>v.ToString("N2",Localization.Culture)+" "+Currency(r);
 public static string Hour(int h)=>Localization.English?(h==24?"12 AM (+1 day)":new DateTime(2000,1,1,h,0,0).ToString("h tt",Localization.Culture)):h.ToString("00")+" Uhr";
 public static string Time(DateTime t)=>t.ToString(Localization.English?"dd.MM.yyyy h:mm:ss tt":"dd.MM.yyyy HH:mm:ss",Localization.Culture);
 public static string DurationBasis(Report r)=>r.ExcludeWeekends?"Handelsdauer · geschlossene Wochenendtage und pauschale Nachtpause abgezogen":"Kalenderdauer";
}
