using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.System;
using Microsoft.Graphics.Canvas;

namespace win2d_text_game_world_generator
{
    public enum MapDrawType
    {
        HEIGHTMAP,
        REGIONS,
        SUBREGIONS
    }

    public class win2d_Map : win2d_Control
    {
        public bool DebugDrawFullScreen = true;

        public World World { get; set; }
        private int _offsetX = 0;
        private int _offsetY = 0;
        private int _movefactor = 3;

        private int _scale;
        public int Scale
        {
            get { return _scale; }
            set
            {
                // clamp to [1,3]
                _scale = Math.Min(Math.Max(value, 1), 50);
            }
        }

        private MapDrawType DrawType = MapDrawType.REGIONS;
        private bool DrawPaths = false;

        public win2d_Map(Vector2 position, int width, int height, World world) : base(position, width, height)
        {
            World = world;
            Scale = 5;
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // START DEBUG - DRAW WHOLE MAP
            //args.DrawingSession.DrawImage(World.RenderTargetRegions,
            //    new Rect(Position.X, 0, World.RenderTargetRegions.Size.Width, World.RenderTargetRegions.Size.Height),
            //    new Rect(0, 0, World.RenderTargetRegions.Size.Width, World.RenderTargetRegions.Size.Height), 1.0f, CanvasImageInterpolation.NearestNeighbor);
            // END DEBUG - DRAW WHOLE MAP

            switch (DrawType)
            {
                case MapDrawType.REGIONS:
                    // START DEBUG - DRAW WHOLE MAP WITH TRANSPARENCY
                    //args.DrawingSession.DrawImage(World.RenderTargetRegions,
                    //    new Rect(Position.X, Position.Y, World.RenderTargetRegions.Size.Width, World.RenderTargetRegions.Size.Height),
                    //    new Rect(0, 0, World.RenderTargetRegions.Size.Width, World.RenderTargetRegions.Size.Height), 0.5f, CanvasImageInterpolation.NearestNeighbor);
                    // END DEBUG - DRAW WHOLE MAP WITH TRANSPARENCY

                    args.DrawingSession.DrawImage(World.RenderTargetRegions,
                        new Rect(Position.X, Position.Y, Width, Height),
                        new Rect(_offsetX, _offsetY, Width / Scale, Height / Scale), 1.0f, CanvasImageInterpolation.NearestNeighbor);
                    break;
                case MapDrawType.SUBREGIONS:
                    args.DrawingSession.DrawImage(World.RenderTargetSubregions,
                        new Rect(Position.X, Position.Y, Width, Height),
                        new Rect(_offsetX, _offsetY, Width / Scale, Height / Scale), 1.0f, CanvasImageInterpolation.NearestNeighbor);
                    break;
                case MapDrawType.HEIGHTMAP:
                    args.DrawingSession.DrawImage(World.RenderTargetHeightMap,
                        new Rect(Position.X, Position.Y, Width, Height),
                        new Rect(_offsetX, _offsetY, Width / Scale, Height / Scale), 1.0f, CanvasImageInterpolation.NearestNeighbor);
                    break;
            }

            if (DrawPaths)
            {
                args.DrawingSession.DrawImage(World.RenderTargetPaths,
                    new Rect(Position.X, Position.Y, Width, Height),
                    new Rect(_offsetX, _offsetY, Width / Scale, Height / Scale), 1.0f, CanvasImageInterpolation.NearestNeighbor);
            }

            DrawBorder(args);
        }

        private void DrawBorder(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(Rect, Colors.White);
        }

        public override bool KeyDown(VirtualKey vk)
        {
            switch (vk)
            {
                case Windows.System.VirtualKey.H:
                    DrawType = MapDrawType.HEIGHTMAP;
                    Debug.HeightMapOpacity = 255;
                    break;
                case Windows.System.VirtualKey.R:
                    DrawType = MapDrawType.REGIONS;
                    Debug.HeightMapOpacity = 75;
                    break;
                case Windows.System.VirtualKey.P:
                    DrawPaths = !DrawPaths;
                    break;
                case Windows.System.VirtualKey.S:
                    DrawType = MapDrawType.SUBREGIONS;
                    break;
                case VirtualKey.Down:
                    _offsetY = Math.Min(_offsetY + _movefactor, World.Height - Height / Scale);
                    break;
                case VirtualKey.Up:
                    _offsetY = Math.Max(_offsetY - _movefactor, 0);
                    break;
                case VirtualKey.Left:
                    _offsetX = Math.Max(_offsetX - _movefactor, 0);
                    break;
                case VirtualKey.Right:
                    _offsetX = Math.Min(_offsetX + _movefactor, World.Width - Width / Scale);
                    break;
                case VirtualKey.Add:
                    switch(Scale)
                    {
                        case 1:
                            Scale = 2;
                            break;
                        case 2:
                            Scale = 5;
                            break;
                        default:
                            Scale += 5;
                            break;
                    }
                    break;
                case VirtualKey.Subtract:
                    switch(Scale)
                    {
                        case 2:
                            Scale = 1;
                            break;
                        case 5:
                            Scale = 2;
                            break;
                        default:
                            Scale -= 5;
                            break;
                    }

                    _offsetX = Math.Min(_offsetX, World.Width - Width / Scale);
                    _offsetY = Math.Min(_offsetY, World.Height - Height / Scale);
                    break;
            }

            return true;
        }

        private int _calloutPositionX = 0;
        private int _calloutPositionY = 0;
        private void CenterOnCalloutPosition()
        {
            if(_calloutPositionX < Width / 2)
            {
                _offsetX = Width / 2;
            }
        }
    }
}
