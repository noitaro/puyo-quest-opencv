using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Windows;

namespace WpfApp1.models
{
    internal class PuyoPosition
    {
        private readonly Dictionary<PuyoEnum, Scalar> PuyoColors = new()
        {
            { PuyoEnum.Red, Scalar.Red },
            { PuyoEnum.Blue, Scalar.Blue },
            { PuyoEnum.Yellow, Scalar.Yellow },
            { PuyoEnum.Green, Scalar.Green },
            { PuyoEnum.Purple, Scalar.Purple },
            { PuyoEnum.Heart, Scalar.Pink }
        };

        const int COLS = 8;
        const int ROWS = 6;

        internal PuyoEnum[,] HeadersArray { get; set; }
        internal PuyoEnum[,] CellsArray { get; set; }
        public Int32Rect CellsRect { get; set; }
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }
        public Int32Rect HeadersRect { get; set; }
        public int HeaderWidth { get; set; }
        public int HeaderHeight { get; set; }

        internal PuyoPosition()
        {
            HeadersArray = new PuyoEnum[1, COLS];
            CellsArray = new PuyoEnum[ROWS, COLS];
        }

        internal void SetHeadersArray(List<(Position Position, PuyoEnum Puyo)> headers)
        {
            foreach (var header in headers)
            {
                SetPuyo(HeadersArray, header.Position, header.Puyo);
            }
        }

        internal void SetCellsArray(List<(Position Position, PuyoEnum Puyo)> cells)
        {
            foreach (var cell in cells)
            {
                SetPuyo(CellsArray, cell.Position, cell.Puyo);
            }
        }

        private static void SetPuyo(PuyoEnum[,] array, Position position, PuyoEnum puyo)
        {
            array[position.Y, position.X] = puyo;
        }

        private static PuyoEnum GetPuyo(PuyoEnum[,] array, Position position)
        {
            // 配列範囲外の場合、Noneを戻す
            if (position.Y < 0 || array.GetLength(0) <= position.Y) return PuyoEnum.None;
            if (position.X < 0 || array.GetLength(1) <= position.X) return PuyoEnum.None;

            return array[position.Y, position.X];
        }
        internal PuyoEnum GetHeaderPuyo(int x, int y)
        {
            return GetPuyo(HeadersArray, new Position(x, y));
        }

        internal PuyoEnum GetCellPuyo(int x, int y)
        {
            return GetPuyo(CellsArray, new Position(x, y));
        }

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

        internal Scalar GetHeaderPuyoColor(int x, int y)
        {
            return PuyoColors[GetHeaderPuyo(x, y)];
        }

        internal Scalar GetCellPuyoColor(int x, int y)
        {
            return PuyoColors[GetCellPuyo(x, y)];
        }

    }
}
