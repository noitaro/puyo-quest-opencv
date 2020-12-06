using OpenCvSharp;
using System.Windows;
using System.Windows.Media.Imaging;
using Window = System.Windows.Window;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var pqManager = new PuyoQuestManager();

            // OpenCVを使ってぷよのマス状態を取得
            var puyoPosition = pqManager.GetPuyoPosition((BitmapSource)image.Source);

            var bestScore = pqManager.getBestScore(puyoPosition.HeadersArray, puyoPosition.CellsArray);

            image.Source = pqManager.ShowImageBitmapSource((BitmapSource)image.Source);
        }

    }
}
