using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public class win2d_Textblock : win2d_Control
    {
        private static int PaddingX = 10;
        private static int PaddingY = 10;
        private object _stringsObject = new object();
        private CanvasDevice _device;

        private win2d_TextblockStringCollection Strings;
        public int DebugStringsCount
        {
            get
            {
                return Strings.Count;
            }
        }

        public win2d_Textblock(CanvasDevice device, Vector2 position, int width, int height, bool scrolltobottomonappend = false) : base(position, width, height)
        {
            _device = device;
            Strings = new win2d_TextblockStringCollection(_device, new Vector2(Position.X + PaddingX, Position.Y + PaddingY), 
                                                            Width - PaddingY * 2, 
                                                            Height - PaddingY * 2, 
                                                            scrolltobottomonappend);
        }

        #region Drawing
        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawBorder(args);
            DrawStrings(args);
        }

        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            if (Rect != null)
            {
                args.DrawingSession.DrawRectangle(Rect, Colors.White);
            }
        }

        private void DrawStrings(CanvasAnimatedDrawEventArgs args)
        {
            lock (_stringsObject)
            {
                Strings.Draw(args);
            }
        }
        #endregion

        #region Append
        public void Append(string str)
        {
            lock (_stringsObject)
            {
                Strings.Add(str);
            }
        }
        #endregion

        #region Scrolling
        public void ScrollUp() { Strings.ScrollUp(); }
        public void ScrollDown() { Strings.ScrollDown(); }
        public void ScrollToTop() { Strings.ScrollToTop(); }
        public void ScrollToBottom() { Strings.ScrollToBottom(); }
        #endregion

        public override void RecalculateLayout()
        {
            base.RecalculateLayout();
            Strings.Position = new Vector2(Position.X + PaddingX, Position.Y + PaddingY);
        }
    }
}
