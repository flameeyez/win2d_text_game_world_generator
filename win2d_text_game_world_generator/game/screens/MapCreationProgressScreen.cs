using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public static class MapCreationProgressScreen
    {
        private static Rect MapCreationScreenBackgroundRect;

        private static CanvasTextLayout ProgressPhaseTextLayout;
        private static Vector2 ProgressPhaseTextLayoutPosition;

        private static float ProgressPercentage;
        private static Rect ProgressPercentageRect;
        private static Rect ProgressPercentageBorderRect;

        private static int ProgressBarWidth = 400;

        public static void Initialize()
        {
            MapCreationScreenBackgroundRect = new Rect(0, 0, Statics.CanvasWidth, Statics.CanvasHeight);
        }

        public static void Draw(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(MapCreationScreenBackgroundRect, Colors.CornflowerBlue);

            if (ProgressPhaseTextLayout != null)
            {
                args.DrawingSession.DrawTextLayout(ProgressPhaseTextLayout, ProgressPhaseTextLayoutPosition, Colors.White);
                args.DrawingSession.FillRectangle(ProgressPercentageRect, Colors.White);
                args.DrawingSession.DrawRectangle(ProgressPercentageBorderRect, Colors.White);
            }
        }

        public static void Set(CanvasDevice device, Tuple<string, float> progress)
        {
            ProgressPhaseTextLayout = new CanvasTextLayout(device, progress.Item1, Statics.FontMedium, 0, 0);
            float x = (float)(Statics.CanvasWidth - ProgressPhaseTextLayout.LayoutBounds.Width) / 2;
            float y = (float)(Statics.CanvasHeight - ProgressPhaseTextLayout.LayoutBounds.Height) / 2;
            ProgressPhaseTextLayoutPosition = new Vector2(x, y);

            ProgressPercentage = progress.Item2;
            ProgressPercentageRect = new Rect((Statics.CanvasWidth - ProgressBarWidth) / 2,
                                                      ProgressPhaseTextLayoutPosition.Y + ProgressPhaseTextLayout.LayoutBounds.Height + 10,
                                                      ProgressBarWidth * ProgressPercentage,
                                                      20);
            ProgressPercentageBorderRect = new Rect((Statics.CanvasWidth - ProgressBarWidth) / 2,
                                                            ProgressPhaseTextLayoutPosition.Y + ProgressPhaseTextLayout.LayoutBounds.Height + 10,
                                                            ProgressBarWidth,
                                                            20);
        }
    }
}
