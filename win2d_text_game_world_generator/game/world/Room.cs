using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class Room
    {
        private PointInt _coordinates;
        public PointInt Coordinates { get { return _coordinates; } }
        private Region _region;
        public Region Region { get { return _region; } }
        private Subregion _subregion;
        public Subregion Subregion { get { return _subregion; } }
        public ReadOnlyCollection<string> DirectionalRoomConnections;
        private int _elevation;
        public int Elevation { get { return _elevation; } }
        private Color _elevationcolor;
        public Color ElevationColor { get { return _elevationcolor; } }

        private Room() { }
        public static Room FromProtoRoom(Region region, Subregion subregion, ProtoRoom pr)
        {
            Room room = new Room();
            room._coordinates = pr.Coordinates;
            room._region = region;
            room._subregion = subregion;
            room.DirectionalRoomConnections = new ReadOnlyCollection<string>(pr.DirectionalRoomConnections);
            room._elevation = pr.Elevation;
            room._elevationcolor = pr.ElevationColor;
            return room;
        }

        #region Draw
        public void DrawTile(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args, bool bDrawSubregions)
        {
            args.DrawingSession.FillRectangle(
                new Rect(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale,
                    MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale,
                    Statics.PixelScale,
                    Statics.PixelScale),
                    bDrawSubregions ? Subregion.Color : Region.Color);

            if (Statics.DrawGrid)
            {
                DrawBorder(MapPosition, args);
            }
        }
        public void DrawHeight(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(
                new Rect(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale,
                    MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale,
                    Statics.PixelScale,
                    Statics.PixelScale),
                    ElevationColor);
        }
        public void DrawBorder(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(
                new Rect(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale,
                    MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale,
                    Statics.PixelScale,
                    Statics.PixelScale),
                    Colors.Black);
        }
        public void DrawRoomConnections(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            foreach (string DirectionalRoomConnection in DirectionalRoomConnections)
            {
                switch (DirectionalRoomConnection)
                {
                    case "nw":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (Coordinates.X - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (Coordinates.Y - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "n":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (Coordinates.Y - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "ne":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (Coordinates.X + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (Coordinates.Y - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "w":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (Coordinates.X - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "o":
                        break;
                    case "e":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (Coordinates.X + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "sw":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (Coordinates.X - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (Coordinates.Y + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "s":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (Coordinates.Y + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "se":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + Coordinates.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + Coordinates.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (Coordinates.X + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (Coordinates.Y + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }
        #endregion

        #region Cut Code
        //private static List<string> DebugAddRandomDirectionalRoomConnections()
        //{
        //    List<string> DebugDirectionalRoomConnections = new List<string>();
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("nw"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("n"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("ne"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("w"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("o"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("e"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("sw"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("s"); }
        //    if (Statics.Random.Next(5) == 0) { DebugDirectionalRoomConnections.Add("se"); }
        //    return DebugDirectionalRoomConnections;
        //}
        #endregion
    }
}
