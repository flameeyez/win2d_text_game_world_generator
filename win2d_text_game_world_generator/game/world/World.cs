using Microsoft.Graphics.Canvas;
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
    public class World
    {
        public Vector2 Position { get; set; }
        public int WidthInPixels { get; set; }
        public int HeightInPixels { get; set; }

        public int WidthInTiles { get { return WidthInPixels / Statics.PixelScale; } }
        public int HeightInTiles { get { return HeightInPixels / Statics.PixelScale; } }

        public List<Region> Regions = new List<Region>();
        public ProtoRoom[,] MasterRoomList;

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
        #endregion

        public CanvasRenderTarget RenderTargetRegions { get; set; }
        public CanvasRenderTarget RenderTargetSubregions { get; set; }
        public CanvasRenderTarget RenderTargetPaths { get; set; }
        public CanvasRenderTarget RenderTargetHeightMap { get; set; }

        #region Initialization
        private World() { }
        public static World Create(int width, int height, IProgress<Tuple<string, float>> progress)
        {
            // START DEBUG
            Stopwatch s = Stopwatch.StartNew();
            // END DEBUG

            // declared here for abort tracking
            World world = new World();

            ProtoWorld pw = new ProtoWorld(width, height, progress);
            while (pw.Aborted) { ++world.DebugAbortedCount; pw = new ProtoWorld(width, height, progress); }

            world.Position = pw.Position;
            world.WidthInPixels = pw.WidthInPixels;
            world.HeightInPixels = pw.HeightInPixels;
            world.MasterRoomList = pw.MasterRoomList;
            foreach (ProtoRegion pr in pw.ProtoRegions)
            {
                world.Regions.Add(Region.FromProtoRegion(pr));
            }

            world.RenderTargetRegions = pw.RenderTargetRegions;
            world.RenderTargetSubregions = pw.RenderTargetSubregions;
            world.RenderTargetPaths = pw.RenderTargetPaths;
            world.RenderTargetHeightMap = pw.RenderTargetHeightMap;

            // START DEBUG
            s.Stop();
            world.DebugCreationTime = s.Elapsed;
            world.TilesNotInMainPath = pw.TilesNotInMainPath;
            world.MainPath = pw.MainPath;
            world.DebugFixConnectionsTime = pw.DebugFixConnectionsTime;
            world.DebugFixConnectionsCount = pw.DebugFixConnectionsCount;
            world.DebugCreateRoomConnectionsTime = pw.DebugCreateRoomConnectionsTime;
            world.DebugCreateRoomConnectionsCount = pw.DebugCreateRoomConnectionsCount;
            // END DEBUG

            return world;
        }
        #endregion

        #region Draw
        //public void Draw(CanvasAnimatedDrawEventArgs args)
        //{
        //    switch (DebugMapDrawType)
        //    {
        //        case WorldDrawType.REGIONS:
        //            DrawRegions(args, DebugDrawSubregions, DebugDrawPaths, DebugDrawGrid);
        //            break;
        //        case WorldDrawType.HEIGHTMAP:
        //            DrawHeightMap(args, DebugDrawPaths);
        //            DrawTilesNotInMainPath(args);
        //            break;
        //    }
        //}
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
            foreach (Room room in currentMouseSubregion.Rooms)
            {
                if (room.Coordinates.X == x && room.Coordinates.Y == y)
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
