using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using MediaExtension;

namespace MediaData
{
    public class CreatingPlaylist : IPlayList
    {
        private List<Track> trackList;

        public List<Track> Tracklist { get { return this.trackList; } set { this.trackList = value; } }

        //global positions sets outside
        public void CreatePlayList()
        {
            trackList = new List<Track>();

            AddTrackInPlayList(10, "Assets\\02 Quutamo.mp3");
            AddTrackInPlayList(0, "Assets\\1 Caroline Duris - Barrage(original mix).mp3");
            //AddTrackInPlayList(25, "Assets\\02 - Master of Puppets.mp3");
            
        }

        public virtual IRandomAccessStream GetStream(int trackNumber)
        {
            var t = Package.Current.InstalledLocation.GetFileAsync(trackList[trackNumber].GetName()).AsTask();
            t.Wait();
            StorageFile file = t.Result;

            var t2 = file.OpenAsync(FileAccessMode.Read).AsTask();
            t2.Wait();

            return t2.Result;
        }

        public virtual bool CheckNext(int currentNumber)
        {
            if (trackList[currentNumber + 1].GetName() != "" && trackList[currentNumber + 1] != null)
                return true;
            return false;
        }

        public virtual int GetPlayListLength()
        {
            return trackList.Count;
        }

        private void AddTrackInPlayList(int trackNumber, string trackName)
        {
            var track = new Track(trackNumber, trackName);
            trackList.Add(track);
        }


        public ITrack GetTrack(int index)
        {
            return this.trackList[index];
        }

        public void SortPlaylist()
        {
            this.trackList.Sort((x, y) => x.Position - y.Position);
        }
    }
}
