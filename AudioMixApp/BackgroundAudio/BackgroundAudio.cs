using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using MediaData;
using MediaExtension;

namespace BackgroundAudio
{
    public sealed class BackgroundAudio : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private SystemMediaTransportControls _systemMediaTransportControl;
        Reader player;
        CreatingPlaylist playList;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
            _systemMediaTransportControl.IsEnabled = true;

            BackgroundMediaPlayer.MessageReceivedFromForeground += MessageReceivedFromForeground;

            BackgroundMediaPlayer.Current.MediaOpened += CurrentOnMediaOpened;

            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += Taskcompleted;

            player = new Reader();
            playList = new CreatingPlaylist();

            _deferral = taskInstance.GetDeferral();
        }

        void FakePause()
        {
            BackgroundMediaPlayer.Current.Position = new TimeSpan(0);
            BackgroundMediaPlayer.Current.PlaybackRate = 0.0;
        }

        private void CurrentOnMediaOpened(MediaPlayer sender, object args)
        {
            playList.CreatePlayList();
            player.Play(playList);
            FakePause();
        }

        private void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            ValueSet valueSet = e.Data;
            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "Play":
                        Play(valueSet[key].ToString());
                        break;
                }
            }
        }

        private async void Play(string s)
        {
            MediaPlayer mediaPlayer = BackgroundMediaPlayer.Current;
            mediaPlayer.AutoPlay = true;
            //playList = list;

            var crutchFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\silence1sec.mp3");
            mediaPlayer.SetFileSource(crutchFile);

            FakePause();

            //Update the universal volume control
            _systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;
            _systemMediaTransportControl.IsPauseEnabled = true;
            _systemMediaTransportControl.IsPlayEnabled = false;
            _systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
            _systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = "Music";
            _systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist = "Different ";
            _systemMediaTransportControl.DisplayUpdater.Update();
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    BackgroundMediaPlayer.Current.Play();
                    FakePause();
                    player.Play(playList);
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    BackgroundMediaPlayer.Current.Pause();
                    player.Stop();
                    break;
            }
        }

        private void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            BackgroundMediaPlayer.Shutdown();
            _deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // You get some time here to save your state before process and resources are reclaimed
            BackgroundMediaPlayer.Shutdown();
            _deferral.Complete();
        }
    }
}
