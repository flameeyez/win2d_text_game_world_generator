using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public static class MainMenu
    {
        private static Rect Rect;
        public static List<MenuItem> MenuItems = new List<MenuItem>();

        private static int _selectedindex;
        private static MenuItem _selecteditem;
        public static MenuItem SelectedItem { get { return _selecteditem; } }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            DrawBackground(args);
            DrawItems(args);
        }

        private static void DrawBackground(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(Rect, Colors.CornflowerBlue);
        }

        private static void DrawItems(CanvasAnimatedDrawEventArgs args)
        {
            for (int i = 0; i < MenuItems.Count; i++)
            {
                if (i == _selectedindex)
                {
                    MenuItems[i].DrawSelected(args);
                }
                else
                {
                    MenuItems[i].Draw(args);
                }
            }
        }

        public static void ScrollDown()
        {
            if (MenuItems.Count == 0) { return; }

            _selectedindex = (_selectedindex + 1) % MenuItems.Count;
            _selecteditem = MenuItems[_selectedindex];
        }

        public static void ScrollUp()
        {
            if (MenuItems.Count == 0) { return; }

            _selectedindex = (_selectedindex - 1) % MenuItems.Count;
            _selecteditem = MenuItems[_selectedindex];
        }
    }
}
