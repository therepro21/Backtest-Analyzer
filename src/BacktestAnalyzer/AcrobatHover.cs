using System.Globalization;
using System.Text;
using System.Text.Json;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace BacktestAnalyzer;

public static class AcrobatHover
{
    public static void Add(PdfDocument document,PdfPage page,Report report,double plotX,double plotTop,double plotWidth,double plotHeight,double detailX,double detailTop,double detailWidth,double detailHeight)
    {
        var points=new List<(DateTime Time,decimal Balance,decimal Money,decimal Percent,decimal Buy,decimal Sell)>();decimal peak=report.InitialDeposit,buy=0,sell=0;
        if(report.Balances.Count>0)points.Add((report.Balances[0].Time,report.Balances[0].Balance,0,0,0,0));
        foreach(var deal in report.Deals.OrderBy(d=>d.Time))
        {
            if(deal.Entry=="in"){if(deal.Side=="buy")buy+=deal.Volume;else sell+=deal.Volume;}
            else if(deal.Entry is "out" or "outby" or "out/by"){if(deal.Side=="sell")buy-=deal.Volume;else sell-=deal.Volume;}
            else if(deal.Entry is "inout" or "in/out")
            {
                if(deal.Side=="sell"){var closed=Math.Min(buy,deal.Volume);buy-=closed;sell+=deal.Volume-closed;}
                else {var closed=Math.Min(sell,deal.Volume);sell-=closed;buy+=deal.Volume-closed;}
            }
            if(!deal.Balance.HasValue)continue;var balance=deal.Balance.Value;peak=Math.Max(peak,balance);var money=peak-balance;
            points.Add((deal.Time,balance,money,peak>0?money/peak*100:0,buy,sell));
        }
        if(points.Count<2)return;
        var form=document.Internals.Catalog.Elements.GetDictionary("/AcroForm");
        if(form==null){form=new PdfDictionary(document);document.Internals.AddObject(form);document.Internals.Catalog.Elements["/AcroForm"]=form.Reference;}
        var fields=form.Elements.GetArray("/Fields");if(fields==null){fields=new PdfArray(document);form.Elements["/Fields"]=fields;}
        var font=new PdfDictionary(document);font.Elements.SetName("/Type","/Font");font.Elements.SetName("/Subtype","/Type1");font.Elements.SetName("/BaseFont","/Helvetica");font.Elements.SetName("/Encoding","/WinAnsiEncoding");document.Internals.AddObject(font);
        var fonts=new PdfDictionary(document);fonts.Elements["/Helv"]=font.Reference;var resources=new PdfDictionary(document);resources.Elements["/Font"]=fonts;form.Elements["/DR"]=resources;form.Elements.SetString("/DA","/Helv 9 Tf 0 g");form.Elements.SetBoolean("/NeedAppearances",false);
        var annots=page.Elements.GetArray("/Annots");if(annots==null){annots=new PdfArray(document);page.Elements["/Annots"]=annots;}
        PdfDictionary Widget(string name,string type,double x,double top,double width,double height,int flags,int fieldFlags)
        {
            var field=new PdfDictionary(document);field.Elements.SetName("/Type","/Annot");field.Elements.SetName("/Subtype","/Widget");field.Elements.SetName("/FT",type);field.Elements.SetString("/T",name);field.Elements.SetInteger("/F",flags);field.Elements.SetInteger("/Ff",fieldFlags);field.Elements["/Rect"]=new PdfRectangle(new XRect(x,page.Height.Point-top-height,width,height));field.Elements["/P"]=page.Reference;field.Elements["/Border"]=new PdfArray(document,new PdfInteger(0),new PdfInteger(0),new PdfInteger(0));field.Elements.SetName("/H","/N");
            var appearance=new PdfDictionary(document);appearance.Elements.SetName("/Type","/XObject");appearance.Elements.SetName("/Subtype","/Form");appearance.Elements["/BBox"]=new PdfRectangle(new XRect(0,0,width,height));appearance.CreateStream(Encoding.ASCII.GetBytes("q\nQ\n"));document.Internals.AddObject(appearance);var ap=new PdfDictionary(document);ap.Elements["/N"]=appearance.Reference;field.Elements["/AP"]=ap;
            document.Internals.AddObject(field);fields.Elements.Add(field.Reference!);annots.Elements.Add(field.Reference!);return field;
        }
        string fieldName="BA_Detail_"+document.Pages.Count;
        var detail=Widget(fieldName,"/Tx",detailX,detailTop,detailWidth,detailHeight,2,4097);detail.Elements.SetString("/V","");detail.Elements.SetString("/DV","");detail.Elements.SetString("/DA","/Helv 9 Tf 0 g");
        var mk=new PdfDictionary(document);mk.Elements["/BG"]=new PdfArray(document,new PdfReal(.97),new PdfReal(.98),new PdfReal(1));detail.Elements["/MK"]=mk;
        string hide="var f=this.getField("+JsonSerializer.Serialize(fieldName)+");if(f)f.display=display.hidden;";
        PdfDictionary Action(string script){var action=new PdfDictionary(document);action.Elements.SetName("/S","/JavaScript");action.Elements.SetString("/JS",script);return action;}
        var pageActions=page.Elements.GetDictionary("/AA")??new PdfDictionary(document);pageActions.Elements["/C"]=Action(hide);page.Elements["/AA"]=pageActions;
        const int bands=160;double total=Math.Max(1,(points[^1].Time-points[0].Time).TotalSeconds);int cursor=0;
        string N(decimal value)=>value.ToString("N2",CultureInfo.GetCultureInfo("de-DE"));string currency=report.Find("Währung","Currency");
        for(int i=0;i<bands;i++)
        {
            var start=points[0].Time.AddSeconds(total*i/bands);var end=points[0].Time.AddSeconds(total*(i+1)/bands);while(cursor+1<points.Count&&points[cursor+1].Time<start)cursor++;
            int best=cursor;for(int j=cursor;j<points.Count&&points[j].Time<=end;j++)if(points[j].Percent>points[best].Percent)best=j;var point=points[best];
            string text="Zeitfenster: "+start.ToString("dd.MM.yy HH:mm")+" - "+end.ToString("dd.MM.yy HH:mm")+"\nDD-Spitzenbeobachtung / letzter Stand: "+point.Time.ToString("dd.MM.yy HH:mm:ss")+"\nBalance: "+N(point.Balance)+" "+currency+"    Drawdown: "+N(point.Money)+" "+currency+" / "+N(point.Percent)+" %\nBuy-Lots: "+N(point.Buy)+"    Sell-Lots: "+N(point.Sell)+"    Netto-Lots: "+N(point.Buy-point.Sell)+"\nEquity und bestaetigte Positionsanzahl: nicht exportiert";
            var hit=Widget("BA_Hit_"+document.Pages.Count+"_"+i,"/Btn",plotX+plotWidth*i/bands,plotTop,plotWidth/bands,plotHeight,0,65536);hit.Elements.SetString("/TU",text);
            string show="var f=this.getField("+JsonSerializer.Serialize(fieldName)+");if(f){f.value="+JsonSerializer.Serialize(text)+";f.display=display.noPrint;}";
            var actions=new PdfDictionary(document);actions.Elements["/E"]=Action(show);actions.Elements["/X"]=Action(hide);actions.Elements["/Fo"]=Action(show);actions.Elements["/Bl"]=Action(hide);actions.Elements["/U"]=Action(show);hit.Elements["/AA"]=actions;
        }
    }
}
