using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFImage.ChainImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> fileList;
        private int currentIndex;
        private string folder;
        private int preLoadNum;
        private bool isNext;
        List<ChainImage> listChainImage;
        private static readonly object lockObj = new object();

        public MainWindow()
        {
            InitializeComponent();

            folder = ConfigurationManager.AppSettings["folder"];
            preLoadNum = int.Parse(ConfigurationManager.AppSettings["preLoadNum"]);
            if (Directory.Exists(folder))
            {
                fileList = Directory.GetFiles(folder, "*.jpg")
                    .OrderBy(p => StringComparison.OrdinalIgnoreCase).ToList();
                if (fileList.Count > 0)
                {
                    InitChainImage();
                    ShowImage();
                }
            }
            else
            {
                fileList = new List<string>();
            }
        }

        private void InitChainImage()
        {
            listChainImage = new List<ChainImage>();
            var bitMapImage0 = new BitmapImage(new Uri(fileList[0]));
            bitMapImage0.Freeze();
            var chainImage0 = new ChainImage
            {
                ChainIndex = 0,
                ImageControl = new Image { Visibility = Visibility.Hidden, Source = bitMapImage0 },
            };
            MyGrid.Children.Add(chainImage0.ImageControl);
            listChainImage.Add(chainImage0);
            for (int i = 1; i < preLoadNum * 2 && i < fileList.Count; i++)
            {
                var bitMapImage = new BitmapImage(new Uri(fileList[i]));
                bitMapImage.Freeze();
                var chainImage = new ChainImage
                {
                    ChainIndex = i,
                    ImageControl = new Image { Visibility = Visibility.Hidden, Source = bitMapImage },
                    PreImage = listChainImage[i - 1]
                };
                MyGrid.Children.Add(chainImage.ImageControl);
                listChainImage[i - 1].NextImage = chainImage;
                listChainImage.Add(chainImage);
            }
            chainImage0.PreImage = listChainImage.Last();
            listChainImage.Last().NextImage = chainImage0;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                ExecuteNextImage();
            }
            if (e.Key == Key.Left)
            {
                ExecutePreImage();
            }
            if (e.Key == Key.Enter)
            {
                var fileName = fileList[currentIndex];
                var destName = GetDestName();
                File.Copy(fileName, destName);
                MessageBox.Show("完成");
            }
        }

        private void ExecutePreImage()
        {
            isNext = false;
            currentIndex--;
            ShowImage();
        }

        private void ExecuteNextImage()
        {
            isNext = true;
            currentIndex++;
            ShowImage();
        }

        private void ShowImage()
        {
            if (currentIndex >= fileList.Count)
            {
                currentIndex = fileList.Count - 1;
            }
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            this.Title = System.IO.Path.GetFileName(fileList[currentIndex]);
            var chainIndex = currentIndex % (preLoadNum * 2);
            var chainImage = listChainImage.FirstOrDefault(p => p.ChainIndex == chainIndex);

            chainImage.ImageControl.Visibility = Visibility.Visible;
            chainImage.PreImage.ImageControl.Visibility = Visibility.Hidden;
            chainImage.NextImage.ImageControl.Visibility = Visibility.Hidden;

            Task.Run(() => LockMethod(currentIndex));
        }

        private void LockMethod(int currentIndex)
        {
            lock (lockObj)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LoadMemoryChainImage(currentIndex);
                }
            , System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void LoadMemoryChainImage(int displayIndex)
        {
            if (isNext)
            {
                if (displayIndex < fileList.Count - preLoadNum && displayIndex >= preLoadNum)
                {
                    var memoryChainIndex = (displayIndex + preLoadNum) % (preLoadNum * 2);
                    var chainImage = listChainImage.FirstOrDefault(p => p.ChainIndex == memoryChainIndex);
                    var bitMapImage = new BitmapImage(new Uri(fileList[displayIndex + preLoadNum]));
                    bitMapImage.Freeze();
                    chainImage.ImageControl.Source = bitMapImage;
                }
            }
            else
            {
                if (displayIndex + 1 - preLoadNum >= 0 && displayIndex < fileList.Count - preLoadNum)
                {
                    var memoryChainIndex = (displayIndex + 1 - preLoadNum) % (preLoadNum * 2);
                    var chainImage = listChainImage.FirstOrDefault(p => p.ChainIndex == memoryChainIndex);
                    var bitMapImage = new BitmapImage(new Uri(fileList[displayIndex + 1 - preLoadNum]));
                    bitMapImage.Freeze();
                    chainImage.ImageControl.Source = bitMapImage;
                }
            }
        }

        private string GetDestName()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var files = Directory.GetFiles(folder, today + "*.jpg");
            if (files.Length == 0)
            {
                return System.IO.Path.Combine(folder, today + ".JPG");
            }
            return System.IO.Path.Combine(folder, today + "-" + (files.Length) + ".JPG");
        }
    }

    public class ChainImage
    {
        public int ChainIndex { get; set; }
        public Image ImageControl { get; set; }
        public ChainImage NextImage { get; set; }
        public ChainImage PreImage { get; set; }
    }

}
