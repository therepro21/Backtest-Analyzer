namespace BacktestAnalyzer;

public static class AccountPainter
{
    static List<(DateTime Time,decimal Balance)> Reduce(List<(DateTime Time,decimal Balance)> values,DateTime start,double span,double pixels)
    {
        if(values.Count<pixels*16)return values;var keep=new SortedSet<int>{0,values.Count-1};
        foreach(var group in Enumerable.Range(0,values.Count).GroupBy(i=>(int)((values[i].Time-start).TotalSeconds/span*pixels*4)))
        {keep.Add(group.MinBy(i=>values[i].Balance));keep.Add(group.MaxBy(i=>values[i].Balance));keep.Add(group.Last());}
        return keep.Select(i=>values[i]).ToList();
    }
    public static void Draw(ICanvas c,Report report,Palette palette,double x,double y,double width,double height,bool overlay=false)
    {
        var axisDates=report.Balances.Select(b=>b.Time).Concat(report.EquityPoints.Select(b=>b.Time)).ToList();var marketStart=report.AxisStart??axisDates.DefaultIfEmpty(DateTime.Today).Min();var marketEnd=report.AxisEnd??axisDates.DefaultIfEmpty(DateTime.Today).Max();var marketInterval=MarketData.Interval(report,marketStart,marketEnd,width-111);
        var title=overlay?"Balance und Equity · Orange: Equity unter Balance":"Balance und echter Equity-Drawdown";
        if(report.SymbolAttributed)title=Localization.English?"Symbol result line (not account equity)":"Symbol-Ergebnislinie (keine Konto-Equity)";
        if(report.MarketEnabled)title+=" · "+marketInterval;
        c.Rect(x,y,width,height,palette.Surface);c.Text(title,x+12,y+10,12,palette.Ink,true);
        c.Text(report.Find("Währung","Currency")+" · Balance: "+report.BalanceWindowSeconds+" s Buchungsfenster · Equity: gespeicherte Tester-Punkte",x+12,y+31,8,palette.Muted);
        if(report.MarketEnabled)c.Text((Localization.English?"Source: ":"Quelle: ")+report.MarketSource+" · H1 → "+marketInterval+" · "+(Localization.English?"fixed time blocks in broker time":"feste Zeitblöcke in Brokerzeit"),x+12,y+44,5.5,palette.Muted);
        var balances=AccountCurves.DisplayBalances(report);var fullBalances=balances;var dd=AccountCurves.EquityDrawdowns(report);
        if(balances.Count<2){c.Text("Keine ausreichende Balance-Zeitreihe vorhanden.",x+15,y+65,10,palette.Muted);return;}
        if(dd.Count<2)
        {
            c.Text("Echte Equity-Zeitreihe fehlt · Equity-DD nicht verfügbar",x+15,y+height-30,9,palette.Muted);
            double left0=x+62,right0=x+width-(report.MarketEnabled?49:12),top0=y+60,bottom0=y+height-65;var start0=report.AxisStart??balances[0].Time;var end0=report.AxisEnd??balances[^1].Time;double span0=Math.Max(1,(end0-start0).TotalSeconds);decimal lo=balances.Min(p=>p.Balance),hi=balances.Max(p=>p.Balance);decimal padding=Math.Max(1,(hi-lo)*.05m);lo=Math.Max(0,lo-padding);hi+=padding;
            double XX(DateTime time)=>left0+(right0-left0)*(time-start0).TotalSeconds/span0;double YY(decimal value)=>bottom0-(double)((value-lo)/(hi-lo))*(bottom0-top0);
            balances=Reduce(balances,start0,span0,right0-left0);
            for(int j=0;j<=4;j++){double yy=bottom0-(bottom0-top0)*j/4;c.Line(left0,yy,right0,yy,palette.Line);c.RightText((lo+(hi-lo)*j/4).ToString("N0")+" "+DisplayFormat.Currency(report),left0-5,yy-5,8,palette.Muted);}
            int count=(end0.Year-start0.Year)*12+end0.Month-start0.Month+1;var month0=new DateTime(start0.Year,start0.Month,1);for(int j=0;month0<end0;month0=month0.AddMonths(1),j++){if(month0<start0)continue;double xx=XX(month0);c.Line(xx,top0,xx,bottom0,palette.Line);if(j%Math.Max(1,(int)Math.Ceiling(count/8d))==0)c.Text(month0.ToString("MM.yy"),xx,bottom0+7,8,palette.Muted);}
            MarketPainter.Draw(c,report,palette,start0,end0,left0,right0,top0,bottom0);
            for(int j=1;j<balances.Count;j++){c.Line(XX(balances[j-1].Time),YY(balances[j-1].Balance),XX(balances[j].Time),YY(balances[j-1].Balance),palette.Positive,.8);c.Line(XX(balances[j].Time),YY(balances[j-1].Balance),XX(balances[j].Time),YY(balances[j].Balance),palette.Positive,.8);}
            EventMarkers.Draw(c,report,palette,start0,end0,left0,right0,top0,bottom0,null,null,y+height-15);return;
        }
        DateTime start=report.AxisStart??new[]{balances[0].Time,dd[0].Time}.Min(),end=report.AxisEnd??new[]{balances[^1].Time,dd[^1].Time}.Max();double span=Math.Max(1,(end-start).TotalSeconds);
        double left=x+62,right=x+width-(report.MarketEnabled?49:12),top=y+67,bottom=y+height-32,plot=right-left,split=overlay?bottom:top+(bottom-top)*.65,balBottom=overlay?bottom:split-12,ddTop=split+12;
        balances=Reduce(balances,start,span,plot);
        decimal low=balances.Min(p=>p.Balance),high=balances.Max(p=>p.Balance);if(overlay||report.MarketEnabled){low=Math.Min(low,dd.Min(p=>p.Equity));high=Math.Max(high,dd.Max(p=>p.Equity));}
        decimal pad=Math.Max(1,(high-low)*.04m);low=Math.Max(0,low-pad);high+=pad;var axis=ChartAxis.Nice((double)low,(double)high,8);low=(decimal)axis.Low;high=(decimal)axis.High;
        double X(DateTime time)=>left+plot*(time-start).TotalSeconds/span;double Y(decimal value)=>balBottom-(double)((value-low)/(high-low))*(balBottom-top);
        double ddScale=Math.Max(10,Math.Ceiling((double)dd.Max(p=>p.Percent)/10)*10);double DY(decimal pct)=>ddTop+(double)pct/ddScale*(bottom-ddTop);
        foreach(var tick in axis.Ticks){double yy=Y((decimal)tick);c.Line(left,yy,right,yy,palette.Line,.5);c.RightText(ChartAxis.Number(tick)+" "+DisplayFormat.Currency(report),left-5,yy-5,8,palette.Muted);double minor=yy-(balBottom-top)*axis.Step/(axis.High-axis.Low)/2;if(minor>=top)c.Line(left,minor,right,minor,palette.Line,.25);}
        var month=new DateTime(start.Year,start.Month,1);int months=(end.Year-start.Year)*12+end.Month-start.Month+1;int labelStride=report.AnalysisYear.HasValue?1:Math.Max(1,(int)Math.Ceiling(months/8d));
        if(span<60*86400){for(int j=0;j<=6;j++){var tick=start.AddSeconds(span*j/6);double xx=X(tick);c.Line(xx,top,xx,balBottom,palette.Line,.4);if(!overlay)c.Line(xx,ddTop,xx,bottom,palette.Line,.4);c.Text(tick.ToString(Localization.English?"MMM d h tt":"dd.MM HH:mm"),xx-(j==6?48:0),bottom+9,7,palette.Muted);}}
        else for(int i=0;month<end;month=month.AddMonths(1),i++){if(month<start)continue;double xx=X(month);c.Line(xx,top,xx,balBottom,palette.Line,.4);if(!overlay)c.Line(xx,ddTop,xx,bottom,palette.Line,.4);if(i%labelStride==0)c.Text(month.ToString(report.AnalysisYear.HasValue?"MMM":"MM.yy"),xx+1,bottom+9,8,palette.Muted);}
        c.VerticalText("Balance ("+report.Find("Währung","Currency")+")",x+13,(top+balBottom)/2,7,palette.Muted);if(!overlay)c.VerticalText("Equity-DD (%)",x+13,(ddTop+bottom)/2,7,palette.Muted);
        if(!overlay){double step=ddScale>50?20:10;for(double value=0;value<=ddScale;value+=step){double yy=DY((decimal)value);c.Line(left,yy,right,yy,palette.Line,.4);c.RightText((value==0?"0":(-value).ToString("0"))+" %",left-5,yy-5,8,palette.Muted);double half=DY((decimal)(value+step/2));if(half<=bottom)c.Line(left,half,right,half,palette.Line,.22);}}
        // Step chart: a booked balance remains unchanged until the next settled booking window.
        for(int i=1;i<balances.Count;i++)
        {double xx=X(balances[i-1].Time),next=X(balances[i].Time),yy=Y(balances[i-1].Balance);c.Rect(xx,yy,Math.Max(0,next-xx),Math.Max(0,balBottom-yy),palette.PositiveFill);c.Line(xx,yy,next,yy,palette.Positive,.8);c.Line(next,yy,next,Y(balances[i].Balance),palette.Positive,.8);}
        // Retain local equity minima and DD maxima before display reduction.
        int stride=Math.Max(1,dd.Count/Math.Max(100,(int)(plot*6)));var keep=new SortedSet<int>{0,dd.Count-1};
        for(int i=0;i<dd.Count;i+=stride){var indices=Enumerable.Range(i,Math.Min(stride,dd.Count-i));keep.Add(indices.MinBy(j=>dd[j].Equity));keep.Add(indices.MaxBy(j=>dd[j].Equity));keep.Add(indices.MaxBy(j=>dd[j].Percent));keep.Add(i);keep.Add(i+Math.Min(stride,dd.Count-i)-1);}
        var points=keep.Select(i=>dd[i]).ToList();int cursor=0;
        for(int i=1;i<points.Count;i++)
        {
            var a=points[i-1];var b=points[i];double xx=X(a.Time),next=X(b.Time);
            if(overlay)
            {
                for(double pixel=xx;pixel<next;pixel+=1){var time=start.AddSeconds(span*(pixel-left)/plot);while(cursor+1<balances.Count&&balances[cursor+1].Time<=time)cursor++;decimal eq=a.Equity+(b.Equity-a.Equity)*(decimal)((pixel-xx)/Math.Max(.001,next-xx));if(eq<balances[cursor].Balance)c.Line(pixel,Y(balances[cursor].Balance),pixel,Y(eq),palette.NegativeFill,1.2);}
                c.Line(xx,Y(a.Equity),next,Y(b.Equity),palette.Negative,.8);
            }
            else {for(double pixel=xx;pixel<next;pixel+=1){decimal pct=a.Percent+(b.Percent-a.Percent)*(decimal)((pixel-xx)/Math.Max(.001,next-xx));c.Line(pixel,ddTop,pixel,DY(pct),palette.NegativeFill,1.2);}c.Line(xx,DY(a.Percent),next,DY(b.Percent),palette.Negative,.8);}
        }
        MarketPainter.Draw(c,report,palette,start,end,left,right,top,balBottom);
        if(report.MarketEnabled){for(int i=1;i<balances.Count;i++){c.Line(X(balances[i-1].Time),Y(balances[i-1].Balance),X(balances[i].Time),Y(balances[i-1].Balance),palette.Positive,.8);c.Line(X(balances[i].Time),Y(balances[i-1].Balance),X(balances[i].Time),Y(balances[i].Balance),palette.Positive,.8);}for(int i=1;i<points.Count;i++)c.Line(X(points[i-1].Time),Y(points[i-1].Equity),X(points[i].Time),Y(points[i].Equity),palette.Dark?"#C8A0EB":"#8158A8",.65);c.Text("Balance · Equity · Marktkerzen (rechte Achse)",left,top+3,6,palette.Muted);}
        for(int band=0;band<64;band++){
            DateTime begin=start.AddSeconds(span*band/64),finish=start.AddSeconds(span*(band+1)/64);var samples=dd.Where(q=>q.Time>=begin&&q.Time<finish).ToList();if(samples.Count==0)continue;var sample=samples.MaxBy(q=>q.Percent);var balance=fullBalances.LastOrDefault(q=>q.Time<=sample.Time).Balance;var margin=report.EquityPoints.LastOrDefault(q=>q.Time<=sample.Time).DepositLoad;
            c.Hover(left+plot*band/64,top,plot/64,bottom-top,"Zeitfenster: "+DisplayFormat.Time(begin)+" – "+DisplayFormat.Time(finish)+"\nDD-Spitzenbeobachtung: "+DisplayFormat.Time(sample.Time)+"\nBalance: "+DisplayFormat.Money(balance,report)+"\nEquity: "+DisplayFormat.Money(sample.Equity,report)+"\nEquity-DD: "+DisplayFormat.Money(sample.Money,report)+$" / {sample.Percent:N2} %\nMargin-Auslastung: {margin:N2} %\n"+Exposure.Text(report,sample.Time)+MarketPainter.Details(report,begin,finish,marketInterval));
        }

        if(report.MarketEnabled)MarketPainter.Draw(c,report,palette,start,end,left,right,top,balBottom,true);
        EventMarkers.Draw(c,report,palette,start,end,left,right,top,balBottom,overlay?null:ddTop,overlay?null:bottom,y+height-11);
    }
}
