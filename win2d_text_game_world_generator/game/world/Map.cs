using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Vector2 Position { get; set; }
        public int WidthInPixels { get; set; }
        public int HeightInPixels { get; set; }

        public int WidthInTiles { get { return WidthInPixels / Statics.PixelScale; } }
        public int HeightInTiles { get { return HeightInPixels / Statics.PixelScale; } }

        public List<Region> Regions = new List<Region>();

        #region Debug
        public TimeSpan DebugCreationTime { get; set; }
        public int DebugAbortedCount { get; set; }
        #endregion

        #region Initialization
        private Map() { }
        public static Map Create(int width, int height)
        {
            // START DEBUG
            Stopwatch s = Stopwatch.StartNew();
            // END DEBUG

            // declared here for abort tracking
            Map map = new Map();

            ProtoMap pm = new ProtoMap(width, height);
            while (pm.Aborted) { ++map.DebugAbortedCount; pm = new ProtoMap(width, height); }
            
            map.Position = pm.Position;
            map.WidthInPixels = pm.WidthInPixels;
            map.HeightInPixels = pm.HeightInPixels;
            foreach (ProtoRegion pr in pm.ProtoRegions)
            {
                map.Regions.Add(Region.FromProtoRegion(pr));
            }

            // START DEBUG
            s.Stop();
            map.DebugCreationTime = s.Elapsed;
            // END DEBUG

            return map;
        }
        #endregion

        #region Draw
        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(new Rect(Position.X + Statics.Padding, Position.Y + Statics.Padding, WidthInPixels, HeightInPixels), Colors.White);
        }
        public void DrawRegions(CanvasAnimatedDrawEventArgs args)
        {
            DrawBorder(args);
            foreach (Region region in Regions)
            {
                region.DrawRegion(Position, args);
            }
        }
        public void DrawConnections(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(Position.X, Position.Y, WidthInPixels, HeightInPixels), Colors.Black);

            DrawBorder(args);
            foreach (Region region in Regions)
            {
                region.DrawRoomConnections(Position, args);
            }
        }
        public void DrawHeightMap(CanvasAnimatedDrawEventArgs args)
        {
            DrawBorder(args);
            foreach (Region region in Regions)
            {
                region.DrawHeightMap(Position, args);
            }
        }
        //public void Draw(CanvasAnimatedDrawEventArgs args)
        //{
        //    DrawMap(args);
        //}
        //private void DrawMap(CanvasAnimatedDrawEventArgs args)
        //{
        //    foreach (Region region in Regions)
        //    {
        //        region.DrawTiles(Position, args);
        //    }

        //    foreach (Region region in Regions)
        //    {
        //        region.DrawRoomConnections(Position, args);
        //    }
        //}
        #endregion

        #region Update
        public void Update(CanvasAnimatedUpdateEventArgs args)
        {

        }
        #endregion

        #region Get Region/Subregion
        public Region GetRegion(int x, int y)
        {
            foreach (Region r in Regions)
            {
                foreach (Subregion s in r.Subregions)
                {
                    foreach (Room rm in s.Rooms)
                    {
                        if (rm.Coordinates.X == x && rm.Coordinates.Y == y) { return r; }
                    }
                }
            }

            return null;
        }

        public Room GetRoom(Subregion currentMouseSubregion, int x, int y)
        {
            foreach(Room room in currentMouseSubregion.Rooms)
            {
                if(room.Coordinates.X == x && room.Coordinates.Y == y)
                {
                    return room;
                }
            }

            return null;
        }

        public Subregion GetSubregion(Region region, int x, int y)
        {
            foreach (Subregion s in region.Subregions)
            {
                foreach (Room rm in s.Rooms)
                {
                    if (rm.Coordinates.X == x && rm.Coordinates.Y == y) { return s; }
                }
            }

            return null;
        }
        #endregion
    }
}
