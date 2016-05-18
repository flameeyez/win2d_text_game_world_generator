using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using System.Numerics;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
using Windows.Foundation;

namespace win2d_text_game_world_generator
{
    public class win2d_Slider : win2d_Control
    {
        private CanvasDevice _device;

        private int _minValue;
        private int _maxValue;
        private int _currentValue;
        public int CurrentValue
        {
            get { return _currentValue; }
            set { _currentValue = Math.Min(Math.Max(value, _minValue), _maxValue); }
        }

        private CanvasTextLayout _textlayout;
        private Rect _sliderbarrect;
        private Vector2 _slidercirclecenterpoint;

        public win2d_Slider(CanvasDevice device, Vector2 position, int width, string text, int minValue, int maxValue, int startingValue) : base(position, width, -1)
        {
            _device = device;
            _minValue = minValue;
            _maxValue = maxValue;

            _textlayout = new CanvasTextLayout(_device, text, Statics.DefaultFontNoWrap, 0, 0);

            _sliderbarrect = new Rect(Position.X, Position.Y + _textlayout.LayoutBounds.Height + Statics.Padding, Width, 10);
            Height = (int)_textlayout.LayoutBounds.Height + Statics.Padding + 10;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawSliderBar(args);
            DrawSliderCircle(args);
            DrawText(args);
        }

        private void DrawSliderBar(CanvasAnimatedDrawEventArgs args) { args.DrawingSession.FillRectangle(_sliderbarrect, Colors.White); }
        private void DrawSliderCircle(CanvasAnimatedDrawEventArgs args) { args.DrawingSession.FillCircle(_slidercirclecenterpoint, 20, Colors.White); }
        private void DrawText(CanvasAnimatedDrawEventArgs args) { args.DrawingSession.DrawTextLayout(_textlayout, Position, Colors.White); }
    }
}
