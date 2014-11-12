using System.IO;
using MediaExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace MediaData
{
    //make an interfase in Reader. sent List of Track instead of IPlayList
    [ProtoContract] //move into CreatingPlaylist
    public class Track : ITrack
    {
        private int globalPosInPlayList; //song position relatively begin of global playlist duration
        private string trackName;

        public Track()
        {
        }

        public Track(int globalPos, string name)
        {
            globalPosInPlayList = globalPos;
            trackName = name;
        }

        [ProtoMember(1)]//move into CreatingPlaylist
        public int Position { get { return globalPosInPlayList; } set { this.globalPosInPlayList = value; } }

        [ProtoMember(2)]
        public string Name { get { return trackName; } set { this.trackName = value; } }

        public int GetPosition()
        {
            return globalPosInPlayList;
        }

        public string GetName()
        {
            return trackName;
        }

        public void Serialize(Stream stream)    //move into CreatingPlaylist
        {
            Serializer.Serialize(stream, this);
        }
    }
}
