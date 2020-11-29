using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = OpenCvSharp.Point;
using Window = System.Windows.Window;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int COLS = 8;
        const int ROWS = 6;

        public MainWindow()
        {
            InitializeComponent();
        }

        private readonly List<(string PuyoColor, string Uri, Scalar ScalarColor)>
            TemplateList = new List<(string, string, Scalar)>()
        {
            ("Red", "template/aka_puyo.png", Scalar.Red),
            ("Blue", "template/ao_puyo.png", Scalar.Blue),
            ("Yellow", "template/ki-ro_puyo.png", Scalar.Yellow),
            ("Green", "template/midori_puyo.png", Scalar.Green),
            ("Purple", "template/murasaki_puyo.png", Scalar.Purple),
            ("Heart", "template/ha-to.png", Scalar.Pink)
        };

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Image コントロールから画像データを BitmapSource 形式で取得する。
            var imageBitmapSource = (BitmapSource)image.Source;
            // トリミング
            imageBitmapSource = new CroppedBitmap(imageBitmapSource, new Int32Rect(35, 1086, 1009, 708));
            // ピクセルフォーマットの変更
            imageBitmapSource = new FormatConvertedBitmap(imageBitmapSource, PixelFormats.Bgr24, null, 0);

            // BitmapSource 形式を OpenCV の Mat 形式に変換する。
            var imageMat = BitmapSourceConverter.ToMat(imageBitmapSource);

            var cellWidth = (int)(imageBitmapSource.Width / COLS); // 126
            var cellHeight = (int)(imageBitmapSource.Height / ROWS); // 118

            var resultList = new List<(int X, int Y, Scalar ScalarColor)>();
            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    // トリミング
                    var cell = new CroppedBitmap(imageBitmapSource, new Int32Rect(x * cellWidth, y * cellHeight, cellWidth, cellHeight));

                    // BitmapSource 形式を OpenCV の Mat 形式に変換する。
                    var cellMat = BitmapSourceConverter.ToMat(cell);

                    // グレースケール化
                    Cv2.CvtColor(cellMat, cellMat, ColorConversionCodes.RGB2GRAY);

                    var tmpResult = new List<(Scalar ScalarColor, double MaxVal)>();
                    foreach (var template in TemplateList)
                    {
                        var templateBitmapSource = new BitmapImage();
                        templateBitmapSource.BeginInit();
                        templateBitmapSource.UriSource = new Uri(template.Uri, UriKind.Relative);
                        templateBitmapSource.EndInit();
                        var templateMat = BitmapSourceConverter.ToMat(templateBitmapSource);

                        // グレースケール化
                        Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.RGB2GRAY);

                        var resultMatch = new Mat();

                        // 空Matに全座標の比較データ（配列）を格納
                        Cv2.MatchTemplate(cellMat, templateMat, resultMatch, TemplateMatchModes.CCoeffNormed);

                        Cv2.MinMaxLoc(resultMatch, out _, out double maxVal);

                        tmpResult.Add((template.ScalarColor, maxVal));
                    }

                    var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                    resultList.Add((x, y, result.ScalarColor));
                }
            }

            foreach ((var x, var y, var scalarColor) in resultList)
            {
                Cv2.Rectangle(imageMat,
                    new Point((x * cellWidth) + 5, (y * cellHeight) + 5),
                    new Point((x * cellWidth) + cellWidth - 5, (y * cellHeight) + cellHeight - 5),
                    scalarColor, 5, LineTypes.Link8, 0);
            }

            // Image コントロールに BitmapSource 形式の画像データを設定する。
            image.Source = BitmapSourceConverter.ToBitmapSource(imageMat);
        }

    }
}
