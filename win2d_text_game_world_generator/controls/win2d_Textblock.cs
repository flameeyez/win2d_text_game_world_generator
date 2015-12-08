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

        private Rect Border { get; set; }

        private win2d_TextblockStringCollection Strings;
        public int DebugStringsCount
        {
            get
            {
                return Strings.Count;
            }
        }

        public win2d_Textblock(Vector2 position, int width, int height, bool scrolltobottomonappend = false) : base(position, width, height)
        {
            Border = new Rect(Position.X, Position.Y, Width, Height);
            Strings = new win2d_TextblockStringCollection(new Vector2(Position.X + PaddingX, Position.Y + PaddingY), 
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
            if (Border != null)
            {
                args.DrawingSession.DrawRectangle(Border, Colors.White);
            }
        }

        private void DrawStrings(CanvasAnimatedDrawEventArgs args)
        {
            lock (Strings)
            {
                Strings.Draw(args);
            }
        }
        #endregion

        #region Append
        public void Append(CanvasDevice device, string str)
        {
            lock (Strings)
            {
                Strings.Add(device, str);
            }
        }
        #endregion

        #region Scrolling
        public void ScrollUp() { Strings.ScrollUp(); }
        public void ScrollDown() { Strings.ScrollDown(); }
        public void ScrollToTop() { Strings.ScrollToTop(); }
        public void ScrollToBottom() { Strings.ScrollToBottom(); }
        #endregion
    }
}
