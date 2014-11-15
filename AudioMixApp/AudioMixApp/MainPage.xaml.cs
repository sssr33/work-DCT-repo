using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
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
        private bool backgroundStarted = false;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            ocrEngine = new OcrEngine(OcrLanguage.English);
            OcrText.IsReadOnly = true;
            sliderVolume.Value = 100;
        }

        private void MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            ValueSet valueSet = e.Data;
            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "ExistTrue":
                        CheckPlayList((byte[])valueSet[key]);
                        break;
                    case "BackgroundCreated":
                        CheckBackground((byte[])valueSet[key]);
                        break;
                }
            }
        }

        private void CheckBackground(byte[] serialized)
        {
            using (var ms = new MemoryStream(serialized))
            {
                CreatingPlaylist tmp = CreatingPlaylist.DeSerialize(ms);

                //backgroundStarted = true;

                if (tmp.Tracklist != null)
                {
                    if (tmp.Tracklist.Count != 0)
                    {
                        playList = CreatingPlaylist.DeSerialize(ms);
                        backgroundStarted = true;
                    }
                }
            }

            IfBackgroundExist();
        }

        private void CheckPlayList(byte[] serialized) 
        {
            using (var ms = new MemoryStream(serialized))
            {
                CreatingPlaylist tmp = CreatingPlaylist.DeSerialize(ms);

                backgroundStarted = true;   

                if (tmp.Tracklist != null)
                {
                    if (tmp.Tracklist.Count != 0)
                    {
                        playList = CreatingPlaylist.DeSerialize(ms);
                        //backgroundStarted = true;
                    }
                }
            }

        }

        private async void IfBackgroundExist()
        {
            if (backgroundStarted)
            {
                StorageFile testFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("test_file.txt", CreationCollisionOption.OpenIfExists);
                IRandomAccessStream writeStream = await testFile.OpenAsync(FileAccessMode.ReadWrite);
                IOutputStream outputSteam = writeStream.GetOutputStreamAt(0);
                var dataWriter = new DataWriter(outputSteam);
                dataWriter.WriteString("The background is already created" + "\r\n");
                await dataWriter.StoreAsync();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mediaPlayer = BackgroundMediaPlayer.Current;
            var messageToBackground = new ValueSet { { "Background", "Is background existing" } };
            BackgroundMediaPlayer.SendMessageToBackground(messageToBackground);
            BackgroundMediaPlayer.MessageReceivedFromBackground += MessageReceivedFromBackground;
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
            byte[] serialized;

            if (playList == null)
            {
                playList = new CreatingPlaylist();
                playList.CreatePlayList();
            }

            using (var ms = new MemoryStream())
            {
                serialized = playList.Serialize(ms);
            }

            var messageToBackground = new ValueSet { { "Play", serialized } };
            BackgroundMediaPlayer.SendMessageToBackground(messageToBackground);

            player = new Reader();
            //BackgroundMediaPlayer.IsMediaPlaying();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
                sliderProgress.Value = 0;
                newPosition = 0;
                var messageToBackground = new ValueSet { { "Stop", 0 } };
                BackgroundMediaPlayer.SendMessageToBackground(messageToBackground);
        }

        private void Slider1_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (player != null)
            {
                var messageToBackground = new ValueSet {{"Volume", (float) e.NewValue / 100}};
                BackgroundMediaPlayer.SendMessageToBackground(messageToBackground);
            }
        }

        private void Slider2_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (player != null)
            {
                newPosition = e.NewValue;
                var messageToBackground = new ValueSet { { "Rewind", e.NewValue } };
                BackgroundMediaPlayer.SendMessageToBackground(messageToBackground);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}