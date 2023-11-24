using System;
using System.ComponentModel;
using MvvmHelpers;
using System.Windows.Media;

namespace WebCamApp
{
    public class ImageViewModel : BaseViewModel
    {      
        private ImageSource _ImageSource;
        public ImageSource ImageSource
        {
            get { return this._ImageSource; }
            set { this._ImageSource = value; this.OnPropertyChanged("ImageSource"); }
        }
        private string _ImageSourceString;
        public string ImageSourceString
        {
            get { return this._ImageSourceString; }
            set { this._ImageSourceString = value; this.OnPropertyChanged("ImageSourceString"); }
        }
        private bool _EnableProperty;
        public bool EnableProperty
        {
            get { return this._EnableProperty; }
            set { this._EnableProperty = value; this.OnPropertyChanged("EnableProperty"); }
        }
        public event Action AlarmReceived
        {
            add => _webCamPlcManager.AlarmReceived += value;
            remove => _webCamPlcManager.AlarmReceived -= value;
        }
        public event Action DisabledButton
        {
            add => _webCamPlcManager.DisabledButton += value;
            remove => _webCamPlcManager.DisabledButton -= value;
        }
        private void OnPropertyChanged(string v)
        {
            // throw new NotImplementedException();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(v));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private WebCamPlcManager _webCamPlcManager = new WebCamPlcManager();
        public ImageViewModel()
        {
            EnableProperty = false;
            _webCamPlcManager.CheckPhotoNeeded();
        }
        //public void GetPhoto()
        //{
        //    try
        //    {
        //        DigestAuthFixer digest = new DigestAuthFixer("http://10.180.12.63", "user", "user");
        //        Stream stream = digest.GrabResponseStream("/jpg/image.jpg");

        //        SaveStreamAsFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), stream, "capture-" + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + ".jpg");
        //    }
        //    catch (HttpRequestException e)
        //    {
        //        Console.WriteLine("Exception Caught!");
        //        Console.WriteLine("Message :{0} ", e.Message);
        //    }
        //}
        //public void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        //{
        //    DirectoryInfo info = new DirectoryInfo(filePath);
        //    if (!info.Exists)
        //    {
        //        info.Create();
        //    }

        //    string path = System.IO.Path.Combine(filePath, fileName);
        //    string nameFile = "";
        //    using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
        //    {
        //        inputStream.CopyTo(outputFileStream);
        //        nameFile = outputFileStream.Name;
        //    }
        //    var img = new BitmapImage();
        //    img.BeginInit();
        //    img.UriSource = new Uri(nameFile, UriKind.Absolute);
        //    img.EndInit();

        //    ImageSource = img;
        //    //ImageSource = new BitmapImage(new Uri(nameFile, UriKind.Absolute));
        //    //ImageSourceString = nameFile;
        //}
        //public bool EnableProperty
        //{
        //    get
        //    {
        //        return (bool)this.GetValue(EnablePropertyProperty);
        //    }
        //    set
        //    {
        //        this.SetValue(EnablePropertyProperty, value);
        //    }
        //}
        //public string DisplayedImage
        //{
        //    get { return (string)this.GetValue(DisplayedImageProperty); }
        //    set { this.SetValue(DisplayedImageProperty, value); }
        //}
        //private async void OnAlarmReceived()
        //{
        //    EnableProperty = true;
        //    GetPhoto();
        //}
        //private async void OnDisableButtonReceived()
        //{
        //    EnableProperty = false;
        //}
    }
}