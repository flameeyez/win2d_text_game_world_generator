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
    public class MainMenu
    {
        public List<MenuItem> MenuItems = new List<MenuItem>();

        private static int _selectedindex;
        public static int SelectedIndex { get { return _selectedindex; } }
        private static MenuItem _selecteditem;
        public static MenuItem SelectedItem { get { return _selecteditem; } }

        #region Reset
        public void Reset()
        {
            if (MenuItems.Count > 0)
            {
                _selectedindex = 0;
                _selecteditem = MenuItems[_selectedindex];
            }
        }
        #endregion

        #region Event Handling
        public void ScrollDown()
        {
            if (MenuItems.Count == 0) { return; }

            _selectedindex = (_selectedindex + 1) % MenuItems.Count;
            _selecteditem = MenuItems[_selectedindex];
        }
        public void ScrollUp()
        {
            if (MenuItems.Count == 0) { return; }

            _selectedindex = _selectedindex - 1;
            if (_selectedindex < 0) { _selectedindex += MenuItems.Count; }
            _selecteditem = MenuItems[_selectedindex];
        }
        public void KeyDown(VirtualKey vk)
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
        public void AddMenuItem(MenuItem m)
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
