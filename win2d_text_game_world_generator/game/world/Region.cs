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
    public class Region
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }

        // list of x,y coordinates for the region
        public List<Room> Rooms = new List<Room>();

        private Region() { }
        public static Region FromProtoRegion(ProtoRegion pr)
        {
            Region region = new Region();
            region.ID = pr.ID;
            region.Name = pr.Name;
            region.Color = pr.Color;
            foreach(ProtoRoom pt in pr.ProtoRooms)
            {
                region.Rooms.Add(Room.FromProtoRoom(region, pt));
            }
            return region;
        }

        public void Draw(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            foreach (Room tile in Rooms)
            {
                args.DrawingSession.FillRectangle(
                    new Rect(MapPosition.X + Statics.Padding + tile.Coordinates.X * Statics.PixelScale,
                             MapPosition.Y + Statics.Padding + tile.Coordinates.Y * Statics.PixelScale,
                             Statics.PixelScale,
                             Statics.PixelScale),
                    Color);
            }
        }
    }
}
