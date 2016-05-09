using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace win2d_text_game_world_generator
{
    enum GAMESTATE
    {
        MAIN_MENU_DISPLAY,
        GAME_INITIALIZE,
        CUSTOMIZATION_DISPLAY,
        UI_DISPLAY
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        World world;
        GAMESTATE State = GAMESTATE.MAIN_MENU_DISPLAY;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            Application.Current.DebugSettings.EnableFrameRateCounter = false;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        #region Keyboard
        private void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args) { }
        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (State)
            {
                case GAMESTATE.MAIN_MENU_DISPLAY:
                    ScreenMainMenu.KeyDown(args.VirtualKey);
                    break;
                case GAMESTATE.CUSTOMIZATION_DISPLAY:
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Escape:
                            ScreenMainMenu.Reset();
                            State = GAMESTATE.MAIN_MENU_DISPLAY;
                            break;
                        default:
                            ScreenMapCustomization.KeyDown(args.VirtualKey);
                            break;
                    }
                    break;
                case GAMESTATE.UI_DISPLAY:
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Escape:
                            ScreenMainMenu.Reset();
                            State = GAMESTATE.MAIN_MENU_DISPLAY;
                            break;
                        default:
                            ScreenMainGameUI.KeyDown(args.VirtualKey);
                            break;
                    }
                    break;
            }
        }
        #endregion

        #region Mouse
        private void gridMain_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPointProperties pointProperties = e.GetCurrentPoint(gridMain).Properties;
            PointerPoint point = e.GetCurrentPoint(canvasMain);

            switch (State)
            {
                case GAMESTATE.MAIN_MENU_DISPLAY:
                    break;
                case GAMESTATE.CUSTOMIZATION_DISPLAY:
                    ScreenMapCustomization.PointerPressed(point, pointProperties);
                    break;
                case GAMESTATE.UI_DISPLAY:
                    if (world == null) { return; }
                    if (pointProperties.IsRightButtonPressed) { Reset(); }
                    else
                    {
                        ScreenMainGameUI.PointerPressed(point, pointProperties);
                    }
                    break;
            }
        }
        private void gridMain_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            PointerPointProperties pointProperties = e.GetCurrentPoint(gridMain).Properties;
            PointerPoint point = e.GetCurrentPoint(canvasMain);

            switch (State)
            {
                case GAMESTATE.MAIN_MENU_DISPLAY:
                    break;
                case GAMESTATE.CUSTOMIZATION_DISPLAY:
                    ScreenMapCustomization.PointerReleased(point, pointProperties);
                    break;
                case GAMESTATE.UI_DISPLAY:
                    if (world == null) { return; }
                    if (pointProperties.IsRightButtonPressed) { Reset(); }
                    else
                    {
                        ScreenMainGameUI.PointerReleased(point, pointProperties);
                    }
                    break;
            }
        }

        private void gridMain_PointerMoved(object sender, PointerRoutedEventArgs e) { }
        #endregion

        #region Main Menu Handling
        private void AddMainMenuItems()
        {
            MenuItem menuItemCreateNewMap = new MenuItem(canvasMain.Device, "Create new map");
            menuItemCreateNewMap.Select += MenuItemCreateNewMap_Select;
            MenuItem menuItem2 = new MenuItem(canvasMain.Device, "Anything else");
            menuItem2.Select += MenuItem2_Select;

            ScreenMainMenu.AddMenuItem(menuItemCreateNewMap);
            ScreenMainMenu.AddMenuItem(menuItem2);
        }
        private void MenuItemCreateNewMap_Select()
        {
            Reset();
        }
        private void MenuItem2_Select()
        {

        }
        #endregion

        #region Draw
        private void canvasMain_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            switch (State)
            {
                case GAMESTATE.MAIN_MENU_DISPLAY:
                    ScreenMainMenu.Draw(args);
                    break;
                case GAMESTATE.GAME_INITIALIZE:
                    ScreenMapCreationProgress.Draw(args);
                    break;
                case GAMESTATE.CUSTOMIZATION_DISPLAY:
                    ScreenMapCustomization.Draw(args);
                    break;
                case GAMESTATE.UI_DISPLAY:
                    ScreenMainGameUI.Draw(args);
                    break;
            }

            //if (World.DebugDrawDebug) { DrawDebug(args); }
        }
        private void DrawDebug(CanvasAnimatedDrawEventArgs args)
        {

        }
        #endregion

        #region Update
        private void canvasMain_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            Debug.FrameCount++;
            // map.Update(args);

            switch (State)
            {
                case GAMESTATE.UI_DISPLAY:
                    ScreenMainGameUI.Update(args);
                    break;
            }
        }
        #endregion

        #region Initialization
        private void canvasMain_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }
        private async Task CreateResourcesAsync(CanvasAnimatedControl sender)
        {
            Statics.CanvasWidth = (int)sender.Size.Width;
            Statics.CanvasHeight = (int)sender.Size.Height;

            ScreenMapCreationProgress.Initialize();

            ScreenMapCustomization.Initialize(sender.Device);
            AddControlsToMapCustomizationPanel();

            ScreenMainMenu.Initialize(sender.Device);
            AddMainMenuItems();
        }

        private void AddControlsToMapCustomizationPanel()
        {
            int btnRegenerateWidth = 360;
            int btnRegenerateHeight = 30;
            Vector2 btnRegeneratePosition = new Vector2((400 - btnRegenerateWidth - Statics.Padding) / 2, Statics.CanvasHeight - Statics.Padding * 3 - btnRegenerateHeight);
            win2d_Button btnRegenerate = new win2d_Button(canvasMain.Device, btnRegeneratePosition, btnRegenerateWidth, btnRegenerateHeight, "Regenerate");
            btnRegenerate.Click += BtnRegenerate_Click;
            ScreenMapCustomization.AddControl(btnRegenerate);

            int btnAcceptWidth = 360;
            int btnAcceptHeight = 30;
            Vector2 btnAcceptPosition = new Vector2((400 - btnAcceptWidth - Statics.Padding) / 2, btnRegeneratePosition.Y - Statics.Padding - btnAcceptHeight);
            win2d_Button btnAccept = new win2d_Button(canvasMain.Device, btnAcceptPosition, btnAcceptWidth, btnAcceptHeight, "Accept");
            btnAccept.Click += BtnAccept_Click;
            ScreenMapCustomization.AddControl(btnAccept);
        }

        private void BtnRegenerate_Click(PointerPoint point)
        {
            Reset();
        }

        private void BtnAccept_Click(PointerPoint point)
        {
            ScreenMainGameUI.Initialize(canvasMain.Device, world);
            State = GAMESTATE.UI_DISPLAY;
        }

        private async void Reset()
        {
            State = GAMESTATE.GAME_INITIALIZE;

            world = null;
            await Task.Run(() => world = World.Create(canvasMain.Device, 600, 420,
            //await Task.Run(() => world = World.Create(canvasMain.Device, 200, 200,
                new Progress<Tuple<string, float>>(progress => ScreenMapCreationProgress.Set(canvasMain.Device, progress))));

            ScreenMapCustomization.SetWorldData(world);
            State = GAMESTATE.CUSTOMIZATION_DISPLAY;
        }
        #endregion
    }
}

#region Old Code
// if (map == null) { return; }

//lock (Debug.lockLists)
//{
//    DrawDebugRadar(args);

//    List<string> DebugStrings = new List<string>();

//    // populate list
//    if (Debug.MapCreationTimes != null && Debug.MapCreationTimes.Count > 0)
//    {
//        DebugStrings.Add("Average map creation time: " + Debug.MapCreationTimes.Average().ToString() + "ms");
//        DebugStrings.Add("Min map creation time: " + Debug.MapCreationTimes.Min().ToString() + "ms");
//        DebugStrings.Add("Max map creation time: " + Debug.MapCreationTimes.Max().ToString() + "ms");
//    }

//    DebugStrings.Add("Last map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms");

//    if (Debug.MapAbortCounts != null && Debug.MapAbortCounts.Count > 0)
//    {
//        DebugStrings.Add("Average map abort count: " + Debug.MapAbortCounts.Average().ToString());
//        DebugStrings.Add("Min map abort count: " + Debug.MapAbortCounts.Min().ToString());
//        DebugStrings.Add("Max map abort count: " + Debug.MapAbortCounts.Max().ToString());
//    }

//    DebugStrings.Add("Last map abort count: " + map.DebugAbortedCount.ToString());

//    DebugStrings.Add(Debug.MapTotalTileCountString);
//    DebugStrings.Add("Map width (tiles): " + map.WidthInTiles.ToString());
//    DebugStrings.Add("Map height (tiles): " + map.HeightInTiles.ToString());
//    DebugStrings.Add("Map count: " + Debug.MapCount.ToString());
//    DebugStrings.Add(Debug.HeightString);

//    if (Debug.FixRoomConnectionsCounts != null && Debug.FixRoomConnectionsCounts.Count > 0)
//    {
//        DebugStrings.Add("Average fix connections attempts: " + Debug.FixRoomConnectionsCounts.Average().ToString());
//        DebugStrings.Add("Min fix connections attempts: " + Debug.FixRoomConnectionsCounts.Min().ToString());
//        DebugStrings.Add("Max fix connections attempts: " + Debug.FixRoomConnectionsCounts.Max().ToString());
//    }
//    DebugStrings.Add("Last fix connections attempts: " + map.DebugFixConnectionsCount.ToString());
//    DebugStrings.Add("Last fix connections time: " + map.DebugFixConnectionsTime.ToString() + "ms");

//    if (Debug.CreateRoomConnectionsCounts != null && Debug.CreateRoomConnectionsCounts.Count > 0)
//    {
//        DebugStrings.Add("Average room connections attempts: " + Debug.CreateRoomConnectionsCounts.Average().ToString());
//        DebugStrings.Add("Min room connections attempts: " + Debug.CreateRoomConnectionsCounts.Min().ToString());
//        DebugStrings.Add("Max room connections attempts: " + Debug.CreateRoomConnectionsCounts.Max().ToString());
//    }
//    DebugStrings.Add("Last room connections attempts: " + map.DebugCreateRoomConnectionsCount.ToString());
//    DebugStrings.Add("Last room connections time: " + map.DebugCreateRoomConnectionsTime.ToString() + "ms");

//    DebugStrings.Add("Mouse: " + ((int)Statics.MouseX).ToString() + ", " + ((int)Statics.MouseY).ToString());
//    if (Debug.CurrentMouseRegion != null)
//    {
//        DebugStrings.Add("Region ID: " + Debug.CurrentMouseRegion.ID.ToString());
//        DebugStrings.Add("Region name: " + Debug.CurrentMouseRegion.Name);
//        DebugStrings.Add("Region room count: " + Debug.CurrentMouseRegion.RoomCount.ToString());
//        DebugStrings.Add("Region subregion count: " + Debug.CurrentMouseRegion.Subregions.Count.ToString());
//    }

//    // draw
//    Rect DebugRect = new Rect(1500, 10, 400, 20 * (DebugStrings.Count + 1));
//    args.DrawingSession.FillRectangle(DebugRect, Colors.CornflowerBlue);

//    float fCurrentY = 20;
//    foreach (string strDebugString in DebugStrings)
//    {
//        args.DrawingSession.DrawText(strDebugString, new Vector2(1510, fCurrentY), Colors.White);
//        fCurrentY += 20;
//    }

//if (Statics.CurrentMouseSubregion != null)
//{
//    args.DrawingSession.DrawText("Subregion: " + Statics.CurrentMouseSubregion.ID.ToString(), new Vector2(1510, 280), Colors.White);
//    args.DrawingSession.DrawText("Subregion room count: " + Statics.CurrentMouseSubregion.Rooms.Count.ToString(), new Vector2(1510, 300), Colors.White);
//}

//args.DrawingSession.DrawText("NW: " + Debug.NWConnectionCount.ToString(), new Vector2(1510, 340), Colors.White);
//args.DrawingSession.DrawText("N: " + Debug.NConnectionCount.ToString(), new Vector2(1510, 360), Colors.White);
//args.DrawingSession.DrawText("NE: " + Debug.NEConnectionCount.ToString(), new Vector2(1510, 380), Colors.White);
//args.DrawingSession.DrawText("W: " + Debug.WConnectionCount.ToString(), new Vector2(1510, 400), Colors.White);
//args.DrawingSession.DrawText("E: " + Debug.EConnectionCount.ToString(), new Vector2(1510, 420), Colors.White);
//args.DrawingSession.DrawText("SW: " + Debug.SWConnectionCount.ToString(), new Vector2(1510, 440), Colors.White);
//args.DrawingSession.DrawText("S: " + Debug.SConnectionCount.ToString(), new Vector2(1510, 460), Colors.White);
//args.DrawingSession.DrawText("SE: " + Debug.SEConnectionCount.ToString(), new Vector2(1510, 480), Colors.White);
// }


//Statics.RollingReset = !Statics.RollingReset;
//if (Statics.RollingReset)
//{
//    RollingReset();
//}


//if (map == null) { return; }

//Point p = e.GetCurrentPoint(gridMain).Position;
//Statics.MouseX = p.X;
//Statics.MouseY = p.Y;
//int x = (int)(p.X - Statics.Padding) / Statics.PixelScale;
//int y = (int)(p.Y - Statics.Padding) / Statics.PixelScale;
//Debug.CurrentMouseRegion = map.GetRegion(x, y);
//if (Debug.CurrentMouseRegion != null)
//{
//    Debug.CurrentMouseSubregion = map.GetSubregion(Debug.CurrentMouseRegion, x, y);

//    if (Debug.CurrentMouseSubregion != null)
//    {
//        Room room = map.GetRoom(Debug.CurrentMouseSubregion, x, y);
//        if (room != null)
//        {
//            Debug.HeightString = "Elevation: " + room.Elevation.ToString();
//        }
//    }
//}
//else
//{
//    Debug.CurrentMouseSubregion = null;
//}



//private void DrawDebugRadar(CanvasAnimatedDrawEventArgs args)
//{
//    CanvasTextLayout LayoutHundred = new CanvasTextLayout(args.DrawingSession, (100 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
//    args.DrawingSession.DrawTextLayout(LayoutHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 70), Colors.White);

//    CanvasTextLayout LayoutTwoHundred = new CanvasTextLayout(args.DrawingSession, (200 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
//    args.DrawingSession.DrawTextLayout(LayoutTwoHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 120), Colors.White);

//    CanvasTextLayout LayoutThreeHundred = new CanvasTextLayout(args.DrawingSession, (300 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
//    args.DrawingSession.DrawTextLayout(LayoutThreeHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 170), Colors.White);

//    args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 50, Statics.MouseY - 50, 100, 100), Colors.White);
//    args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 100, Statics.MouseY - 100, 200, 200), Colors.White);
//    args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 150, Statics.MouseY - 150, 300, 300), Colors.White);
//}





// Debug.SetMapCreationMetadata(world);
// if (map.DebugCreationTime.TotalMilliseconds > 20000) { Statics.RollingReset = false; }




//Debug.NWConnectionCount = 0;
//Debug.NConnectionCount = 0;
//Debug.NEConnectionCount = 0;
//Debug.WConnectionCount = 0;
//Debug.EConnectionCount = 0;
//Debug.SWConnectionCount = 0;
//Debug.SConnectionCount = 0;
//Debug.SEConnectionCount = 0;
//Debug.FrameCount = 0;
//Debug.MapCount++;

//private void RollingReset()
//{
//    while (Debug.RollingReset) { Reset(); }
//}

#endregion