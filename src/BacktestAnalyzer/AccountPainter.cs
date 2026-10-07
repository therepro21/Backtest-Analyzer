namespace BacktestAnalyzer;

public static class AccountPainter
{
    static List<(DateTime Time,decimal Balance)> Reduce(List<(DateTime Time,decimal Balance)> values,DateTime start,double span,double pixels)
    {
        if(values.Count<pixels*4)return values;var keep=new SortedSet<int>{0,values.Count-1};
        foreach(var group in Enumerable.Range(0,values.Count).GroupBy(i=>(int)((values[i].Time-start).TotalSeconds/span*pixels)))
        {keep.Add(group.MinBy(i=>values[i].Balance));keep.Add(group.MaxBy(i=>values[i].Balance));keep.Add(group.Last());}
        return keep.Select(i=>values[i]).ToList();
    }
    public static void Draw(ICanvas c,Report report,Palette palette,double x,double y,double width,double height,bool overlay=false)
    {
        c.Rect(x,y,width,height,palette.Surface);c.Text(overlay?"Balance und Equity · Orange: Equity unter Balance":"Balance und echter Equity-Drawdown",x+12,y+10,12,palette.Ink,true);
        c.Text(report.Find("Währung","Currency")+" · Balance: "+report.BalanceWindowSeconds+" s Buchungsfenster · Equity: gespeicherte Tester-Punkte",x+12,y+31,8,palette.Muted);
        var balances=AccountCurves.DisplayBalances(report);var fullBalances=balances;var dd=AccountCurves.EquityDrawdowns(report);
        if(balances.Count<2){c.Text("Keine ausreichende Balance-Zeitreihe vorhanden.",x+15,y+65,10,palette.Muted);return;}
        if(dd.Count<2)
        {
            c.Text("Echte Equity-Zeitreihe fehlt · Equity-DD nicht verfügbar",x+15,y+height-30,9,palette.Muted);
            double left0=x+100,right0=x+width-20,top0=y+60,bottom0=y+height-65;var start0=report.AxisStart??balances[0].Time;var end0=report.AxisEnd??balances[^1].Time;double span0=Math.Max(1,(end0-start0).TotalSeconds);decimal lo=balances.Min(p=>p.Balance),hi=balances.Max(p=>p.Balance);decimal padding=Math.Max(1,(hi-lo)*.05m);lo-=padding;hi+=padding;
            double XX(DateTime time)=>left0+(right0-left0)*(time-start0).TotalSeconds/span0;double YY(decimal value)=>bottom0-(double)((value-lo)/(hi-lo))*(bottom0-top0);
            balances=Reduce(balances,start0,span0,right0-left0);
            for(int j=0;j<=4;j++){double yy=bottom0-(bottom0-top0)*j/4;c.Line(left0,yy,right0,yy,palette.Line);c.Text((lo+(hi-lo)*j/4).ToString("N0"),x+6,yy-5,8,palette.Muted);}
            for(int j=1;j<balances.Count;j++){c.Line(XX(balances[j-1].Time),YY(balances[j-1].Balance),XX(balances[j].Time),YY(balances[j-1].Balance),palette.Positive,1.2);c.Line(XX(balances[j].Time),YY(balances[j-1].Balance),XX(balances[j].Time),YY(balances[j].Balance),palette.Positive,1.2);}
            int count=(end0.Year-start0.Year)*12+end0.Month-start0.Month+1;var month0=new DateTime(start0.Year,start0.Month,1);for(int j=0;month0<end0;month0=month0.AddMonths(1),j++){if(month0<start0)continue;double xx=XX(month0);c.Line(xx,top0,xx,bottom0,palette.Line);if(j%Math.Max(1,(int)Math.Ceiling(count/8d))==0)c.Text(month0.ToString("MM.yy"),xx,bottom0+7,8,palette.Muted);}return;
        }
        DateTime start=report.AxisStart??new[]{balances[0].Time,dd[0].Time}.Min(),end=report.AxisEnd??new[]{balances[^1].Time,dd[^1].Time}.Max();double span=Math.Max(1,(end-start).TotalSeconds);
        double left=x+100,right=x+width-20,top=y+58,bottom=y+height-40,plot=right-left,split=overlay?bottom:top+(bottom-top)*.65,balBottom=overlay?bottom:split-12,ddTop=split+12;
        balances=Reduce(balances,start,span,plot);
        decimal low=balances.Min(p=>p.Balance),high=balances.Max(p=>p.Balance);if(overlay){low=Math.Min(low,dd.Min(p=>p.Equity));high=Math.Max(high,dd.Max(p=>p.Equity));}
        decimal pad=Math.Max(1,(high-low)*.04m);low-=pad;high+=pad;var axis=ChartAxis.Nice((double)low,(double)high,6);low=(decimal)axis.Low;high=(decimal)axis.High;
        double X(DateTime time)=>left+plot*(time-start).TotalSeconds/span;double Y(decimal value)=>balBottom-(double)((value-low)/(high-low))*(balBottom-top);
        double ddScale=Math.Max(10,Math.Ceiling((double)dd.Max(p=>p.Percent)/10)*10);double DY(decimal pct)=>ddTop+(double)pct/ddScale*(bottom-ddTop);
        foreach(var tick in axis.Ticks){double yy=Y((decimal)tick);c.Line(left,yy,right,yy,palette.Line);c.Text(ChartAxis.Number(tick)+" "+DisplayFormat.Currency(report),x+29,yy-5,8,palette.Muted);}
        var month=new DateTime(start.Year,start.Month,1);int months=(end.Year-start.Year)*12+end.Month-start.Month+1;int labelStride=report.AnalysisYear.HasValue?1:Math.Max(1,(int)Math.Ceiling(months/8d));
        for(int i=0;month<end;month=month.AddMonths(1),i++){if(month<start)continue;double xx=X(month);c.Line(xx,top,xx,bottom,palette.Line);if(i%labelStride==0)c.Text(month.ToString(report.AnalysisYear.HasValue?"MMM":"MM.yy"),xx+1,bottom+9,8,palette.Muted);}
        c.VerticalText("Balance ("+report.Find("Währung","Currency")+")",x+13,(top+balBottom)/2,7,palette.Muted);if(!overlay)c.VerticalText("Equity-DD (%)",x+13,(ddTop+bottom)/2,7,palette.Muted);
        // Step chart: a booked balance remains unchanged until the next settled booking window.
        for(int i=1;i<balances.Count;i++)
        {double xx=X(balances[i-1].Time),next=X(balances[i].Time),yy=Y(balances[i-1].Balance);c.Rect(xx,yy,Math.Max(0,next-xx),Math.Max(0,balBottom-yy),palette.Dark?"#365E86":"#E7EFF7");c.Line(xx,yy,next,yy,palette.Positive,1.2);c.Line(next,yy,next,Y(balances[i].Balance),palette.Positive,1.2);}
        // Retain local equity minima and DD maxima before display reduction.
        int stride=Math.Max(1,dd.Count/Math.Max(100,(int)plot));var keep=new SortedSet<int>{0,dd.Count-1};
        for(int i=0;i<dd.Count;i+=stride){var indices=Enumerable.Range(i,Math.Min(stride,dd.Count-i));keep.Add(indices.MinBy(j=>dd[j].Equity));keep.Add(indices.MaxBy(j=>dd[j].Equity));keep.Add(indices.MaxBy(j=>dd[j].Percent));}
        var points=keep.Select(i=>dd[i]).ToList();int cursor=0;
        for(int i=1;i<points.Count;i++)
        {
            var a=points[i-1];var b=points[i];double xx=X(a.Time),next=X(b.Time);
            if(overlay)
            {
                for(double pixel=xx;pixel<next;pixel+=1){var time=start.AddSeconds(span*(pixel-left)/plot);while(cursor+1<balances.Count&&balances[cursor+1].Time<=time)cursor++;decimal eq=a.Equity+(b.Equity-a.Equity)*(decimal)((pixel-xx)/Math.Max(.001,next-xx));if(eq<balances[cursor].Balance)c.Line(pixel,Y(balances[cursor].Balance),pixel,Y(eq),palette.Dark?"#8D6639":"#FBEBD8",1.2);}
                c.Line(xx,Y(a.Equity),next,Y(b.Equity),palette.Negative,1.2);
            }
            else {for(double pixel=xx;pixel<next;pixel+=1){decimal pct=a.Percent+(b.Percent-a.Percent)*(decimal)((pixel-xx)/Math.Max(.001,next-xx));c.Line(pixel,ddTop,pixel,DY(pct),palette.Dark?"#8D6639":"#FBEBD8",1.2);}c.Line(xx,DY(a.Percent),next,DY(b.Percent),palette.Negative,1.2);}
        }
        for(int band=0;band<64;band++){
            DateTime begin=start.AddSeconds(span*band/64),finish=start.AddSeconds(span*(band+1)/64);var samples=dd.Where(q=>q.Time>=begin&&q.Time<finish).ToList();if(samples.Count==0)continue;var sample=samples.MaxBy(q=>q.Percent);var balance=fullBalances.LastOrDefault(q=>q.Time<=sample.Time).Balance;var margin=report.EquityPoints.LastOrDefault(q=>q.Time<=sample.Time).DepositLoad;
            c.Hover(left+plot*band/64,top,plot/64,bottom-top,"Zeitfenster: "+DisplayFormat.Time(begin)+" – "+DisplayFormat.Time(finish)+"\nDD-Spitzenbeobachtung: "+DisplayFormat.Time(sample.Time)+"\nBalance: "+DisplayFormat.Money(balance,report)+"\nEquity: "+DisplayFormat.Money(sample.Equity,report)+"\nEquity-DD: "+DisplayFormat.Money(sample.Money,report)+$" / {sample.Percent:N2} %\nMargin-Auslastung: {margin:N2} %\n"+Exposure.Text(report,sample.Time));
        }
        if(!overlay){double step=ddScale>50?25:10;for(double value=0;value<=ddScale;value+=step){double yy=DY((decimal)value);c.Line(left,yy,right,yy,palette.Line);c.Text((value==0?"0":(-value).ToString("0"))+" %",x+40,yy-5,8,palette.Muted);}}
    }
}
