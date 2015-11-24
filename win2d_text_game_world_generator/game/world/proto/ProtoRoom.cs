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
        public ProtoRegion ProtoRegion { get; set; }
        public ProtoSubregion ProtoSubregion { get; set; }
        public bool Available { get; set; }

        public ProtoRoom(PointInt coordinates)
        {
            ProtoRegion = null;
            ProtoSubregion = null;
            Coordinates = coordinates;
            Available = true;
        }
    }
}