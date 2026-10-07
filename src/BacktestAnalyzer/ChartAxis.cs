using System.Globalization;
namespace BacktestAnalyzer;
public sealed record ChartAxis(double Low,double High,double Step)
{
    public IEnumerable<double> Ticks {get {for(double v=Low;v<=High+Step*.001;v+=Step)yield return Math.Abs(v)<Step*.001?0:v;}}
    public static ChartAxis Nice(double low,double high,int intervals=4)
    {
        if(high<=low)high=low+1;double raw=(high-low)/intervals,power=Math.Pow(10,Math.Floor(Math.Log10(raw))),n=raw/power;
        double step=(n<=1?1:n<=2?2:n<=2.5?2.5:n<=5?5:10)*power;
        return new(Math.Floor(low/step)*step,Math.Ceiling(high/step)*step,step);
    }
    public static string Number(double value)=>value.ToString(Math.Abs(value)>=1?"N0":"0.##",CultureInfo.GetCultureInfo("de-DE"));
    public static string Duration(double seconds)=>seconds==0?"0":seconds<3600?(seconds/60).ToString("0.#",CultureInfo.GetCultureInfo("de-DE"))+" min":seconds<86400?(seconds/3600).ToString("0.#",CultureInfo.GetCultureInfo("de-DE"))+" h":(seconds/86400).ToString("0.#",CultureInfo.GetCultureInfo("de-DE"))+" Tage";
}
