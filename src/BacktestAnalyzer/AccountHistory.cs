namespace BacktestAnalyzer;
public static class AccountHistory
{
 public static bool External(AccountBooking b)=>b.Kind is "initial" or "balance" or "credit" or "bonus";
 public static string Kind(string side,string comment)=>side=="balance"&&System.Text.RegularExpressions.Regex.IsMatch(comment.Trim(),@"^(commission|fees?|kommission|gebühr)(\s|:|$)",System.Text.RegularExpressions.RegexOptions.IgnoreCase)?"commission":side;
 public static decimal TradingNet(Report r)=>r.Deals.Sum(d=>d.Net)+r.AccountBookings.Where(b=>!External(b)&&(!r.AxisStart.HasValue||b.Time>=r.AxisStart)&&(!r.AxisEnd.HasValue||b.Time<r.AxisEnd)).Sum(b=>b.Amount);
 public static void Complete(Report r)
 {
  var timeline=r.Deals.Select(d=>(d.Time,d.Net,d.Balance)).Concat(r.AccountBookings.Select(b=>(b.Time,b.Kind=="credit"?0:b.Amount,b.Balance))).OrderBy(q=>q.Time).ToList();
  if(timeline.Count==0)return;
  bool initialExported=r.Find("Initial Deposit","Ersteinlage","Ersteinzahlung","Anfangseinzahlung").Length>0;
  if(!initialExported){var first=timeline.FirstOrDefault(q=>q.Balance.HasValue);if(first.Balance.HasValue)r.InitialDeposit=first.Balance.Value-timeline.Where(q=>q.Time<=first.Time).Sum(q=>q.Item2);else r.InitialDeposit=0;}
  var seed=r.AccountBookings.FirstOrDefault(b=>b.Kind=="balance"&&b.Amount>0&&(!r.Deals.Any()||b.Time<=r.Deals.Min(d=>d.Time)));
  decimal rawOpening=r.InitialDeposit;
  if(seed!=null&&((initialExported&&seed.Amount==r.InitialDeposit)||(!initialExported&&r.InitialDeposit==0))){int at=r.AccountBookings.IndexOf(seed);r.AccountBookings[at]=seed with{Kind="initial"};r.InitialDeposit=seed.Amount;rawOpening=0;}
  if(r.Balances.Count==0){decimal balance=rawOpening;r.Balances.Add((timeline[0].Time,balance));foreach(var group in timeline.GroupBy(q=>q.Time)){balance+=group.Sum(q=>q.Item2);r.Balances.Add((group.Key,balance));}}
  else if(r.Balances[0].Time>=timeline[0].Time)r.Balances.Insert(0,(timeline[0].Time,rawOpening));
 }
 public static string Label(AccountBooking b)=>b.Kind switch {"initial"=>"Ersteinzahlung","balance"=>b.Amount>=0?"Einzahlung":"Auszahlung","credit"=>"Kredit / Bonus","commission"=>"Kommission / Gebühren",_=>"Kontobuchung: "+b.Kind};
 public static decimal Flows(Report r,DateTime after,DateTime through,bool equity=false)=>r.AccountBookings.Where(b=>External(b)&&b.Kind!="initial"&&(equity||b.Kind!="credit")&&b.Time>after&&b.Time<=through).Sum(b=>b.Amount);
 // Remove external bookings from the result line; raw balances remain unchanged.
 public static List<(DateTime Time,decimal Balance)> Performance(Report r)=>r.Balances.OrderBy(q=>q.Time).Select(q=>(q.Time,(q.Balance==0&&r.AccountBookings.Any(b=>b.Kind=="initial"&&b.Time==q.Time)?r.InitialDeposit:q.Balance)-Flows(r,r.AxisStart??DateTime.MinValue,q.Time))).ToList();
 public static List<(DateTime Time,decimal Balance,decimal Equity,decimal DepositLoad)> AdjustedEquity(Report r)=>r.EquityPoints.OrderBy(q=>q.Time).Select(q=>{decimal flow=Flows(r,r.AxisStart??DateTime.MinValue,q.Time,true);return(q.Time,(q.Balance==0&&r.AccountBookings.Any(b=>b.Kind=="initial"&&b.Time==q.Time)?r.InitialDeposit:q.Balance)-Flows(r,r.AxisStart??DateTime.MinValue,q.Time),q.Equity-flow,q.DepositLoad);}).ToList();
 public static List<double> DailyReturns(Report r)
 {
  var points=(r.EquityPoints.Count>1?r.EquityPoints.Select(q=>(q.Time,q.Equity)):r.Balances.Select(q=>(q.Time,q.Balance))).OrderBy(q=>q.Time).ToList();var daily=points.GroupBy(q=>q.Time.Date).Select(g=>g.Last()).ToList();var result=new List<double>();
  for(int i=1;i<daily.Count;i++){var a=daily[i-1];var b=daily[i];double seconds=Math.Max(1,(b.Time-a.Time).TotalSeconds);var flows=r.AccountBookings.Where(q=>External(q)&&(r.EquityPoints.Count>1||q.Kind!="credit")&&q.Time>a.Time&&q.Time<=b.Time).ToList();decimal weighted=flows.Sum(q=>q.Amount*(decimal)((b.Time-q.Time).TotalSeconds/seconds));decimal denominator=a.Item2+weighted;if(denominator>0)result.Add((double)((b.Item2-a.Item2-flows.Sum(q=>q.Amount))/denominator));}return result;
 }
}
