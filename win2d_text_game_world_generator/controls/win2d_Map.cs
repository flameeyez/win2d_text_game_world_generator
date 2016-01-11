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
        private List<win2d_Control> Controls = new List<win2d_Control>();

        public win2d_Map(Vector2 position, int width, int height, Map world) : base(position, width, height)
        {
            World = world;
        }

        public Map World { get; set; }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // border
            // args.DrawingSession.DrawRectangle(Rect, Colors.White);

            // draw scoped world
        }
    }
}
