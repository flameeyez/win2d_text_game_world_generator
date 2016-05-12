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
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public static class ScreenMainMenu
    {
        public static event TransitionStateEventHandler TransitionState;
        private static void OnTransitionState(GAMESTATE state) { if (TransitionState != null) { TransitionState(state); } }

        private static CanvasDevice _device;

        public static Menu MainMenu = new Menu();

        private static Rect BackgroundRect;

        private static Rect MenuItemsBorderRect;
        private static Vector2 MenuItemsPosition;

        private static string Title = "All kinds of stuff and things!";
        private static CanvasTextLayout TitleLayout;
        private static Vector2 TitlePosition;

        private static float fNextMenuItemPositionY;
        private static float fMenuItemPadding = 5;

        private static Color BackgroundColor = Colors.CornflowerBlue;

        public static void Initialize(CanvasDevice device)
        {
            _device = device;
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

            fNextMenuItemPositionY = MenuItemsPosition.Y;
            AddMainMenuItems();
        }

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

            for (int i = 0; i < MainMenu.MenuItems.Count; i++)
            {
                Vector2 position = new Vector2(fCurrentX, fCurrentY);

                if (i == Menu.SelectedIndex)
                {
                    MainMenu.MenuItems[i].DrawSelected(args);
                }
                else
                {
                    MainMenu.MenuItems[i].Draw(args);
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
        private static void ScrollUp() { MainMenu.ScrollUp(); }
        private static void ScrollDown() { MainMenu.ScrollDown(); }
        public static void KeyDown(VirtualKey vk) { MainMenu.KeyDown(vk); }
        internal static void PointerPressed(PointerPoint point, PointerPointProperties pointProperties) { }
        internal static void PointerReleased(PointerPoint point, PointerPointProperties pointProperties) { }
        private static void MenuItemCreateNewMap_Select() { OnTransitionState(GAMESTATE.GAME_INITIALIZE); }
        private static void MenuItem2_Select() { }
        #endregion

        #region Menu Items
        private static void AddMainMenuItems()
        {
            MenuItem menuItemCreateNewMap = new MenuItem(_device, "Create new map");
            menuItemCreateNewMap.Select += MenuItemCreateNewMap_Select;
            MenuItem menuItem2 = new MenuItem(_device, "Anything else");
            menuItem2.Select += MenuItem2_Select;

            AddMainMenuItem(menuItemCreateNewMap);
            AddMainMenuItem(menuItem2);
        }
        private static void AddMainMenuItem(MenuItem m)
        {
            m.Position = new Vector2(MenuItemsPosition.X, fNextMenuItemPositionY);
            fNextMenuItemPositionY += (float)m.TextLayout.LayoutBounds.Height + fMenuItemPadding;
            MainMenu.AddMenuItem(m);
        }
        #endregion

        public static void Reset() { MainMenu.Reset(); }
    }
}
