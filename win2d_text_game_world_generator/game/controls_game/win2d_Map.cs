using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.System;

namespace win2d_text_game_world_generator
{
    public enum MapDrawType
    {
        HEIGHTMAP,
        REGIONS,
        SUBREGIONS
    }

    public class win2d_Map : win2d_Control
    {
        public bool DebugDrawFullScreen = true;

        public World World { get; set; }

        public int Scale { get; set; } // tile side length, in pixels
        public int WidthInTiles { get { return Width / Scale; } }
        public int HeightInTiles { get { return Height / Scale; } }

        private MapDrawType DrawType = MapDrawType.REGIONS;
        private bool DrawPaths = false;

        public win2d_Map(Vector2 position, int width, int height, World world, int scale) : base(position, width, height)
        {
            World = world;
            Scale = scale;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            switch (DrawType)
            {
                case MapDrawType.REGIONS:
                    break;
                case MapDrawType.SUBREGIONS:
                    break;
                case MapDrawType.HEIGHTMAP:
                    break;
            }

            if (DrawPaths)
            {

            }

            DrawBorder(args);
        }

        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(Rect, Colors.White);
        }

        public override bool KeyDown(VirtualKey vk)
        {
            switch (vk)
            {
                case Windows.System.VirtualKey.H:
                    DrawType = MapDrawType.HEIGHTMAP;
                    Debug.HeightMapOpacity = 255;
                    break;
                case Windows.System.VirtualKey.R:
                    DrawType = MapDrawType.REGIONS;
                    Debug.HeightMapOpacity = 75;
                    break;
                case Windows.System.VirtualKey.P:
                    DrawPaths = !DrawPaths;
                    break;
                case Windows.System.VirtualKey.S:
                    DrawType = MapDrawType.SUBREGIONS;
                    break;
                    //case Windows.System.VirtualKey.D:
                    //    DebugDrawDebug = !DebugDrawDebug;
                    //    break;
                    //case Windows.System.VirtualKey.G:
                    //    DebugDrawGrid = !DebugDrawGrid;
                    //    break;
            }

            return true;
        }

        public void CycleDrawType()
        {
            if (DrawType == MapDrawType.REGIONS) { DrawType = MapDrawType.SUBREGIONS; }
            else if (DrawType == MapDrawType.SUBREGIONS) { DrawType = MapDrawType.HEIGHTMAP; }
            else if (DrawType == MapDrawType.HEIGHTMAP) { DrawType = MapDrawType.REGIONS; }
        }
    }
}
