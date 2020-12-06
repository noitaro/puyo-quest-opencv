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

        // 上下左右
        private readonly List<Position> SerchList = new()
        {
            new Position(0, -1),
            new Position(0, 1),
            new Position(-1, 0),
            new Position(1, 0)
        };

        private static PuyoEnum GetPuyo(PuyoEnum[,] array, Position position)
        {
            // 配列範囲外の場合、Noneを戻す
            if (position.Y < 0 || array.GetLength(0) <= position.Y) return PuyoEnum.None;
            if (position.X < 0 || array.GetLength(1) <= position.X) return PuyoEnum.None;

            return array[position.Y, position.X];
        }

        private static (Position Position, PuyoEnum Puyo, int Score) GetPuyo
            (List<(Position Position, PuyoEnum Puyo, int Score)> array, Position position)
        {
            var puyoPosition = array.FirstOrDefault(i => i.Position.X == position.X && i.Position.Y == position.Y);

            // 配列範囲外の場合、Noneを戻す
            if (puyoPosition.Position == null) return (null, PuyoEnum.None, 0);

            return puyoPosition;
        }

        private static void SetPuyo(PuyoEnum[,] array, Position position, PuyoEnum puyo)
        {
            array[position.Y, position.X] = puyo;
        }

        private static T ArrayCopy<T>(T array)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(array));
        }

        internal (PuyoEnum[,] HeadersArray, PuyoEnum[,] CellsArray) GetPuyoPosition(BitmapSource imageBitmapSource)
        {
            var cellsRect = new Int32Rect(35, 1086, 1009, 708);
            var cells = getCells(imageBitmapSource, cellsRect);

            var headersRect = new Int32Rect(35, 1022, 1009, 64);

            // トリミング
            var imageBitmapSourceHeaders = new CroppedBitmap(imageBitmapSource, headersRect);
            var headerWidth = headersRect.Width / COLS; // 126
            var headerHeight = headersRect.Height; // 64
            var headers = getHeaders(imageBitmapSourceHeaders, headerWidth, headerHeight);

            // Array変換
            var puyoPosition = new PuyoPosition();

            var HeadersArray = new PuyoEnum[1, COLS];
            var CellsArray = new PuyoEnum[ROWS, COLS];

            foreach (var header in headers)
            {
                SetPuyo(HeadersArray, header.Position, header.Puyo);
            }
            foreach (var cell in cells)
            {
                SetPuyo(CellsArray, cell.Position, cell.Puyo);
            }

            return (HeadersArray, CellsArray);
        }

        private List<(Position Position, PuyoEnum Puyo)> getCells(BitmapSource imageBitmapSource, Int32Rect cellsRect)
        {
            var cellWidth = cellsRect.Width / COLS; // 126
            var cellHeight = cellsRect.Height / ROWS; // 118

            // トリミング
            var imageBitmapSourceCells = new CroppedBitmap(imageBitmapSource, cellsRect);

            var cells = new List<(Position Position, PuyoEnum Puyo)>();
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

                        tmpResult.Add((template.Puyo, maxVal));
                    }

                    var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                    cells.Add((new Position(x, y), result.Puyo));
                }
            }

            return cells;
        }

        private List<(Position Position, PuyoEnum Puyo)> getHeaders(BitmapSource imageBitmapSourceHeaders, int headerWidth, int headerHeight)
        {
            var headers = new List<(Position position, PuyoEnum Puyo)>();
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

                    tmpResult.Add((template.Puyo, maxVal));
                }

                var result = tmpResult.OrderByDescending(x => x.MaxVal).First();
                headers.Add((new Position(x, 0), result.Puyo));
            }

            return headers;
        }

        internal (List<Position> pos, int score) getBestScore(PuyoEnum[,] headersArray, PuyoEnum[,] cellsArray)
        {
            var scores = new List<(List<Position> pos, int score)>();
            scores.Add((new List<Position>() { new Position(0, 0), new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4) }, 0));

            int x = 3; // デバッグ用
            int y = 3; // デバッグ用
            //for (int x = 0; x < COLS; x++)
            {
                //for (int y = 0; y < ROWS; y++)
                {
                    // 配列コピー
                    var tmpHeadersArray = ArrayCopy(headersArray);
                    var tmpCellsArray = ArrayCopy(cellsArray);

                    // ぷよ消し
                    SetPuyo(tmpCellsArray, new Position(x, y), PuyoEnum.None);

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
                    if (GetPuyo(tmpCellsArray, new Position(x, y1)) != PuyoEnum.None) continue;

                    if (y1 == 0)
                    {
                        // ヘッダー部のぷよを入れる
                        SetPuyo(tmpCellsArray, new Position(x, y1), GetPuyo(tmpHeadersArray, new Position(x, 0)));
                        SetPuyo(tmpHeadersArray, new Position(x, 0), PuyoEnum.None);
                    }
                    else
                    {
                        // 上のマスのぷよを入れる
                        SetPuyo(tmpCellsArray, new Position(x, y1), GetPuyo(tmpCellsArray, new Position(x, y1 - 1)));
                        SetPuyo(tmpCellsArray, new Position(x, y1 - 1), PuyoEnum.None);
                    }

                    // 落ちるところまで落とす
                    for (int y2 = y1 + 1; y2 < ROWS; y2++)
                    {
                        if (GetPuyo(tmpCellsArray, new Position(x, y2)) != PuyoEnum.None) break;

                        SetPuyo(tmpCellsArray, new Position(x, y2), GetPuyo(tmpCellsArray, new Position(x, y2 - 1)));
                        SetPuyo(tmpCellsArray, new Position(x, y2 - 1), PuyoEnum.None);
                    }
                }
            }
        }

        private int setPuyoClear(PuyoEnum[,] array)
        {
            var puyoScores = new List<(Position Position, PuyoEnum Puyo, int Score)>();

            for (int x = 0; x < COLS; x++)
            {
                for (int y = 0; y < ROWS; y++)
                {
                    var puyo = GetPuyo(array, new Position(x, y));

                    // 自分のマスにぷよがいるか？
                    if (puyo == PuyoEnum.None) continue;

                    // 繋がったぷよを探す
                    var searchedList = SearchPuyoClear(array, new List<Position>() { new Position(x, y) });

                    puyoScores.Add((new Position(x, y), puyo, searchedList.Count));
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
            (List<(Position Position, PuyoEnum Puyo, int Score)> puyoScores, int depth, Position position)
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
                    if (GetPuyo(puyoScores, nowPosition).Puyo == PuyoEnum.None) continue;

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

        private List<Position> SearchPuyoClear(PuyoEnum[,] array, List<Position> searchedList)
        {
            // 配列コピー
            var tmpSearchedList = ArrayCopy(searchedList);

            foreach (var position in SerchList)
            {
                // 前回探索位置取得
                var oldPosition = tmpSearchedList.Last();
                // 今回探索位置取得
                var nowPosition = new Position(oldPosition.X + position.X, oldPosition.Y + position.Y);

                // ぷよがあるか？
                if (GetPuyo(array, nowPosition) == PuyoEnum.None) continue;
                // 探索済みか？
                if (tmpSearchedList.Any(i => i.X == nowPosition.X && i.Y == nowPosition.Y)) continue;

                // 上下左右の同色ぷよを探す
                if (GetPuyo(array, oldPosition) == GetPuyo(array, nowPosition))
                {
                    // 同色の場合
                    tmpSearchedList.Add(nowPosition);

                    // 再起処理
                    var result = SearchPuyoClear(array, tmpSearchedList);
                    foreach (var item in result)
                    {
                        // 未探索の場合、追加
                        if (!tmpSearchedList.Any(i => i.X == item.X && i.Y == item.Y)) tmpSearchedList.Add(item);
                    }


                }
            }

            // 同色のぷよが見つからなかった場合、再起処理を抜ける
            return tmpSearchedList;
        }

        internal ImageSource ShowImageBitmapSource(BitmapSource imageBitmapSource)
        {
            // ピクセルフォーマットを OpenCV の Mat 形式に変換
            var imageMat = OpenCVManager.BitmapSourceToMat(imageBitmapSource);

            var cellsRect = new Int32Rect(35, 1086, 1009, 708);
            var cells = getCells(imageBitmapSource, cellsRect);

            var cellWidth = cellsRect.Width / COLS; // 126
            var cellHeight = cellsRect.Height / ROWS; // 118
            foreach (var cell in cells)
            {
                Cv2.Rectangle(imageMat,
                    new Point(cellsRect.X + (cell.Position.X * cellWidth) + 5, cellsRect.Y + (cell.Position.Y * cellHeight) + 5),
                    new Point(cellsRect.X + (cell.Position.X * cellWidth) + cellWidth - 5, cellsRect.Y + (cell.Position.Y * cellHeight) + cellHeight - 5),
                    PuyoColors[cell.Puyo], 5, LineTypes.Link8, 0);
            }

            var headersRect = new Int32Rect(35, 1022, 1009, 64);
            var headerWidth = headersRect.Width / COLS; // 126
            var headerHeight = headersRect.Height; // 64

            // トリミング
            var imageBitmapSourceHeaders = new CroppedBitmap(imageBitmapSource, headersRect);

            var headers = getHeaders(imageBitmapSourceHeaders, headerWidth, headerHeight);
            foreach (var header in headers)
            {
                Cv2.Rectangle(imageMat,
                    new Point(headersRect.X + (header.Position.X * headerWidth) + 5, headersRect.Y + (header.Position.Y * headerHeight) + 5),
                    new Point(headersRect.X + (header.Position.X * headerWidth) + headerWidth - 5, headersRect.Y + (header.Position.Y * headerHeight) + headerHeight - 5),
                    PuyoColors[header.Puyo], 5, LineTypes.Link8, 0);
            }

            // Image コントロールに BitmapSource 形式の画像データを設定する。
            return BitmapSourceConverter.ToBitmapSource(imageMat);

        }

        internal PuyoEnum[,] HeadersArray { get; set; }
        internal PuyoEnum[,] CellsArray { get; set; }
        public Int32Rect CellsRect { get; set; }
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }
        public Int32Rect HeadersRect { get; set; }
        public int HeaderWidth { get; set; }
        public int HeaderHeight { get; set; }

        internal void SetHeaderRect(int x, int y, int width, int height)
        {
            HeadersRect = new Int32Rect(35, 1022, 1009, 64);
            HeaderWidth = HeadersRect.Width / COLS; // 126
            HeaderHeight = HeadersRect.Height; // 64
        }

        internal void SetCellRect(int x, int y, int width, int height)
        {
            CellsRect = new Int32Rect(35, 1086, 1009, 708);
            CellWidth = CellsRect.Width / COLS; // 126
            CellHeight = CellsRect.Height / ROWS; // 118
        }

    }
}
