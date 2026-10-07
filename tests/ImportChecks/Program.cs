using BacktestAnalyzer;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

var folder=Path.Combine(Path.GetTempPath(),"backtest-import-check-"+Guid.NewGuid());Directory.CreateDirectory(folder);
try
{
    var rows=new List<string[]> {
        new[]{"Expert:","Fixture"},new[]{"Initial Deposit:","100"},new[]{"Inputs:","Mode=1"},new[]{"","Second=2"},
        new[]{"Total Net Profit:","16"},
        new[]{"Time","Deal","Symbol","Type","Direction","Volume","Price","Order","Commission","Swap","Profit","Balance","Position","Comment","Fee"},
        new[]{"2026.01.01 00:00:00","1","","balance","","0","0","","0","0","100","100","","","0"},
        new[]{"2026.01.01 01:00:00","2","XAUUSD","buy","in","1","100","2","-1","0","0","99","42","ä & <text>","0"},
        new[]{"2026.01.01 02:00:00","3","XAUUSD","sell","out","1","120","3","-1","-2","20","116","42","close","0"}
    };
    string Escape(string value)=>System.Net.WebUtility.HtmlEncode(value);
    string html="<html><body>"+new string(' ',65520)+"<table>"+string.Join("",rows.Select(row=>"<tr>"+string.Join("",row.Select(v=>"<td>"+Escape(v)+"</td>"))+"</tr>"))+"</table></body></html>";
    var htmlPath=Path.Combine(folder,"fixture.html");File.WriteAllText(htmlPath,html,new UnicodeEncoding(false,true));
    var a=Parser.Load(htmlPath);Check(a);
    XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    var excelPath=Path.Combine(folder,"fixture.xlsx");
    using(var zip=ZipFile.Open(excelPath,ZipArchiveMode.Create))
    {
        var sheet=new XElement(ns+"worksheet",new XElement(ns+"sheetData",rows.Select((row,i)=>new XElement(ns+"row",new XAttribute("r",i+1),row.Select((value,j)=>new XElement(ns+"c",new XAttribute("r",((char)('A'+j)).ToString()+(i+1)),new XAttribute("t","inlineStr"),new XElement(ns+"is",new XElement(ns+"t",value))))))));
        using var writer=new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open(),new UnicodeEncoding(false,true));writer.Write(sheet.ToString());
    }
    var b=Parser.Load(excelPath);Check(b);
    if(!a.Deals.SequenceEqual(b.Deals))throw new Exception("HTML/XLSX deal parity failed");
    if(a.Trades.Single().Seconds!=b.Trades.Single().Seconds)throw new Exception("Duration parity failed");
    File.WriteAllText(htmlPath,html,new UTF8Encoding(true));Check(Parser.Load(htmlPath));
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);File.WriteAllText(htmlPath,html,Encoding.GetEncoding(1252));Check(Parser.Load(htmlPath));
    var s=new Stats(a.Closed);if(s.Std!=0||s.Mean!=3600||s.Weighted!=3600)throw new Exception("Duration statistics failed");
    var cross=new Report{InitialDeposit=100,Balances=new(){(new DateTime(2024,1,1),100),(new DateTime(2024,12,31,23,0,0),99),(new DateTime(2025,1,1,1,0,0),116)},
        Deals=new(){new("1","1","42",new DateTime(2024,12,31,23,0,0),"X","buy","in",1,100,0,-1,0,0,99,""),new("2","2","42",new DateTime(2025,1,1,1,0,0),"X","sell","out",1,120,20,-1,-2,0,116,"")}};
    Parser.Reconstruct(cross);var years=YearAnalysis.Scopes(cross);var y2025=years[2];
    if(years.Count!=3||y2025.InitialDeposit!=99||y2025.OpeningBuyLots!=1||y2025.Closed.Single().Seconds!=7200||y2025.Deals.Sum(d=>d.Net)!=17||years.Skip(1).Sum(y=>y.Deals.Sum(d=>d.Net))!=cross.Deals.Sum(d=>d.Net))throw new Exception("Annual carry and booking reconciliation failed");
    if(YearAnalysis.MaxDrawdown(y2025,true).Money!=0)throw new Exception("Annual DD reset failed");
    var cycles=SeriesAnalysis.Build(cross.Deals);if(cycles.Count!=1||cycles[0].Entries!=1||cycles[0].WeekdaySeconds!=7200||cycles[0].Net!=16)throw new Exception("Flat-to-flat cycle failed");
    var atomicReport=new Report();var cycleStart=new DateTime(2026,1,1);
    Deal CycleDeal(string id,int second,string side,string entry,decimal net,string comment="")=>new(id,id,id,cycleStart.AddSeconds(second),"XAUUSD",side,entry,1,1,net,0,0,0,null,comment);
    atomicReport.Deals.AddRange(new[]{CycleDeal("1",0,"buy","in",0),CycleDeal("2",10,"sell","out",3),CycleDeal("3",10,"buy","in",0),CycleDeal("4",20,"sell","out",4),CycleDeal("5",30,"buy","in",0),CycleDeal("6",50,"sell","out",-2,"end of test")});
    var atomicCycles=SeriesAnalysis.Build(atomicReport.Deals);if(atomicCycles.Count!=2||atomicCycles[0].Net!=7||atomicCycles[0].End!=cycleStart.AddSeconds(20))throw new Exception("Same-second bookings split a cycle");
    var adjustedCycle=SeriesAnalysis.Adjusted(atomicReport);if(adjustedCycle.Profit!=7||adjustedCycle.Excluded!=-2||adjustedCycle.Time!=cycleStart.AddSeconds(20)||SeriesAnalysis.Regular(atomicReport).Count!=1)throw new Exception("Test-end cycle adjustment failed");
    var friday=new Trade{Open=new DateTime(2026,5,8,23,0,0),Close=new DateTime(2026,5,11,1,0,0)};
    if(friday.Seconds!=180000||Stats.HoldingSeconds(friday,true,false,false,0)!=7200)throw new Exception("Weekend overlap failed");
    var grouping=new Report{InitialDeposit=100,Balances=new(){(new DateTime(2026,1,1),100),(new DateTime(2026,1,1).AddSeconds(10),60),(new DateTime(2026,1,1).AddSeconds(40),110),(new DateTime(2026,1,1).AddSeconds(90),50),(new DateTime(2026,1,1).AddSeconds(150),115)},EquityPoints=new(){(new DateTime(2026,1,1),100,100,0),(new DateTime(2026,1,1).AddSeconds(10),60,70,0),(new DateTime(2026,1,1).AddSeconds(40),110,110,0)}};
    var display=AccountCurves.DisplayBalances(grouping);if(display.Count!=3||display.Any(p=>p.Balance<100)||AccountCurves.EquityDrawdowns(grouping).Max(p=>p.Percent)!=30)throw new Exception("Display grouping changed genuine equity or retained intra-window balance spikes");
    grouping.BalanceWindowSeconds=0;if(AccountCurves.DisplayBalances(grouping).Count!=5)throw new Exception("Raw balance display failed");
    var cachePath=Path.Combine(folder,"fixture.tst");byte[] cache=new byte[1456+1640+4+3*256+4+4+64+4+2*32];
    void Int(int offset,int value)=>BitConverter.GetBytes(value).CopyTo(cache,offset);
    void Long(int offset,long value)=>BitConverter.GetBytes(value).CopyTo(cache,offset);
    void Double(int offset,double value)=>BitConverter.GetBytes(value).CopyTo(cache,offset);
    void Unicode(int offset,string value)=>Encoding.Unicode.GetBytes(value).CopyTo(cache,offset);
    Int(0,505);Unicode(132,"SingleTestCache");Unicode(432,"Fixture");Unicode(944,"XAUUSD");Unicode(1284,"USD");Double(1348,100);Int(1404,3);Int(1412,1);Int(1416,2);
    int dealsOffset=1456+1640+4;Int(dealsOffset-4,3);long epoch=1767225600;
    for(int i=0;i<3;i++){int offset=dealsOffset+i*256;Long(offset,i+1);Long(offset+16,epoch+i*3600);Unicode(offset+24,i==0?"":"XAUUSD");Long(offset+248,i==0?0:42);Int(offset+224,i==0?2:i==1?0:1);Int(offset+228,i==2?1:0);Long(offset+120,i==0?0:100000000);Double(offset+128,i==0?100:i==2?20:0);Double(offset+136,i==0?0:-1);Double(offset+152,i==2?-2:0);}
    int positionCount=dealsOffset+3*256+4;Int(positionCount,1);int statesCount=positionCount+4+64;Int(statesCount,2);int stateOffset=statesCount+4;
    Long(stateOffset,epoch);Double(stateOffset+8,100);Double(stateOffset+16,100);Long(stateOffset+32,epoch+7200);Double(stateOffset+40,116);Double(stateOffset+48,116);
    File.WriteAllBytes(cachePath,cache);var c=TesterCache.Load(cachePath);if(c.Deals.Count!=2||c.Closed.Count!=1||c.Closed[0].Net!=16||c.EquityPoints.Count!=2||c.Closed[0].Estimated)throw new Exception("Cache data validation failed");
    Int(0,506);File.WriteAllBytes(cachePath,cache);try{TesterCache.Load(cachePath);throw new Exception("Unknown cache version accepted");}catch(InvalidDataException){}
    Int(0,505);Int(1404,4);File.WriteAllBytes(cachePath,cache);try{TesterCache.Load(cachePath);throw new Exception("Corrupt section accepted");}catch(InvalidDataException){}
    if(Stats.HoldingSeconds(friday,true)!=3600)throw new Exception("Night pause and weekend overlap failed");
    var night=new Trade{Open=new DateTime(2026,1,5,0,15,0),Close=new DateTime(2026,1,5,0,45,0)};if(Stats.HoldingSeconds(night,true)!=0)throw new Exception("Partial overnight pause failed");
    var saturday=new Trade{Open=new DateTime(2026,1,10,10,0,0),Close=new DateTime(2026,1,10,12,0,0)};if(Stats.HoldingSeconds(saturday,true)!=0||Stats.HoldingSeconds(saturday,true,true)!=7200)throw new Exception("Selectable Saturday failed");
    var saved=Path.Combine(folder,"analysis.bta");AnalysisFile.Save(c,saved);var loaded=AnalysisFile.Load(saved);if(loaded.Deals.Count!=c.Deals.Count||loaded.EquityPoints.Count!=2||loaded.EquityPoints[1].Equity!=116||loaded.Closed.Single().Net!=16)throw new Exception("Saved analysis roundtrip failed");if(Exposure.At(c,c.Deals.First().Time).Buy!=1||Exposure.At(c,c.Deals.Last().Time).Buy!=0)throw new Exception("Exposure reconstruction failed");
    Console.WriteLine("Passed: streamed imports, encodings, costs, position IDs, HTML/XLSX parity, annual carry/booking reconciliation, weekend overlap, flat-to-flat cycles, balance display grouping with unchanged equity DD, and accepted/rejected cache versions and section sizes.");
}
finally {Directory.Delete(folder,true);}
static void Check(Report r)
{
    if(r.InitialDeposit!=100||r.Deals.Count!=2||r.Closed.Count!=1||r.Closed[0].Seconds!=3600||r.Closed[0].Net!=16||r.Closed[0].Estimated||r.InputParameters.Count!=2||r.Deals[0].Comment!="ä & <text>")throw new Exception("Import control failed");
    if(r.Warnings.Any(w=>w.Contains("Separate Gebühren")))throw new Exception("Present fee column marked missing");
}
