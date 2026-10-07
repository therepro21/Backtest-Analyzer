using System.Windows.Media.Imaging;
using System.Windows.Media;
using PdfSharp.Drawing;
namespace BacktestAnalyzer;
public static class LogoBackground
{
 static byte[]? dark;
 public static byte[] Bytes(bool isDark){if(!isDark)return PdfExport.Logo;if(dark!=null)return dark;using var stream=new MemoryStream(PdfExport.Logo);var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);var bitmap=new FormatConvertedBitmap(decoder.Frames[0],PixelFormats.Bgra32,null,0);int stride=bitmap.PixelWidth*4;byte[] pixels=new byte[stride*bitmap.PixelHeight];bitmap.CopyPixels(pixels,stride,0);for(int i=0;i<pixels.Length;i+=4){double white=Math.Clamp((Math.Min(pixels[i],Math.Min(pixels[i+1],pixels[i+2]))-220)/35d,0,1);pixels[i]=(byte)(pixels[i]*(1-white)+255*white);pixels[i+1]=(byte)(pixels[i+1]*(1-white)+235*white);pixels[i+2]=(byte)(pixels[i+2]*(1-white)+214*white);}var result=BitmapSource.Create(bitmap.PixelWidth,bitmap.PixelHeight,96,96,PixelFormats.Bgra32,null,pixels,stride);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(result));using var output=new MemoryStream();encoder.Save(output);return dark=output.ToArray();}
 public static void Paint(XGraphics g,XImage unused,double x,double y,double w,double h){using var image=XImage.FromStream(new MemoryStream(Bytes(true)));g.DrawImage(image,x,y,w,h);}
}
