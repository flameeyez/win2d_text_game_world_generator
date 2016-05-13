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
        public int ID { get; set; }
        private PointInt _coordinatesXY;
        public PointInt CoordinatesXY { get { return _coordinatesXY; } }
        private Region _region;
        public Region Region { get { return _region; } }
        private Subregion _subregion;
        public Subregion Subregion { get { return _subregion; } }
        public ReadOnlyDictionary<string, Tuple<int, int, int>> DirectionalRoomConnections;
        public ReadOnlyCollection<RoomConnection> RoomConnections;
        private int _elevation;
        public int Elevation { get { return _elevation; } }
        private Color _elevationcolor;
        public Color ElevationColor { get { return _elevationcolor; } }

        public string DisplayString
        {
            get
            {
                StringBuilder sbDisplayString = new StringBuilder();
                sbDisplayString.Append("Region: " + Region.ID.ToString() + " ");
                sbDisplayString.Append("Subregion: " + Subregion.ID.ToString() + " ");
                sbDisplayString.Append("ID: " + ID.ToString() + "\n");
                sbDisplayString.Append(DirectionalRoomConnectionsString + "\n");
                return sbDisplayString.ToString();
            }
        }

        private string DirectionalRoomConnectionsString
        {
            get
            {
                if (DirectionalRoomConnections.Count == 0) { return "Obvious exits: none"; }
                StringBuilder sbDirectionalExitsString = new StringBuilder();
                sbDirectionalExitsString.Append("Obvious exits: ");

                foreach (string strExit in DirectionalRoomConnections.Keys)
                {
                    sbDirectionalExitsString.Append(strExit + ", ");
                }

                //sbDirectionalExitsString.Remove();
                return sbDirectionalExitsString.ToString(0, sbDirectionalExitsString.Length - 2);
            }
        }

        private Room() { }
        public static Room FromProtoRoom(Region region, Subregion subregion, ProtoRoom pr)
        {
            Room room = new Room();
            room.ID = pr.ID;
            room._coordinatesXY = pr.CoordinatesXY;
            room._region = region;
            room._subregion = subregion;
            room.DirectionalRoomConnections = new ReadOnlyDictionary<string, Tuple<int, int, int>>(pr.DirectionalRoomConnections);
            room.RoomConnections = new ReadOnlyCollection<RoomConnection>(pr.ProtoRoomConnections);
            room._elevation = pr.Elevation;
            room._elevationcolor = pr.ElevationColor;

            return room;
        }

        #region Draw
        public void DrawTile(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args, bool bDrawSubregions, bool bDrawGrid)
        {
            args.DrawingSession.FillRectangle(
                new Rect(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale,
                    MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale,
                    Statics.PixelScale,
                    Statics.PixelScale),
                    bDrawSubregions ? Subregion.Color : Region.Color);

            if (bDrawGrid)
            {
                DrawBorder(MapPosition, args);
            }
        }
        public void DrawHeight(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(
                new Rect(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale,
                    MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale,
                    Statics.PixelScale,
                    Statics.PixelScale),
                    Color.FromArgb(Debug.HeightMapOpacity, ElevationColor.R, ElevationColor.G, ElevationColor.B));
        }
        public void DrawBorder(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            args.DrawingSession.DrawRectangle(
                new Rect(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale,
                    MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale,
                    Statics.PixelScale,
                    Statics.PixelScale),
                    Colors.Black);
        }
        public void DrawRoomConnections(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            foreach (string DirectionalRoomConnection in DirectionalRoomConnections.Keys)
            {
                switch (DirectionalRoomConnection)
                {
                    case "nw":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (CoordinatesXY.X - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (CoordinatesXY.Y - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "n":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (CoordinatesXY.Y - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "ne":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (CoordinatesXY.X + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (CoordinatesXY.Y - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "w":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (CoordinatesXY.X - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "o":
                        break;
                    case "e":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (CoordinatesXY.X + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "sw":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (CoordinatesXY.X - 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (CoordinatesXY.Y + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "s":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (CoordinatesXY.Y + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             Colors.White);
                        break;
                    case "se":
                        args.DrawingSession.DrawLine(MapPosition.X + Statics.Padding + CoordinatesXY.X * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + CoordinatesXY.Y * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.X + Statics.Padding + (CoordinatesXY.X + 1) * Statics.PixelScale + Statics.PixelScale / 2,
                             MapPosition.Y + Statics.Padding + (CoordinatesXY.Y + 1) * Statics.PixelScale + Statics.PixelScale / 2,
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
