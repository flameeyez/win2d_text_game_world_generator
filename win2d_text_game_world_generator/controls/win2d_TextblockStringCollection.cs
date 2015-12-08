using Microsoft.Graphics.Canvas;
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
    class win2d_TextblockStringCollection
    {
        // keep track of total height
        private List<win2d_TextblockString> Strings = new List<win2d_TextblockString>();
        public int Count { get { return Strings.Count; } }

        private Vector2 StringsPosition { get; set; }
        private static int StringPaddingY = 5;

        private int nFirstStringToDraw = 0;
        private int nLastStringToDraw = -1;

        private int DrawingWidth { get; set; }
        private int DrawingHeight { get; set; }

        private int _totalstringsheight = 0;
        private int TotalStringsHeight { get { return _totalstringsheight; } }

        private bool ScrollToBottomOnAppend { get; set; }

        public win2d_TextblockStringCollection(Vector2 position, int drawingwidth, int drawingheight, bool scrolltobottomonappend = false)
        {
            StringsPosition = position;
            DrawingWidth = drawingwidth;
            DrawingHeight = drawingheight;
            ScrollToBottomOnAppend = scrolltobottomonappend;
        }

        #region Drawing
        public void Draw(CanvasAnimatedDrawEventArgs args)
        {
            if (Strings.Count == 0) { return; }
            if (CanDrawAllStrings())
            {
                DrawAllStrings(args);
            }
            else if (nLastStringToDraw == -1)
            {
                int i = nFirstStringToDraw;
                float fCurrentY = StringsPosition.Y;
                while (i < Strings.Count && fCurrentY + Strings[i].Height < StringsPosition.Y + DrawingHeight)
                {
                    args.DrawingSession.DrawTextLayout(Strings[i].Text, new Vector2(StringsPosition.X, fCurrentY), Colors.White);
                    fCurrentY += Strings[i].Height + StringPaddingY;
                    i++;
                }

                nLastStringToDraw = --i;
            }
            else
            {
                float fCurrentY = StringsPosition.Y;
                for (int i = nFirstStringToDraw; i <= nLastStringToDraw; i++)
                {
                    args.DrawingSession.DrawTextLayout(Strings[i].Text, new Vector2(StringsPosition.X, fCurrentY), Colors.White);
                    fCurrentY += Strings[i].Height + StringPaddingY;
                }
            }
        }

        private void DrawAllStrings(CanvasAnimatedDrawEventArgs args)
        {
            float fCurrentY = StringsPosition.Y;

            foreach (win2d_TextblockString str in Strings)
            {
                args.DrawingSession.DrawTextLayout(str.Text, new Vector2(StringsPosition.X, fCurrentY), Colors.White);
                fCurrentY += str.Height + StringPaddingY;
            }
        }

        private bool CanDrawAllStrings()
        {
            return TotalStringsHeight <= DrawingHeight;
        }
        #endregion

        #region Add
        public void Add(CanvasDevice device, string str)
        {
            win2d_TextblockString s = new win2d_TextblockString(device, str, DrawingWidth);
            Strings.Add(s);
            _totalstringsheight += s.Height + StringPaddingY;

            if(ScrollToBottomOnAppend)
            {
                ScrollToBottom();
            }
        }
        #endregion

        #region Scrolling
        public void ScrollUp()
        {
            if (nFirstStringToDraw > 0)
            {
                nFirstStringToDraw--;
                nLastStringToDraw = -1;
            }
        }

        public void ScrollDown()
        {
            if (nFirstStringToDraw < Strings.Count - 1 && nLastStringToDraw != Strings.Count - 1 && nLastStringToDraw != -1)
            {
                nFirstStringToDraw++;
                nLastStringToDraw = -1;
            }
        }

        public void ScrollToTop()
        {
            if (Strings.Count > 0)
            {
                nFirstStringToDraw = 0;
                nLastStringToDraw = -1;
            }
        }
        public void ScrollToBottom()
        {
            if (Strings.Count == 0) { return; }
            if ((CanDrawAllStrings() && Strings.Count > 0)
                || (Strings.Count == 1))
            {
                nFirstStringToDraw = 0;
                nLastStringToDraw = 0;
                return;
            }
            else
            {
                int nHeight = Strings[Strings.Count - 1].Height;
                int i = Strings.Count - 2;
                while (nHeight + Strings[i].Height + StringPaddingY <= DrawingHeight)
                {
                    nHeight += Strings[i].Height + StringPaddingY;
                    i--;
                }

                nFirstStringToDraw = ++i;
                nLastStringToDraw = -1;
            }
        }
        #endregion
    }
}
