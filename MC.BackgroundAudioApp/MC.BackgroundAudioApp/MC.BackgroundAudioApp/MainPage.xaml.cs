using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MC.BackgroundAudioApp
{
    public sealed partial class MainPage : Page
    {
        private MediaPlayer _mediaPlayer;

        public MainPage()
        {
            InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _mediaPlayer = BackgroundMediaPlayer.Current;
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            var message = new ValueSet
                          {
                              {
                                  "Play",
                                  "E://SkyDrive/WORK DCT/MC.BackgroundAudioApp/MC.BackgroundAudioApp/MC.BackgroundAudioApp/Assets/1 Caroline Duris - Barrage(original mix).mp3"
                              }
                          };
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }
    }
}