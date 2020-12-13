using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Window = System.Windows.Window;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Button[,] buttons = new Button[PuyoQuestManager.ROWS, PuyoQuestManager.COLS];

        public MainWindow()
        {
            InitializeComponent();

            buttons[0, 0] = btn_0_0;
            buttons[1, 0] = btn_0_1;
            buttons[2, 0] = btn_0_2;
            buttons[3, 0] = btn_0_3;
            buttons[4, 0] = btn_0_4;
            buttons[5, 0] = btn_0_5;
            buttons[0, 1] = btn_1_0;
            buttons[1, 1] = btn_1_1;
            buttons[2, 1] = btn_1_2;
            buttons[3, 1] = btn_1_3;
            buttons[4, 1] = btn_1_4;
            buttons[5, 1] = btn_1_5;
            buttons[0, 2] = btn_2_0;
            buttons[1, 2] = btn_2_1;
            buttons[2, 2] = btn_2_2;
            buttons[3, 2] = btn_2_3;
            buttons[4, 2] = btn_2_4;
            buttons[5, 2] = btn_2_5;
            buttons[0, 3] = btn_3_0;
            buttons[1, 3] = btn_3_1;
            buttons[2, 3] = btn_3_2;
            buttons[3, 3] = btn_3_3;
            buttons[4, 3] = btn_3_4;
            buttons[5, 3] = btn_3_5;
            buttons[0, 4] = btn_4_0;
            buttons[1, 4] = btn_4_1;
            buttons[2, 4] = btn_4_2;
            buttons[3, 4] = btn_4_3;
            buttons[4, 4] = btn_4_4;
            buttons[5, 4] = btn_4_5;
            buttons[0, 5] = btn_5_0;
            buttons[1, 5] = btn_5_1;
            buttons[2, 5] = btn_5_2;
            buttons[3, 5] = btn_5_3;
            buttons[4, 5] = btn_5_4;
            buttons[5, 5] = btn_5_5;
            buttons[0, 6] = btn_6_0;
            buttons[1, 6] = btn_6_1;
            buttons[2, 6] = btn_6_2;
            buttons[3, 6] = btn_6_3;
            buttons[4, 6] = btn_6_4;
            buttons[5, 6] = btn_6_5;
            buttons[0, 7] = btn_7_0;
            buttons[1, 7] = btn_7_1;
            buttons[2, 7] = btn_7_2;
            buttons[3, 7] = btn_7_3;
            buttons[4, 7] = btn_7_4;
            buttons[5, 7] = btn_7_5;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var pqManager = new PuyoQuestManager();

            // OpenCVを使ってぷよのマス状態を取得
            var imageBitmapSource = (BitmapSource)image.Source;
            var cellsArray = PuyoQuestManager.GetCells(imageBitmapSource);
            var headersArray = PuyoQuestManager.GetHeaders(imageBitmapSource);

            //var bestScore = pqManager.getBestScore(headersArray, cellsArray);

            cellsArray = pqManager.GetConnectCount(cellsArray);
            var bestScorePosition = pqManager.GetBestScore(cellsArray);

            imageBitmapSource = PuyoQuestManager.DrawPuyoColor(imageBitmapSource, headersArray, cellsArray);
            imageBitmapSource = PuyoQuestManager.DrawConnectCount(imageBitmapSource, cellsArray);
            imageBitmapSource = PuyoQuestManager.DrawBestScore(imageBitmapSource, bestScorePosition);

            image.Source = imageBitmapSource;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var tag = Convert.ToString(((Button)sender).Tag).Split(",");

            var pqManager = new PuyoQuestManager();

            // OpenCVを使ってぷよのマス状態を取得
            var imageBitmapSource = (BitmapSource)image.Source;
            var cellsArray = PuyoQuestManager.GetCells(imageBitmapSource);
            var headersArray = PuyoQuestManager.GetHeaders(imageBitmapSource);

            for (int x = 0; x < PuyoQuestManager.COLS; x++)
            {
                for (int y = 0; y < PuyoQuestManager.ROWS; y++)
                {
                    var color = PuyoQuestManager.PuyoColors[cellsArray[y, x].Color];
                    buttons[y, x].Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)color.Val2, (byte)color.Val1, (byte)color.Val0));
                }
            }

        }
    }
}
