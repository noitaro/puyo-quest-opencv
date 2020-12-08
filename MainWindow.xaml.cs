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
            var imageBitmapSource = (BitmapSource)image.Source;
            var cellsArray = pqManager.getCells(imageBitmapSource);
            var headersArray = pqManager.getHeaders(imageBitmapSource);

            //var bestScore = pqManager.getBestScore(headersArray, cellsArray);

            cellsArray = pqManager.getConnectCount(cellsArray);

            image.Source = pqManager.ShowImageBitmapSource(imageBitmapSource, headersArray, cellsArray);
        }

    }
}
