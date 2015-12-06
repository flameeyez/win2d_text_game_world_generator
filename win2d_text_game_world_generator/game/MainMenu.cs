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
using Windows.System;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public static class MainMenu
    {
        private static Rect BackgroundRect;

        private static List<MenuItem> MenuItems = new List<MenuItem>();
        private static Rect MenuItemsBorderRect;
        private static Vector2 MenuItemsPosition;

        private static int _selectedindex;
        private static MenuItem _selecteditem;
        public static MenuItem SelectedItem { get { return _selecteditem; } }

        private static string Title = "All kinds of stuff and things!";
        private static CanvasTextLayout TitleLayout;
        private static Vector2 TitlePosition;

        private static Color BackgroundColor = Colors.CornflowerBlue;

        #region Initialization
        public static void Initialize(CanvasDevice device)
        {
            BackgroundRect = new Rect(0, 0, Statics.CanvasWidth, Statics.CanvasHeight);

            TitleLayout = new CanvasTextLayout(device, Title, Statics.FontLarge, 0, 0);
            TitlePosition = new Vector2(
                (float)(Statics.CanvasWidth - TitleLayout.LayoutBounds.Width) / 2,
                (float)((Statics.CanvasHeight / 4) - (TitleLayout.LayoutBounds.Height / 2)));

            int nRectWidth = 200;
            int nRectHeight = 200;
            int nPadding = 10;
            int nX = (Statics.CanvasWidth - nRectWidth) / 2;
            int nY = Statics.CanvasHeight * 3 / 4 - nRectHeight / 2;
            MenuItemsBorderRect = new Rect(nX, nY, nRectWidth, nRectHeight);
            MenuItemsPosition = new Vector2(nX + nPadding, nY + nPadding);
        }
        public static void Reset()
        {
            if (MenuItems.Count > 0)
            {
                _selectedindex = 0;
                _selecteditem = MenuItems[_selectedindex];
            }
        }
        #endregion

        #region Draw
        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawBackground(args);
            DrawTitle(args);
            DrawItemsBorder(args);
            DrawItems(args);
        }
        private static void DrawBackground(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(BackgroundRect, BackgroundColor);
        }
        private static void DrawItemsBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRoundedRectangle(MenuItemsBorderRect, 5, 5, Colors.White, 3);
        }
        private static void DrawItems(CanvasAnimatedDrawEventArgs args)
        {
            float fCurrentX = MenuItemsPosition.X;
            float fCurrentY = MenuItemsPosition.Y;

            for (int i = 0; i < MenuItems.Count; i++)
            {
                Vector2 position = new Vector2(fCurrentX, fCurrentY);

                if (i == _selectedindex)
                {
                    MenuItems[i].DrawSelected(args, position);
                }
                else
                {
                    MenuItems[i].Draw(args, position);
                }

                fCurrentY += 20;
            }
        }
        private static void DrawTitle(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawTextLayout(TitleLayout, TitlePosition, Colors.White);
        }
        #endregion

        #region Update
        public static void Update(CanvasAnimatedUpdateEventArgs args)
        {

        }
        #endregion

        #region Event Handling
        private static void ScrollDown()
        {
            if (MenuItems.Count == 0) { return; }

            _selectedindex = (_selectedindex + 1) % MenuItems.Count;
            _selecteditem = MenuItems[_selectedindex];
        }
        private static void ScrollUp()
        {
            if (MenuItems.Count == 0) { return; }

            _selectedindex = _selectedindex - 1;
            if (_selectedindex < 0) { _selectedindex += MenuItems.Count; }
            _selecteditem = MenuItems[_selectedindex];
        }
        public static void KeyDown(VirtualKey vk)
        {
            switch (vk)
            {
                case VirtualKey.Down:
                    ScrollDown();
                    break;
                case VirtualKey.Up:
                    ScrollUp();
                    break;
                default:
                    if (SelectedItem != null) { SelectedItem.KeyDown(vk); }
                    break;
            }
        }
        #endregion

        #region Menu Item Handling
        public static void AddMenuItem(MenuItem m)
        {
            MenuItems.Add(m);
            if (MenuItems.Count == 1)
            {
                _selecteditem = m;
                _selectedindex = 0;
            }
        }
        #endregion
    }
}
