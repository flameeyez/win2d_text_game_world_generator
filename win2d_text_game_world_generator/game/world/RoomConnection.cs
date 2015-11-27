using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace win2d_text_game_world_generator
{
    // immutable
    public class RoomConnection
    {
        private Region _region;
        public Region Region { get { return _region; } }
        private Subregion _subregion;
        public Subregion Subregion { get { return _subregion; } }
        private Room _room;
        public Room Room { get { return _room; } }

        public RoomConnection(Region region, Subregion subregion, Room room)
        {
            _region = region;
            _subregion = subregion;
            _room = room;
        }
    }
}
