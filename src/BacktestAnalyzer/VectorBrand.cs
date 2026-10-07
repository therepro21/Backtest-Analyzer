using System.Windows;
using System.Windows.Media;
namespace BacktestAnalyzer;
public static class VectorBrand
{
    public static DrawingImage Image(bool dark=false)
    {
        var group=new DrawingGroup();using(var d=group.Open())
        {
            var navy=new SolidColorBrush(Color.FromRgb(16,48,81));var orange=new SolidColorBrush(Color.FromRgb(236,155,18));var pen=new Pen(navy,6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            d.DrawEllipse(null,pen,new Point(43,43),32,32);d.DrawLine(new Pen(navy,9){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},new Point(67,67),new Point(89,89));
            foreach(int deg in new[]{0,45,90,135,180,225,270,315}){double a=deg*Math.PI/180;d.DrawLine(new Pen(navy,1.5),new Point(43+Math.Cos(a)*25,43+Math.Sin(a)*25),new Point(43+Math.Cos(a)*29,43+Math.Sin(a)*29));}
            foreach(var (x,top,bottom,bodyTop,bodyHeight,b) in new[]{(29d,29d,58d,35d,15d,(Brush)navy),(43d,20d,64d,29d,26d,(Brush)orange),(57d,27d,58d,34d,17d,(Brush)navy)}){d.DrawLine(new Pen(b,2),new Point(x,top),new Point(x,bottom));d.DrawRoundedRectangle(b,null,new Rect(x-3,bodyTop,6,bodyHeight),.7,.7);}
        }group.Freeze();var image=new DrawingImage(group);image.Freeze();return image;
    }
    public static void Draw(ICanvas c,double x,double y,double size,string background)
    {
        string navy="#103051",orange="#EC9B12";double k=size/100;double X(double a)=>x+a*k;double Y(double a)=>y+a*k;
        c.Line(X(66),Y(66),X(89),Y(89),navy,13*k);c.Circle(X(43),Y(43),35*k,navy);c.Circle(X(43),Y(43),29*k,background);
        foreach(int deg in new[]{0,45,90,135,180,225,270,315}){double a=deg*Math.PI/180;c.Line(X(43+Math.Cos(a)*25),Y(43+Math.Sin(a)*25),X(43+Math.Cos(a)*28),Y(43+Math.Sin(a)*28),navy,1.8*k);}
        foreach(var (cx,top,bottom,bt,bh,color) in new[]{(29d,29d,58d,35d,15d,navy),(43d,20d,64d,29d,26d,orange),(57d,27d,58d,34d,17d,navy)}){c.Line(X(cx),Y(top),X(cx),Y(bottom),color,3*k);c.Rect(X(cx-3),Y(bt),6*k,bh*k,color);}
    }
}
