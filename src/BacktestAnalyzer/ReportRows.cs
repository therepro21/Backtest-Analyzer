using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace BacktestAnalyzer;

// Reads exported report rows without constructing a whole HTML/XML document.
public static class ReportRows
{
    static readonly XmlReaderSettings Settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
    public static IEnumerable<string[]> Read(string path) => Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ? Excel(path) : Html(path);
    static IEnumerable<string[]> Excel(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var shared = new List<string>();
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry != null)
        {
            using var stream = entry.Open(); using var xml = XmlReader.Create(stream, Settings);
            while (xml.Read()) if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "si")
            { using var sub = xml.ReadSubtree(); var el = XElement.Load(sub); shared.Add(string.Concat(el.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value))); }
        }
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("MT5-Export benötigt Sheet1.");
        using var sheetStream = sheet.Open(); using var reader = XmlReader.Create(sheetStream, Settings);
        bool history = false;
        while (reader.Read()) if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "row")
        {
            using var sub = reader.ReadSubtree(); var row = XElement.Load(sub); var values = new List<string>();
            foreach (var cell in row.Elements().Where(e => e.Name.LocalName == "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? ""; int col = 0;
                foreach (char ch in reference.TakeWhile(char.IsLetter)) col = col * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
                if (col > 256) throw new InvalidDataException("Kein unterstützter MT5-Bericht: zu viele Spalten.");
                while (values.Count < col) values.Add("");
                string val = cell.Elements().FirstOrDefault(e => e.Name.LocalName == "v")?.Value ?? "";
                string type = (string?)cell.Attribute("t") ?? "";
                if (type == "s") { if (!int.TryParse(val, out int index) || index < 0 || index >= shared.Count) throw new InvalidDataException("Ungültiger Shared-String-Verweis."); val = shared[index]; }
                else if (type == "inlineStr") val = string.Concat(cell.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value));
                else if (type == "e") throw new InvalidDataException("Excel-Fehlerzelle: " + val);
                if (col > 0) values[col - 1] = val.Trim();
            }
            if (values.Any(v => v is "Richtung" or "Direction" or "Entry")) history = true;
            // Merged headings use D/H/L in MT5 XLSX; history columns must retain their indices.
            if (!history && !values.Any(v => v is "Zeit" or "Time"))
            {
                bool blankFirst = values.Count > 0 && values[0].Length == 0;
                values = values.Where(v => v.Length > 0).ToList();
                if (blankFirst && values.Count == 1 && values[0].Contains('=')) values.Insert(0, "");
            }
            yield return values.ToArray();
        }
    }
    static IEnumerable<string[]> Html(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.OpenRead(path);
        // UTF-16/UTF-8 BOMs are detected; old MT4 Windows-1252 remains supported.
        var prefix = new byte[4]; int count = stream.Read(prefix); stream.Position = 0;
        Encoding encoding = Encoding.GetEncoding(1252);
        if (count >= 3 && prefix[0] == 239 && prefix[1] == 187 && prefix[2] == 191) encoding = Encoding.UTF8;
        else if (count >= 2 && prefix[0] == 255 && prefix[1] == 254) encoding = Encoding.Unicode;
        else if (count >= 2 && prefix[0] == 254 && prefix[1] == 255) encoding = Encoding.BigEndianUnicode;
        else { using var probe = new StreamReader(path, new UTF8Encoding(false, true), false); try { var buffer = new char[65536]; while (probe.Read(buffer, 0, buffer.Length) > 0) { } encoding = Encoding.UTF8; } catch (DecoderFallbackException) { } }
        using var reader = new StreamReader(stream, encoding, true, 65536);
        var pending = new StringBuilder(); var chunk = new char[65536]; int n;
        while ((n = reader.Read(chunk, 0, chunk.Length)) > 0)
        {
            pending.Append(chunk, 0, n);
            string text = pending.ToString(); int consumed = 0;
            while (true)
            {
                int start = text.IndexOf("<tr", consumed, StringComparison.OrdinalIgnoreCase);
                if (start < 0) { consumed = Math.Max(consumed, text.Length - 4); break; }
                int end = text.IndexOf("</tr>", start, StringComparison.OrdinalIgnoreCase);
                if (end < 0) { consumed = start; break; }
                var doc = new HtmlDocument(); doc.LoadHtml(text.Substring(start, end + 5 - start));
                yield return (doc.DocumentNode.SelectSingleNode("//tr")?.SelectNodes("./td|./th") ?? new HtmlNodeCollection(null)).Select(c => HtmlEntity.DeEntitize(c.InnerText).Trim()).ToArray();
                consumed = end + 5;
            }
            pending.Remove(0, consumed);
            if (pending.Length > 4_000_000) throw new InvalidDataException("HTML-Zeile zu lang oder unvollständig.");
        }
        if (pending.ToString().Contains("<tr", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unvollständiger HTML-Bericht.");
    }
}
