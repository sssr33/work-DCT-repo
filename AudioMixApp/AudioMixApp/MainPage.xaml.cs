using System;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Media.Ocr;
using Windows.Graphics.Imaging;
using MediaData;
using MediaExtension;

namespace AudioMixApp
{
    public sealed partial class MainPage : Page
    {
        
        Reader player;
        CreatingPlaylist playList;
        double newPosition = 0;
        OcrEngine ocrEngine;
        UInt32 width;
        UInt32 height;
        private MediaPlayer mediaPlayer;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            ocrEngine = new OcrEngine(OcrLanguage.English);
            OcrText.IsReadOnly = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mediaPlayer = BackgroundMediaPlayer.Current;
        }

        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync("Assets\\sample.png");
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            //ImageProperties imgProp = await file.Properties.GetImagePropertiesAsync();
            
            width = decoder.PixelWidth;
            height = decoder.PixelHeight;

            var pixels = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        new BitmapTransform(), ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);

            OcrResult or = await ocrEngine.RecognizeAsync(height, width, pixels.DetachPixelData());

            if (or.Lines != null)
            {
                string recognizedText = "";
                foreach (var line in or.Lines)
                {
                    foreach (var word in line.Words)
                        recognizedText += word.Text + " ";
                    recognizedText += Environment.NewLine;
                }

                OcrText.Text = recognizedText;
            }
        }

        private void OpenButtonClick(Object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Stop();
            
            //playList = new CreatingPlaylist();
            //playList.CreatePlayList();
            //player = new Reader();
            //player.Play(playList);

            //if (player != null)
            //{
            //    TimeSpan tmpDuration = player.Duration.Duration();
            //}
            sliderVolume.Value = 100;

            //playList = new CreatingPlaylist();
            //playList.CreatePlayList();

            var messageToBackground = new ValueSet { { "Play", "qwerty" } };
            BackgroundMediaPlayer.SendMessageToBackground(messageToBackground);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
            {
                sliderProgress.Value = 0;
                newPosition = 0;
                player.Stop();
            }
        }

        private void Slider1_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (player != null)
                player.Volume((float)e.NewValue/100);
        }

        private void Slider2_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (player != null)
            {
                player.Rewinding(e.NewValue*((double) player.GetGlobalDuration()/100));
                newPosition = e.NewValue;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}