using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public enum MapDrawType
    {
        HEIGHTMAP,
        REGIONS
    }

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
        public HashSet<PointInt> TilesNotInMainPath { get; set; }
        public HashSet<PointInt> MainPath { get; set; }
        public int DebugFixConnectionsTime { get; set; }
        public int DebugFixConnectionsCount { get; set; }
        public int DebugCreateRoomConnectionsTime { get; set; }
        public int DebugCreateRoomConnectionsCount { get; set; }
        public static bool DebugDrawDebug = true;
        public static bool DebugDrawPaths = false;
        public static bool DebugDrawSubregions = false;
        public static bool DebugDrawGrid = false;
        public static MapDrawType DebugMapDrawType = MapDrawType.REGIONS;
        #endregion

        #region Initialization
        private Map() { }
        public static Map Create(int width, int height, IProgress<Tuple<string, float>> progress)
        {
            // START DEBUG
            Stopwatch s = Stopwatch.StartNew();
            // END DEBUG

            // declared here for abort tracking
            Map map = new Map();

            ProtoMap pm = new ProtoMap(width, height, progress);
            while (pm.Aborted) { ++map.DebugAbortedCount; pm = new ProtoMap(width, height, progress); }
            
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
            map.TilesNotInMainPath = pm.TilesNotInMainPath;
            map.MainPath = pm.MainPath;
            map.DebugFixConnectionsTime = pm.DebugFixConnectionsTime;
            map.DebugFixConnectionsCount = pm.DebugFixConnectionsCount;
            map.DebugCreateRoomConnectionsTime = pm.DebugCreateRoomConnectionsTime;
            map.DebugCreateRoomConnectionsCount = pm.DebugCreateRoomConnectionsCount;
            // END DEBUG

            return map;
        }
        #endregion

        #region Draw
        public void Draw(CanvasAnimatedDrawEventArgs args)
        {
            switch (DebugMapDrawType)
            {
                case MapDrawType.REGIONS:
                    DrawRegions(args, DebugDrawSubregions, DebugDrawPaths, DebugDrawGrid);
                    break;
                case MapDrawType.HEIGHTMAP:
                    DrawHeightMap(args, DebugDrawPaths);
                    DrawTilesNotInMainPath(args);
                    break;
            }
        }
        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(new Rect(Position.X + Statics.Padding, Position.Y + Statics.Padding, WidthInPixels, HeightInPixels), Colors.White);
        }
        private void DrawRegions(CanvasAnimatedDrawEventArgs args, bool bDrawSubregions, bool bDrawPaths, bool bDrawGrid)
        {
            DrawBorder(args);
            foreach (Region region in Regions)
            {
                region.DrawRegion(Position, args, bDrawSubregions, bDrawPaths, bDrawGrid);
                region.DrawHeightMap(Position, args, bDrawPaths);
            }
        }
        private void DrawConnections(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(Position.X, Position.Y, WidthInPixels, HeightInPixels), Colors.Black);

            DrawBorder(args);
            foreach (Region region in Regions)
            {
                region.DrawRoomConnections(Position, args);
            }
        }
        private void DrawHeightMap(CanvasAnimatedDrawEventArgs args, bool bDrawPaths)
        {
            DrawBorder(args);
            foreach (Region region in Regions)
            {
                region.DrawHeightMap(Position, args, bDrawPaths);
            }
        }
        private void DrawTilesNotInMainPath(CanvasAnimatedDrawEventArgs args)
        {
            if (TilesNotInMainPath.Count > 0)
            {
                foreach (PointInt pi in TilesNotInMainPath)
                {
                    args.DrawingSession.FillRectangle(
                        new Rect(Position.X + Statics.Padding + pi.X * Statics.PixelScale,
                                 Position.Y + Statics.Padding + pi.Y * Statics.PixelScale,
                                 Statics.PixelScale,
                                 Statics.PixelScale),
                                 Colors.Red);
                }
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

        #region Keyboard
        public void KeyDown(VirtualKey vk)
        {
            switch (vk)
            {
                case Windows.System.VirtualKey.H:
                    DebugMapDrawType = MapDrawType.HEIGHTMAP;
                    Statics.DebugHeightMapOpacity = 255;
                    break;
                case Windows.System.VirtualKey.R:
                    DebugMapDrawType = MapDrawType.REGIONS;
                    Statics.DebugHeightMapOpacity = 75;
                    break;
                case Windows.System.VirtualKey.P:
                    DebugDrawPaths = !DebugDrawPaths;
                    break;
                case Windows.System.VirtualKey.S:
                    DebugDrawSubregions = !DebugDrawSubregions;
                    break;
                case Windows.System.VirtualKey.D:
                    DebugDrawDebug = !DebugDrawDebug;
                    break;
                case Windows.System.VirtualKey.G:
                    DebugDrawGrid = !DebugDrawGrid;
                    break;
            }
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
