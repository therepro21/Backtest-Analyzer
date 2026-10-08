namespace BacktestAnalyzer;
public sealed record BasketExcursion(DateTime Start,DateTime End,decimal MAE,decimal MFE,int Observations,double MaxGapSeconds,decimal Net);
public static class BasketExcursions
{
 public static List<BasketExcursion> Calculate(Report r)
 {
  if(r.SymbolAttributed||r.StrategyMode!="basket"||r.EquityPoints.Count==0)return new();
  var cycles=SeriesAnalysis.Selected(r).OrderBy(s=>s.Start).ToList();var points=(r.FullEquityPoints.Count>0?r.FullEquityPoints:r.EquityPoints).OrderBy(q=>q.Time).ToArray();var balances=r.Balances.OrderBy(q=>q.Time).ToArray();var history=(r.FullHistory.Count>0?r.FullHistory:r.Deals).OrderBy(d=>d.Time).ToArray();int di=0;var cash=history.Where(d=>d.Symbol.Length==0&&d.Net!=0).Select(d=>d.Time).Order().ToArray();
  var result=new List<BasketExcursion>();int pi=0,bi=0,ci=0;
  foreach(var cycle in cycles){while(pi<points.Length&&points[pi].Time<cycle.Start)pi++;while(bi+1<balances.Length&&balances[bi+1].Time<cycle.Start)bi++;while(ci<cash.Length&&cash[ci]<cycle.Start)ci++;if(ci<cash.Length&&cash[ci]<=cycle.End)continue;
   while(di+1<history.Length&&history[di+1].Time<cycle.Start)di++;decimal initial=history.Length>0?(history[di].Time<cycle.Start?history[di].Balance??r.InitialDeposit:(history[0].Balance??r.InitialDeposit)-history[0].Net):bi<balances.Length&&balances[bi].Time<cycle.Start?balances[bi].Balance:r.InitialDeposit;decimal min=0,max=0;int count=0;double gap=0;DateTime last=cycle.Start;
   for(int j=pi;j<points.Length&&points[j].Time<=cycle.End;j++){if(points[j].Time<cycle.Start)continue;decimal value=points[j].Equity-initial;min=Math.Min(min,value);max=Math.Max(max,value);count++;gap=Math.Max(gap,(points[j].Time-last).TotalSeconds);last=points[j].Time;}
   if(count==0)continue;min=Math.Min(min,cycle.Net);max=Math.Max(max,cycle.Net);gap=Math.Max(gap,(cycle.End!.Value-last).TotalSeconds);result.Add(new(cycle.Start,cycle.End.Value,-min,max,count,gap,cycle.Net));
  }return result;
 }
}
