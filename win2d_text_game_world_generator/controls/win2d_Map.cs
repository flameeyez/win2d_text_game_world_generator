using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class win2d_Map : win2d_Control
    {
        public bool DebugDrawFullScreen = true;

        public World World { get; set; }

        public int Scale { get; set; } // tile side length, in pixels
        public int WidthInTiles { get { return Width / Scale; } }
        public int HeightInTiles { get { return Height / Scale; } }

        public win2d_Map(Vector2 position, int width, int height, World world, int scale) : base(position, width, height)
        {
            World = world;
            Scale = scale;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // draw scoped world
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    // 
                    // args.DrawingSession.FillRectangle();
                }
            }

            // draw border
            args.DrawingSession.DrawRectangle(Rect, Colors.White);
        }
    }
}
