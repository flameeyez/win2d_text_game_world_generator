using Microsoft.Graphics.Canvas;
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
    public static class MainGameScreen
    {
        public static win2d_Map Map { get; set; }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // draw map
            Map.Draw(args);
        }

        public static void Initialize(CanvasDevice device, World world)
        {
            int mapWidth = 200;
            int mapHeight = 200;
            float mapPositionX = Statics.CanvasWidth - mapWidth - Statics.Padding;
            float mapPositionY = Statics.CanvasHeight - mapHeight - Statics.Padding;

            Map = new win2d_Map(new Vector2(mapPositionX, mapPositionY), mapWidth, mapHeight, world, drawCallout:true, drawStretched:false);
        }

        public static void AddControl(win2d_Control control)
        {

        }

        public static void KeyDown(VirtualKey vk)
        {
            if (Map != null)
            {
                Map.KeyDown(vk);
            }
        }
    }
}
