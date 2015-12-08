using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace win2d_text_game_world_generator
{
    class win2d_TextblockString
    {
        public CanvasTextLayout Text { get; set; }
        public int Width { get { return (int)Text.LayoutBounds.Width; } }
        public int Height { get { return (int)Text.LayoutBounds.Height; } }

        public win2d_TextblockString(CanvasDevice device, string text, int requestedwidth)
        {
            Text = new CanvasTextLayout(device, text, Statics.DefaultFont, requestedwidth, 0);
        }
    }
}
