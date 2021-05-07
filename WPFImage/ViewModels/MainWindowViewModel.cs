using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media.Imaging;

namespace WPFImage.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Dependency
        private string title = "图片快速处理";
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private BitmapImage imageSource;
        public BitmapImage ImageSource
        {
            get { return imageSource; }
            set { SetProperty(ref imageSource, value); }
        }
        
        private DelegateCommand nextImage;
        public DelegateCommand NextImage =>
            nextImage ?? (nextImage = new DelegateCommand(ExecuteNextImage));

        void ExecuteNextImage()
        {
            if (fileList.Count > currentIndex)
            {
                currentIndex++;
                isNext = true;
                SetImageSource();
            }
        }

        private DelegateCommand preImage;
        public DelegateCommand PreImage =>
            preImage ?? (preImage = new DelegateCommand(ExecutePreImage));

        void ExecutePreImage()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                isNext = false;
                SetImageSource();
            }
        }

        private DelegateCommand<KeyEventArgs> _keyUpControl;
        public DelegateCommand<KeyEventArgs> KeyUpControl =>
            _keyUpControl ?? (_keyUpControl = new DelegateCommand<KeyEventArgs>(ExecuteKeyUpControl));

        void ExecuteKeyUpControl(KeyEventArgs e)
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


        #endregion

        private List<string> fileList;
        private int currentIndex;
        private Dictionary<int, BitmapImage> memoryBitmapImageDic;
        private string folder;
        private int preLoadNum;
        private bool isNext;


        public MainWindowViewModel()
        {
            folder = ConfigurationManager.AppSettings["folder"];
            preLoadNum = int.Parse(ConfigurationManager.AppSettings["preLoadNum"]);
            if (Directory.Exists(folder))
            {
                fileList = Directory.GetFiles(folder, "*.jpg")
                    .OrderBy(p => StringComparison.OrdinalIgnoreCase).ToList();
                if (fileList.Count > 0)
                {
                    LoadMemoryBitmapImage();
                    SetImageSource();
                }
            }
            else
            {
                fileList = new List<string>();
            }
        }



        private void SetImageSource()
        {
            Task.Run(() => LoadMemoryBitmapImage());

            if (memoryBitmapImageDic.Keys.Max() < currentIndex)
            {
                currentIndex = memoryBitmapImageDic.Keys.Max();
            }
            if (memoryBitmapImageDic.Keys.Min() > currentIndex)
            {
                currentIndex = memoryBitmapImageDic.Keys.Min();
            }
            if (memoryBitmapImageDic.ContainsKey(currentIndex))
            {
                ImageSource = memoryBitmapImageDic[currentIndex];
            }
        }
        private void LoadMemoryBitmapImage()
        {
            if (memoryBitmapImageDic == null)
            {
                memoryBitmapImageDic = new Dictionary<int, BitmapImage>();
                for (int i = 0; i < fileList.Count && i < preLoadNum; i++)
                {
                    var bitMapImage = new BitmapImage(new Uri(fileList[i]));
                    memoryBitmapImageDic.Add(i, bitMapImage);
                    bitMapImage.Freeze();
                }
            }
            if (isNext)
            {

                if (currentIndex < fileList.Count - preLoadNum)
                {
                    if (!memoryBitmapImageDic.ContainsKey(currentIndex + preLoadNum - 1))
                    {
                        var bitMapImage = new BitmapImage(new Uri(fileList[currentIndex + preLoadNum - 1]));
                        memoryBitmapImageDic.Add(currentIndex + preLoadNum - 1
                       , bitMapImage);
                        bitMapImage.Freeze();
                    }
                }
                if (memoryBitmapImageDic.Count > preLoadNum * 2)
                {
                    memoryBitmapImageDic.Remove(memoryBitmapImageDic.Keys.Min());

                }
            }
            else
            {
                if (currentIndex > preLoadNum)
                {
                    if (!memoryBitmapImageDic.ContainsKey(currentIndex - preLoadNum - 1))
                    {
                        var bitMapImage = new BitmapImage(new Uri(fileList[currentIndex - preLoadNum - 1]));
                        memoryBitmapImageDic.Add(currentIndex - preLoadNum - 1
                        , bitMapImage);
                        bitMapImage.Freeze();
                    }
                }
                if (memoryBitmapImageDic.Count > preLoadNum * 2)
                {
                    memoryBitmapImageDic.Remove(memoryBitmapImageDic.Keys.Max());
                }
            }
        }
        private string GetDestName()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var files = Directory.GetFiles(folder, today + "*.jpg");
            if (files.Length == 0)
            {
                return Path.Combine(folder, today + ".JPG");
            }
            return Path.Combine(folder, today + "-" + (files.Length) + ".JPG");
        }
    }

    public class InvokeEventCommand : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(InvokeEventCommand));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (Command != null && Command.CanExecute(parameter))
                Command.Execute(parameter);
        }
    }
}
