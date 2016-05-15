using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
using Windows.Foundation;
using Microsoft.Graphics.Canvas;
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public delegate void CheckedValueChangedEventHandler(bool bChecked);

    public class win2d_Checkbox : win2d_Control
    {
        private Rect _boxrect;
        private CanvasTextLayout _textlayout;
        private Vector2 _textposition;
        private static int _boxsidelength = 10;

        public bool Checked { get; set; }
        public event CheckedValueChangedEventHandler CheckedValueChanged;

        public win2d_Checkbox(CanvasDevice device, Vector2 position, string text) : base(position, -1, -1)
        {
            _textlayout = new CanvasTextLayout(device, text, Statics.DefaultFontNoWrap, 0, 0);
            RecalculateLayout();
        }

        #region Drawing
        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawTextLayout(args);
            DrawBox(args);
            if (Checked) { DrawX(args); }
        }
        private void DrawTextLayout(CanvasAnimatedDrawEventArgs args) { args.DrawingSession.DrawTextLayout(_textlayout, _textposition, Colors.White); }
        private void DrawBox(CanvasAnimatedDrawEventArgs args) { args.DrawingSession.DrawRectangle(_boxrect, Colors.White); }
        private void DrawBorder(CanvasAnimatedDrawEventArgs args) { args.DrawingSession.DrawRectangle(Rect, Colors.White); }
        private void DrawX(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawLine((float)_boxrect.Left, (float)_boxrect.Top, (float)_boxrect.Right, (float)_boxrect.Bottom, Colors.White);
            args.DrawingSession.DrawLine((float)_boxrect.Left, (float)_boxrect.Bottom, (float)_boxrect.Right, (float)_boxrect.Top, Colors.White);
        }
        #endregion

        protected void OnCheckedValueChanged() { if (CheckedValueChanged != null) { CheckedValueChanged(Checked); } }
        public override void RecalculateLayout()
        {
            _boxrect = new Rect(Position.X, Position.Y + (Height - _boxsidelength) / 2, _boxsidelength, _boxsidelength);
            _textposition = new Vector2((float)_boxrect.Right + Statics.Padding / 2, Position.Y);

            Width = (int)(_boxsidelength + Statics.Padding / 2 + _textlayout.LayoutBounds.Width);
            Height = (int)_textlayout.LayoutBounds.Height;

            base.RecalculateLayout();
        }

        public override void MouseUp(PointerPoint p)
        {
            Checked = !Checked;
            OnCheckedValueChanged();
        }
    }
}
