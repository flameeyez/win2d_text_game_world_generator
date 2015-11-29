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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Map map;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        private void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch(args.VirtualKey)
            {
                case Windows.System.VirtualKey.H:
                    Statics.MapDrawType = MapDrawType.HEIGHTMAP;
                    break;
                case Windows.System.VirtualKey.R:
                    Statics.MapDrawType = MapDrawType.REGIONS;
                    break;
                case Windows.System.VirtualKey.P:
                    Statics.DrawPaths = !Statics.DrawPaths;
                    break;
                case Windows.System.VirtualKey.S:
                    Statics.DrawSubregions = !Statics.DrawSubregions;
                    break;
                case Windows.System.VirtualKey.D:
                    Statics.DrawDebug = !Statics.DrawDebug;
                    break;
                case Windows.System.VirtualKey.G:
                    Statics.DrawGrid = !Statics.DrawGrid;
                    break;
            }
        }

        #region Mouse
        private void gridMain_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPointProperties p = e.GetCurrentPoint(gridMain).Properties;
            if (p.IsLeftButtonPressed)
            {
                Statics.DrawSubregions = !Statics.DrawSubregions;
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
        }
        private void gridMain_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
        }
        private void gridMain_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Point p = e.GetCurrentPoint(gridMain).Position;
            Statics.MouseX = p.X;
            Statics.MouseY = p.Y;
            Statics.CurrentMouseRegion = map.GetRegion((int)(p.X - Statics.Padding) / Statics.PixelScale, (int)(p.Y - Statics.Padding) / Statics.PixelScale);
            if (Statics.CurrentMouseRegion != null)
            {
                Statics.CurrentMouseSubregion = map.GetSubregion(Statics.CurrentMouseRegion, (int)p.X / Statics.PixelScale, (int)p.Y / Statics.PixelScale);

                if (Statics.CurrentMouseSubregion != null)
                {
                    Room room = map.GetRoom(Statics.CurrentMouseSubregion, (int)p.X / Statics.PixelScale, (int)p.Y / Statics.PixelScale);
                    if (room != null)
                    {
                        Statics.DebugHeightString = "Elevation: " + room.Elevation.ToString();
                    }
                }
            }
            else
            {
                Statics.CurrentMouseSubregion = null;
            }
        }
        #endregion

        #region Draw
        private void canvasMain_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            switch (Statics.MapDrawType)
            {
                case MapDrawType.REGIONS:
                    map.DrawRegions(args);
                    break;
                case MapDrawType.HEIGHTMAP:
                    map.DrawHeightMap(args);
                    break;
            }

            if (Statics.DrawDebug)
            {
                DrawDebug(args);
            }
        }
        Rect DebugRect = new Rect(1500, 10, 400, 560);
        private void DrawDebug(CanvasAnimatedDrawEventArgs args)
        {
            lock(Statics.lockDebugLists)
            {
                //CanvasTextLayout LayoutHundred = new CanvasTextLayout(args.DrawingSession, (100 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
                //args.DrawingSession.DrawTextLayout(LayoutHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 70), Colors.White);

                //CanvasTextLayout LayoutTwoHundred = new CanvasTextLayout(args.DrawingSession, (200 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
                //args.DrawingSession.DrawTextLayout(LayoutTwoHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 120), Colors.White);

                //CanvasTextLayout LayoutThreeHundred = new CanvasTextLayout(args.DrawingSession, (300 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
                //args.DrawingSession.DrawTextLayout(LayoutThreeHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 170), Colors.White);

                //args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 50, Statics.MouseY - 50, 100, 100), Colors.White);
                //args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 100, Statics.MouseY - 100, 200, 200), Colors.White);
                //args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 150, Statics.MouseY - 150, 300, 300), Colors.White);

                args.DrawingSession.FillRectangle(DebugRect, Colors.CornflowerBlue);
                // args.DrawingSession.DrawText(Statics.DebugMapCreationTimeString, new Vector2(1510, 20), Colors.White);
                if (Statics.MapCreationTimes != null && Statics.MapCreationTimes.Count > 0)
                {
                    args.DrawingSession.DrawText("Average creation time: " + Statics.MapCreationTimes.Average().ToString() + "ms", new Vector2(1510, 20), Colors.White);
                    args.DrawingSession.DrawText("Min creation time: " + Statics.MapCreationTimes.Min().ToString() + "ms", new Vector2(1510, 40), Colors.White);
                    args.DrawingSession.DrawText("Max creation time: " + Statics.MapCreationTimes.Max().ToString() + "ms", new Vector2(1510, 60), Colors.White);
                }
                if (Statics.MapAbortCounts != null && Statics.MapAbortCounts.Count > 0)
                {
                    args.DrawingSession.DrawText("Average abort count: " + Statics.MapAbortCounts.Average().ToString(), new Vector2(1510, 80), Colors.White);
                    args.DrawingSession.DrawText("Min abort count: " + Statics.MapAbortCounts.Min().ToString(), new Vector2(1510, 100), Colors.White);
                    args.DrawingSession.DrawText("Max abort count: " + Statics.MapAbortCounts.Max().ToString(), new Vector2(1510, 120), Colors.White);
                }

                //if (Statics.FixLoopCounts != null && Statics.FixLoopCounts.Count > 0)
                //{
                //    args.DrawingSession.DrawText("Average fix loop count: " + Statics.FixLoopCounts.Average().ToString(), new Vector2(1510, 80), Colors.White);
                //    args.DrawingSession.DrawText("Min fix loop count: " + Statics.FixLoopCounts.Min().ToString(), new Vector2(1510, 100), Colors.White);
                //    args.DrawingSession.DrawText("Max fix loop count: " + Statics.FixLoopCounts.Max().ToString(), new Vector2(1510, 120), Colors.White);
                //}
                args.DrawingSession.DrawText(Statics.DebugMapTotalRegionCountString, new Vector2(1510, 140), Colors.White);
                args.DrawingSession.DrawText(Statics.DebugMapTotalTileCountString, new Vector2(1510, 160), Colors.White);
                args.DrawingSession.DrawText("Mouse: " + ((int)Statics.MouseX).ToString() + ", " + ((int)Statics.MouseY).ToString(), new Vector2(1510, 180), Colors.White);
                if (Statics.CurrentMouseRegion != null)
                {
                    args.DrawingSession.DrawText("Region ID: " + Statics.CurrentMouseRegion.ID.ToString(), new Vector2(1510, 200), Colors.White);
                    args.DrawingSession.DrawText("Region name: " + Statics.CurrentMouseRegion.Name, new Vector2(1510, 220), Colors.White);
                    args.DrawingSession.DrawText("Region room count: " + Statics.CurrentMouseRegion.RoomCount.ToString(), new Vector2(1510, 240), Colors.White);
                    args.DrawingSession.DrawText("Region subregion count: " + Statics.CurrentMouseRegion.Subregions.Count.ToString(), new Vector2(1510, 260), Colors.White);
                }

                if (Statics.CurrentMouseSubregion != null)
                {
                    args.DrawingSession.DrawText("Subregion: " + Statics.CurrentMouseSubregion.ID.ToString(), new Vector2(1510, 280), Colors.White);
                    args.DrawingSession.DrawText("Subregion room count: " + Statics.CurrentMouseSubregion.Rooms.Count.ToString(), new Vector2(1510, 300), Colors.White);
                }

                args.DrawingSession.DrawText("Map count: " + Statics.MapCount.ToString(), new Vector2(1510, 320), Colors.White);

                args.DrawingSession.DrawText("NW: " + Statics.DebugNWConnectionCount.ToString(), new Vector2(1510, 340), Colors.White);
                args.DrawingSession.DrawText("N: " + Statics.DebugNConnectionCount.ToString(), new Vector2(1510, 360), Colors.White);
                args.DrawingSession.DrawText("NE: " + Statics.DebugNEConnectionCount.ToString(), new Vector2(1510, 380), Colors.White);
                args.DrawingSession.DrawText("W: " + Statics.DebugWConnectionCount.ToString(), new Vector2(1510, 400), Colors.White);
                args.DrawingSession.DrawText("E: " + Statics.DebugEConnectionCount.ToString(), new Vector2(1510, 420), Colors.White);
                args.DrawingSession.DrawText("SW: " + Statics.DebugSWConnectionCount.ToString(), new Vector2(1510, 440), Colors.White);
                args.DrawingSession.DrawText("S: " + Statics.DebugSConnectionCount.ToString(), new Vector2(1510, 460), Colors.White);
                args.DrawingSession.DrawText("SE: " + Statics.DebugSEConnectionCount.ToString(), new Vector2(1510, 480), Colors.White);

                args.DrawingSession.DrawText(Statics.DebugMapCreationTimeString, new Vector2(1510, 500), Colors.White);
                args.DrawingSession.DrawText(Statics.DebugHeightString, new Vector2(1510, 520), Colors.White);
            }
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
            Reset();
        }

        private void Reset()
        {
            Statics.DebugNWConnectionCount = 0;
            Statics.DebugNConnectionCount = 0;
            Statics.DebugNEConnectionCount = 0;
            Statics.DebugWConnectionCount = 0;
            Statics.DebugEConnectionCount = 0;
            Statics.DebugSWConnectionCount = 0;
            Statics.DebugSConnectionCount = 0;
            Statics.DebugSEConnectionCount = 0;

            Statics.FrameCount = 0;
            Statics.MapCount++;

            map = Map.Create(Statics.MapWidthInPixels, Statics.MapHeightInPixels);

            lock(Statics.lockDebugLists)
            {
                Statics.MapCreationTimes.Add(map.DebugCreationTime.TotalMilliseconds);
                Statics.MapAbortCounts.Add(map.DebugAbortedCount);
            }

            Statics.DebugMapCreationTimeString = "Map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms";
            Statics.DebugMapTotalRegionCountString = "Total regions: " + map.Regions.Count.ToString();
            Statics.DebugMapTotalTileCountString = "Total tiles: " + (map.WidthInTiles * map.HeightInTiles).ToString();

            if (map.DebugCreationTime.TotalMilliseconds > 20000) { Statics.RollingReset = false; }
        }

        private void RollingReset()
        {
            while (Statics.RollingReset)
            {
                Reset();
            }
        }
        #endregion
    }
}
