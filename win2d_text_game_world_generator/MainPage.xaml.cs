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
            Statics.CurrentMouseRegion = map.GetRegion((int)p.X / Statics.PixelScale, (int)p.Y / Statics.PixelScale);
            if (Statics.CurrentMouseRegion != null)
            {
                Statics.CurrentMouseSubregion = map.GetSubregion(Statics.CurrentMouseRegion, (int)p.X / Statics.PixelScale, (int)p.Y / Statics.PixelScale);
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
            map.Draw(args);
            DrawDebug(args);
        }
        Rect DebugRect = new Rect(1200, 10, 400, 300);
        private void DrawDebug(CanvasAnimatedDrawEventArgs args)
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
            args.DrawingSession.DrawText(Statics.DebugMapCreationTimeString, new Vector2(1210, 20), Colors.White);
            args.DrawingSession.DrawText(Statics.DebugMapTotalRegionCountString, new Vector2(1210, 40), Colors.White);
            args.DrawingSession.DrawText(Statics.DebugMapTotalTileCountString, new Vector2(1210, 60), Colors.White);
            args.DrawingSession.DrawText(Statics.TilesInMainPath1.ToString(), new Vector2(1210, 80), Colors.White);
            args.DrawingSession.DrawText(Statics.TilesInMainPath2.ToString(), new Vector2(1210, 100), Colors.White);
            args.DrawingSession.DrawText("Mouse: " + ((int)Statics.MouseX).ToString() + ", " + ((int)Statics.MouseY).ToString(), new Vector2(1210, 120), Colors.White);
            if (Statics.CurrentMouseRegion != null)
            {
                args.DrawingSession.DrawText("Region ID: " + Statics.CurrentMouseRegion.ID.ToString(), new Vector2(1210, 140), Colors.White);
                args.DrawingSession.DrawText("Region name: " + Statics.CurrentMouseRegion.Name, new Vector2(1210, 160), Colors.White);
                args.DrawingSession.DrawText("Region room count: " + Statics.CurrentMouseRegion.RoomCount.ToString(), new Vector2(1210, 180), Colors.White);
                args.DrawingSession.DrawText("Region subregion count: " + Statics.CurrentMouseRegion.Subregions.Count.ToString(), new Vector2(1210, 200), Colors.White);
            }

            if (Statics.CurrentMouseSubregion != null)
            {
                args.DrawingSession.DrawText("Subregion: " + Statics.CurrentMouseSubregion.ID.ToString(), new Vector2(1210, 220), Colors.White);
                args.DrawingSession.DrawText("Subregion room count: " + Statics.CurrentMouseSubregion.Rooms.Count.ToString(), new Vector2(1210, 240), Colors.White);
            }
        }
        #endregion

        #region Update
        private void canvasMain_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            Statics.FrameCount++;
            map.Update(args);
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
            Statics.FrameCount = 0;
            Stopwatch s = Stopwatch.StartNew();
            map = Map.Create(Statics.MapWidthInPixels, Statics.MapHeightInPixels);
            s.Stop();

            Statics.DebugMapCreationTimeString = "Map creation time: " + s.ElapsedMilliseconds.ToString() + "ms";
            Statics.DebugMapTotalRegionCountString = "Total regions: " + map.Regions.Count.ToString();
            Statics.DebugMapTotalTileCountString = "Total tiles: " + (map.WidthInTiles * map.HeightInTiles).ToString();
        }
        #endregion
    }
}
