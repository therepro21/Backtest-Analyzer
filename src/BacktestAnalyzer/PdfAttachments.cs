using PdfSharp.Pdf;
using System.IO.Compression;
namespace BacktestAnalyzer;
public static class PdfAttachments
{
 public static void Add(PdfDocument doc,IEnumerable<string> files)
 {
  var names=new PdfArray(doc);
  foreach(var path in files.Where(File.Exists).Distinct()){
   byte[] bytes=File.ReadAllBytes(path);using var output=new MemoryStream();using(var zipper=new ZLibStream(output,CompressionLevel.Optimal,true))zipper.Write(bytes);
   var stream=new PdfDictionary(doc);stream.Elements.SetName("/Type","/EmbeddedFile");stream.CreateStream(output.ToArray());stream.Elements.SetName("/Filter","/FlateDecode");doc.Internals.AddObject(stream);
   var spec=new PdfDictionary(doc);spec.Elements.SetName("/Type","/Filespec");spec.Elements.SetString("/F",Path.GetFileName(path));spec.Elements.SetString("/UF",Path.GetFileName(path));spec.Elements.SetString("/Desc","Unveränderte Originaldatei / original source file");var ef=new PdfDictionary(doc);ef.Elements["/F"]=stream.Reference;spec.Elements["/EF"]=ef;doc.Internals.AddObject(spec);
   names.Elements.Add(new PdfString(Path.GetFileName(path)));names.Elements.Add(spec.Reference!);
  }
  if(names.Elements.Count==0)return;var tree=new PdfDictionary(doc);tree.Elements["/Names"]=names;var catalogNames=doc.Internals.Catalog.Elements.GetDictionary("/Names")??new PdfDictionary(doc);catalogNames.Elements["/EmbeddedFiles"]=tree;doc.Internals.Catalog.Elements["/Names"]=catalogNames;
 }
}
