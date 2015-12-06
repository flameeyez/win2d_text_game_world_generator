using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public delegate void OnSelect();

    public class MenuItem
    {
        public event OnSelect Select;

        private CanvasTextLayout _textlayout;
        public CanvasTextLayout TextLayout { get { return _textlayout; } }

        public MenuItem(CanvasDevice device, string text)
        {
            _textlayout = new CanvasTextLayout(device, text, Statics.FontMedium, 0, 0);
        }

        #region Draw
        public void Draw(CanvasAnimatedDrawEventArgs args, Vector2 position)
        {
            args.DrawingSession.DrawTextLayout(TextLayout, position, Colors.White);
        }
        public void DrawSelected(CanvasAnimatedDrawEventArgs args, Vector2 position)
        {
            args.DrawingSession.DrawTextLayout(TextLayout, position, Colors.Yellow);
        }
        #endregion

        #region Keyboard
        public void KeyDown(VirtualKey vk)
        {
            switch (vk)
            {
                case VirtualKey.Enter:
                    if (Select != null) { Select(); }
                    break;
            }
        }
        #endregion
    }
}
