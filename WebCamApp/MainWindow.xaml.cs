using AxisCameraApp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WebCamApp;

namespace WebCamService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageSource _ImageSource;
        public ImageSource ImageSource
        {
            get { return this._ImageSource; }
            set { this._ImageSource = value; this.OnPropertyChanged("ImageSource"); }
        }
        private void OnPropertyChanged(string v)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(v));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private WebCamPlcManager _webCamPlcManager = new WebCamPlcManager();
        private ImageViewModel _imageViewModel = new ImageViewModel();
        public MainWindow()
        {
            InitializeComponent();

            DataContext = _imageViewModel;
            _imageViewModel.AlarmReceived += OnAlarmReceived;
            _imageViewModel.DisabledButton += OnDisableButtonReceived;
            _imageViewModel.EnableProperty = false;
            _imageViewModel.ImageSource = GetBindingImage(Environment.CurrentDirectory + @"\jpg\placeholder_no_image.png");
            //_webCamPlcManager.AlarmReceived += OnAlarmReceived;
            //_webCamPlcManager.DisabledButton += OnDisableButtonReceived;
            //_webCamPlcManager.CheckPhotoNeeded();
        }
        private async void OnAlarmReceived()
        {
            _imageViewModel.EnableProperty = true;
            (this.buttonOk).IsEnabled = true;
            GetPhoto();
        }
        public void GetPhoto()
        {
            try
            {
                DigestAuthFixer digest = new DigestAuthFixer("http://10.180.12.63", "user", "user");
                Stream stream = digest.GrabResponseStream("/jpg/image.jpg");

                SaveStreamAsFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), stream, "capture-" + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + ".jpg");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Exception Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
        public void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            if (!info.Exists)
            {
                info.Create();
            }
            string path = System.IO.Path.Combine(filePath, fileName);
            string nameFile = "";
            using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
            {
                inputStream.CopyTo(outputFileStream);
                nameFile = outputFileStream.Name;
            }
            BitmapImage img = GetBindingImage(nameFile);

            //_imageViewModel.ImageSource = img;
            //(this.imageG).Source = img;
        }
        private BitmapImage GetBindingImage(string nameFile)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(nameFile, UriKind.RelativeOrAbsolute);
            img.EndInit();
            _imageViewModel.ImageSource = img;
            (this.imageG).Source = img;
            return img;
        }
        private async void OnDisableButtonReceived()
        {
            _imageViewModel.EnableProperty = false;
            (this.buttonOk).IsEnabled = false;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _webCamPlcManager.WriteMem(1650, 0);
            _imageViewModel.EnableProperty = false;
            (this.buttonOk).IsEnabled = false;
            _imageViewModel.ImageSource = GetBindingImage(Environment.CurrentDirectory + @"\jpg\placeholder_no_image.png");
        }
        //    public DependencyProperty EnablePropertyProperty =
        //    DependencyProperty.Register("EnableProperty", typeof(bool), typeof(Window), new UIPropertyMetadata(false));

        //    public DependencyProperty DisplayedImageProperty =
        //    DependencyProperty.Register("DisplayedImage", typeof(string), typeof(Window), new UIPropertyMetadata(string.Empty));

        //    public void GetPhoto()
        //    {
        //        try
        //        {
        //            DigestAuthFixer digest = new DigestAuthFixer("http://10.180.12.63", "user", "user");
        //            Stream stream = digest.GrabResponseStream("/jpg/image.jpg");

        //            SaveStreamAsFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), stream, "capture-" + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + ".jpg");
        //        }
        //        catch (HttpRequestException e)
        //        {
        //            Console.WriteLine("Exception Caught!");
        //            Console.WriteLine("Message :{0} ", e.Message);
        //        }
        //    }
        //    public void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        //    {
        //        DirectoryInfo info = new DirectoryInfo(filePath);
        //        if (!info.Exists)
        //        {
        //            info.Create();
        //        }

        //        string path = System.IO.Path.Combine(filePath, fileName);
        //        string nameFile = "";
        //        using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
        //        {
        //            inputStream.CopyTo(outputFileStream);
        //            nameFile = outputFileStream.Name;
        //        }
        //        ImageSource = new BitmapImage(new Uri(nameFile, UriKind.Absolute));
        //        ImageSourceString = nameFile;
        //    }
        //    public bool EnableProperty
        //    {
        //        get
        //        {
        //            return (bool)this.GetValue(EnablePropertyProperty);
        //        }
        //        set
        //        {
        //            this.SetValue(EnablePropertyProperty, value);
        //        }
        //    }
        //    public string DisplayedImage
        //    {
        //        get { return (string)this.GetValue(DisplayedImageProperty); }
        //        set { this.SetValue(DisplayedImageProperty, value); }
        //    }
        //    private async void OnAlarmReceived()
        //    {
        //        EnableProperty = true;
        //        GetPhoto();
        //    }
        //    private async void OnDisableButtonReceived()
        //    {
        //        EnableProperty = false;
        //    }
    }
}