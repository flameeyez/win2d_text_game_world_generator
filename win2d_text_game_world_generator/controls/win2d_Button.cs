using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public class win2d_Button : win2d_Control
    {
        private Rect ButtonRectangle { get; set; }
        private CanvasTextLayout TextLayout { get; set; }
        private Vector2 TextLayoutPosition { get; set; }

        private bool MouseCurrentlyOverControl { get; set; }

        public win2d_Button(CanvasDevice device, Vector2 position, int width, int height, string text)
            : base(position, width, height)
        {
            ButtonRectangle = new Rect(Position.X, Position.Y, Width, Height);

            TextLayout = new CanvasTextLayout(device, text, Statics.DefaultFontNoWrap, 0, 0);
            TextLayoutPosition = new Vector2(Position.X + (Width - (float)TextLayout.LayoutBounds.Width) / 2, Position.Y + (Height - (float)TextLayout.LayoutBounds.Height) / 2);

            Color = Colors.Gray;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawRectangle(args);
            DrawText(args);
        }

        private void DrawRectangle(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(ButtonRectangle, Color);
            args.DrawingSession.DrawRectangle(ButtonRectangle, Colors.White);
        }

        private void DrawText(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawTextLayout(TextLayout, TextLayoutPosition, Colors.White);
        }

        public override void Update(CanvasAnimatedUpdateEventArgs args) { }

        #region Event Handlers
        public override void MouseUp(PointerPoint p)
        {
            Color = Colors.Gray;
            base.MouseUp(p);
        }

        public override void MouseDown(PointerPoint p)
        {
            Color = Colors.LightGray;
        }

        public override void MouseEnter(PointerPoint p)
        {
            if(HasFocus && p.Properties.IsLeftButtonPressed)
            {
                Color = Colors.LightGray;
            }
        }

        public override void MouseLeave()
        {
            Color = Colors.Gray;
        }
        #endregion
    }
}
