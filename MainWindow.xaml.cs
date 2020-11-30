using Newtonsoft.Json;
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
            None,
            Red,
            Blue,
            Yellow,
            Green,
            Purple,
            Heart
        }

        private readonly Dictionary<PuyoEnum, Scalar> PuyoColors = new()
        {
            { PuyoEnum.Red, Scalar.Red },
            { PuyoEnum.Blue, Scalar.Blue },
            { PuyoEnum.Yellow, Scalar.Yellow },
            { PuyoEnum.Green, Scalar.Green },
            { PuyoEnum.Purple, Scalar.Purple },
            { PuyoEnum.Heart, Scalar.Pink }
        };

        private readonly List<(PuyoEnum Puyo, string PuyoUri, string TamaUri)> TemplateList = new()
        {
            (PuyoEnum.Red, "aka_puyo.png", "aka_tama.png"),
            (PuyoEnum.Blue, "ao_puyo.png", "ao_tama.png"),
            (PuyoEnum.Yellow, "ki-ro_puyo.png", "ki-ro_tama.png"),
            (PuyoEnum.Green, "midori_puyo.png", "midori_tama.png"),
            (PuyoEnum.Purple, "murasaki_puyo.png", "murasaki_tama.png"),
            (PuyoEnum.Heart, "ha-to.png", string.Empty)
        };

        private static PuyoEnum GetPuyo(PuyoEnum[,] array, int x, int y)
        {
            // 配列範囲外の場合、Noneを戻す
            if (y < 0 || array.GetLength(0) <= y) return PuyoEnum.None;
            if (x < 0 || array.GetLength(1) <= x) return PuyoEnum.None;

            return array[y, x];
        }

        private static void SetPuyo(PuyoEnum[,] array, int x, int y, PuyoEnum puyo)
        {
            array[y, x] = puyo;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var puyos = getPuyos();

            var bestScore = getBestScore(puyos.HeadersArray, puyos.CellsArray);
        }

        private (PuyoEnum[,] HeadersArray, PuyoEnum[,] CellsArray) getPuyos()
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
            foreach ((var x, var y, var puyo) in cells)
            {
                Cv2.Rectangle(imageMat,
                    new Point(cellsRect.X + (x * cellWidth) + 5, cellsRect.Y + (y * cellHeight) + 5),
                    new Point(cellsRect.X + (x * cellWidth) + cellWidth - 5, cellsRect.Y + (y * cellHeight) + cellHeight - 5),
                    PuyoColors[puyo], 5, LineTypes.Link8, 0);
            }

            var headersRect = new Int32Rect(35, 1022, 1009, 64);
            var headerWidth = headersRect.Width / COLS; // 126
            var headerHeight = headersRect.Height; // 64

            // トリミング
            var imageBitmapSourceHeaders = new CroppedBitmap(imageBitmapSource, headersRect);

            var headers = getHeaders(imageBitmapSourceHeaders, headerWidth, headerHeight);
            foreach ((var x, var y, var puyo) in headers)
            {
                Cv2.Rectangle(imageMat,
                    new Point(headersRect.X + (x * headerWidth) + 5, headersRect.Y + (y * headerHeight) + 5),
                    new Point(headersRect.X + (x * headerWidth) + headerWidth - 5, headersRect.Y + (y * headerHeight) + headerHeight - 5),
                    PuyoColors[puyo], 5, LineTypes.Link8, 0);
            }

            // Image コントロールに BitmapSource 形式の画像データを設定する。
            image.Source = BitmapSourceConverter.ToBitmapSource(imageMat);

            // Array変換
            var headersArray = new PuyoEnum[1, COLS];
            var cellsArray = new PuyoEnum[ROWS, COLS];
            foreach ((var x, var y, var puyo) in headers) headersArray[y, x] = puyo;
            foreach ((var x, var y, var puyo) in cells) cellsArray[y, x] = puyo;

            return (headersArray, cellsArray);
        }

        private List<(int X, int Y, PuyoEnum Puyo)> getCells(BitmapSource imageBitmapSource, Int32Rect cellsRect)
        {
            var cellWidth = cellsRect.Width / COLS; // 126
            var cellHeight = cellsRect.Height / ROWS; // 118

            // トリミング
            var imageBitmapSourceCells = new CroppedBitmap(imageBitmapSource, cellsRect);

            var cells = new List<(int X, int Y, PuyoEnum Puyo)>();
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

                    var tmpResult = new List<(PuyoEnum Puyo, double MaxVal)>();
                    foreach ((var puyo, var uri, _) in TemplateList)
                    {
                        var templateBitmapSource = new BitmapImage();
                        templateBitmapSource.BeginInit();
                        templateBitmapSource.UriSource = new Uri($@"template/{uri}", UriKind.Relative);
                        templateBitmapSource.EndInit();
                        var templateMat = BitmapSourceConverter.ToMat(templateBitmapSource);

                        // グレースケール化
                        Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.RGB2GRAY);

                        var resultMatch = new Mat();

                        // 空Matに全座標の比較データ（配列）を格納
                        Cv2.MatchTemplate(cellMat, templateMat, resultMatch, TemplateMatchModes.CCoeffNormed);

                        Cv2.MinMaxLoc(resultMatch, out _, out double maxVal);

                        tmpResult.Add((puyo, maxVal));
                    }

                    var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                    cells.Add((x, y, result.Puyo));
                }
            }

            return cells;
        }

        private List<(int X, int Y, PuyoEnum Puyo)> getHeaders(BitmapSource imageBitmapSourceHeaders, int headerWidth, int headerHeight)
        {
            var headers = new List<(int X, int Y, PuyoEnum Puyo)>();
            for (int x = 0; x < COLS; x++)
            {
                // トリミング
                var header = new CroppedBitmap(imageBitmapSourceHeaders, new Int32Rect(x * headerWidth, 0, headerWidth, headerHeight));

                // BitmapSource 形式を OpenCV の Mat 形式に変換
                var headerMat = BitmapSourceConverter.ToMat(header);

                // hsv座標系への変換
                Cv2.CvtColor(headerMat, headerMat, ColorConversionCodes.BGR2HSV_FULL);

                // 色に基づく物体検出
                var tmpResult = new List<(PuyoEnum Puyo, double MaxVal)>();
                foreach ((var puyo, _, var uri) in TemplateList)
                {
                    if (uri == string.Empty) continue;

                    var templateBitmapSource = new BitmapImage();
                    templateBitmapSource.BeginInit();
                    templateBitmapSource.UriSource = new Uri($@"template/{uri}", UriKind.Relative);
                    templateBitmapSource.EndInit();
                    var templateMat = BitmapSourceConverter.ToMat(templateBitmapSource);

                    // hsv座標系への変換
                    Cv2.CvtColor(templateMat, templateMat, ColorConversionCodes.BGR2HSV_FULL);

                    var resultMatch = new Mat();

                    // 空Matに全座標の比較データ（配列）を格納
                    Cv2.MatchTemplate(headerMat, templateMat, resultMatch, TemplateMatchModes.CCorrNormed);

                    Cv2.MinMaxLoc(resultMatch, out _, out double maxVal);

                    tmpResult.Add((puyo, maxVal));
                }

                var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                headers.Add((x, 0, result.Puyo));
            }

            return headers;
        }

        private (List<(int x, int y)> pos, int score) getBestScore(PuyoEnum[,] headersArray, PuyoEnum[,] cellsArray)
        {
            var scores = new List<(List<(int x, int y)> pos, int score)>();
            scores.Add((new List<(int x, int y)>() { (0, 0), (0, 1), (0, 2), (0, 3), (0, 4) }, 0));

            int x = 3; // デバッグ用
            int y = 3; // デバッグ用
            //for (int x = 0; x < COLS; x++)
            {
                //for (int y = 0; y < ROWS; y++)
                {
                    // 配列コピー
                    var tmpHeadersArray = JsonConvert.DeserializeObject<PuyoEnum[,]>(JsonConvert.SerializeObject(headersArray));
                    var tmpCellsArray = JsonConvert.DeserializeObject<PuyoEnum[,]>(JsonConvert.SerializeObject(cellsArray));

                    // ぷよ消し
                    SetPuyo(tmpCellsArray, x, y, PuyoEnum.None);

                    // 上のぷよを下に落とす
                    setPuyoDrop(tmpHeadersArray, tmpCellsArray);

                    // 4つ以上繋がったぷよを消す
                    var score = setPuyoClear(tmpCellsArray);
                    if (score != 0)
                    {

                    }

                }
            }




            return scores[0];
        }

        private void setPuyoDrop(PuyoEnum[,] tmpHeadersArray, PuyoEnum[,] tmpCellsArray)
        {
            for (int x = 0; x < COLS; x++)
            {
                // 下から回す
                for (int y1 = ROWS - 1; y1 >= 0; y1--)
                {
                    // 自分のマスにぷよがいるか？
                    if (GetPuyo(tmpCellsArray, x, y1) != PuyoEnum.None) continue;

                    if (y1 == 0)
                    {
                        // ヘッダー部のぷよを入れる
                        SetPuyo(tmpCellsArray, x, y1, GetPuyo(tmpHeadersArray, x, 0));
                        SetPuyo(tmpHeadersArray, x, 0, PuyoEnum.None);
                    }
                    else
                    {
                        // 上のマスのぷよを入れる
                        SetPuyo(tmpCellsArray, x, y1, GetPuyo(tmpCellsArray, x, y1 - 1));
                        SetPuyo(tmpCellsArray, x, y1 - 1, PuyoEnum.None);
                    }

                    // 落ちるところまで落とす
                    for (int y2 = y1 + 1; y2 < ROWS; y2++)
                    {
                        if (GetPuyo(tmpCellsArray, x, y2) != PuyoEnum.None) break;

                        SetPuyo(tmpCellsArray, x, y2, GetPuyo(tmpCellsArray, x, y2 - 1));
                        SetPuyo(tmpCellsArray, x, y2 - 1, PuyoEnum.None);
                    }
                }
            }
        }

        private int setPuyoClear(PuyoEnum[,] array)
        {
            var score = 0;

            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    // 自分のマスにぷよがいるか？
                    var puyo = GetPuyo(array, x, y);
                    if (puyo == PuyoEnum.None) continue;

                    // 4つ以上繋がったぷよを探す
                    var result = SearchPuyoClear(1, array, new List<(int x, int y)>() { (x, y) });

                }
            }

            return score;
        }

        private List<(int x, int y)> SearchPuyoClear(int depth, PuyoEnum[,] array, List<(int x, int y)> searchedList)
        {
            var serchList = new List<(int x, int y)>() { (0, -1), (0, 1), (-1, 0), (1, 0) }; // 上下左右
            foreach ((var x, var y) in serchList)
            {
                // 前回検索位置取得
                var lastPosition = searchedList.Last();
                // 初期ぷよ取得
                var puyo = GetPuyo(array, lastPosition.x, lastPosition.y);

                // 上下左右の同色ぷよを探す
                if (GetPuyo(array, lastPosition.x + x, lastPosition.y + y) == puyo)
                {
                    // 同色の場合、再起処理

                }
                else
                {

                }
            }
        }
    }
}
