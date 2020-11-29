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

        enum PuyoEnum
        {

        }

        private readonly List<(string PuyoColor, string PuyoUri, string TamaUri, Scalar ScalarColor)>
            TemplateList = new List<(string, string, string, Scalar)>()
        {
            ("Red", "aka_puyo.png", "aka_tama.png", Scalar.Red),
            ("Blue", "ao_puyo.png", "ao_tama.png", Scalar.Blue),
            ("Yellow", "ki-ro_puyo.png", "ki-ro_tama.png", Scalar.Yellow),
            ("Green", "midori_puyo.png", "midori_tama.png", Scalar.Green),
            ("Purple", "murasaki_puyo.png", "murasaki_tama.png", Scalar.Purple),
            ("Heart", "ha-to.png", string.Empty, Scalar.Pink)
        };

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var puyos = getPuyos();


        }

        private (List<(int X, int Y, Scalar ScalarColor)> Headers, List<(int X, int Y, Scalar ScalarColor)> Cells) getPuyos()
        {
            // Image コントロールから画像データを BitmapSource 形式で取得する。
            var imageBitmapSource = (BitmapSource)image.Source;
            // ピクセルフォーマットの変更
            imageBitmapSource = new FormatConvertedBitmap(imageBitmapSource, PixelFormats.Bgr24, null, 0);
            // BitmapSource 形式を OpenCV の Mat 形式に変換
            var imageMat = BitmapSourceConverter.ToMat(imageBitmapSource);

            var cellsRect = new Int32Rect(35, 1086, 1009, 708);
            var cells = getCells(imageBitmapSource, cellsRect);

            var cellWidth = cellsRect.Width / COLS; // 126
            var cellHeight = cellsRect.Height / ROWS; // 118
            foreach ((var x, var y, var scalarColor) in cells)
            {
                Cv2.Rectangle(imageMat,
                    new Point(cellsRect.X + (x * cellWidth) + 5, cellsRect.Y + (y * cellHeight) + 5),
                    new Point(cellsRect.X + (x * cellWidth) + cellWidth - 5, cellsRect.Y + (y * cellHeight) + cellHeight - 5),
                    scalarColor, 5, LineTypes.Link8, 0);
            }

            var headersRect = new Int32Rect(35, 1022, 1009, 64);
            var headerWidth = headersRect.Width / COLS; // 126
            var headerHeight = headersRect.Height; // 64

            // トリミング
            var imageBitmapSourceHeaders = new CroppedBitmap(imageBitmapSource, headersRect);

            var headers = getHeaders(imageBitmapSourceHeaders, headerWidth, headerHeight);
            foreach ((var x, var y, var scalarColor) in headers)
            {
                Cv2.Rectangle(imageMat,
                    new Point(headersRect.X + (x * headerWidth) + 5, headersRect.Y + (y * headerHeight) + 5),
                    new Point(headersRect.X + (x * headerWidth) + headerWidth - 5, headersRect.Y + (y * headerHeight) + headerHeight - 5),
                    scalarColor, 5, LineTypes.Link8, 0);
            }

            // Image コントロールに BitmapSource 形式の画像データを設定する。
            image.Source = BitmapSourceConverter.ToBitmapSource(imageMat);

            return (headers, cells);
        }

        private List<(int X, int Y, Scalar ScalarColor)> getCells(BitmapSource imageBitmapSource, Int32Rect cellsRect)
        {
            var cellWidth = cellsRect.Width / COLS; // 126
            var cellHeight = cellsRect.Height / ROWS; // 118

            // トリミング
            var imageBitmapSourceCells = new CroppedBitmap(imageBitmapSource, cellsRect);

            var cells = new List<(int X, int Y, Scalar ScalarColor)>();
            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    // トリミング
                    var cell = new CroppedBitmap(imageBitmapSourceCells, new Int32Rect(x * cellWidth, y * cellHeight, cellWidth, cellHeight));

                    // BitmapSource 形式を OpenCV の Mat 形式に変換
                    var cellMat = BitmapSourceConverter.ToMat(cell);

                    // グレースケール化
                    Cv2.CvtColor(cellMat, cellMat, ColorConversionCodes.RGB2GRAY);

                    var tmpResult = new List<(Scalar ScalarColor, double MaxVal)>();
                    foreach (var template in TemplateList)
                    {
                        var templateBitmapSource = new BitmapImage();
                        templateBitmapSource.BeginInit();
                        templateBitmapSource.UriSource = new Uri($@"template/{template.PuyoUri}", UriKind.Relative);
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
                    cells.Add((x, y, result.ScalarColor));
                }
            }

            return cells;
        }

        private List<(int X, int Y, Scalar ScalarColor)> getHeaders(BitmapSource imageBitmapSourceHeaders, int headerWidth, int headerHeight)
        {
            var headers = new List<(int X, int Y, Scalar ScalarColor)>();
            for (int x = 0; x < COLS; x++)
            {
                // トリミング
                var header = new CroppedBitmap(imageBitmapSourceHeaders, new Int32Rect(x * headerWidth, 0, headerWidth, headerHeight));

                // BitmapSource 形式を OpenCV の Mat 形式に変換
                var headerMat = BitmapSourceConverter.ToMat(header);

                // hsv座標系への変換
                Cv2.CvtColor(headerMat, headerMat, ColorConversionCodes.BGR2HSV_FULL);

                // 色に基づく物体検出
                var tmpResult = new List<(Scalar ScalarColor, double MaxVal)>();
                foreach (var template in TemplateList)
                {
                    if (template.TamaUri == string.Empty) continue;

                    var templateBitmapSource = new BitmapImage();
                    templateBitmapSource.BeginInit();
                    templateBitmapSource.UriSource = new Uri($@"template/{template.TamaUri}", UriKind.Relative);
                    templateBitmapSource.EndInit();
                    var templateMat = BitmapSourceConverter.ToMat(templateBitmapSource);

                    // hsv座標系への変換
                    Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGR2HSV_FULL);

                    var resultMatch = new Mat();

                    // 空Matに全座標の比較データ（配列）を格納
                    Cv2.MatchTemplate(headerMat, templateMat, resultMatch, TemplateMatchModes.CCorrNormed);

                    Cv2.MinMaxLoc(resultMatch, out _, out double maxVal);

                    tmpResult.Add((template.ScalarColor, maxVal));
                }

                var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                headers.Add((x, 0, result.ScalarColor));
            }

            return headers;
        }

    }
}
