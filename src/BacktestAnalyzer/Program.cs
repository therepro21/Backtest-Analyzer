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
            if(args.Length>=2&&args[0]=="--discover"){File.WriteAllText(args[1],JsonSerializer.Serialize(TerminalDiscovery.Find(!args.Contains("--all")),new JsonSerializerOptions{WriteIndented=true}));return 0;}
            if(args.Length>=3&&args[0]=="--export")
            {
                var r=Parser.Load(args[1],!args.Contains("--fifo"));var confirmed=args.Contains("--confirmed");var trades=r.Closed.Where(t=>!confirmed||!t.ModelDependent).ToList();
                int cacheIndex=Array.IndexOf(args,"--cache");if(cacheIndex>=0&&cacheIndex+1<args.Length){TesterCache.Attach(r,args[cacheIndex+1]);trades=r.Closed.Where(t=>!confirmed||!t.ModelDependent).ToList();}
                var layout=args.Contains("--detail")?"detail":"quick";PdfExport.Save(r,trades,args[2],args.Contains("--dark"),layout,args.Contains("--appendix"));
                var jsonPath=Path.ChangeExtension(args[2],".json");var s=new Stats(trades);
                File.WriteAllText(jsonPath,JsonSerializer.Serialize(new{r.Platform,r.Hash,r.Strategy,r.Metadata,DealCount=r.Deals.Count,OpenCount=r.Trades.Count(t=>!t.Close.HasValue),Count=s.Count,FifoCount=trades.Count(t=>t.Estimated),ProfitModelCount=trades.Count(t=>t.MatchMethod=="P/L-Konsistenzmodell"),Net=s.Net,MeanSeconds=s.Mean,MedianSeconds=s.Median,MaxSeconds=s.Max,MinSeconds=s.Min,P90Seconds=s.Quantile(.90),P95Seconds=s.Quantile(.95),r.Warnings,Trades=trades.Select(t=>new{t.Id,t.Symbol,t.Side,t.Open,t.Close,t.Volume,t.Net,t.Seconds,t.Quality})},new JsonSerializerOptions{WriteIndented=true}));return 0;
            }
            if(args.Length>=3&&args[0]=="--export-html") {var report=Parser.Load(args[1]);int ci=Array.IndexOf(args,"--cache");if(ci>=0&&ci+1<args.Length)TesterCache.Attach(report,args[ci+1]);HtmlExport.Save(report,args[2],args.Contains("--dark"));return 0;}
            var window=new MainWindow();
            if(args.Length>=3&&args[0]=="--preview")
            {
                window.Dark=args.Contains("--dark");window.Current=Parser.Load(args[1]);int vi=Array.IndexOf(args,"--view"),yi=Array.IndexOf(args,"--year");window.ConfigurePreview(vi>=0?args[vi+1]:null,yi>=0?args[yi+1]:null);window.Render();window.Width=1440;window.Height=1040;
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
    string selectedYear="Gesamter Test";Report? viewReport;
    string active="Haltezeiten";string layout="Quick Report (A4 quer)";string symbol="Alle Symbole";string side="Long + Short";
    string status="Beta 1.0 · lokal und offline · unabhängiger Analyzer";
    Palette P=>new(Dark);Brush B(string s)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(s));
    public MainWindow(){Title="Backtest-Analyzer | Beta 0.3";Width=1440;Height=1040;MinWidth=980;MinHeight=720;WindowStartupLocation=WindowStartupLocation.CenterScreen;AllowDrop=true;Drop+=async(_,e)=>{if(e.Data.GetData(DataFormats.FileDrop) is string[] files&&files.Length>0)await Import(files[0]);};Loaded+=(_,_)=>Render();}
    public void ConfigurePreview(string? tab,string? year){if(tab!=null)active=tab;if(year!=null)selectedYear=year;}
    TextBlock Text(string value,double size=14,bool bold=false,string? color=null)=>new(){Text=value,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,Foreground=B(color??P.Ink),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,5)};
    Button Button(string text,Action action,bool primary=false){var b=new Button{Content=text,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,8,6),Background=B(primary?P.Positive:P.Surface),Foreground=B(primary?(Dark?"#112035":"#FFFFFF"):P.Ink),BorderBrush=B(P.Line),BorderThickness=new Thickness(1),FontSize=13};b.Click+=(_,_)=>action();return b;}
    ComboBox Select(IEnumerable<string> options,string selected,Action<string> changed){var box=new ComboBox{ItemsSource=options.ToList(),SelectedItem=selected,MinWidth=142,Margin=new Thickness(0,0,12,6),Padding=new Thickness(8,6,8,6),Background=Brushes.White,Foreground=Brushes.Black,BorderBrush=B(P.Line)};box.SelectionChanged+=(_,_)=>{if(box.SelectedItem is string s&&s!=selected)changed(s);};return box;}
    CheckBox Check(string label,bool selected,Action<bool> changed){var box=new CheckBox{Content=label,IsChecked=selected,Foreground=B(P.Ink),Margin=new Thickness(0,6,15,8)};box.Click+=(_,_)=>changed(box.IsChecked==true);return box;}
    Border Panel(UIElement child,double padding=20)=>new(){Child=child,Background=B(P.Surface),CornerRadius=new CornerRadius(10),Padding=new Thickness(padding),Margin=new Thickness(0,0,15,15),BorderBrush=B(P.Line),BorderThickness=new Thickness(1)};
    List<Trade> ExportSelection=>Current?.Closed.Where(t=>(!onlyConfirmed||!t.ModelDependent)&&(symbol=="Alle Symbole"||t.Symbol==symbol)&&(side=="Long + Short"||t.Side==(side=="Long"?"buy":"sell"))).ToList()??new();
    List<Trade> Selected=>(viewReport??Current)?.Closed.Where(t=>(!onlyConfirmed||!t.ModelDependent)&&(symbol=="Alle Symbole"||t.Symbol==symbol)&&(side=="Long + Short"||t.Side==(side=="Long"?"buy":"sell"))).ToList()??new();
    public void Render()
    {
        viewReport=Current==null?null:selectedYear=="Gesamter Test"?Current:YearAnalysis.ForYear(Current,int.Parse(selectedYear));
        Background=B(P.Background);Foreground=B(P.Ink);FontFamily=new FontFamily("Segoe UI");
        var shell=new DockPanel{Margin=new Thickness(22)};
        var footer=new DockPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(footer,Dock.Bottom);
        var github=Button("GitHub ↗",()=>Process.Start(new ProcessStartInfo(PdfExport.Repository){UseShellExecute=true}));DockPanel.SetDock(github,Dock.Right);footer.Children.Add(github);footer.Children.Add(Text("© "+DateTime.Now.Year+" Michael P. Thiess · "+status,12,false,P.Muted));shell.Children.Add(footer);
        var header=new DockPanel{Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(header,Dock.Top);
        using(var stream=new MemoryStream(PdfExport.Logo)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();var badge=new Border{Background=Brushes.White,CornerRadius=new CornerRadius(8),Padding=new Thickness(3),Margin=new Thickness(0,0,16,0),Child=new Image{Source=bitmap,Width=78,Height=78}};DockPanel.SetDock(badge,Dock.Left);header.Children.Add(badge);}
        var right=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};right.Children.Add(Select(new[]{"App: Light","App: Dark"},Dark?"App: Dark":"App: Light",s=>{Dark=s.EndsWith("Dark");Render();}));right.Children.Add(Button("MT5 finden",()=>ChooseTerminal()));right.Children.Add(Button("Bericht importieren",()=>ChooseFile(),true));DockPanel.SetDock(right,Dock.Right);header.Children.Add(right);
        var brand=new StackPanel{VerticalAlignment=VerticalAlignment.Center};brand.Children.Add(Text("Backtest-Analyzer",27,true));brand.Children.Add(Text("Trades verstehen. Haltezeiten sichtbar machen.  |  BETA",13,false,P.Muted));header.Children.Add(brand);shell.Children.Add(header);
        var tabs=new WrapPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(tabs,Dock.Top);foreach(var tab in new[]{"Übersicht","Haltezeiten","Zeitmuster","Trades","Datenprüfung"})tabs.Children.Add(Button(tab,()=>{active=tab;Render();},active==tab));shell.Children.Add(tabs);
        if(Current==null)
        {
            var start=new StackPanel{Margin=new Thickness(40,65,40,0)};start.Children.Add(Text("Dein Backtest. Klar ausgewertet.",32,true));start.Children.Add(Text("Importiere einen vollständigen MT4-HTML- oder MT5-HTML/XLSX-Bericht. Alle Daten bleiben auf diesem Rechner.",17));start.Children.Add(Text("• Haltezeiten mit Median und Perzentilen\n• Diagramme mit Blau für Gewinn und Orange für Verlust\n• Einseitiger A4-Quick-Report im Querformat\n• Mehrseitige Detailanalyse im Hochformat\n• Light- und Darkmode für App und PDF",16));start.Children.Add(Text("HTML- oder XLSX-Datei hier ablegen oder oben „Bericht importieren“ wählen. Archive bitte vorher entpacken.",14,false,P.Muted));shell.Children.Add(Panel(start));Content=shell;return;
        }
        var controls=new WrapPanel();DockPanel.SetDock(controls,Dock.Top);controls.Children.Add(Select(new[]{"Gesamter Test"}.Concat(YearAnalysis.Scopes(Current).Skip(1).Select(r=>r.AnalysisYear!.Value.ToString())),selectedYear,v=>{selectedYear=v;Render();}));controls.Children.Add(Select(new[]{"Balance: Rohbuchungen","Balance: 5 s","Balance: 60 s","Balance: 120 s"},Current.BalanceWindowSeconds==0?"Balance: Rohbuchungen":"Balance: "+Current.BalanceWindowSeconds+" s",v=>{Current.BalanceWindowSeconds=v.Contains("Roh")?0:int.Parse(v.Split(' ')[1]);Render();}));controls.Children.Add(Select(new[]{"Haltezeit: Kalender","Haltezeit: ohne Sa/So"},Current.ExcludeWeekends?"Haltezeit: ohne Sa/So":"Haltezeit: Kalender",v=>{Current.ExcludeWeekends=v.EndsWith("Sa/So");Render();}));controls.Children.Add(Select(new[]{"Alle Symbole"}.Concat(Current.Closed.Select(t=>t.Symbol).Distinct()),symbol,s=>{symbol=s;Render();}));controls.Children.Add(Select(new[]{"Long + Short","Long","Short"},side,s=>{side=s;Render();}));controls.Children.Add(Check("Nur eindeutige History",onlyConfirmed,b=>{onlyConfirmed=b;Render();}));if(Current.EquitySource.Length==0)controls.Children.Add(Check("P/L-Konsistenzmodell",useProfitModel,b=>{useProfitModel=b;_=Import(Current.Source);}));shell.Children.Add(controls);
        var exports=new WrapPanel{Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(exports,Dock.Top);exports.Children.Add(Select(new[]{"Quick Report (A4 quer)","Detailanalyse (A4 hoch)"},layout,s=>layout=s));exports.Children.Add(Select(new[]{"PDF: Light","PDF: Dark"},reportDark?"PDF: Dark":"PDF: Light",s=>reportDark=s.EndsWith("Dark")));exports.Children.Add(Check("Trade-Anhang",appendix,b=>appendix=b));exports.Children.Add(Button("PDF erstellen",Export,true));exports.Children.Add(Button("Interaktiver HTML-Report",ExportHtml));exports.Children.Add(Button("Trades als CSV",ExportCsv));exports.Children.Add(Button("Passenden Cache ergänzen",AttachCache));shell.Children.Add(exports);
        var content=new StackPanel();content.Children.Add(Text(Current.Strategy+" · "+Current.Platform,22,true));content.Children.Add(Text(Path.GetFileName(Current.Source)+" | "+Selected.Count+" abgeschlossene Trades | Kalenderzeit (Broker)",12,false,P.Muted));
        var s=new Stats(Selected,Current.ExcludeWeekends);var inferred=s.Trades.Count(t=>t.MatchMethod=="P/L-Konsistenzmodell"&&!t.Estimated);var fifo=s.Trades.Count(t=>t.Estimated);
        if(fifo>0||inferred>0){var caution=new Border{Background=B(Dark?"#352919":"#FFF1DA"),Padding=new Thickness(14),CornerRadius=new CornerRadius(6),Margin=new Thickness(0,8,15,16),Child=Text($"Zuordnung: {fifo} FIFO-Schätzungen · {inferred} P/L-Modellzuordnungen. Das Modell prüft Preise und Gewinn, ersetzt aber keine Position-ID. „Nur eindeutige History“ schließt beide aus.",13,false,Dark?"#FFD395":"#744600")};content.Children.Add(caution);}
        if(active is "Haltezeiten" or "Übersicht")
        {
            var metrics=new WrapPanel();void Metric(string label,string value){var group=new StackPanel{Width=205};group.Children.Add(Text(label,12,false,P.Muted));group.Children.Add(Text(value,22,true));metrics.Children.Add(Panel(group,15));}
            if(active=="Haltezeiten"){Metric("Durchschnitt",s.Count>0?Stats.Duration(s.Mean):"—");Metric("Mittlere Haltezeit (50 %)",s.Count>0?Stats.Duration(s.Median):"—");Metric("Längste Haltezeit",s.Count>0?Stats.Duration(s.Max):"—");Metric("95 % geschlossen nach",s.Count>0?Stats.Duration(s.Quantile(.95)):"—");}
            else{Metric("Netto (Kontowährung)",s.Net.ToString("N2"));Metric("Gewinn / Verlust / Null",$"{s.Wins} / {s.Losses} / {s.Count-s.Wins-s.Losses}");Metric("Profitfaktor (Netto)",s.ProfitFactor?.ToString("N2")??"nicht definiert");Metric("Ababgeschlossene Trades",s.Count.ToString());}content.Children.Add(metrics);
            if(active=="Übersicht"){var adjusted=SeriesAnalysis.Adjusted(viewReport!);if(adjusted.Time.HasValue){content.Children.Add(Text($"Um letzten Zyklus bereinigt: {adjusted.Profit:N2} {Current.Find("Währung","Currency")} · Gesamtgewinn am {adjusted.Time:dd.MM.yyyy HH:mm:ss}",16,true));content.Children.Add(Text($"Testende-Zyklus separat: {adjusted.Excluded:N2}",12,false,P.Negative));}}

        }
        if(active=="Haltezeiten"||active=="Übersicht") content.Children.Add(Text("MT5-Original (Gesamttest): Ø Haltezeit "+Current.Find("Durchschnittliche Positionshaltedauer","Average position holding time")+" · Maximum "+Current.Find("Maximale Positionshaltedauer","Maximum position holding time")+". Kalenderzeit und Zeit ohne Sa/So sind getrennte Berechnungen.",13,false,P.Muted));
        if(active=="Haltezeiten")
        {
            var frequency=new WrapPanel();frequency.Children.Add(Select(new[]{"5 min","15 min","30 min","60 min"},Current.HoldingBinMinutes+" min",v=>{Current.HoldingBinMinutes=int.Parse(v.Split(' ')[0]);Render();}));frequency.Children.Add(Select(new[]{"Prozent","Anzahl"},Current.HoldingAsPercent?"Prozent":"Anzahl",v=>{Current.HoldingAsPercent=v=="Prozent";Render();}));frequency.Children.Add(Select(new[]{"Gesamte Dauer","0–6 Stunden","0–24 Stunden"},Current.HoldingMaxHours==0?"Gesamte Dauer":Current.HoldingMaxHours==6?"0–6 Stunden":"0–24 Stunden",v=>{Current.HoldingMaxHours=v=="Gesamte Dauer"?0:v=="0–6 Stunden"?6:24;Render();}));content.Children.Add(frequency);
        }
        var charts=new WrapPanel();void Chart(ChartKind kind){charts.Children.Add(new Border{Child=new ChartView{Report=viewReport!,Trades=s.Trades,Palette=P,Kind=kind,Width=570,Height=280},Margin=new Thickness(0,0,15,15),CornerRadius=new CornerRadius(10),Background=B(P.Surface),ClipToBounds=true});}
        if(active=="Haltezeiten"){Chart(ChartKind.Histogram);Chart(ChartKind.Ecdf);Chart(ChartKind.Boxplot);Chart(ChartKind.Scatter);content.Children.Add(charts);content.Children.Add(Text($"Minimum: {Stats.Duration(s.Min)} · P90: {Stats.Duration(s.Quantile(.9))} · Streuung: {Stats.Duration(s.Std)} · Volumengewichtet: {Stats.Duration(s.Weighted)}",13,false,P.Muted));}
        else if(active=="Übersicht"){charts.Children.Add(new ChartView{Report=viewReport!,Trades=s.Trades,Palette=P,Kind=ChartKind.Balance,Width=1160,Height=380});if(viewReport!.EquityPoints.Count>0)charts.Children.Add(new ChartView{Report=viewReport,Trades=s.Trades,Palette=P,Kind=ChartKind.Equity,Width=1160,Height=380});Chart(ChartKind.Monthly);Chart(ChartKind.Scatter);content.Children.Add(charts);content.Children.Add(Text("Balance-Charts zeigen die Gesamtdatei. Trade-Kennzahlen und übrige Diagramme berücksichtigen die gewählten Filter.",13,false,P.Muted));}
        else if(active=="Zeitmuster")
        {
            var seriesControls=new WrapPanel();seriesControls.Children.Add(Select(new[]{"6 h","12 h","24 h","48 h","72 h"},Current.LongSeriesHours+" h",v=>{Current.LongSeriesHours=double.Parse(v.Split(' ')[0]);Render();}));seriesControls.Children.Add(Select(new[]{"Serien: Anzahl","Serien: Anteil (%)"},Current.SeriesAsPercent?"Serien: Anteil (%)":"Serien: Anzahl",v=>{Current.SeriesAsPercent=v.Contains("Anteil");Render();}));content.Children.Add(seriesControls);
            charts.Children.Add(new ChartView{Report=viewReport!,Trades=s.Trades,Palette=P,Kind=ChartKind.SeriesHeatmap,Width=1160,Height=360});charts.Children.Add(new ChartView{Report=viewReport!,Trades=s.Trades,Palette=P,Kind=ChartKind.SeriesCurve,Width=1160,Height=360});Chart(ChartKind.Hourly);Chart(ChartKind.Weekday);Chart(ChartKind.Monthly);content.Children.Add(charts);
            content.Children.Add(Text("10 ungünstigste Startzeitfenster · Anteil langer regulärer Zyklen",17,true));
            foreach(var g in SeriesAnalysis.Regular(viewReport!).GroupBy(c=>new{Day=((int)c.Start.DayOfWeek+6)%7,Hour=c.Start.Hour}).Select(g=>new{g.Key.Day,g.Key.Hour,Total=g.Count(),Long=g.Count(c=>c.WeekdaySeconds>Current.LongSeriesHours*3600)}).Where(g=>g.Long>0).OrderByDescending(g=>g.Long/(double)g.Total).ThenByDescending(g=>g.Total).Take(10))content.Children.Add(Text(new[]{"Montag","Dienstag","Mittwoch","Donnerstag","Freitag","Samstag","Sonntag"}[g.Day]+$" {g.Hour:00}:00–{g.Hour+1:00}:00 · {g.Long} lange / {g.Total} Starts · {100d*g.Long/g.Total:N1} %"+(g.Total<20?" · wenige Fälle":""),13));
            content.Children.Add(Text("Serien: erster Einstieg bis alle Positionen geschlossen, Dauer ohne Sa/So; vollständige Konto-History. Stunden und Wochentage beziehen sich auf den Start in Broker-Zeit. Monatsbalken zeigen Deal-Buchungen.",13,false,P.Muted));
        }
        else if(active=="Trades")
        {
            var grid=new DataGrid{ItemsSource=s.Trades.OrderByDescending(t=>t.Seconds),AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Height=520,Background=B(P.Surface),Foreground=B(P.Ink),RowBackground=B(P.Surface),AlternatingRowBackground=B(P.Background),BorderBrush=B(P.Line),GridLinesVisibility=DataGridGridLinesVisibility.Horizontal,HorizontalGridLinesBrush=B(P.Line),HeadersVisibility=DataGridHeadersVisibility.Column};
            var headStyle=new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));headStyle.Setters.Add(new Setter(Control.BackgroundProperty,B(P.Background)));headStyle.Setters.Add(new Setter(Control.ForegroundProperty,B(P.Ink)));headStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(8)));grid.ColumnHeaderStyle=headStyle;
            foreach(var col in new[]{("ID","Id"),("Symbol","Symbol"),("Richtung","Side"),("Einstieg","OpenText"),("Vollschluss","CloseText"),("Haltezeit","Duration"),("Netto","Result"),("Zuordnung","Quality")})grid.Columns.Add(new DataGridTextColumn{Header=col.Item1,Binding=new System.Windows.Data.Binding(col.Item2),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});content.Children.Add(grid);
        }
        else
        {
            content.Children.Add(Text("Import und Berechnung",17,true));foreach(var warning in Current.Warnings.Distinct())content.Children.Add(Text("• "+warning,13));content.Children.Add(Text("SHA-256: "+Current.Hash,11,false,P.Muted));
            void Compact(IEnumerable<(string Name,string Value)> fields){var entries=fields.ToList();int columns=entries.Count>40?3:2;var grid=new WrapPanel();double width=(Math.Max(940,ActualWidth)-110)/columns;foreach(var pair in entries){var item=new StackPanel{Width=width,Margin=new Thickness(0,0,12,12)};var label=Text(pair.Name,10,false,P.Muted);label.FontWeight=FontWeights.Light;label.Margin=new Thickness(0);item.Children.Add(label);item.Children.Add(Text(pair.Value,17,true));grid.Children.Add(item);}content.Children.Add(grid);}
            content.Children.Add(Text("Originalkennzahlen aus MetaTrader · Gesamttest",17,true));Compact(Current.Metadata.Where(m=>!new[]{"eingaben","inputs","parameters"}.Contains(Parser.Key(m.Key))).Select(m=>(m.Key,m.Value)));
            content.Children.Add(Text("Strategieeingaben · Gesamttest",17,true));Compact(Current.InputParameters.Where(p=>p.Trim()!="=").Select(p=>{int split=p.IndexOf('=');return(split>=0?p[..split]:p,split>=0?p[(split+1)..]:"");}));
            content.Children.Add(Text("BETA · Mögliche Fehler · Keine Anlageberatung · Unabhängiges Projekt ohne Verbindung zu MetaQuotes. Haftung nur soweit gesetzlich zulässig ausgeschlossen. Details: DISCLAIMER.md im Repository.",13,false,P.Muted));
        }
        shell.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Content=shell;
    }
    async void AttachCache(){if(Current==null)return;var picker=new OpenFileDialog{Filter="MT5-Tester-Cache|*.tst",Title="Passenden Cache auswählen (jede Deal-Zeile wird abgeglichen)"};if(picker.ShowDialog()!=true)return;try{var source=Current;await Task.Run(()=>TesterCache.Attach(source,picker.FileName));status="Cache abgeglichen: echte Position-IDs und Equity verfügbar";Render();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Cache-Zuordnung");}}
    async void ChooseFile(){var dialog=new OpenFileDialog{Filter="MetaTrader-Bericht oder Tester-Cache|*.html;*.htm;*.xlsx;*.tst",Title="Vollständigen Backtestbericht oder MT5-Cache auswählen"};if(dialog.ShowDialog()==true)await Import(dialog.FileName);}
    void ChooseTerminal()
    {
        var window=new Window{Owner=this,Title="MT5-Instanzen und gespeicherte Backtests",Width=1050,Height=740,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var panel=new DockPanel{Margin=new Thickness(18)};var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);
        var note=new TextBlock{Text="Installationen werden über Programm-Pfad, PID und Datenordner unterschieden. Portable Datenordner können manuell gewählt werden. Cache-Version 505 wird experimentell direkt importiert; andere Versionen werden abgewiesen.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};top.Children.Add(note);
        var controls=new StackPanel{Orientation=Orientation.Horizontal};var opened=new CheckBox{Content="Nur geöffnete MT5",IsChecked=true,Margin=new Thickness(0,8,20,8)};controls.Children.Add(opened);
        var refresh=new Button{Content="Aktualisieren",Padding=new Thickness(12,6,12,6)};controls.Children.Add(refresh);
        var manual=new Button{Content="Daten-/Exportordner wählen",Padding=new Thickness(12,6,12,6),Margin=new Thickness(12,0,0,0)};controls.Children.Add(manual);top.Children.Add(controls);
        var terminals=new ListBox{Height=190,Margin=new Thickness(0,12,0,12),DisplayMemberPath="Label"};top.Children.Add(terminals);panel.Children.Add(top);
        var files=new ListBox{DisplayMemberPath="Label"};panel.Children.Add(files);
        var bottom=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);panel.Children.Remove(files);panel.Children.Add(bottom);panel.Children.Add(files);
        var import=new Button{Content="Ausgewählten Bericht importieren",Padding=new Thickness(12,8,12,8),IsEnabled=false};bottom.Children.Add(import);
        var summary=new TextBlock{Margin=new Thickness(16,8,0,0)};bottom.Children.Add(summary);
        int scanGeneration=0;
        async Task Scan(string folder){int generation=++scanGeneration;files.ItemsSource=null;summary.Text="Dateisuche läuft …";try{var found=await Task.Run(()=>TerminalDiscovery.Files(folder));if(generation!=scanGeneration)return;files.ItemsSource=found;summary.Text=$"{found.Count(f=>Path.GetExtension(f.Path)!=".tst")} Berichte · {found.Count(f=>Path.GetExtension(f.Path)==".tst")} Tester-Caches";}catch(Exception ex){if(generation==scanGeneration)summary.Text=ex.Message;}}
        void Refresh(){scanGeneration++;terminals.ItemsSource=TerminalDiscovery.Find(opened.IsChecked==true);files.ItemsSource=null;}
        terminals.SelectionChanged+=async(_,_)=>{if(terminals.SelectedItem is TerminalInstance terminal)await Scan(terminal.DataFolder);};
        files.SelectionChanged+=(_,_)=>import.IsEnabled=files.SelectedItem is BacktestFile file&&file.Importable;
        import.Click+=async(_,_)=>{if(files.SelectedItem is BacktestFile file&&file.Importable){window.Close();await Import(file.Path);}};
        refresh.Click+=(_,_)=>Refresh();opened.Click+=(_,_)=>Refresh();manual.Click+=async(_,_)=>{var picker=new OpenFolderDialog{Title="MT5-Datenordner oder Ordner mit exportierten Backtests"};if(picker.ShowDialog(window)==true)await Scan(picker.FolderName);};
        window.Content=panel;Refresh();window.ShowDialog();
    }
    async Task Import(string path)
    {
        try{status="Import läuft ...";var report=await Task.Run(()=>Parser.Load(path,useProfitModel));Current=report;selectedYear="Gesamter Test";symbol="Alle Symbole";status=$"Beta 1.0 · {report.Deals.Count:N0} Deals · Import {report.ImportMilliseconds/1000:F2} s · Zuordnung {report.ReconstructionMilliseconds/1000:F2} s";Render();}
        catch(Exception ex){status="Import fehlgeschlagen";MessageBox.Show(this,ex.Message,"Importprüfung",MessageBoxButton.OK,MessageBoxImage.Warning);Render();}
    }
    void Export(){if(Current==null)return;var file=new SaveFileDialog{Filter="PDF-Bericht|*.pdf",FileName="Backtest-"+DateTime.Now.ToString("yyyyMMdd-HHmm")+".pdf"};if(file.ShowDialog()==true)try{PdfExport.Save(Current,ExportSelection,file.FileName,reportDark,layout.StartsWith("Quick")?"quick":"detail",appendix);status="PDF gespeichert: "+Path.GetFileName(file.FileName);Render();Process.Start(new ProcessStartInfo(file.FileName){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message,"PDF-Export fehlgeschlagen");}}
    void ExportCsv(){var file=new SaveFileDialog{Filter="CSV-Datei|*.csv",FileName="Backtest-Trades.csv"};if(file.ShowDialog()!=true)return;try{string Quote(string s)=>"\""+s.Replace("\"","\"\"")+"\"";var lines=new List<string>{"Id;Symbol;Side;Open;Close;Volume;Net;HoldingSeconds;Quality"};lines.AddRange(Selected.Select(t=>string.Join(";",new[]{t.Id,t.Symbol,t.Side,t.OpenText,t.CloseText,t.Volume.ToString(CultureInfo.InvariantCulture),t.Net.ToString(CultureInfo.InvariantCulture),t.Seconds.ToString(CultureInfo.InvariantCulture),t.Quality}.Select(Quote))));File.WriteAllLines(file.FileName,lines,new UTF8Encoding(true));status="CSV gespeichert";Render();}catch(Exception ex){MessageBox.Show(this,ex.Message,"CSV-Export fehlgeschlagen");}}
    void ExportHtml(){if(Current==null)return;var file=new SaveFileDialog{Filter="Interaktiver HTML-Report|*.html",FileName="Backtest-Interaktiv-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".html"};if(file.ShowDialog()!=true)return;try{HtmlExport.Save(Current,file.FileName,reportDark);status="Interaktiver Report gespeichert (Gesamtdatei, ohne Tradefilter)";Render();Process.Start(new ProcessStartInfo(file.FileName){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message,"HTML-Export fehlgeschlagen");}}
}
