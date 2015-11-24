using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace win2d_text_game_world_generator
{
    public class Room
    {
        public PointInt Coordinates { get; set; }
        public Region Region { get; set; }
        public Subregion Subregion { get; set; }

        private Room() { }
        public static Room FromProtoRoom(Region region, Subregion subregion, ProtoRoom pr)
        {
            Room tile = new Room();
            tile.Coordinates = pr.Coordinates;
            tile.Region = region;
            tile.Subregion = subregion;
            return tile;
        }
    }
}
