using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input;
using Windows.System;

namespace win2d_text_game_world_generator
{
    public static class MapCustomizationScreen
    {
        public static win2d_Map Map { get; set; }
        public static win2d_Panel CustomizationPanel { get; set; }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            Map.Draw(args);
            CustomizationPanel.Draw(args);
        }

        public static void Initialize(CanvasDevice device)
        {
            // set panel dimensions
            Vector2 CustomizationPanelPosition = new Vector2(Statics.CanvasWidth - 400, Statics.Padding);
            int CustomizationPanelWidth = 400 - Statics.Padding;
            int CustomizationPanelHeight = Statics.CanvasHeight - Statics.Padding * 2;

            CustomizationPanel = new win2d_Panel(CustomizationPanelPosition, CustomizationPanelWidth, CustomizationPanelHeight, Colors.RosyBrown);
        }

        public static void SetWorldData(World world)
        {
            Vector2 _mapPosition = new Vector2(Statics.Padding, Statics.Padding);
            int _mapWidth = Statics.CanvasWidth - 400 - Statics.Padding * 2;
            int _mapHeight = Statics.CanvasHeight - Statics.Padding * 2;

            Map = new win2d_Map(_mapPosition, _mapWidth, _mapHeight, world, drawCallout: false, drawStretched: true);
        }

        public static void AddControl(win2d_Control control)
        {
            CustomizationPanel.AddControl(control);
        }

        internal static void PointerPressed(PointerPoint point, PointerPointProperties pointProperties)
        {
            if (CustomizationPanel.HitTest(point.Position))
            {
                CustomizationPanel.MouseDown(point);
            }
        }

        internal static void PointerReleased(PointerPoint point, PointerPointProperties pointProperties)
        {
            if (CustomizationPanel.HitTest(point.Position))
            {
                CustomizationPanel.MouseUp(point);
            }
        }

        internal static void KeyDown(VirtualKey vk)
        {
            if (Map != null)
            {
                Map.KeyDown(vk);
            }
        }
    }
}
