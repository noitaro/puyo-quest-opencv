using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp1.models;
using Point = OpenCvSharp.Point;

namespace WpfApp1
{
    public class PuyoQuestManager
    {
        const int COLS = 8;
        const int ROWS = 6;

        private static readonly Int32Rect CellsRect = new Int32Rect(35, 1086, 1009, 708);
        private static readonly Int32Rect HeadersRect = new Int32Rect(35, 1022, 1009, 64);

        private static readonly int CellWidth = CellsRect.Width / COLS; // 126
        private static readonly int CellHeight = CellsRect.Height / ROWS; // 118

        private static readonly int HeaderWidth = HeadersRect.Width / COLS; // 126

        private static readonly int HeaderHeight = HeadersRect.Height; // 64

        private static readonly Dictionary<PuyoColor, Scalar> PuyoColors = new()
        {
            { PuyoColor.Red, Scalar.Red },
            { PuyoColor.Blue, Scalar.Blue },
            { PuyoColor.Yellow, Scalar.Yellow },
            { PuyoColor.Green, Scalar.Green },
            { PuyoColor.Purple, Scalar.Purple },
            { PuyoColor.Heart, Scalar.Pink }
        };

        private static readonly List<(PuyoColor Color, string PuyoUri, string TamaUri)> TemplateList = new()
        {
            (PuyoColor.Red, "aka_puyo.png", "aka_tama.png"),
            (PuyoColor.Blue, "ao_puyo.png", "ao_tama.png"),
            (PuyoColor.Yellow, "ki-ro_puyo.png", "ki-ro_tama.png"),
            (PuyoColor.Green, "midori_puyo.png", "midori_tama.png"),
            (PuyoColor.Purple, "murasaki_puyo.png", "murasaki_tama.png"),
            (PuyoColor.Heart, "ha-to.png", string.Empty)
        };

        // 上下左右
        private static readonly List<Position> SerchDirection = new()
        {
            new Position(0, -1),
            new Position(0, 1),
            new Position(-1, 0),
            new Position(1, 0)
        };

        private static Puyo GetPuyo(Puyo[,] array, Position position)
        {
            // 配列範囲外の場合、Noneを戻す
            if (position.Y < 0 || array.GetLength(0) <= position.Y) return new Puyo() { Color = PuyoColor.None };
            if (position.X < 0 || array.GetLength(1) <= position.X) return new Puyo() { Color = PuyoColor.None };

            return array[position.Y, position.X];
        }

        private static (Position Position, PuyoColor Color, int Score) GetPuyo
            (List<(Position Position, PuyoColor Color, int Score)> array, Position position)
        {
            var puyoPosition = array.FirstOrDefault(i => i.Position.X == position.X && i.Position.Y == position.Y);

            // 配列範囲外の場合、Noneを戻す
            if (puyoPosition.Position == null) return (null, PuyoColor.None, 0);

            return puyoPosition;
        }

        private static void SetPuyo(Puyo[,] array, Position position, PuyoColor color)
        {
            array[position.Y, position.X] = new Puyo() { Color = color };
        }

        private static T ArrayCopy<T>(T array)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(array));
        }

        public Puyo[,] getCells(BitmapSource imageBitmapSource)
        {
            // トリミング
            var imageBitmapSourceCells = new CroppedBitmap(imageBitmapSource, CellsRect);

            var CellsArray = new Puyo[ROWS, COLS];

            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    // トリミング
                    var cell = new CroppedBitmap(imageBitmapSourceCells, new Int32Rect(x * CellWidth, y * CellHeight, CellWidth, CellHeight));

                    // BitmapSource 形式を OpenCV の Mat 形式に変換
                    var cellMat = BitmapSourceConverter.ToMat(cell);

                    // グレースケール化
                    Cv2.CvtColor(cellMat, cellMat, ColorConversionCodes.RGB2GRAY);

                    var tmpResult = new List<(PuyoColor Color, double MaxVal)>();
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

                        tmpResult.Add((template.Color, maxVal));
                    }

                    var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                    SetPuyo(CellsArray, new Position(x, y), result.Color);
                }
            }

            return CellsArray;
        }

        public Puyo[,] getHeaders(BitmapSource imageBitmapSource)
        {
            var headersArray = new Puyo[1, COLS];

            // トリミング
            var imageBitmapSourceHeaders = new CroppedBitmap(imageBitmapSource, HeadersRect);

            for (int x = 0; x < COLS; x++)
            {
                // トリミング
                var header = new CroppedBitmap(imageBitmapSourceHeaders, new Int32Rect(x * HeaderWidth, 0, HeaderWidth, HeaderHeight));

                // BitmapSource 形式を OpenCV の Mat 形式に変換
                var headerMat = BitmapSourceConverter.ToMat(header);

                // hsv座標系への変換
                Cv2.CvtColor(headerMat, headerMat, ColorConversionCodes.BGR2HSV_FULL);

                // 色に基づく物体検出
                var tmpResult = new List<(PuyoColor Color, double MaxVal)>();
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

                    tmpResult.Add((template.Color, maxVal));
                }

                var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                SetPuyo(headersArray, new Position(x, 0), result.Color);
            }

            return headersArray;
        }

        internal Puyo[,] getBestScore(Puyo[,] headersArray, Puyo[,] cellsArray)
        {
            var scores = new List<(List<Position> pos, int score)>();
            scores.Add((new List<Position>() { new Position(0, 0), new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4) }, 0));

            // 配列コピー
            var tmpHeadersArray = ArrayCopy(headersArray);
            var tmpCellsArray = ArrayCopy(cellsArray);

            int x = 3; // デバッグ用
            int y = 3; // デバッグ用
            //for (int x = 0; x < COLS; x++)
            {
                //for (int y = 0; y < ROWS; y++)
                {
                    // ぷよ消し
                    //SetPuyo(tmpCellsArray, new Position(x, y), PuyoEnum.None);

                    // 上のぷよを下に落とす
                    //setPuyoDrop(tmpHeadersArray, tmpCellsArray);

                    // 4つ以上繋がったぷよを消す
                    var score = setPuyoClear(tmpCellsArray);
                    if (score != 0)
                    {

                    }

                }
            }




            return tmpCellsArray;
        }

        private void setPuyoDrop(Puyo[,] tmpHeadersArray, Puyo[,] tmpCellsArray)
        {
            for (int x = 0; x < COLS; x++)
            {
                // 下から回す
                for (int y1 = ROWS - 1; y1 >= 0; y1--)
                {
                    // 自分のマスにぷよがいるか？
                    if (GetPuyo(tmpCellsArray, new Position(x, y1)).Color != PuyoColor.None) continue;

                    if (y1 == 0)
                    {
                        // ヘッダー部のぷよを入れる
                        SetPuyo(tmpCellsArray, new Position(x, y1), GetPuyo(tmpHeadersArray, new Position(x, 0)).Color);
                        SetPuyo(tmpHeadersArray, new Position(x, 0), PuyoColor.None);
                    }
                    else
                    {
                        // 上のマスのぷよを入れる
                        SetPuyo(tmpCellsArray, new Position(x, y1), GetPuyo(tmpCellsArray, new Position(x, y1 - 1)).Color);
                        SetPuyo(tmpCellsArray, new Position(x, y1 - 1), PuyoColor.None);
                    }

                    // 落ちるところまで落とす
                    for (int y2 = y1 + 1; y2 < ROWS; y2++)
                    {
                        if (GetPuyo(tmpCellsArray, new Position(x, y2)).Color != PuyoColor.None) break;

                        SetPuyo(tmpCellsArray, new Position(x, y2), GetPuyo(tmpCellsArray, new Position(x, y2 - 1)).Color);
                        SetPuyo(tmpCellsArray, new Position(x, y2 - 1), PuyoColor.None);
                    }
                }
            }
        }

        private int setPuyoClear(Puyo[,] array)
        {
            var puyoScores = new List<(Position Position, PuyoColor Color, int Score)>();

            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    var puyo = GetPuyo(array, new Position(x, y));

                    // 自分のマスにぷよがいるか？
                    if (puyo.Color == PuyoColor.None) continue;

                    // 繋がったぷよを探す
                    var searchedList = SearchConnectedPuyos(array, new List<Position>() { new Position(x, y) });

                    puyoScores.Add((new Position(x, y), puyo.Color, searchedList.Count));
                }
            }

            var bestScore = 0;
            var bestScorePositions = new List<Position>();

            // 一番多く消せるぷよを５個選別する
            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    var positions = getBestScorePositions(puyoScores, 1, new Position(x, y));

                    // スコアを求める
                    var score = positions.Sum(pos => GetPuyo(puyoScores, pos).Score);

                    // 一番高いスコアを保持
                    if (bestScore < score)
                    {
                        bestScore = score;
                        bestScorePositions = ArrayCopy(positions);
                    }
                }
            }




            return 0;
        }

        private List<Position> getBestScorePositions
            (List<(Position Position, PuyoColor Color, int Score)> puyoScores, int depth, Position position)
        {
            // 終点
            if (depth >= 5)
            {
                return new List<Position>() { position };
            }

            // 配列コピー
            var tmpScorePositions = new List<Position>();
            var bestScore = 0;
            var bestScorePositions = new List<Position>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    // 今回探索位置取得
                    var nowPosition = new Position(position.X + x, position.Y + y);
                    // ぷよがあるか？
                    if (GetPuyo(puyoScores, nowPosition).Color == PuyoColor.None) continue;

                    // 上下左右斜めの8方向を探索
                    var positions = getBestScorePositions(puyoScores, depth + 1, nowPosition);

                    // 未探索の場合、追加
                    if (!positions.Any(i => i.X == nowPosition.X && i.Y == nowPosition.Y)) positions.Add(nowPosition);
                    // 呼び出し元も追加
                    if (!positions.Any(i => i.X == position.X && i.Y == position.Y)) positions.Add(position);

                    // スコアを求める
                    var score = positions.Sum(pos => GetPuyo(puyoScores, pos).Score);
                    // 一番高いスコアを保持
                    if (bestScore < score)
                    {
                        bestScore = score;
                        bestScorePositions = ArrayCopy(positions);
                    }


                }
            }

            return bestScorePositions;
        }

        /// <summary>
        /// 上下左右に繋がっている同じ色のぷよを探します。
        /// </summary>
        /// <param name="array"></param>
        /// <param name="connectedPositions"></param>
        /// <returns></returns>
        private List<Position> SearchConnectedPuyos(Puyo[,] array, List<Position> connectedPositions)
        {
            // 配列コピー
            var positions = ArrayCopy(connectedPositions);

            foreach (var position in SerchDirection)
            {
                // 前回探索位置取得
                var oldPosition = connectedPositions.Last();
                // 今回探索位置取得
                var nowPosition = new Position(oldPosition.X + position.X, oldPosition.Y + position.Y);

                // ぷよがあるか？
                if (GetPuyo(array, nowPosition).Color == PuyoColor.None) continue;
                // 探索済みか？
                if (positions.Any(i => i.X == nowPosition.X && i.Y == nowPosition.Y)) continue;

                // 上下左右の同色ぷよを探す
                if (GetPuyo(array, oldPosition).Color == GetPuyo(array, nowPosition).Color)
                {
                    // 同色の場合
                    positions.Add(nowPosition);

                    // 再起処理
                    var result = SearchConnectedPuyos(array, positions);
                    foreach (var item in result)
                    {
                        // 未探索の場合、追加
                        if (!positions.Any(i => i.X == item.X && i.Y == item.Y)) positions.Add(item);
                    }


                }
            }

            // 同色のぷよが見つからなかった場合、再起処理を抜ける
            return positions;
        }

        internal ImageSource ShowImageBitmapSource(BitmapSource imageBitmapSource, Puyo[,] headersArray, Puyo[,] cellsArray)
        {
            // ピクセルフォーマットを OpenCV の Mat 形式に変換
            var imageMat = OpenCVManager.BitmapSourceToMat(imageBitmapSource);

            for (var x = 0; x < COLS; x++)
            {
                for (var y = 0; y < ROWS; y++)
                {
                    var puyo = GetPuyo(cellsArray, new Position(x, y));

                    var pt1 = new Point(CellsRect.X + (x * CellWidth) + 5, CellsRect.Y + (y * CellHeight) + 5);
                    var pt2 = new Point(CellsRect.X + (x * CellWidth) + CellWidth - 5, CellsRect.Y + (y * CellHeight) + CellHeight - 5);
                    Cv2.Rectangle(imageMat, pt1, pt2, PuyoColors[puyo.Color], 5, LineTypes.Link8, 0);
                    Cv2.PutText(imageMat, Convert.ToString(puyo.Count), new Point(pt1.X + 80, pt1.Y + 100),
                        HersheyFonts.HersheyPlain, 4, PuyoColors[puyo.Color] == Scalar.Yellow ? Scalar.Gray : Scalar.White, 15);
                    Cv2.PutText(imageMat, Convert.ToString(puyo.Count), new Point(pt1.X + 80, pt1.Y + 100),
                        HersheyFonts.HersheyPlain, 4, PuyoColors[puyo.Color], 5);
                }
            }

            for (var x = 0; x < COLS; x++)
            {
                Cv2.Rectangle(imageMat,
                    new Point(HeadersRect.X + (x * HeaderWidth) + 5, HeadersRect.Y + (0 * HeaderHeight) + 5),
                    new Point(HeadersRect.X + (x * HeaderWidth) + HeaderWidth - 5, HeadersRect.Y + (0 * HeaderHeight) + HeaderHeight - 5),
                    PuyoColors[GetPuyo(headersArray, new Position(x, 0)).Color], 5, LineTypes.Link8, 0);
            }

            // Image コントロールに BitmapSource 形式の画像データを設定する。
            return BitmapSourceConverter.ToBitmapSource(imageMat);
        }

        internal Puyo[,] getConnectCount(Puyo[,] cellsArray)
        {
            // 配列コピー
            var array = ArrayCopy(cellsArray);

            //var x = 2; // デバッグ用
            //var y = 0; // デバッグ用
            for (var x = 0; x < COLS; x++)
            {
                for (var y = 0; y < ROWS; y++)
                {
                    var puyo = GetPuyo(array, new Position(x, y));

                    // 自分のマスにぷよがいるか？
                    if (puyo.Color == PuyoColor.None) continue;

                    // 繋がったぷよを探す
                    var connectedPuyos = SearchConnectedPuyos(array, new List<Position>() { new Position(x, y) });

                    puyo.Count = connectedPuyos.Count;
                }
            }
            return array;
        }

    }
}
