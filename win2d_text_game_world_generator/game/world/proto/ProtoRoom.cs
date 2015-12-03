using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class ProtoRoom
    {
        public PointInt Coordinates { get; set; }
        public ProtoRegion ProtoRegion { get; set; }
        public ProtoSubregion ProtoSubregion { get; set; }
        public List<string> DirectionalRoomConnections;
        public bool Available { get; set; }
        private int _elevation;
        public int Elevation
        {
            get
            {
                return _elevation;
            }
            set
            {
                _elevation = value;

                if (_elevation < 1) { _elevationcolor = Colors.Blue; }
                else if (_elevation < 2) { _elevationcolor = Colors.Gold; }
                else if (_elevation < 10) { _elevationcolor = Colors.Green; }
                else if (_elevation < 15) { _elevationcolor = Colors.DarkGreen; }
                else if (_elevation < 27) { _elevationcolor = Colors.Green; }
                else if (_elevation < 30) { _elevationcolor = Colors.Brown; }
                else { _elevationcolor = Colors.White; }

            }
        }
        private Color _elevationcolor;
        public Color ElevationColor { get { return _elevationcolor; } }

        public ProtoRoom(PointInt coordinates)
        {
            ProtoRegion = null;
            ProtoSubregion = null;
            Coordinates = coordinates;
            DirectionalRoomConnections = new List<string>();
            Available = true;
            Elevation = 3; // initialize to grass/green
        }

        public override bool Equals(object obj)
        {
            ProtoRoom compare = obj as ProtoRoom;
            return this.Coordinates.Equals(compare.Coordinates);
        }

        public override int GetHashCode()
        {
            return Coordinates.GetHashCode();
        }

        public bool IsTraversable()
        {
            return !(Elevation == 0 || Elevation == 30);
        }
    }
}