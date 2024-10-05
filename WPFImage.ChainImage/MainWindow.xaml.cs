using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private string destFolder;
        private int preLoadNum;
        private bool isNext;
        List<ChainImage> listChainImage;
        private bool deleteWhenClose;
        private string specificDateStr;
        private bool isLoading;
        private static readonly object lockObj = new object();

        public MainWindow()
        {
            InitializeComponent();

            folder = ConfigurationManager.AppSettings["folder"];
            destFolder = ConfigurationManager.AppSettings["destFolder"];
            preLoadNum = int.Parse(ConfigurationManager.AppSettings["preLoadNum"]);
            deleteWhenClose = bool.Parse(ConfigurationManager.AppSettings["deleteWhenClose"]);
            specificDateStr = ConfigurationManager.AppSettings["specificDate"];
            
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
            BitmapImage bitMapImage0 = InitBitMap(fileList[0]);
            var chainImage0 = new ChainImage
            {
                ChainIndex = 0,
                ImageControl = new Image { Visibility = Visibility.Hidden, Source = bitMapImage0 },
            };
            MyGrid.Children.Add(chainImage0.ImageControl);
            listChainImage.Add(chainImage0);
            for (int i = 1; i < preLoadNum * 2 && i < fileList.Count; i++)
            {
                var bitMapImage = InitBitMap(fileList[i]);
               
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

        private BitmapImage InitBitMap(string file)
        {
            var bitMapImage = new BitmapImage();
            if (File.Exists(file))
            {
                bitMapImage.BeginInit();
                bitMapImage.CacheOption = BitmapCacheOption.OnLoad;
                using (var fileStream = File.OpenRead(file))
                {
                    bitMapImage.StreamSource = fileStream;
                    bitMapImage.EndInit();
                    bitMapImage.Freeze();
                }
            }
            
            return bitMapImage;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (isLoading)
                return;
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
            this.Title = $"{System.IO.Path.GetFileName(fileList[currentIndex])}({currentIndex + 1}/{fileList.Count})";
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
                    if(isLoading)
                        return;
                    isLoading = true;
                    LoadMemoryChainImage(currentIndex);
                    isLoading = false;
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
                    var bitMapImage = InitBitMap(fileList[displayIndex + preLoadNum]);
                    chainImage.ImageControl.Source = bitMapImage;
                }
            }
            else
            {
                if (displayIndex + 1 - preLoadNum >= 0 && displayIndex < fileList.Count - preLoadNum)
                {
                    var memoryChainIndex = (displayIndex + 1 - preLoadNum) % (preLoadNum * 2);
                    var chainImage = listChainImage.FirstOrDefault(p => p.ChainIndex == memoryChainIndex);
                    var bitMapImage = InitBitMap(fileList[displayIndex + 1 - preLoadNum]);
                    chainImage.ImageControl.Source = bitMapImage;
                }
            }
        }

        private string GetDestName()
        {
            if (string.IsNullOrEmpty(specificDateStr))
            {
                specificDateStr = DateTime.Now.ToString("yyyyMMdd");
            }

            var files = Directory.GetFiles(folder, specificDateStr + "*.jpg");
            if (files.Length == 0)
            {
                return System.IO.Path.Combine(folder, specificDateStr + ".JPG");
            }
            return System.IO.Path.Combine(folder, specificDateStr + "-" + (files.Length) + ".JPG");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (string.IsNullOrEmpty(specificDateStr))
            {
                specificDateStr = DateTime.Now.ToString("yyyyMMdd");
            }
            listChainImage = null;
            MyGrid.Children.Clear();
            GC.Collect(2);
            if (deleteWhenClose)
            {
                var allFile = Directory.GetFiles(folder);
                foreach (var file in allFile)
                {
                    var fileName = System.IO.Path.GetFileName(file);
                    if (fileName.StartsWith(specificDateStr))
                    {
                        var destFileName = System.IO.Path.Combine(destFolder, fileName);
                        if (!File.Exists(destFileName))
                        {
                            File.Copy(file, destFileName);
                        }
                    }
                    else
                    {
                        FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                }
            }
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
