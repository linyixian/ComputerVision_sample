using Microsoft.ProjectOxford.Vision;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace ComputerVision_sample
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture capture;
        private StorageFile file;
        private VisionServiceClient client;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //クライアント
            client = new VisionServiceClient("{your subscription key}", "https://southeastasia.api.cognitive.microsoft.com/vision/v1.0");

            //キャプチャーの設定
            MediaCaptureInitializationSettings captureInitSettings = new MediaCaptureInitializationSettings();
            captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
            captureInitSettings.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;

            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            captureInitSettings.VideoDeviceId = devices[0].Id;

            capture = new MediaCapture();
            await capture.InitializeAsync(captureInitSettings);

            //キャプチャーのサイズなど
            VideoEncodingProperties vp = new VideoEncodingProperties();
            vp.Width = 320;
            vp.Height = 240;
            vp.Subtype = "YUY2";

            await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, vp);

            preview.Source = capture;
            await capture.StartPreviewAsync();
        }

        private async void takepict_btn_Click(object sender, RoutedEventArgs e)
        {
            image.Source = null;

            //ファイルにキャプチャーを保存
            file = await KnownFolders.PicturesLibrary.CreateFileAsync("cvpict.jpg", CreationCollisionOption.ReplaceExisting);
            ImageEncodingProperties imageproperties = ImageEncodingProperties.CreateJpeg();
            await capture.CapturePhotoToStorageFileAsync(imageproperties, file);

            //保存したファイルの呼び出し
            IRandomAccessStream stream = await file.OpenReadAsync();
            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(stream);
            image.Source = bitmap;

            //VisionAPI呼び出し
            Getdata();
        }

        private async void Getdata()
        {
            //ファイル呼び出し
            var datafile = await KnownFolders.PicturesLibrary.GetFileAsync("cvpict.jpg");
            var fileStream = await datafile.OpenAsync(FileAccessMode.Read);
            
            //取得する項目の設定
            var visuals = new VisualFeature[] {
                //VisualFeature.Adult,
                //VisualFeature.Categories,
                //VisualFeature.Color,
                VisualFeature.Description,
                VisualFeature.Faces,
                //VisualFeature.ImageType,
                //VisualFeature.Tags
            };

            //APIの呼び出し
            var response = await client.AnalyzeImageAsync(fileStream.AsStream(), visuals);
            var captions = response.Description.Captions;
            var faces = response.Faces;

            //結果を表示
            var task = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                canvas.Children.Clear();

                resultTbox.Text = "説明:\n" + captions[0].Text + "\n\n";

                var i = 0;
                Windows.UI.Color color;

                foreach (var face in faces)
                {
                    resultTbox.Text = resultTbox.Text + "Face No." + (i + 1).ToString() + "\n";
                    resultTbox.Text = resultTbox.Text + "年齢:\n" + face.Age.ToString() + "\n";
                    resultTbox.Text = resultTbox.Text + "性別:\n" + face.Gender.ToString() + "\n";

                    if (face.Gender == "Male")
                    {
                        color = Windows.UI.Colors.Blue;
                    }
                    else
                    {
                        color = Windows.UI.Colors.Red;
                    }

                    Windows.UI.Xaml.Shapes.Rectangle rect = new Windows.UI.Xaml.Shapes.Rectangle
                    {
                        Height = face.FaceRectangle.Height,
                        Width = face.FaceRectangle.Width,
                        Stroke = new Windows.UI.Xaml.Media.SolidColorBrush(color),
                        StrokeThickness = 2
                    };

                    canvas.Children.Add(rect);
                    Canvas.SetLeft(rect, face.FaceRectangle.Left);
                    Canvas.SetTop(rect, face.FaceRectangle.Top);

                    i++;
                }
            });
        }

        
    }
}
