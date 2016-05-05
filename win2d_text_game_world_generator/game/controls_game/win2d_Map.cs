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
        private int _drawingoffsetX = 0;
        private int _drawingoffsetY = 0;

        private int _calloutPositionX = 0;
        private int _calloutPositionY = 0;
        private float _calloutDrawPositionX = 0.0f;
        private float _calloutDrawPositionY = 0.0f;

        private Rect _calloutDrawRect;
        private int _calloutSideLength = 4;

        private int _movefactor = 1;

        private int _scale;
        public int Scale
        {
            get { return _scale; }
            set
            {
                // clamp to [1,3]
                _scale = Math.Min(Math.Max(value, 1), 10);
            }
        }

        private int ScaledWidth { get { return Width / Scale; } }
        private int ScaledHeight { get { return Height / Scale; } }

        private MapDrawType DrawType = MapDrawType.REGIONS;
        private bool DrawPaths = false;

        public win2d_Map(Vector2 position, int width, int height, World world) : base(position, width, height)
        {
            World = world;
            Scale = 2;
            CenterOnPoint(_calloutPositionX, _calloutPositionY);
        }

        public override void Draw(CanvasAnimatedDrawEventArgs args)
        {
            // START DEBUG - DRAW WHOLE MAP
            args.DrawingSession.DrawImage(World.RenderTargetRegions,
                new Rect(0, 0, World.RenderTargetRegions.Size.Width, World.RenderTargetRegions.Size.Height),
                new Rect(0, 0, World.RenderTargetRegions.Size.Width, World.RenderTargetRegions.Size.Height), 1.0f, CanvasImageInterpolation.NearestNeighbor);

            args.DrawingSession.FillRectangle(new Rect(_calloutPositionX, _calloutPositionY, 10, 10), Colors.Red);
            // END DEBUG - DRAW WHOLE MAP

            switch (DrawType)
            {
                case MapDrawType.REGIONS:
                    args.DrawingSession.DrawImage(World.RenderTargetRegions,
                        new Rect(Position.X, Position.Y, Width, Height),
                        new Rect(_drawingoffsetX, _drawingoffsetY, ScaledWidth, ScaledHeight), 1.0f, CanvasImageInterpolation.NearestNeighbor);
                    break;
                case MapDrawType.SUBREGIONS:
                    args.DrawingSession.DrawImage(World.RenderTargetSubregions,
                        new Rect(Position.X, Position.Y, Width, Height),
                        new Rect(_drawingoffsetX, _drawingoffsetY, ScaledWidth, ScaledHeight), 1.0f, CanvasImageInterpolation.NearestNeighbor);
                    break;
                case MapDrawType.HEIGHTMAP:
                    args.DrawingSession.DrawImage(World.RenderTargetHeightMap,
                        new Rect(Position.X, Position.Y, Width, Height),
                        new Rect(_drawingoffsetX, _drawingoffsetY, ScaledWidth, ScaledHeight), 1.0f, CanvasImageInterpolation.NearestNeighbor);
                    break;
            }

            if (DrawPaths)
            {
                args.DrawingSession.DrawImage(World.RenderTargetPaths,
                    new Rect(Position.X, Position.Y, Width, Height),
                    new Rect(_drawingoffsetX, _drawingoffsetY, ScaledWidth, ScaledHeight), 1.0f, CanvasImageInterpolation.NearestNeighbor);
            }

            DrawCallout(args);
            DrawBorder(args);
        }

        private void DrawCallout(CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(_calloutDrawRect, Colors.Red);
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
                    // _offsetY = Math.Min(_offsetY + _movefactor, World.Height - ScaledHeight);
                    _calloutPositionY = Math.Min(_calloutPositionY + _movefactor, World.Height - 1);
                    CenterOnPoint(_calloutPositionX, _calloutPositionY);
                    break;
                case VirtualKey.Up:
                    // _offsetY = Math.Max(_offsetY - _movefactor, 0);
                    _calloutPositionY = Math.Max(_calloutPositionY - _movefactor, 0);
                    CenterOnPoint(_calloutPositionX, _calloutPositionY);
                    break;
                case VirtualKey.Left:
                    // _offsetX = Math.Max(_offsetX - _movefactor, 0);
                    _calloutPositionX = Math.Max(_calloutPositionX - _movefactor, 0);
                    CenterOnPoint(_calloutPositionX, _calloutPositionY);
                    break;
                case VirtualKey.Right:
                    // _offsetX = Math.Min(_offsetX + _movefactor, World.Width - ScaledWidth);
                    _calloutPositionX = Math.Min(_calloutPositionX + _movefactor, World.Width - 1);
                    CenterOnPoint(_calloutPositionX, _calloutPositionY);
                    break;
                case VirtualKey.Add:
                    switch (Scale)
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

                    CenterOnPoint(_calloutPositionX, _calloutPositionY);
                    break;
                case VirtualKey.Subtract:
                    switch (Scale)
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

                    CenterOnPoint(_calloutPositionX, _calloutPositionY);
                    break;
            }

            return true;
        }

        private void CenterOnPoint(int x, int y)
        {
            if (x < ScaledWidth / 2)
            {
                _drawingoffsetX = 0;
                _calloutDrawPositionX = Position.X + x * Scale;
            }
            else if (x > World.Width - ScaledWidth / 2)
            {
                _drawingoffsetX = World.Width - ScaledWidth;
                _calloutDrawPositionX = Position.X + Width / 2 + (x - World.Width + ScaledWidth / 2) * Scale;
            }
            else
            {
                _drawingoffsetX = x - ScaledWidth / 2;
                _calloutDrawPositionX = Position.X + (x - _drawingoffsetX) * Scale;
            }

            if (y < ScaledHeight / 2)
            {
                _drawingoffsetY = 0;
                _calloutDrawPositionY = Position.Y + y * Scale;
            }
            else if (y > World.Height - ScaledHeight / 2)
            {
                _drawingoffsetY = World.Height - ScaledHeight;
                _calloutDrawPositionY = Position.Y + Height / 2 + (y - World.Height + ScaledHeight / 2) * Scale;
            }
            else
            {
                _drawingoffsetY = y - ScaledHeight / 2;
                _calloutDrawPositionY = Position.Y + (y - _drawingoffsetY) * Scale;
            }

            _calloutDrawRect = new Rect(_calloutDrawPositionX + (Scale - _calloutSideLength) / 2, _calloutDrawPositionY + (Scale - _calloutSideLength) / 2, _calloutSideLength, _calloutSideLength);
        }
    }
}
