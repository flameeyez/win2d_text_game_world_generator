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
        public int Width { get; set; }
        public int Height { get; set; }

        public List<Region> Regions = new List<Region>();
        public List<Cave> Caves = new List<Cave>();

        public CanvasRenderTarget RenderTargetRegions { get; set; }
        public CanvasRenderTarget RenderTargetSubregions { get; set; }
        public CanvasRenderTarget RenderTargetPaths { get; set; }
        public CanvasRenderTarget RenderTargetHeightMap { get; set; }
        public CanvasRenderTarget RenderTargetCaves { get; set; }

        #region Initialization
        private World() { }
        public static World Create(CanvasDevice device, int widthInTiles, int heightInTiles, IProgress<Tuple<string, float>> progress)
        {
            World world = new World();

            ProtoWorld pw = new ProtoWorld(device, widthInTiles, heightInTiles, progress);
            while (pw.Aborted) { pw = new ProtoWorld(device, widthInTiles, heightInTiles, progress); }

            world.Width = pw.Width;
            world.Height = pw.Height;
            foreach (ProtoRegion pr in pw.ProtoRegions)
            {
                world.Regions.Add(Region.FromProtoRegion(pr));
            }

            foreach(ProtoCave pc in pw.ProtoCaves)
            {
                world.Caves.Add(Cave.FromProtoCave(pc));
            }

            world.RenderTargetRegions = pw.RenderTargetRegions;
            world.RenderTargetSubregions = pw.RenderTargetSubregions;
            world.RenderTargetPaths = pw.RenderTargetPaths;
            world.RenderTargetHeightMap = pw.RenderTargetHeightMap;
            world.RenderTargetCaves = pw.RenderTargetCaves;

            return world;
        }
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

#region Old Code
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
//private void DrawRegions(CanvasAnimatedDrawEventArgs args, bool bDrawSubregions, bool bDrawPaths, bool bDrawGrid)
//{
//    foreach (Region region in Regions)
//    {
//        region.DrawRegion(Position, args, bDrawSubregions, bDrawPaths, bDrawGrid);
//        region.DrawHeightMap(Position, args, bDrawPaths);
//    }
//}
//private void DrawConnections(CanvasAnimatedDrawEventArgs args)
//{
//    args.DrawingSession.FillRectangle(new Rect(Position.X, Position.Y, Width, Height), Colors.Black);

//    foreach (Region region in Regions)
//    {
//        region.DrawRoomConnections(Position, args);
//    }
//}
//private void DrawHeightMap(CanvasAnimatedDrawEventArgs args, bool bDrawPaths)
//{
//    foreach (Region region in Regions)
//    {
//        region.DrawHeightMap(Position, args, bDrawPaths);
//    }
//}
//private void DrawTilesNotInMainPath(CanvasAnimatedDrawEventArgs args)
//{
//    if (TilesNotInMainPath.Count > 0)
//    {
//        foreach (PointInt pi in TilesNotInMainPath)
//        {
//            args.DrawingSession.FillRectangle(
//                new Rect(Position.X + Statics.Padding + pi.X * Statics.PixelScale,
//                         Position.Y + Statics.Padding + pi.Y * Statics.PixelScale,
//                         Statics.PixelScale,
//                         Statics.PixelScale),
//                         Colors.Red);
//        }
//    }
//}
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