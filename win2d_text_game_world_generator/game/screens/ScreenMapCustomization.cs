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
    public static class ScreenMapCustomization
    {
        private static CanvasDevice _device;
        public static win2d_Map Map { get; set; }
        public static win2d_Panel CustomizationPanel { get; set; }

        private static World _world;
        public static World World
        {
            get { return _world; }
            set { _world = value; InitializeMap(); }
        }

        public static event TransitionStateEventHandler TransitionState;
        private static void OnTransitionState(GAMESTATE state) { if (TransitionState != null) { TransitionState(state); } }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            Map.Draw(args);
            CustomizationPanel.Draw(args);
        }

        public static void Initialize(CanvasDevice device)
        {
            _device = device;

            // set panel dimensions
            Vector2 CustomizationPanelPosition = new Vector2(Statics.CanvasWidth - 400, Statics.Padding);
            int CustomizationPanelWidth = 400 - Statics.Padding;
            int CustomizationPanelHeight = Statics.CanvasHeight - Statics.Padding * 2;

            CustomizationPanel = new win2d_Panel(CustomizationPanelPosition, CustomizationPanelWidth, CustomizationPanelHeight, Colors.Black);
            AddControls();
        }
        public static void InitializeMap()
        {
            Vector2 _mapPosition = new Vector2(Statics.Padding, Statics.Padding);
            int _mapWidth = Statics.CanvasWidth - 400 - Statics.Padding * 2;
            int _mapHeight = Statics.CanvasHeight - Statics.Padding * 2;

            Map = new win2d_Map(_mapPosition, _mapWidth, _mapHeight, World, drawCallout: false, drawStretched: true);
        }

        private static void AddControls()
        {
            int btnRegenerateWidth = 360;
            int btnRegenerateHeight = 30;
            Vector2 btnRegeneratePosition = new Vector2((400 - btnRegenerateWidth - Statics.Padding) / 2, Statics.CanvasHeight - Statics.Padding * 3 - btnRegenerateHeight);
            win2d_Button btnRegenerate = new win2d_Button(_device, btnRegeneratePosition, btnRegenerateWidth, btnRegenerateHeight, "Regenerate");
            btnRegenerate.Click += BtnRegenerate_Click;
            CustomizationPanel.AddControl(btnRegenerate);

            int btnAcceptWidth = 360;
            int btnAcceptHeight = 30;
            Vector2 btnAcceptPosition = new Vector2((400 - btnAcceptWidth - Statics.Padding) / 2, btnRegeneratePosition.Y - Statics.Padding - btnAcceptHeight);
            win2d_Button btnAccept = new win2d_Button(_device, btnAcceptPosition, btnAcceptWidth, btnAcceptHeight, "Accept");
            btnAccept.Click += BtnAccept_Click;
            CustomizationPanel.AddControl(btnAccept);

            win2d_Checkbox chkDrawPaths = new win2d_Checkbox(_device, new Vector2(10, 200), "Draw Paths");
            chkDrawPaths.CheckedValueChanged += ChkDrawPaths_CheckedValueChanged;
            CustomizationPanel.AddControl(chkDrawPaths);
        }

        private static void ChkDrawPaths_CheckedValueChanged(bool bChecked)
        {
            Map.DrawPaths = bChecked;            
        }

        private static void BtnRegenerate_Click(PointerPoint point) { OnTransitionState(GAMESTATE.GAME_INITIALIZE); }
        private static void BtnAccept_Click(PointerPoint point) { OnTransitionState(GAMESTATE.UI_DISPLAY); }

        #region Event Handling
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
            if (vk == VirtualKey.Escape) { OnTransitionState(GAMESTATE.MAIN_MENU_DISPLAY); }
            else if (Map != null) { Map.KeyDown(vk); }
        }
        #endregion
    }
}
