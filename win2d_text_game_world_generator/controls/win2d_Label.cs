using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class win2d_Label : win2d_Control
    {
        private CanvasTextLayout _text;
        public CanvasTextLayout Text { get { return _text; } }

        public win2d_Label(Vector2 position, int width, int height, CanvasDevice device, string text) : base(position, width, height)
        {
            _text = new CanvasTextLayout(device, text, Statics.DefaultFontNoWrap, width, height);
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawTextLayout(Text, Position, Colors.White);
        }
    }
}
