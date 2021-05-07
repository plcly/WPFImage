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

namespace WPFImage.Origin
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
        private Dictionary<int, Image> imageDic;
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
                    LoadMemoryImage();
                    ShowImage();
                }
            }
            else
            {
                fileList = new List<string>();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
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
            if (imageDic.Keys.Max() < currentIndex)
            {
                currentIndex = imageDic.Keys.Max();
            }
            if (imageDic.Keys.Min() > currentIndex)
            {
                currentIndex = imageDic.Keys.Min();
            }
            if (imageDic.ContainsKey(currentIndex))
            {
                var image = imageDic[currentIndex];
                SetAllHide();
                image.Visibility = Visibility.Visible;
            }
            App.Current.Dispatcher.Invoke(() => LoadMemoryImage()
            , System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SetAllHide()
        {
            foreach (var image in imageDic)
            {
                image.Value.Visibility = Visibility.Hidden;
            }
        }

        private void LoadMemoryImage()
        {
            if (imageDic == null)
            {
                imageDic = new Dictionary<int, Image>();
                for (int i = 0; i < fileList.Count && i < preLoadNum*2; i++)
                {
                    var bitMapImage = new BitmapImage(new Uri(fileList[i]));
                    var image = new Image();
                    image.Visibility = Visibility.Hidden;
                    image.Source = bitMapImage;
                    bitMapImage.Freeze();

                    BaseGrid.Children.Add(image);
                    imageDic.Add(i, image);
                }
            }
            if (isNext)
            {
                if (currentIndex < fileList.Count - preLoadNum)
                {
                    if (!imageDic.ContainsKey(currentIndex + preLoadNum - 1))
                    {
                        var bitMapImage = new BitmapImage(new Uri(fileList[currentIndex + preLoadNum - 1]));
                        var image = new Image();
                        image.Visibility = Visibility.Hidden;
                        image.Source = bitMapImage;
                        bitMapImage.Freeze();

                        BaseGrid.Children.Add(image);
                        imageDic.Add(currentIndex + preLoadNum - 1, image);
                    }
                }
                if (imageDic.Count > preLoadNum * 2)
                {
                    var image = imageDic[imageDic.Keys.Min()];
                    BaseGrid.Children.Remove(image);
                    imageDic.Remove(imageDic.Keys.Min());

                }
            }
            else
            {
                if (currentIndex > preLoadNum)
                {
                    if (!imageDic.ContainsKey(currentIndex - preLoadNum - 1))
                    {
                        var bitMapImage = new BitmapImage(new Uri(fileList[currentIndex - preLoadNum - 1]));
                        var image = new Image();
                        image.Visibility = Visibility.Hidden;
                        image.Source = bitMapImage;
                        bitMapImage.Freeze();
                        BaseGrid.Children.Add(image);
                        imageDic.Add(currentIndex - preLoadNum - 1, image);
                    }
                }
                if (imageDic.Count > preLoadNum * 2)
                {
                    var image = imageDic[imageDic.Keys.Max()];
                    BaseGrid.Children.Remove(image);
                    imageDic.Remove(imageDic.Keys.Max());
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
}
