using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BacktestAnalyzer;

public sealed record TerminalInstance(string Executable,string DataFolder,int? ProcessId,string WindowTitle,string Mapping)
{
    public string Label => (ProcessId.HasValue?"Geöffnet · PID "+ProcessId:"Gespeicherte Installation")+" · "+Path.GetFileName(Path.GetDirectoryName(Executable))+" · "+WindowTitle+"\n"+Executable+"\nDaten: "+DataFolder+" · "+Mapping;
}
public sealed record BacktestFile(string Path,string Kind,long Bytes,DateTime Modified,int? CacheVersion=null)
{
    public bool Importable=>!System.IO.Path.GetExtension(Path).Equals(".tst",StringComparison.OrdinalIgnoreCase)||CacheVersion==505;
    public string Label=>Kind+" · "+System.IO.Path.GetFileName(Path)+" · "+Modified.ToString("dd.MM.yyyy HH:mm")+" · "+(Bytes/1048576d).ToString("N1")+" MB\n"+Path;
}
public static class TerminalDiscovery
{
    public static List<TerminalInstance> Find(bool openedOnly=true)
    {
        // Windows' existing WMI service; no installed helper or persistent registration.
        var commands=new Dictionary<int,string>();
        try
        {
            var type=Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
            if(type!=null){dynamic locator=Activator.CreateInstance(type)!;dynamic service=locator.ConnectServer(".","root\\cimv2");
                foreach(dynamic process in service.ExecQuery("SELECT ProcessId,CommandLine FROM Win32_Process WHERE Name='terminal64.exe'"))commands[(int)process.ProcessId]=((string?)process.CommandLine)??"";}
        }
        catch(Exception e)when(e is System.Runtime.InteropServices.COMException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or InvalidCastException){}
        var mappings=new List<(string Exe,string Data)>();
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MetaQuotes","Terminal");
        if(Directory.Exists(root))foreach(var dir in Directory.EnumerateDirectories(root))
        {
            try{string origin=Path.Combine(dir,"origin.txt");if(!File.Exists(origin)||!Directory.Exists(Path.Combine(dir,"MQL5")))continue;
                string path=File.ReadAllText(origin).Trim().Trim('\0');string exe=Path.Combine(path,"terminal64.exe");
                if(File.Exists(exe))mappings.Add((exe,dir));}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException){}
        }
        var found=new List<TerminalInstance>();var active=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var process in Process.GetProcessesByName("terminal64"))using(process)
        {
            try{string exe=process.MainModule?.FileName??"";if(exe.Length==0)continue;active.Add(exe);
                var matches=mappings.Where(x=>string.Equals(x.Exe,exe,StringComparison.OrdinalIgnoreCase)).ToList();
                string command=commands.GetValueOrDefault(process.Id,"");bool portable=Regex.IsMatch(command,@"(?:^|\s)/portable(?:\s|$)",RegexOptions.IgnoreCase);
                if(!portable)foreach(var match in matches)found.Add(new(exe,match.Data,process.Id,process.MainWindowTitle,command.Length>0?"Normalmodus · origin.txt / Startargumente":"origin.txt · Startargumente nicht lesbar; Datenordner prüfen"));
                string install=Path.GetDirectoryName(exe)!;
                if(portable||(command.Length==0&&Directory.Exists(Path.Combine(install,"MQL5"))&&Directory.Exists(Path.Combine(install,"Tester"))))found.Add(new(exe,install,process.Id,process.MainWindowTitle,portable?"Portable-Modus · Startargument /portable":"Portable-Datenordner-Kandidat; Datenordner prüfen"));
                if(!found.Any(x=>x.ProcessId==process.Id))found.Add(new(exe,"",process.Id,process.MainWindowTitle,"Datenordner nicht erkannt; manuell wählen"));}
            catch(Exception e)when(e is System.ComponentModel.Win32Exception or InvalidOperationException or UnauthorizedAccessException){found.Add(new("Pfad nicht lesbar","",process.Id,"terminal64.exe","Zugriff eingeschränkt; Datenordner manuell wählen"));}
        }
        if(!openedOnly)foreach(var map in mappings.Where(x=>!active.Contains(x.Exe)))found.Add(new(map.Exe,map.Data,null,"","origin.txt"));
        return found.OrderBy(x=>x.ProcessId.HasValue?0:1).ThenBy(x=>x.Executable,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.DataFolder).ToList();
    }
    public static List<BacktestFile> Files(string folder)
    {
        var result=new List<BacktestFile>();if(!Directory.Exists(folder))return result;
        var options=new EnumerationOptions{RecurseSubdirectories=true,IgnoreInaccessible=true,AttributesToSkip=FileAttributes.ReparsePoint};
        foreach(string path in Directory.EnumerateFiles(folder,"*",options))
        {
            string ext=Path.GetExtension(path).ToLowerInvariant();if(ext is not (".html" or ".htm" or ".xlsx" or ".tst"))continue;
            try{var file=new FileInfo(path);int? version=null;if(ext==".tst"){using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);using var reader=new BinaryReader(input);if(input.Length>=4)version=reader.ReadInt32();}result.Add(new(path,ext==".tst"?"Tester-Cache v"+version+(version==505?" (experimentell)":" (nicht unterstützt)"):ext==".xlsx"?"Excel-Bericht":"HTML-Bericht (Inhalt beim Import geprüft)",file.Length,file.LastWriteTime,version));}
            catch(IOException){}
        }
        return result.OrderByDescending(x=>x.Modified).ToList();
    }
}
