using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using MediaExtension;
using ProtoBuf;

namespace MediaData
{
    [ProtoContract]
    public class CreatingPlaylist : IPlayList
    {
        private List<Track> trackList;

        [ProtoMember(1)]
        public List<Track> Tracklist { get { return this.trackList; } set { this.trackList = value; } }

        public CreatingPlaylist()
        {
            
        }

        public CreatingPlaylist(List<Track> trackList)
        {
            this.trackList = trackList;
        }

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

        public byte[] Serialize(MemoryStream stream)
        {
            byte[] serialized;
            Serializer.Serialize(stream, this.Tracklist);
            serialized = stream.ToArray();
            return serialized;
        }

        public static CreatingPlaylist DeSerialize(MemoryStream stream)
        {
            var playlist = Serializer.Deserialize<CreatingPlaylist>(stream);
            return playlist;
        }
    }
}
