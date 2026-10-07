using PdfSharp.Pdf;
using PdfSharp.Drawing;
namespace BacktestAnalyzer;
public static class NativeHover
{
 public static void Add(PdfDocument doc,PdfPage page,double x,double y,double w,double h,string text){
  if(w<=0||h<=0)return;
  var form=doc.Internals.Catalog.Elements.GetDictionary("/AcroForm");if(form==null){form=new PdfDictionary(doc);doc.Internals.Catalog.Elements["/AcroForm"]=form;form.Elements["/Fields"]=new PdfArray(doc);form.Elements.SetBoolean("/NeedAppearances",false);}
  var fields=form.Elements.GetArray("/Fields")!;var a=new PdfDictionary(doc);a.Elements.SetName("/Type","/Annot");a.Elements.SetName("/Subtype","/Widget");a.Elements.SetName("/FT","/Btn");a.Elements.SetInteger("/Ff",65536);a.Elements.SetInteger("/F",0);a.Elements.SetString("/T","BA_Tooltip_"+fields.Elements.Count);a.Elements.SetString("/TU",text);a.Elements["/Rect"]=new PdfRectangle(new XRect(x,page.Height.Point-y-h,w,h));a.Elements["/P"]=page.Reference;a.Elements.SetName("/H","/N");a.Elements["/Border"]=new PdfArray(doc,new PdfInteger(0),new PdfInteger(0),new PdfInteger(0));
  var ap=new PdfDictionary(doc);ap.Elements.SetName("/Type","/XObject");ap.Elements.SetName("/Subtype","/Form");ap.Elements["/BBox"]=new PdfRectangle(new XRect(0,0,w,h));ap.CreateStream(System.Text.Encoding.ASCII.GetBytes("q\nQ\n"));doc.Internals.AddObject(ap);var appearance=new PdfDictionary(doc);appearance.Elements["/N"]=ap.Reference;a.Elements["/AP"]=appearance;doc.Internals.AddObject(a);fields.Elements.Add(a.Reference!);var annotations=page.Elements.GetArray("/Annots")??new PdfArray(doc);annotations.Elements.Add(a.Reference!);page.Elements["/Annots"]=annotations;
 }
}
