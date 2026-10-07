using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
namespace BacktestAnalyzer;
public static class TableAlignment
{
 public static void Apply(DataGrid grid)
 {
  void Align(DataGridColumn column,string field){bool numeric=field is "Result" or "A" or "B" or "Differenz" or "Difference" or "Percent";var alignment=numeric?TextAlignment.Right:TextAlignment.Left;var cell=new Style(typeof(TextBlock));cell.Setters.Add(new Setter(TextBlock.TextAlignmentProperty,alignment));cell.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(8,0,8,0)));if(column is DataGridTextColumn text)text.ElementStyle=cell;var header=new Style(typeof(DataGridColumnHeader),grid.ColumnHeaderStyle);header.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,numeric?HorizontalAlignment.Right:HorizontalAlignment.Left));header.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(8)));column.HeaderStyle=header;}
  grid.AutoGeneratingColumn+=(_,e)=>Align(e.Column,e.PropertyName);foreach(var c in grid.Columns)Align(c,(c as DataGridBoundColumn)?.Binding is System.Windows.Data.Binding b?b.Path.Path:c.Header?.ToString()??"");
 }
}
