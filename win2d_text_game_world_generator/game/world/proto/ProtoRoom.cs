using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace win2d_text_game_world_generator
{
    public class ProtoRoom
    {
        public PointInt Coordinates { get; set; }
        public ProtoRegion Region { get; set; }
        public bool Available { get; set; }

        public ProtoRoom(PointInt coordinates)
        {
            Region = null;
            Coordinates = coordinates;
            Available = true;
        }
    }
}