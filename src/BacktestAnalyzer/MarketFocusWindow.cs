using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace BacktestAnalyzer;
public sealed class MarketFocusWindow:Window
{
    public MarketFocusWindow(Report source,bool dark)
    {
        var p=new Palette(dark);Brush B(string color)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));Title=Localization.English?"Market & account · zoom":"Markt & Konto · Zeitraum-Zoom";Width=1250;Height=770;MinWidth=700;MinHeight=530;Background=B(p.Background);Foreground=B(p.Ink);var dock=new DockPanel{Margin=new Thickness(18)};var controls=new WrapPanel{Margin=new Thickness(0,0,0,15)};DockPanel.SetDock(controls,Dock.Top);dock.Children.Add(controls);
        var from=new DatePicker{SelectedDate=source.Balances.Max(b=>b.Time).Date.AddDays(-7),Margin=new Thickness(0,0,12,0)};var to=new DatePicker{SelectedDate=source.Balances.Max(b=>b.Time).Date.AddDays(1),Margin=new Thickness(0,0,12,0)};controls.Children.Add(new TextBlock{Text=Localization.English?"From ":"Von ",Foreground=B(p.Ink),VerticalAlignment=VerticalAlignment.Center});controls.Children.Add(from);controls.Children.Add(new TextBlock{Text=Localization.English?"To ":"Bis ",Foreground=B(p.Ink),VerticalAlignment=VerticalAlignment.Center});controls.Children.Add(to);var interval=new ComboBox{ItemsSource=new[]{"Auto","H1","H2","H4","H8","H12","D1","D2","D4","W1","MN1"},SelectedIndex=0,Foreground=Brushes.Black,Background=Brushes.White,MinWidth=70,Margin=new Thickness(0,0,12,0)};controls.Children.Add(interval);
        var host=new Grid();dock.Children.Add(host);void Draw(){if(!from.SelectedDate.HasValue||!to.SelectedDate.HasValue)return;try{var range=source.Range(from.SelectedDate.Value,to.SelectedDate.Value);range.MarketInterval=(string)interval.SelectedItem=="T1"?"D1":(string)interval.SelectedItem;host.Children.Clear();host.Children.Add(new ChartView{Report=range,Trades=range.Closed,Palette=p,Kind=ChartKind.Balance});}catch(Exception ex){MessageBox.Show(ex.Message);}}
        void Button(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(12,7,12,7),Margin=new Thickness(0,0,8,0),Foreground=B(p.Ink),Background=B(p.Surface),BorderBrush=B(p.Line)};b.Click+=(_,_)=>action();controls.Children.Add(b);}
        Button(Localization.English?"Apply":"Anzeigen",Draw);Button(Localization.English?"Entire period":"Gesamter Zeitraum",()=>{from.SelectedDate=source.AxisStart??source.Balances.Min(b=>b.Time).Date;to.SelectedDate=source.AxisEnd??source.Balances.Max(b=>b.Time).Date.AddDays(1);Draw();});interval.SelectionChanged+=(_,_)=>Draw();Content=dock;Loaded+=(_,_)=>Draw();
    }
}
