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
        MENU_DISPLAY,
        MAP_CREATE,
        MAP_DISPLAY
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Map map;
        win2d_Panel mapCustomizationPanel;
        GAMESTATE State = GAMESTATE.MENU_DISPLAY;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            Application.Current.DebugSettings.EnableFrameRateCounter = false;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        #region Keyboard
        private void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {

        }
        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (State)
            {
                case GAMESTATE.MENU_DISPLAY:
                    MainMenu.KeyDown(args.VirtualKey);
                    break;
                case GAMESTATE.MAP_DISPLAY:
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.Escape:
                            MainMenu.Reset();
                            State = GAMESTATE.MENU_DISPLAY;
                            break;
                        case VirtualKey.Enter:
                            Reset();
                            break;
                        default:
                            if (map != null) { map.KeyDown(args.VirtualKey); }
                            break;
                    }
                    break;
            }
        }
        #endregion

        #region Mouse
        private void gridMain_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            switch (State)
            {
                case GAMESTATE.MENU_DISPLAY:
                    break;
                case GAMESTATE.MAP_DISPLAY:
                    if (map == null) { return; }

                    PointerPointProperties p = e.GetCurrentPoint(gridMain).Properties;
                    if (p.IsLeftButtonPressed)
                    {
                        Map.DebugDrawSubregions = !Map.DebugDrawSubregions;
                    }
                    else if (p.IsRightButtonPressed)
                    {
                        Reset();

                        //Statics.RollingReset = !Statics.RollingReset;
                        //if (Statics.RollingReset)
                        //{
                        //    RollingReset();
                        //}
                    }
                    break;
            }
        }
        private void gridMain_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
        }
        private void gridMain_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            switch (State)
            {
                case GAMESTATE.MENU_DISPLAY:
                    break;
                case GAMESTATE.MAP_DISPLAY:
                    if (map == null) { return; }

                    Point p = e.GetCurrentPoint(gridMain).Position;
                    Statics.MouseX = p.X;
                    Statics.MouseY = p.Y;
                    int x = (int)(p.X - Statics.Padding) / Statics.PixelScale;
                    int y = (int)(p.Y - Statics.Padding) / Statics.PixelScale;
                    Statics.DebugCurrentMouseRegion = map.GetRegion(x, y);
                    if (Statics.DebugCurrentMouseRegion != null)
                    {
                        Statics.DebugCurrentMouseSubregion = map.GetSubregion(Statics.DebugCurrentMouseRegion, x, y);

                        if (Statics.DebugCurrentMouseSubregion != null)
                        {
                            Room room = map.GetRoom(Statics.DebugCurrentMouseSubregion, x, y);
                            if (room != null)
                            {
                                Statics.DebugHeightString = "Elevation: " + room.Elevation.ToString();
                            }
                        }
                    }
                    else
                    {
                        Statics.DebugCurrentMouseSubregion = null;
                    }
                    break;
            }
        }
        #endregion

        #region Menu Handling
        private void MainMenuInitialize(CanvasDevice device)
        {
            MainMenu.Initialize(device);

            MenuItem menuItem1 = new MenuItem(canvasMain.Device, "Create new map");
            menuItem1.Select += MenuItem1_Select;
            MenuItem menuItem2 = new MenuItem(canvasMain.Device, "Anything else");
            menuItem2.Select += MenuItem2_Select;

            MainMenu.AddMenuItem(menuItem1);
            MainMenu.AddMenuItem(menuItem2);
        }

        private void MenuItem1_Select()
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
                case GAMESTATE.MENU_DISPLAY:
                    MainMenu.Draw(args);
                    break;
                case GAMESTATE.MAP_CREATE:
                    MapCreationScreen.Draw(args);
                    break;
                case GAMESTATE.MAP_DISPLAY:
                    map.Draw(args);
                    mapCustomizationPanel.Draw(args);
                    if (Map.DebugDrawDebug) { DrawDebug(args); }
                    break;
            }
        }

        private void DrawMap(CanvasAnimatedDrawEventArgs args)
        {
            map.Draw(args);
        }
        private void DrawDebug(CanvasAnimatedDrawEventArgs args)
        {
            if (map == null) { return; }

            lock (Statics.lockDebugLists)
            {
                DrawDebugRadar(args);

                List<string> DebugStrings = new List<string>();

                // populate list
                if (Statics.DebugMapCreationTimes != null && Statics.DebugMapCreationTimes.Count > 0)
                {
                    DebugStrings.Add("Average map creation time: " + Statics.DebugMapCreationTimes.Average().ToString() + "ms");
                    DebugStrings.Add("Min map creation time: " + Statics.DebugMapCreationTimes.Min().ToString() + "ms");
                    DebugStrings.Add("Max map creation time: " + Statics.DebugMapCreationTimes.Max().ToString() + "ms");
                }

                DebugStrings.Add("Last map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms");

                if (Statics.DebugMapAbortCounts != null && Statics.DebugMapAbortCounts.Count > 0)
                {
                    DebugStrings.Add("Average map abort count: " + Statics.DebugMapAbortCounts.Average().ToString());
                    DebugStrings.Add("Min map abort count: " + Statics.DebugMapAbortCounts.Min().ToString());
                    DebugStrings.Add("Max map abort count: " + Statics.DebugMapAbortCounts.Max().ToString());
                }

                DebugStrings.Add("Last map abort count: " + map.DebugAbortedCount.ToString());

                DebugStrings.Add(Statics.DebugMapTotalTileCountString);
                DebugStrings.Add("Map width (tiles): " + map.WidthInTiles.ToString());
                DebugStrings.Add("Map height (tiles): " + map.HeightInTiles.ToString());
                DebugStrings.Add("Map count: " + Statics.DebugMapCount.ToString());
                DebugStrings.Add(Statics.DebugHeightString);

                if (Statics.DebugFixRoomConnectionsCounts != null && Statics.DebugFixRoomConnectionsCounts.Count > 0)
                {
                    DebugStrings.Add("Average fix connections attempts: " + Statics.DebugFixRoomConnectionsCounts.Average().ToString());
                    DebugStrings.Add("Min fix connections attempts: " + Statics.DebugFixRoomConnectionsCounts.Min().ToString());
                    DebugStrings.Add("Max fix connections attempts: " + Statics.DebugFixRoomConnectionsCounts.Max().ToString());
                }
                DebugStrings.Add("Last fix connections attempts: " + map.DebugFixConnectionsCount.ToString());
                DebugStrings.Add("Last fix connections time: " + map.DebugFixConnectionsTime.ToString() + "ms");

                if (Statics.DebugCreateRoomConnectionsCounts != null && Statics.DebugCreateRoomConnectionsCounts.Count > 0)
                {
                    DebugStrings.Add("Average room connections attempts: " + Statics.DebugCreateRoomConnectionsCounts.Average().ToString());
                    DebugStrings.Add("Min room connections attempts: " + Statics.DebugCreateRoomConnectionsCounts.Min().ToString());
                    DebugStrings.Add("Max room connections attempts: " + Statics.DebugCreateRoomConnectionsCounts.Max().ToString());
                }
                DebugStrings.Add("Last room connections attempts: " + map.DebugCreateRoomConnectionsCount.ToString());
                DebugStrings.Add("Last room connections time: " + map.DebugCreateRoomConnectionsTime.ToString() + "ms");

                DebugStrings.Add("Mouse: " + ((int)Statics.MouseX).ToString() + ", " + ((int)Statics.MouseY).ToString());
                if (Statics.DebugCurrentMouseRegion != null)
                {
                    DebugStrings.Add("Region ID: " + Statics.DebugCurrentMouseRegion.ID.ToString());
                    DebugStrings.Add("Region name: " + Statics.DebugCurrentMouseRegion.Name);
                    DebugStrings.Add("Region room count: " + Statics.DebugCurrentMouseRegion.RoomCount.ToString());
                    DebugStrings.Add("Region subregion count: " + Statics.DebugCurrentMouseRegion.Subregions.Count.ToString());
                }

                // draw
                Rect DebugRect = new Rect(1500, 10, 400, 20 * (DebugStrings.Count + 1));
                args.DrawingSession.FillRectangle(DebugRect, Colors.CornflowerBlue);

                float fCurrentY = 20;
                foreach (string strDebugString in DebugStrings)
                {
                    args.DrawingSession.DrawText(strDebugString, new Vector2(1510, fCurrentY), Colors.White);
                    fCurrentY += 20;
                }

                //if (Statics.CurrentMouseSubregion != null)
                //{
                //    args.DrawingSession.DrawText("Subregion: " + Statics.CurrentMouseSubregion.ID.ToString(), new Vector2(1510, 280), Colors.White);
                //    args.DrawingSession.DrawText("Subregion room count: " + Statics.CurrentMouseSubregion.Rooms.Count.ToString(), new Vector2(1510, 300), Colors.White);
                //}

                //args.DrawingSession.DrawText("NW: " + Statics.DebugNWConnectionCount.ToString(), new Vector2(1510, 340), Colors.White);
                //args.DrawingSession.DrawText("N: " + Statics.DebugNConnectionCount.ToString(), new Vector2(1510, 360), Colors.White);
                //args.DrawingSession.DrawText("NE: " + Statics.DebugNEConnectionCount.ToString(), new Vector2(1510, 380), Colors.White);
                //args.DrawingSession.DrawText("W: " + Statics.DebugWConnectionCount.ToString(), new Vector2(1510, 400), Colors.White);
                //args.DrawingSession.DrawText("E: " + Statics.DebugEConnectionCount.ToString(), new Vector2(1510, 420), Colors.White);
                //args.DrawingSession.DrawText("SW: " + Statics.DebugSWConnectionCount.ToString(), new Vector2(1510, 440), Colors.White);
                //args.DrawingSession.DrawText("S: " + Statics.DebugSConnectionCount.ToString(), new Vector2(1510, 460), Colors.White);
                //args.DrawingSession.DrawText("SE: " + Statics.DebugSEConnectionCount.ToString(), new Vector2(1510, 480), Colors.White);
            }
        }
        private void DrawDebugRadar(CanvasAnimatedDrawEventArgs args)
        {
            CanvasTextLayout LayoutHundred = new CanvasTextLayout(args.DrawingSession, (100 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
            args.DrawingSession.DrawTextLayout(LayoutHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 70), Colors.White);

            CanvasTextLayout LayoutTwoHundred = new CanvasTextLayout(args.DrawingSession, (200 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
            args.DrawingSession.DrawTextLayout(LayoutTwoHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 120), Colors.White);

            CanvasTextLayout LayoutThreeHundred = new CanvasTextLayout(args.DrawingSession, (300 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
            args.DrawingSession.DrawTextLayout(LayoutThreeHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 170), Colors.White);

            args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 50, Statics.MouseY - 50, 100, 100), Colors.White);
            args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 100, Statics.MouseY - 100, 200, 200), Colors.White);
            args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 150, Statics.MouseY - 150, 300, 300), Colors.White);
        }
        #endregion

        #region Update
        private void canvasMain_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            Statics.FrameCount++;
            // map.Update(args);
        }
        #endregion

        #region Initialization
        private void canvasMain_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }
        private async Task CreateResourcesAsync(CanvasAnimatedControl sender)
        {
            // c.TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 200);
            // c.Paused = true;
            Statics.CanvasWidth = (int)sender.Size.Width;
            Statics.CanvasHeight = (int)sender.Size.Height;

            MainMenuInitialize(sender.Device);
            MapCreationScreen.Initialize();
            CreateMapCustomizationPanel();

            // Reset();
        }
        private void CreateMapCustomizationPanel()
        {
            int nPositionX = Statics.CanvasWidth - 400 + Statics.Padding;
            int nPositionY = Statics.Padding;
            int nWidth = 400 - Statics.Padding * 2;
            int nHeight = Statics.CanvasHeight - Statics.Padding * 2;

            mapCustomizationPanel = new win2d_Panel(new Vector2(nPositionX, nPositionY), nWidth, nHeight, Colors.RosyBrown);
            win2d_Button button = new win2d_Button(canvasMain.Device, new Vector2(10, 10), 200, 40, "Hello!");
            button.Click += Button_Click;
            mapCustomizationPanel.AddControl(button);
        }

        private void Button_Click(PointerPoint point)
        {
            int i = 0;
        }

        private async void Reset()
        {
            map = null;
            State = GAMESTATE.MAP_CREATE;

            Statics.DebugNWConnectionCount = 0;
            Statics.DebugNConnectionCount = 0;
            Statics.DebugNEConnectionCount = 0;
            Statics.DebugWConnectionCount = 0;
            Statics.DebugEConnectionCount = 0;
            Statics.DebugSWConnectionCount = 0;
            Statics.DebugSConnectionCount = 0;
            Statics.DebugSEConnectionCount = 0;

            Statics.FrameCount = 0;
            Statics.DebugMapCount++;

            // map = Map.Create(Statics.MapWidthInPixels, Statics.MapHeightInPixels);
            await Task.Run(() => map = Map.Create(Statics.MapWidthInPixels - 400, Statics.MapHeightInPixels, new Progress<Tuple<string, float>>(progress => MapCreationScreen.Set(canvasMain.Device, progress))));

            DebugSetMapCreationMetadata();

            // if (map.DebugCreationTime.TotalMilliseconds > 20000) { Statics.RollingReset = false; }

            State = GAMESTATE.MAP_DISPLAY;
        }
        private void DebugSetMapCreationMetadata()
        {
            lock (Statics.lockDebugLists)
            {
                Statics.DebugMapCreationTimes.Add(map.DebugCreationTime.TotalMilliseconds);
                Statics.DebugMapAbortCounts.Add(map.DebugAbortedCount);
                Statics.DebugCreateRoomConnectionsCounts.Add(map.DebugCreateRoomConnectionsCount);
                Statics.DebugFixRoomConnectionsCounts.Add(map.DebugFixConnectionsCount);
            }

            Statics.DebugMapCreationTimeString = "Map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms";
            Statics.DebugMapTotalRegionCountString = "Total regions: " + map.Regions.Count.ToString();
            Statics.DebugMapTotalTileCountString = "Total tiles: " + (map.WidthInTiles * map.HeightInTiles).ToString();
        }
        private void RollingReset()
        {
            while (Statics.DebugRollingReset)
            {
                Reset();
            }
        }
        #endregion
    }
}
