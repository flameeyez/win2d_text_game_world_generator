using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class ProtoRoom
    {
        public int ID { get; set; }
        public PointInt CoordinatesXY { get; set; }
        public Tuple<int, int, int> WorldCoordinatesAsTuple { get { return new Tuple<int, int, int>(ProtoRegion.ID, ProtoSubregion.ID, ID); } }
        public ProtoRegion ProtoRegion { get; set; }
        public ProtoSubregion ProtoSubregion { get; set; }
        public Dictionary<string, Tuple<int, int, int>> DirectionalRoomConnections;
        public List<RoomConnection> ProtoRoomConnections;
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

        public ProtoRoom(int id, PointInt coordinatesXY, int elevation = 3)
        {
            ID = id;
            ProtoRegion = null;
            ProtoSubregion = null;
            CoordinatesXY = coordinatesXY;
            // DirectionalRoomConnections = new List<string>();
            DirectionalRoomConnections = new Dictionary<string, Tuple<int, int, int>>();
            ProtoRoomConnections = new List<RoomConnection>();
            Available = true;
            Elevation = elevation; // default (3) is grass/green
        }

        public bool IsTraversable() { return !(Elevation == 0 || Elevation == 30); }

        #region Hashing / Equality
        public override bool Equals(object obj)
        {
            ProtoRoom compare = obj as ProtoRoom;
            return CoordinatesXY.Equals(compare.CoordinatesXY);
        }
        public override int GetHashCode()
        {
            return CoordinatesXY.GetHashCode();
        }
        #endregion       

        internal void AddConnection(string verb, string noun, Tuple<int, int, int> roomWorldCoordinates)
        {
            ProtoRoomConnections.Add(new RoomConnection(roomWorldCoordinates.Item1, roomWorldCoordinates.Item2, roomWorldCoordinates.Item3, verb, noun));
        }
    }
}