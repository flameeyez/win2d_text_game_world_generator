using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class MenuItem
    {
        private CanvasTextLayout _textlayout;
        public CanvasTextLayout TextLayout { get { return _textlayout; } }

        private Vector2 _position;
        public Vector2 Position { get { return _position; } }

        public MenuItem(CanvasDevice device, string text, Vector2 position)
        {
            _textlayout = new CanvasTextLayout(device, text, Statics.FontMedium, 0, 0);
            _position = position;
        }

        public void Draw(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawTextLayout(TextLayout, Position, Colors.White);
        }

        public void DrawSelected(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawTextLayout(TextLayout, Position, Colors.Yellow);
        }
    }
}
