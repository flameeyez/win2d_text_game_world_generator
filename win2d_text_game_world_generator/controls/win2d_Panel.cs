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
    public class win2d_Panel : win2d_Control
    {
        private Rect BackgroundRect;
        private Color BackgroundColor;

        public List<win2d_Control> Controls = new List<win2d_Control>();

        public win2d_Panel(Vector2 position, int width, int height, Color backgroundColor) : base(position, width, height)
        {
            BackgroundRect = new Rect(position.X, position.Y, width, height);
            BackgroundColor = backgroundColor;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // background color
            args.DrawingSession.FillRectangle(BackgroundRect, BackgroundColor);

            foreach (win2d_Control control in Controls)
            {
                control.Draw(args);
            }

            // border
            args.DrawingSession.DrawRectangle(BackgroundRect, Colors.White);
        }
    }
}
