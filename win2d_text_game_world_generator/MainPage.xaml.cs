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
        private void gridMain_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Reset();
        }
        private void gridMain_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //Point p = e.GetCurrentPoint(gridMain).Position;
            //ProtoRegion region = map.GetRegion((int)p.X / Statics.PixelScale, (int)p.Y / Statics.PixelScale);
            //if (region != null) { Statics.CurrentMouseRegionID = region.ID; }
        }
        #endregion

        #region Draw
        private void canvasMain_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            map.Draw(args);
            DrawDebug(args);
        }
        Rect DebugRect = new Rect(1200, 10, 400, 200);
        private void DrawDebug(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(DebugRect, Colors.CornflowerBlue);
            args.DrawingSession.DrawText("Region: " + Statics.CurrentMouseRegionID.ToString(), new Vector2(1210, 20), Colors.White);
            args.DrawingSession.DrawText(Statics.DebugMapCreationTimeString, new Vector2(1210, 40), Colors.White);
            args.DrawingSession.DrawText(Statics.DebugMapTotalRegionCountString, new Vector2(1210, 60), Colors.White);
            args.DrawingSession.DrawText(Statics.DebugMapTotalTileCountString, new Vector2(1210, 80), Colors.White);
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
