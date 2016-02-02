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
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;

namespace win2d_text_game_world_generator
{
    public delegate void ScrollEventHandler();

    public class win2d_ScrollBar : win2d_Control
    {
        private Rect ScrollToTopRect { get; set; }
        private Rect ScrollUpRect { get; set; }
        private Rect MiddleRect { get; set; }
        private Rect ScrollDownRect { get; set; }
        private Rect ScrollToBottomRect { get; set; }

        private Vector2 UpArrowPosition { get; set; }
        private Vector2 DoubleUpArrowPosition { get; set; }
        private Vector2 DownArrowPosition { get; set; }
        private Vector2 DoubleDownArrowPosition { get; set; }

        public event ScrollEventHandler ScrollToTop;
        public event ScrollEventHandler ScrollUp;
        public event ScrollEventHandler ScrollDown;
        public event ScrollEventHandler ScrollToBottom;

        public win2d_ScrollBar(Vector2 position, int width, int height) : base(position, width, height)
        {
            Position = position;
            Width = width;
            Height = height;

            ScrollToTopRect = new Rect(Position.X, Position.Y, Width, Width);
            ScrollUpRect = new Rect(Position.X, Position.Y + Width, Width, Width);
            MiddleRect = new Rect(Position.X, Position.Y + Width * 2, Width, Height - Width * 4);
            ScrollDownRect = new Rect(Position.X, Position.Y + Height - Width * 2, Width, Width);
            ScrollToBottomRect = new Rect(Position.X, Position.Y + Height - Width, Width, Width);

            UpArrowPosition = new Vector2((float)(ScrollUpRect.X + (ScrollUpRect.Width - Statics.UpArrow.LayoutBounds.Width) / 2),
                                          (float)(ScrollUpRect.Y + (ScrollUpRect.Height - Statics.UpArrow.LayoutBounds.Height) / 2));
            DoubleUpArrowPosition = new Vector2((float)(ScrollToTopRect.X + (ScrollToTopRect.Width - Statics.DoubleUpArrow.LayoutBounds.Width) / 2),
                                                (float)(ScrollToTopRect.Y + (ScrollToTopRect.Height - Statics.DoubleUpArrow.LayoutBounds.Height) / 2));
            DownArrowPosition = new Vector2((float)(ScrollDownRect.X + (ScrollDownRect.Width - Statics.DownArrow.LayoutBounds.Width) / 2),
                                            (float)(ScrollDownRect.Y + (ScrollDownRect.Height - Statics.DownArrow.LayoutBounds.Height) / 2));
            DoubleDownArrowPosition = new Vector2((float)(ScrollToBottomRect.X + (ScrollToBottomRect.Width - Statics.DoubleDownArrow.LayoutBounds.Width) / 2),
                                                  (float)(ScrollToBottomRect.Y + (ScrollToBottomRect.Height - Statics.DoubleDownArrow.LayoutBounds.Height) / 2));

            Click += Win2d_ScrollBar_Click;
        }

        private void Win2d_ScrollBar_Click(PointerPoint point)
        {
            if (Statics.HitTestRect(ScrollToTopRect, point.Position))
            {
                if (ScrollToTop != null) { ScrollToTop(); }
            }
            else if (Statics.HitTestRect(ScrollUpRect, point.Position))
            {
                if (ScrollUp != null) { ScrollUp(); }
            }
            if (Statics.HitTestRect(ScrollDownRect, point.Position))
            {
                if (ScrollDown != null) { ScrollDown(); }
            }
            if (Statics.HitTestRect(ScrollToBottomRect, point.Position))
            {
                if (ScrollToBottom != null) { ScrollToBottom(); }
            }
        }

        #region Draw
        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            if (ScrollToTopRect != null) { args.DrawingSession.FillRectangle(ScrollToTopRect, Colors.LightGray); }
            if (ScrollUpRect != null) { args.DrawingSession.FillRectangle(ScrollUpRect, Colors.Gray); }
            if (MiddleRect != null) { args.DrawingSession.FillRectangle(MiddleRect, Colors.DarkGray); }
            if (ScrollDownRect != null) { args.DrawingSession.FillRectangle(ScrollDownRect, Colors.Gray); }
            if (ScrollToBottomRect != null) { args.DrawingSession.FillRectangle(ScrollToBottomRect, Colors.LightGray); }
            if (Rect != null) { args.DrawingSession.DrawRectangle(Rect, Colors.White); }

            args.DrawingSession.DrawTextLayout(Statics.UpArrow, UpArrowPosition, Colors.Black);
            args.DrawingSession.DrawTextLayout(Statics.DoubleUpArrow, DoubleUpArrowPosition, Colors.Black);
            args.DrawingSession.DrawTextLayout(Statics.DownArrow, DownArrowPosition, Colors.Black);
            args.DrawingSession.DrawTextLayout(Statics.DoubleDownArrow, DoubleDownArrowPosition, Colors.Black);
        }
        #endregion
    }
}
