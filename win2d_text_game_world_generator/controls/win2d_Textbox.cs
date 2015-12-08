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
using Windows.System;
using System.Runtime.InteropServices;

namespace win2d_text_game_world_generator
{
    class win2d_Textbox : win2d_Control
    {
        private static int PaddingX = 10;
        private static int PaddingY = 10;

        private string _text;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    bRecalculateLayout = true;
                    if (_text == string.Empty)
                    {
                        CursorStringIndex = 0;
                    }
                }
            }
        }

        private CanvasTextLayout TextBeforeCursor;
        private CanvasTextLayout TextAfterCursor;

        // public int MaxTextLength { get; set; }

        private win2d_TextboxCursor Cursor;

        private int _cursorstringindex;
        private int CursorStringIndex
        {
            get { return _cursorstringindex; }
            set { _cursorstringindex = Math.Max(value, 0); }
        }
        private bool bRecalculateLayout;
        private Rect Border;
        private Vector2 TextPosition;

        private HashSet<VirtualKey> KeyboardState = new HashSet<VirtualKey>();

        public win2d_Textbox(CanvasDevice device, Vector2 position, int width) : base(position, width, -1)
        {
            CanvasTextLayout layout = new CanvasTextLayout(device, "TEST!", Statics.DefaultFontNoWrap, 0, 0);

            Position = position;
            Width = width;
            Height = (int)layout.LayoutBounds.Height + PaddingY * 2;
            Color = Colors.White;

            TextPosition = new Vector2(position.X + PaddingX, position.Y + PaddingY);
            Border = new Rect(position.X, position.Y, width, Height);

            Cursor = new win2d_TextboxCursor(device, Colors.White);
            CursorStringIndex = 0;
            bRecalculateLayout = true;
        }

        #region Draw / Update
        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawBorder(args);
            DrawText(args);

            if (HasFocus)
            {
                DrawCursor(args);
            }
        }
        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(Border, Color);
        }
        private void DrawText(CanvasAnimatedDrawEventArgs args)
        {
            if (TextBeforeCursor != null) { args.DrawingSession.DrawTextLayout(TextBeforeCursor, TextPosition, Colors.White); }
            if (TextAfterCursor != null) { args.DrawingSession.DrawTextLayout(TextAfterCursor, new Vector2(TextPosition.X + (float)TextBeforeCursor.LayoutBounds.Width + 3, TextPosition.Y), Colors.White); }
        }
        private void DrawCursor(CanvasAnimatedDrawEventArgs args)
        {
            if (bRecalculateLayout) { RecalculateLayout(args.DrawingSession); }
            Cursor.Draw(args);
        }
        public override void Update(CanvasAnimatedUpdateEventArgs args)
        {
            if (HasFocus) { Cursor.Update(args); }
        }
        #endregion

        #region Event Handling
        public override bool KeyDown(VirtualKey virtualKey)
        {
            switch (virtualKey)
            {
                case VirtualKey.Enter:
                    return true;
                case VirtualKey.Back:
                    if (Text != null && Text.Length > 0 && CursorStringIndex > 0)
                    {
                        StringBuilder newText = new StringBuilder(Text.Substring(0, CursorStringIndex - 1));
                        newText.Append(Text.Substring(CursorStringIndex));
                        Text = newText.ToString();
                        --CursorStringIndex;
                    }
                    break;
                case VirtualKey.Delete:
                    if (Text != null && Text.Length > 0 && CursorStringIndex < Text.Length)
                    {
                        StringBuilder newText = new StringBuilder(Text.Substring(0, CursorStringIndex));
                        newText.Append(Text.Substring(CursorStringIndex + 1));
                        Text = newText.ToString();
                    }
                    break;
                case VirtualKey.Home:
                    if (Text != null && Text.Length > 0)
                    {
                        CursorStringIndex = 0;
                        bRecalculateLayout = true;
                    }
                    break;
                case VirtualKey.End:
                    if (Text != null && Text.Length > 0)
                    {
                        CursorStringIndex = Text.Length;
                        bRecalculateLayout = true;
                    }
                    break;
                case VirtualKey.Left:
                    if (Text != null && CursorStringIndex > 0)
                    {
                        --CursorStringIndex;
                        bRecalculateLayout = true;
                    }
                    break;
                case VirtualKey.Right:
                    if (Text != null && CursorStringIndex < Text.Length)
                    {
                        ++CursorStringIndex;
                        bRecalculateLayout = true;
                    }
                    break;
                default:
                    // KeyboardState currently only used for shift; could optimize
                    KeyboardState.Add(virtualKey);

                    string s = Statics.VirtualKeyToString(virtualKey, KeyboardState.Contains(VirtualKey.Shift));
                    if (s.Length > 1) { throw new Exception(); }
                    if (s != string.Empty)
                    {
                        StringBuilder sb = new StringBuilder();
                        if (Text != null)
                        {
                            sb.Append(Text.Substring(0, CursorStringIndex));
                            sb.Append(s);
                            sb.Append(Text.Substring(CursorStringIndex));
                            Text = sb.ToString();
                        }
                        else
                        {
                            Text = s;
                        }

                        ++CursorStringIndex;
                        bRecalculateLayout = true;
                    }
                    break;
            }

            return true;
        }
        public override bool KeyUp(VirtualKey virtualKey)
        {
            KeyboardState.Remove(virtualKey);
            return true;
        }
        public override void LoseFocus()
        {
            base.LoseFocus();
            Cursor.Reset();
        }
        #endregion

        #region Layout
        private void RecalculateLayout(ICanvasResourceCreator resourceCreator)
        {
            bRecalculateLayout = false;

            CalculateCursorPosition(resourceCreator);
            CreateTextLayoutBeforeCursor(resourceCreator);
            CreateTextLayoutAfterCursor(resourceCreator);
        }

        private void CreateTextLayoutBeforeCursor(ICanvasResourceCreator resourceCreator)
        {
            if (Text != null)
            {
                TextBeforeCursor = new CanvasTextLayout(resourceCreator, Text.Substring(0, CursorStringIndex), Statics.DefaultFontNoWrap, 0, 0);
            }
        }

        private void CreateTextLayoutAfterCursor(ICanvasResourceCreator resourceCreator)
        {
            if (Text != null && Text.Length >= CursorStringIndex)
            {
                TextAfterCursor = new CanvasTextLayout(resourceCreator, Text.Substring(CursorStringIndex), Statics.DefaultFontNoWrap, 0, 0);
            }
            else
            {
                TextAfterCursor = null;
            }
        }

        private void CalculateCursorPosition(ICanvasResourceCreator resourceCreator)
        {
            if (Text == null || Text.Length == 0 || CursorStringIndex == 0)
            {
                Cursor.Position = new Vector2(Position.X + PaddingX, Position.Y + PaddingY);
            }
            else if (CursorStringIndex == -1)
            {
                CanvasTextLayout layout = new CanvasTextLayout(resourceCreator, Text.Replace(' ', '.'), Statics.DefaultFontNoWrap, 0, 0);
                Cursor.Position = new Vector2(Position.X + (float)layout.LayoutBounds.Width + PaddingX, Position.Y + PaddingY);
            }
            else
            {
                CanvasTextLayout layout = new CanvasTextLayout(resourceCreator, Text.Substring(0, CursorStringIndex), Statics.DefaultFontNoWrap, 0, 0);
                Cursor.Position = new Vector2(Position.X + (float)layout.LayoutBounds.Width + PaddingX, Position.Y + PaddingY);
            }
        }
        #endregion        
    }
}
