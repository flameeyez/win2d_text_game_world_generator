using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public class win2d_Panel : win2d_Control
    {
        private List<win2d_Control> Controls = new List<win2d_Control>();

        public win2d_Panel(Vector2 position, int width, int height, Color backgroundColor) : base(position, width, height)
        {
            Color = backgroundColor;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(Rect, Color);

            foreach (win2d_Control control in Controls)
            {
                control.Draw(args);
            }

            // border
            args.DrawingSession.DrawRectangle(Rect, Colors.White);
        }

        public void AddControl(win2d_Control control)
        {
            // convert relative position to absolute
            control.Position = new Vector2(control.Position.X + Position.X, control.Position.Y + Position.Y);
            control.RecalculateLayout();
            Controls.Add(control);
        }

        public override void MouseDown(PointerPoint p)
        {
            foreach(win2d_Control control in Controls)
            {
                if(control.HitTest(p.Position))
                {
                    control.MouseDown(p);
                }
            }
        }

        public override void MouseUp(PointerPoint p)
        {
            foreach (win2d_Control control in Controls)
            {
                if (control.HitTest(p.Position))
                {
                    control.MouseUp(p);
                }
            }
        }
    }
}
