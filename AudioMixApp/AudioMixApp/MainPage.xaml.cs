using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage.FileProperties;
using WindowsPreview.Media.Ocr;
using Windows.Graphics.Imaging;

using AudioMixApp;
using MediaExtension;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace AudioMixApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        
        Reader player;
        double newPosition = 0;
        OcrEngine ocrEngine;
        UInt32 width;
        UInt32 height;
        CreatingPlaylist playList;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            ocrEngine = new OcrEngine(OcrLanguage.English);
            OcrText.IsReadOnly = true;
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

            player = new Reader();
            playList = new CreatingPlaylist();
            player.Play(playList);

            TimeSpan tmpDuration = player.Duration.Duration();

            sliderVolume.Value = 100;
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
                player.Rewinding(e.NewValue*((double) player.Duration.Duration().Ticks/100));
                newPosition = e.NewValue;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }

    public class CreatingPlaylist : IPlayList
    {
        private IRandomAccessStream stream;
        private List<string> trackList;
        private StorageFile file;
        private int startPosition;

        public virtual IRandomAccessStream GetStream(int trackNumber)
        {
            trackList = new List<string>
            {
                "Assets\\1 Caroline Duris - Barrage(original mix).mp3",
                "Assets\\02 - Master of Puppets.mp3",
                "Assets\\02 Quutamo.mp3"
            };

            OpenAudio(trackNumber);

            return stream;
        }

        public virtual bool CheckNext(int currentNumber)
        {
            if (trackList[currentNumber + 1] != "" && trackList[currentNumber + 1] != null)
                return true;   
            return false;
        }

        public virtual int GetPlayPosition()
        {
            startPosition = 0;  //заглушка
            return startPosition;   
        }

        public int GetPlayListLength()
        {
            return trackList.Count;
        }

        private void OpenAudio(int trackNumber)
        {
            var t = Package.Current.InstalledLocation.GetFileAsync(trackList[trackNumber]).AsTask();
            t.Wait();
            file = t.Result;

            var t2 = file.OpenAsync(FileAccessMode.Read).AsTask();
            t2.Wait();
            stream = t2.Result;
        }

        private void SetPosition(double inputPos)   //позже будет отдельный слайдер для выбора позиции воспроизведения следующего трека
        {
            startPosition = (int) inputPos;
        }
    }
}