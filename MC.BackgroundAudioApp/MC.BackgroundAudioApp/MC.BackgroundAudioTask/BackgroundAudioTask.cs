using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using MediaExtension;

namespace MC.BackgroundAudioTask
{
    public sealed class BackgroundAudioTask : IBackgroundTask
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
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayerCurrentStateChanged;

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += OnCanceled;
            taskInstance.Task.Completed += Taskcompleted;

            player = new Reader();
            playList = new CreatingPlaylist();

            _deferral = taskInstance.GetDeferral();
            
        }

        private void MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            ValueSet valueSet = e.Data;
            foreach (string key in valueSet.Keys)
            {
                switch (key)
                {
                    case "Play":
                        Debug.WriteLine("Starting Playback");
                        Play(valueSet[key].ToString());
                        //player.Play(playList);
                        break;
                }
            }
        }

        private void Play(string s)
        {
            MediaPlayer mediaPlayer = BackgroundMediaPlayer.Current;
            mediaPlayer.AutoPlay = true;
            mediaPlayer.SetUriSource(new Uri(s));
            //player.Play(playList);

            //Update the universal volume control
            _systemMediaTransportControl.ButtonPressed += MediaTransportControlButtonPressed;
            _systemMediaTransportControl.IsPauseEnabled = true;
            _systemMediaTransportControl.IsPlayEnabled = true;
            _systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
            _systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = "Test Title";
            _systemMediaTransportControl.DisplayUpdater.MusicProperties.Artist = "Test Artist";
            _systemMediaTransportControl.DisplayUpdater.Update();
        }


        private void BackgroundMediaPlayerCurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                _systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                _systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }

        private void MediaTransportControlButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    BackgroundMediaPlayer.Current.Play();
                    //player.Play(playList);
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    BackgroundMediaPlayer.Current.Pause();
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

    public sealed class Track : ITrack
    {
        private int globalPosInPlayList; //song position relatively begin of global playlist duration
        private string trackName;

        public Track(int globalPos, string name)
        {
            globalPosInPlayList = globalPos;
            trackName = name;
        }

        public string GetName()
        {
            return trackName;
        }

        public int GetPosition()
        {
            return globalPosInPlayList;
        }
    }

    public sealed class CreatingPlaylist : IPlayList
    {
        private IRandomAccessStream stream;
        private IList<ITrack> trackList;
        private ITrack track;

        //global positions sets outside
        public IList<ITrack> CreatePlayList()
        {
            trackList = new List<ITrack>();

            AddTrackInPlayList(0, "Assets\\1 Caroline Duris - Barrage(original mix).mp3");
            AddTrackInPlayList(10, "Assets\\02 Quutamo.mp3");

            return trackList;
        }

        public IRandomAccessStream GetStream(int trackNumber)
        {
            var t = Package.Current.InstalledLocation.GetFileAsync(trackList[trackNumber].GetName()).AsTask();
            t.Wait();
            StorageFile file = t.Result;

            var t2 = file.OpenAsync(FileAccessMode.Read).AsTask();
            t2.Wait();

            stream = t2.Result;

            return stream;
        }

        public bool CheckNext(int currentNumber)
        {
            if (trackList[currentNumber + 1].GetName() != "" && trackList[currentNumber + 1] != null)
                return true;
            return false;
        }

        public int GetPlayListLength()
        {
            return trackList.Count;
        }

        private void AddTrackInPlayList(int trackNumber, string trackName)
        {
            track = new Track(trackNumber, trackName);
            trackList.Add(track);
        }
    }
}