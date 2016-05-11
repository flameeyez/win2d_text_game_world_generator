using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace win2d_text_game_world_generator
{
    public class RoomConnection
    {
        private int _regionid;
        public int RegionID { get { return _regionid; } }
        private int _subregionid;
        public int SubregionID { get { return _subregionid; } }
        private int _roomid;
        public int RoomID { get { return _roomid; } }

        private string _verb;
        public string Verb { get { return _verb; } }
        private string _noun;
        public string Noun { get { return _noun; } }

        public RoomConnection(int regionID, int subregionID, int roomID, string verb, string noun)
        {
            _regionid = regionID;
            _subregionid = subregionID;
            _roomid = RoomID;
            _verb = verb;
            _noun = noun;
        }
    }
}
