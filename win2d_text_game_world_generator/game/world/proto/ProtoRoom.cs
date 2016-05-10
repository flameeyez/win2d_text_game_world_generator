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

        public ProtoRoom(PointInt coordinates, int elevation = 3)
        {
            ProtoRegion = null;
            ProtoSubregion = null;
            Coordinates = coordinates;
            DirectionalRoomConnections = new List<string>();
            Available = true;
            Elevation = elevation; // default (3) is grass/green
        }

        public bool IsTraversable() { return !(Elevation == 0 || Elevation == 30); }

        #region Hashing
        public override bool Equals(object obj)
        {
            ProtoRoom compare = obj as ProtoRoom;
            return this.Coordinates.Equals(compare.Coordinates);
        }
        public override int GetHashCode()
        {
            return Coordinates.GetHashCode();
        }
        #endregion       

        #region Drawing
        public void DrawHeightMap(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(Coordinates.X * Statics.MapResolution, Coordinates.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), ElevationColor);
        }
        public void DrawPaths(CanvasDrawingSession ds)
        {
            foreach (string DirectionalRoomConnection in DirectionalRoomConnections)
            {
                switch (DirectionalRoomConnection)
                {
                    case "nw":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             ((Coordinates.X - 1) + 0.5f) * Statics.MapResolution,
                             ((Coordinates.Y - 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "n":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             (Coordinates.X + 0.5f) * Statics.MapResolution,
                             ((Coordinates.Y - 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "ne":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             ((Coordinates.X + 1) + 0.5f) * Statics.MapResolution,
                             ((Coordinates.Y - 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "w":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             ((Coordinates.X - 1) + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "o":
                        break;
                    case "e":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             ((Coordinates.X + 1) + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "sw":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             ((Coordinates.X - 1) + 0.5f) * Statics.MapResolution,
                             ((Coordinates.Y + 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "s":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             (Coordinates.X + 0.5f) * Statics.MapResolution,
                             ((Coordinates.Y + 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    case "se":
                        ds.DrawLine((Coordinates.X + 0.5f) * Statics.MapResolution,
                             (Coordinates.Y + 0.5f) * Statics.MapResolution,
                             ((Coordinates.X + 1) + 0.5f) * Statics.MapResolution,
                             ((Coordinates.Y + 1) + 0.5f) * Statics.MapResolution,
                             Colors.White);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }
        public void DrawRegions(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(Coordinates.X * Statics.MapResolution, Coordinates.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), ProtoRegion.Color);
        }
        public void DrawSubregions(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(Coordinates.X * Statics.MapResolution, Coordinates.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), ProtoSubregion.Color);
        }
        public void DrawCaves(CanvasDrawingSession ds)
        {
            ds.FillRectangle(new Rect(Coordinates.X * Statics.MapResolution, Coordinates.Y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), Colors.Gray);
        }
        #endregion
    }
}