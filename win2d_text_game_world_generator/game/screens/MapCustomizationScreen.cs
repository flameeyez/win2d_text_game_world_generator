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
    public static class MapCustomizationScreen
    {
        public static win2d_Map Map { get; set; }
        public static win2d_Panel CustomizationPanel { get; set; }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // draw map
            Map.Draw(args);

            // draw panel
            CustomizationPanel.Draw(args);
        }

        public static void Initialize(CanvasDevice device)
        {
            // TODO
            // set map dimensions
            //

            // set panel dimensions
            int nPositionX = Statics.CanvasWidth - 400 + Statics.Padding;
            int nPositionY = Statics.Padding;
            int nWidth = 400 - Statics.Padding * 2;
            int nHeight = Statics.CanvasHeight - Statics.Padding * 2;

            CustomizationPanel = new win2d_Panel(new Vector2(nPositionX, nPositionY), nWidth, nHeight, Colors.RosyBrown);
        }

        public static void AddControl(win2d_Control control)
        {
            CustomizationPanel.AddControl(control);
        }
    }
}
