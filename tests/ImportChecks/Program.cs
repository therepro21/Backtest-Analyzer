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
    Console.WriteLine("Passed: streamed chunk boundaries, UTF-16/UTF-8/Windows-1252, entities, XLSX inline strings, original inputs, signed costs, position IDs and HTML/XLSX parity.");
}
finally {Directory.Delete(folder,true);}
static void Check(Report r)
{
    if(r.InitialDeposit!=100||r.Deals.Count!=2||r.Closed.Count!=1||r.Closed[0].Seconds!=3600||r.Closed[0].Net!=16||r.Closed[0].Estimated||r.InputParameters.Count!=2||r.Deals[0].Comment!="ä & <text>")throw new Exception("Import control failed");
    if(r.Warnings.Any(w=>w.Contains("Separate Gebühren")))throw new Exception("Present fee column marked missing");
}
