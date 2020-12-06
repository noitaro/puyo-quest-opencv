using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public class OpenCVManager
    {
        public static Mat BitmapSourceToMat(BitmapSource imageBitmapSource)
        {
            // ピクセルフォーマットの変更
            imageBitmapSource = new FormatConvertedBitmap(imageBitmapSource, PixelFormats.Bgr24, null, 0);
            // BitmapSource 形式を OpenCV の Mat 形式に変換
            return BitmapSourceConverter.ToMat(imageBitmapSource);
        }
    }
}
