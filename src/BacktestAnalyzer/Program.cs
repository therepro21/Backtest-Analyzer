using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BacktestAnalyzer;
public static class Program
{
    [STAThread] public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
            if(args.Length>=3&&args[0]=="--export")
            {
                var r=Parser.Load(args[1],!args.Contains("--fifo"));var confirmed=args.Contains("--confirmed");var trades=r.Closed.Where(t=>!confirmed||!t.ModelDependent).ToList();
                var layout=args.Contains("--detail")?"detail":"quick";PdfExport.Save(r,trades,args[2],args.Contains("--dark"),layout,args.Contains("--appendix"));
                var jsonPath=Path.ChangeExtension(args[2],".json");var s=new Stats(trades);
                File.WriteAllText(jsonPath,JsonSerializer.Serialize(new{r.Platform,r.Hash,r.Strategy,r.Metadata,DealCount=r.Deals.Count,OpenCount=r.Trades.Count(t=>!t.Close.HasValue),Count=s.Count,FifoCount=trades.Count(t=>t.Estimated),ProfitModelCount=trades.Count(t=>t.MatchMethod=="P/L-Konsistenzmodell"),Net=s.Net,MeanSeconds=s.Mean,MedianSeconds=s.Median,MaxSeconds=s.Max,MinSeconds=s.Min,P90Seconds=s.Quantile(.90),P95Seconds=s.Quantile(.95),r.Warnings,Trades=trades.Select(t=>new{t.Id,t.Symbol,t.Side,t.Open,t.Close,t.Volume,t.Net,t.Seconds,t.Quality})},new JsonSerializerOptions{WriteIndented=true}));return 0;
            }
            var window=new MainWindow();
            if(args.Length>=3&&args[0]=="--preview")
            {
                window.Dark=args.Contains("--dark");window.Current=Parser.Load(args[1]);window.Render();window.Width=1440;window.Height=1040;
                var content=(UIElement)window.Content;window.Content=null;var surface=new Border{Child=content,Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(new Palette(window.Dark).Background)),Width=1440,Height=1040};
                surface.Measure(new Size(1440,1040));surface.Arrange(new Rect(0,0,1440,1040));surface.UpdateLayout();
                var target=new RenderTargetBitmap(1440,1040,96,96,PixelFormats.Pbgra32);target.Render(surface);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(target));using var file=File.Create(args[2]);encoder.Save(file);return 0;
            }
            if(args.Length>0&&File.Exists(args[0]))window.Current=Parser.Load(args[0]);
            app.ShutdownMode=ShutdownMode.OnMainWindowClose;app.Run(window);return 0;
        }
        catch(Exception ex){var path=Path.Combine(AppContext.BaseDirectory,"last-error.txt");try{File.WriteAllText(path,ex.ToString());}catch{}if(args.Length==0)MessageBox.Show(ex.Message,"Backtest-Analyzer");return 1;}
    }
}
public sealed class MainWindow:Window
{
    public bool Dark {get;set;}public Report? Current {get;set;}bool reportDark=false;bool onlyConfirmed=false;bool useProfitModel=true;bool appendix=false;
    string active="Haltezeiten";string layout="Quick Report (A4 quer)";string symbol="Alle Symbole";string side="Long + Short";
    string status="Beta 0.1 · lokal und offline · keine Verbindung zu MetaTrader";
    Palette P=>new(Dark);Brush B(string s)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(s));
    public MainWindow(){Title="Backtest-Analyzer | Beta 0.1";Width=1440;Height=1040;MinWidth=980;MinHeight=720;WindowStartupLocation=WindowStartupLocation.CenterScreen;AllowDrop=true;Drop+=async(_,e)=>{if(e.Data.GetData(DataFormats.FileDrop) is string[] files&&files.Length>0)await Import(files[0]);};Loaded+=(_,_)=>Render();}
    TextBlock Text(string value,double size=14,bool bold=false,string? color=null)=>new(){Text=value,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,Foreground=B(color??P.Ink),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,5)};
    Button Button(string text,Action action,bool primary=false){var b=new Button{Content=text,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,8,6),Background=B(primary?P.Positive:P.Surface),Foreground=B(primary?(Dark?"#112035":"#FFFFFF"):P.Ink),BorderBrush=B(P.Line),BorderThickness=new Thickness(1),FontSize=13};b.Click+=(_,_)=>action();return b;}
    ComboBox Select(IEnumerable<string> options,string selected,Action<string> changed){var box=new ComboBox{ItemsSource=options.ToList(),SelectedItem=selected,MinWidth=142,Margin=new Thickness(0,0,12,6),Padding=new Thickness(8,6,8,6),Background=B(P.Surface),Foreground=B(P.Ink),BorderBrush=B(P.Line)};box.SelectionChanged+=(_,_)=>{if(box.SelectedItem is string s&&s!=selected)changed(s);};return box;}
    CheckBox Check(string label,bool selected,Action<bool> changed){var box=new CheckBox{Content=label,IsChecked=selected,Foreground=B(P.Ink),Margin=new Thickness(0,6,15,8)};box.Click+=(_,_)=>changed(box.IsChecked==true);return box;}
    Border Panel(UIElement child,double padding=20)=>new(){Child=child,Background=B(P.Surface),CornerRadius=new CornerRadius(10),Padding=new Thickness(padding),Margin=new Thickness(0,0,15,15),BorderBrush=B(P.Line),BorderThickness=new Thickness(1)};
    List<Trade> Selected=>Current?.Closed.Where(t=>(!onlyConfirmed||!t.ModelDependent)&&(symbol=="Alle Symbole"||t.Symbol==symbol)&&(side=="Long + Short"||t.Side==(side=="Long"?"buy":"sell"))).ToList()??new();
    public void Render()
    {
        Background=B(P.Background);Foreground=B(P.Ink);FontFamily=new FontFamily("Segoe UI");
        var shell=new DockPanel{Margin=new Thickness(22)};
        var footer=new DockPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(footer,Dock.Bottom);
        var github=Button("GitHub ↗",()=>Process.Start(new ProcessStartInfo(PdfExport.Repository){UseShellExecute=true}));DockPanel.SetDock(github,Dock.Right);footer.Children.Add(github);footer.Children.Add(Text("© "+DateTime.Now.Year+" Michael P. Thiess · "+status,12,false,P.Muted));shell.Children.Add(footer);
        var header=new DockPanel{Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(header,Dock.Top);
        using(var stream=new MemoryStream(PdfExport.Logo)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();var badge=new Border{Background=Brushes.White,CornerRadius=new CornerRadius(8),Padding=new Thickness(3),Margin=new Thickness(0,0,16,0),Child=new Image{Source=bitmap,Width=78,Height=78}};DockPanel.SetDock(badge,Dock.Left);header.Children.Add(badge);}
        var right=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};right.Children.Add(Select(new[]{"App: Light","App: Dark"},Dark?"App: Dark":"App: Light",s=>{Dark=s.EndsWith("Dark");Render();}));right.Children.Add(Button("Bericht importieren",()=>ChooseFile(),true));DockPanel.SetDock(right,Dock.Right);header.Children.Add(right);
        var brand=new StackPanel{VerticalAlignment=VerticalAlignment.Center};brand.Children.Add(Text("Backtest-Analyzer",27,true));brand.Children.Add(Text("Trades verstehen. Haltezeiten sichtbar machen.  |  BETA",13,false,P.Muted));header.Children.Add(brand);shell.Children.Add(header);
        var tabs=new WrapPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(tabs,Dock.Top);foreach(var tab in new[]{"Übersicht","Haltezeiten","Zeitmuster","Trades","Datenprüfung"})tabs.Children.Add(Button(tab,()=>{active=tab;Render();},active==tab));shell.Children.Add(tabs);
        if(Current==null)
        {
            var start=new StackPanel{Margin=new Thickness(40,65,40,0)};start.Children.Add(Text("Dein Backtest. Klar ausgewertet.",32,true));start.Children.Add(Text("Importiere einen vollständigen MT4- oder MT5-HTML-Bericht. Alle Daten bleiben auf diesem Rechner.",17));start.Children.Add(Text("• Haltezeiten mit Median und Perzentilen\n• Diagramme mit Blau für Gewinn und Orange für Verlust\n• Einseitiger A4-Quick-Report im Querformat\n• Mehrseitige Detailanalyse im Hochformat\n• Light- und Darkmode für App und PDF",16));start.Children.Add(Text("HTML-Datei hier ablegen oder oben „Bericht importieren“ wählen. Archive bitte vorher entpacken.",14,false,P.Muted));shell.Children.Add(Panel(start));Content=shell;return;
        }
        var controls=new WrapPanel();DockPanel.SetDock(controls,Dock.Top);controls.Children.Add(Select(new[]{"Alle Symbole"}.Concat(Current.Closed.Select(t=>t.Symbol).Distinct()),symbol,s=>{symbol=s;Render();}));controls.Children.Add(Select(new[]{"Long + Short","Long","Short"},side,s=>{side=s;Render();}));controls.Children.Add(Check("Nur eindeutige History",onlyConfirmed,b=>{onlyConfirmed=b;Render();}));controls.Children.Add(Check("P/L-Konsistenzmodell",useProfitModel,b=>{useProfitModel=b;_=Import(Current.Source);}));shell.Children.Add(controls);
        var exports=new WrapPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(exports,Dock.Top);exports.Children.Add(Select(new[]{"Quick Report (A4 quer)","Detailanalyse (A4 hoch)"},layout,s=>layout=s));exports.Children.Add(Select(new[]{"PDF: Light","PDF: Dark"},reportDark?"PDF: Dark":"PDF: Light",s=>reportDark=s.EndsWith("Dark")));exports.Children.Add(Check("Trade-Anhang",appendix,b=>appendix=b));exports.Children.Add(Button("PDF erstellen",Export,true));exports.Children.Add(Button("Trades als CSV",ExportCsv));shell.Children.Add(exports);
        var content=new StackPanel();content.Children.Add(Text(Current.Strategy+" · "+Current.Platform,22,true));content.Children.Add(Text(Path.GetFileName(Current.Source)+" | "+Selected.Count+" geschlossene Entry-Lots | Kalenderzeit (Broker)",12,false,P.Muted));
        var s=new Stats(Selected);var inferred=s.Trades.Count(t=>t.MatchMethod=="P/L-Konsistenzmodell");var fifo=s.Trades.Count(t=>t.Estimated);
        if(fifo>0||inferred>0){var caution=new Border{Background=B(Dark?"#352919":"#FFF1DA"),Padding=new Thickness(14),CornerRadius=new CornerRadius(6),Margin=new Thickness(0,8,15,16),Child=Text($"Zuordnung: {fifo} FIFO-Schätzungen · {inferred} P/L-Modellzuordnungen. Das Modell prüft Preise und Gewinn, ersetzt aber keine Position-ID. „Nur eindeutige History“ schließt beide aus.",13,false,Dark?"#FFD395":"#744600")};content.Children.Add(caution);}
        if(active is "Haltezeiten" or "Übersicht")
        {
            var metrics=new WrapPanel();void Metric(string label,string value){var group=new StackPanel{Width=205};group.Children.Add(Text(label,12,false,P.Muted));group.Children.Add(Text(value,22,true));metrics.Children.Add(Panel(group,15));}
            if(active=="Haltezeiten"){Metric("Durchschnitt",s.Count>0?Stats.Duration(s.Mean):"—");Metric("Median",s.Count>0?Stats.Duration(s.Median):"—");Metric("Maximum",s.Count>0?Stats.Duration(s.Max):"—");Metric("95. Perzentil",s.Count>0?Stats.Duration(s.Quantile(.95)):"—");}
            else{Metric("Netto (Kontowährung)",s.Net.ToString("N2"));Metric("Gewinn / Verlust / Null",$"{s.Wins} / {s.Losses} / {s.Count-s.Wins-s.Losses}");Metric("Profitfaktor (Netto)",s.ProfitFactor?.ToString("N2")??"nicht definiert");Metric("Abgeschlossene Entry-Lots",s.Count.ToString());}content.Children.Add(metrics);
        }
        var charts=new WrapPanel();void Chart(ChartKind kind){charts.Children.Add(new Border{Child=new ChartView{Report=Current,Trades=s.Trades,Palette=P,Kind=kind,Width=570,Height=280},Margin=new Thickness(0,0,15,15),CornerRadius=new CornerRadius(10),Background=B(P.Surface),ClipToBounds=true});}
        if(active=="Haltezeiten"){Chart(ChartKind.Histogram);Chart(ChartKind.Ecdf);Chart(ChartKind.Boxplot);Chart(ChartKind.Scatter);content.Children.Add(charts);content.Children.Add(Text($"Minimum: {Stats.Duration(s.Min)} · P90: {Stats.Duration(s.Quantile(.9))} · Streuung: {Stats.Duration(s.Std)} · Volumengewichtet: {Stats.Duration(s.Weighted)}",13,false,P.Muted));}
        else if(active=="Übersicht"){Chart(ChartKind.Balance);Chart(ChartKind.Drawdown);Chart(ChartKind.Monthly);Chart(ChartKind.Scatter);content.Children.Add(charts);content.Children.Add(Text("Balance-Charts zeigen die Gesamtdatei. Trade-Kennzahlen und übrige Diagramme berücksichtigen die gewählten Filter.",13,false,P.Muted));}
        else if(active=="Zeitmuster"){Chart(ChartKind.Hourly);Chart(ChartKind.Weekday);Chart(ChartKind.Monthly);content.Children.Add(charts);content.Children.Add(Text("Stunden und Wochentage beziehen sich auf den Einstieg in Broker-Zeit. Monate beziehen sich auf den Vollschluss.",13,false,P.Muted));}
        else if(active=="Trades")
        {
            var grid=new DataGrid{ItemsSource=s.Trades.OrderByDescending(t=>t.Seconds),AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Height=520,Background=B(P.Surface),Foreground=B(P.Ink),RowBackground=B(P.Surface),AlternatingRowBackground=B(P.Background),BorderBrush=B(P.Line),GridLinesVisibility=DataGridGridLinesVisibility.Horizontal,HorizontalGridLinesBrush=B(P.Line),HeadersVisibility=DataGridHeadersVisibility.Column};
            var headStyle=new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));headStyle.Setters.Add(new Setter(Control.BackgroundProperty,B(P.Background)));headStyle.Setters.Add(new Setter(Control.ForegroundProperty,B(P.Ink)));headStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(8)));grid.ColumnHeaderStyle=headStyle;
            foreach(var col in new[]{("ID","Id"),("Symbol","Symbol"),("Richtung","Side"),("Einstieg","OpenText"),("Vollschluss","CloseText"),("Haltezeit","Duration"),("Netto","Result"),("Zuordnung","Quality")})grid.Columns.Add(new DataGridTextColumn{Header=col.Item1,Binding=new System.Windows.Data.Binding(col.Item2),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});content.Children.Add(grid);
        }
        else
        {
            content.Children.Add(Text("Import und Berechnung",17,true));foreach(var warning in Current.Warnings.Distinct())content.Children.Add(Text("• "+warning,13));content.Children.Add(Text("SHA-256: "+Current.Hash,11,false,P.Muted));content.Children.Add(Text("Originalkennzahlen aus MetaTrader",17,true));foreach(var pair in Current.Metadata)content.Children.Add(Text(pair.Key+": "+pair.Value,13));content.Children.Add(Text("BETA · Mögliche Fehler · Keine Anlageberatung · Unabhängiges Projekt ohne Verbindung zu MetaQuotes. Haftung nur soweit gesetzlich zulässig ausgeschlossen. Details: DISCLAIMER.md im Repository.",13,false,P.Muted));
        }
        shell.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Content=shell;
    }
    async void ChooseFile(){var dialog=new OpenFileDialog{Filter="MetaTrader HTML-Bericht|*.html;*.htm",Title="Vollständigen Backtestbericht auswählen"};if(dialog.ShowDialog()==true)await Import(dialog.FileName);}
    async Task Import(string path)
    {
        try{status="Import läuft ...";var report=await Task.Run(()=>Parser.Load(path,useProfitModel));Current=report;symbol="Alle Symbole";status=$"Beta 0.1 · {report.Deals.Count} Deals importiert · offline";Render();}
        catch(Exception ex){status="Import fehlgeschlagen";MessageBox.Show(this,ex.Message,"Importprüfung",MessageBoxButton.OK,MessageBoxImage.Warning);Render();}
    }
    void Export(){if(Current==null)return;var file=new SaveFileDialog{Filter="PDF-Bericht|*.pdf",FileName="Backtest-"+DateTime.Now.ToString("yyyyMMdd-HHmm")+".pdf"};if(file.ShowDialog()==true)try{PdfExport.Save(Current,Selected,file.FileName,reportDark,layout.StartsWith("Quick")?"quick":"detail",appendix);status="PDF gespeichert: "+Path.GetFileName(file.FileName);Render();Process.Start(new ProcessStartInfo(file.FileName){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message,"PDF-Export fehlgeschlagen");}}
    void ExportCsv(){var file=new SaveFileDialog{Filter="CSV-Datei|*.csv",FileName="Backtest-Trades.csv"};if(file.ShowDialog()!=true)return;try{string Quote(string s)=>"\""+s.Replace("\"","\"\"")+"\"";var lines=new List<string>{"Id;Symbol;Side;Open;Close;Volume;Net;HoldingSeconds;Quality"};lines.AddRange(Selected.Select(t=>string.Join(";",new[]{t.Id,t.Symbol,t.Side,t.OpenText,t.CloseText,t.Volume.ToString(CultureInfo.InvariantCulture),t.Net.ToString(CultureInfo.InvariantCulture),t.Seconds.ToString(CultureInfo.InvariantCulture),t.Quality}.Select(Quote))));File.WriteAllLines(file.FileName,lines,new UTF8Encoding(true));status="CSV gespeichert";Render();}catch(Exception ex){MessageBox.Show(this,ex.Message,"CSV-Export fehlgeschlagen");}}
}
