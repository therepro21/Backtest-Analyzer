namespace BacktestAnalyzer;
public enum ChartKind { Concentration,Streaks,Rolling,Underwater,Basket,Costs,Correlation,Risk,Excursion,MonteCarlo,Stress,Regimes,StrategyGroups, Histogram, Ecdf, Scatter, Balance, Equity, Drawdown, Monthly, Hourly, Weekday, Boxplot,SeriesHeatmap,SeriesCurve,EntryWins,EntryLosses,EntryLossRate,EntryHour,EntryWeekday,EntryMonth,ResultHour,ResultWeekday,ResultMonth }
public static class AdvancedPainter {public static string Title(ChartKind k)=>k.ToString();}
