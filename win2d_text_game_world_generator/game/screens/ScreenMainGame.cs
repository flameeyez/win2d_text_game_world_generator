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
using Windows.UI.Input;

namespace win2d_text_game_world_generator
{
    public static class ScreenMainGameUI
    {
        private static CanvasDevice _device;

        public static win2d_Panel PanelLeft { get; set; }
        public static win2d_Panel PanelCenter { get; set; }
        public static win2d_Panel PanelRight { get; set; }

        public static win2d_Map Map { get; set; }
        public static win2d_Textblock TextblockMain { get; set; }
        public static win2d_Textbox TextboxInput { get; set; }
        public static win2d_Button ButtonSubmitInput { get; set; }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            PanelLeft.Draw(args);
            PanelCenter.Draw(args);
            PanelRight.Draw(args);
        }

        public static void Initialize(CanvasDevice device, World world)
        {
            _device = device;

            // START PANELS
            int panelLeftWidth = 300;
            int panelLeftHeight = Statics.CanvasHeight - Statics.Padding * 2;
            Vector2 panelLeftPosition = new Vector2(Statics.Padding, Statics.Padding);
            PanelLeft = new win2d_Panel(panelLeftPosition, panelLeftWidth, panelLeftHeight, Colors.Black);

            int panelRightWidth = 300;
            int panelRightHeight = Statics.CanvasHeight - Statics.Padding * 2;
            Vector2 panelRightPosition = new Vector2(Statics.CanvasWidth - panelRightWidth - Statics.Padding, Statics.Padding);
            PanelRight = new win2d_Panel(panelRightPosition, panelRightWidth, panelRightHeight, Colors.Black);

            int panelCenterWidth = Statics.CanvasWidth - panelLeftWidth - panelRightWidth - Statics.Padding * 4;
            int panelCenterHeight = Statics.CanvasHeight - Statics.Padding * 2;
            Vector2 panelCenterPosition = new Vector2(panelLeftWidth + Statics.Padding * 2, Statics.Padding);
            PanelCenter = new win2d_Panel(panelCenterPosition, panelCenterWidth, panelCenterHeight, Colors.Black);
            // END PANELS

            // note: controls in panels have positions relative to the panels

            // START MAP
            int mapWidth = panelRightWidth - Statics.Padding * 2;
            int mapHeight = 200;
            float mapPositionX = Statics.Padding; // panelRightWidth - mapWidth - Statics.Padding) / 2;
            float mapPositionY = Statics.Padding; // panelRightHeight - mapHeight - Statics.Padding;
            Map = new win2d_Map(new Vector2(mapPositionX, mapPositionY), mapWidth, mapHeight, world, drawCallout: true, drawStretched: false);
            PanelRight.AddControl(Map);
            // STARTDEBUG
            int x = Statics.Random.Next(world.Width);
            int y = Statics.Random.Next(world.Height);
            Map.CenterOnPoint(x, y);
            // END DEBUG
            // END MAP

            // START BUTTON
            int buttonSubmitInputWidth = 100;
            int buttonSubmitInputHeight = 36; // TODO: reconcile with height derivation in textbox constructor
            Vector2 buttonSubmitPosition = new Vector2(panelCenterWidth - Statics.Padding - buttonSubmitInputWidth, panelCenterHeight - Statics.Padding - buttonSubmitInputHeight);
            ButtonSubmitInput = new win2d_Button(_device, buttonSubmitPosition, buttonSubmitInputWidth, buttonSubmitInputHeight, "->");
            ButtonSubmitInput.Click += ButtonSubmitInput_Click;
            PanelCenter.AddControl(ButtonSubmitInput);
            // END BUTTON

            // START TEXTBOX
            int textboxInputWidth = panelCenterWidth - Statics.Padding * 3 - buttonSubmitInputWidth;
            int textboxInputHeight = 36; // TODO: reconcile with height derivation in textbox constructor
            Vector2 textboxInputPosition = new Vector2(Statics.Padding, panelCenterHeight - Statics.Padding - textboxInputHeight);
            TextboxInput = new win2d_Textbox(_device, textboxInputPosition, textboxInputWidth);
            TextboxInput.GiveFocus();
            PanelCenter.AddControl(TextboxInput);
            // END TEXTBOX

            // START TEXTBLOCK
            Vector2 textblockMainPosition = new Vector2(Statics.Padding, Statics.Padding);
            int textblockMainWidth = panelCenterWidth - Statics.Padding * 2;
            int textblockMainHeight = panelCenterHeight - textboxInputHeight - Statics.Padding * 3;
            TextblockMain = new win2d_Textblock(textblockMainPosition, textblockMainWidth, textblockMainHeight, scrolltobottomonappend: true);
            PanelCenter.AddControl(TextblockMain);
            // END TEXTBLOCK
        }

        #region Event Handling
        internal static void PointerPressed(PointerPoint point, PointerPointProperties pointProperties)
        {
            if (ButtonSubmitInput.HitTest(point.Position)) { ButtonSubmitInput.MouseDown(point); }
            else if (TextboxInput.HitTest(point.Position)) { TextboxInput.MouseDown(point); }

        }

        internal static void PointerReleased(PointerPoint point, PointerPointProperties pointProperties)
        {
            if (ButtonSubmitInput.HitTest(point.Position)) { ButtonSubmitInput.MouseUp(point); }
        }

        private static void ButtonSubmitInput_Click(PointerPoint point)
        {
            AppendInput();
        }

        private static void AppendInput()
        {
            string strInput = TextboxInput.Text.Trim();
            TextboxInput.Text = string.Empty;

            if (strInput.Length > 0)
            {
                TextblockMain.Append(_device, strInput);
            }
        }

        public static void KeyDown(VirtualKey vk)
        {
            if (vk == VirtualKey.Enter && TextboxInput.HasFocus)
            {
                AppendInput();
            }
            else
            {
                TextboxInput.KeyDown(vk);
            }
        }

        internal static void Update(CanvasAnimatedUpdateEventArgs args)
        {
            PanelLeft.Update(args);
            PanelCenter.Update(args);
            PanelRight.Update(args);            
        }
        #endregion
    }
}
