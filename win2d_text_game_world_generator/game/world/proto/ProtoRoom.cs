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
        // public List<string> DirectionalRoomConnections;
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

        #region Hashing
        public override bool Equals(object obj)
        {
            ProtoRoom compare = obj as ProtoRoom;
            return this.CoordinatesXY.Equals(compare.CoordinatesXY);
        }
        public override int GetHashCode()
        {
            return CoordinatesXY.GetHashCode();
        }
        #endregion       

        #region Drawing
        public void DrawHeightMap(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(CoordinatesXY.X * Statics.MapResolution, CoordinatesXY.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), ElevationColor);
        }
        public void DrawPaths(CanvasDrawingSession ds)
        {
            foreach (string DirectionalRoomConnection in DirectionalRoomConnections.Keys)
            {
                switch (DirectionalRoomConnection)
                {
                    case "nw":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.X - 1) + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.Y - 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "n":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.Y - 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "ne":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.X + 1) + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.Y - 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "w":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.X - 1) + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "o":
                        break;
                    case "e":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.X + 1) + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "sw":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.X - 1) + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.Y + 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "s":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.Y + 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "se":
                        ds.DrawLine((CoordinatesXY.X + 0.5f) * Statics.MapResolution,
                             (CoordinatesXY.Y + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.X + 1) + 0.5f) * Statics.MapResolution,
                             ((CoordinatesXY.Y + 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }
        public void DrawRegions(CanvasDrawingSession ds)
        {
            Tuple<int, int, int> debug;
            if (DirectionalRoomConnections.TryGetValue("o", out debug))
            {
                ds.FillRectangle(new Rect(CoordinatesXY.X * Statics.MapResolution, CoordinatesXY.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), Colors.Red);
            }
            else
            {
                ds.FillRectangle(new Rect(CoordinatesXY.X * Statics.MapResolution, CoordinatesXY.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), ProtoRegion.Color);
            }
        }
        public void DrawSubregions(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(CoordinatesXY.X * Statics.MapResolution, CoordinatesXY.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), ProtoSubregion.Color);
        }
        public void DrawCaves(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(CoordinatesXY.X * Statics.MapResolution, CoordinatesXY.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), Colors.Gray);
        }
        internal void AddConnection(string verb, string noun, Tuple<int, int, int> roomWorldCoordinates)
        {
            ProtoRoomConnections.Add(new RoomConnection(roomWorldCoordinates.Item1, roomWorldCoordinates.Item2, roomWorldCoordinates.Item3, verb, noun));
        }
        #endregion
    }
}