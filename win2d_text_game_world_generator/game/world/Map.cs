using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class Map
    {
        #region Layout
        public Vector2 Position { get; set; }
        public int WidthInPixels { get; set; }
        public int HeightInPixels { get; set; }

        public int WidthInTiles { get { return WidthInPixels / Statics.PixelScale; } }
        public int HeightInTiles { get { return HeightInPixels / Statics.PixelScale; } }
        #endregion

        public List<Region> Regions = new List<Region>();

        private Map() { }
        public static Map Create(int width, int height)
        {
            ProtoMap pm = new ProtoMap(width, height);

            Map map = new Map();
            map.Position = pm.Position;
            map.WidthInPixels = pm.WidthInPixels;
            map.HeightInPixels = pm.HeightInPixels;
            foreach (ProtoRegion pr in pm.ProtoRegions)
            {
                map.Regions.Add(Region.FromProtoRegion(pr));
            }
            return map;
        }

        public void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawBorder(args);
            DrawMap(args);
        }
        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(new Rect(Position.X + Statics.Padding, Position.Y + Statics.Padding, WidthInPixels, HeightInPixels), Colors.White);
        }
        private void DrawMap(CanvasAnimatedDrawEventArgs args)
        {
            foreach (Region region in Regions)
            {
                region.DrawTiles(Position, args);
            }

            foreach (Region region in Regions)
            {
                region.DrawRoomConnections(Position, args);
            }
        }

        public void Update(CanvasAnimatedUpdateEventArgs args)
        {

        }

        public Region GetRegion(int x, int y)
        {
            foreach(Region r in Regions)
            {
                foreach(Subregion s in r.Subregions)
                {
                    foreach(Room rm in s.Rooms)
                    {
                        if(rm.Coordinates.X == x && rm.Coordinates.Y == y) { return r; }
                    }
                }
            }

            return null;
        }

        public Subregion GetSubregion(Region region, int x, int y)
        {
            foreach(Subregion s in region.Subregions)
            {
                foreach(Room rm in s.Rooms)
                {
                    if (rm.Coordinates.X == x && rm.Coordinates.Y == y) { return s; }
                }
            }

            return null;
        }
    }
}
