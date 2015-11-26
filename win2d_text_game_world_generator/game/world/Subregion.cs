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
    public class Subregion
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public Region Region { get; set; }
        // list of x,y coordinates for the region
        public List<Room> Rooms = new List<Room>();

        private Subregion() { }
        public static Subregion FromProtoSubregion(Region region, ProtoSubregion ps)
        {
            Subregion subregion = new Subregion();
            subregion.ID = ps.ID;
            subregion.Name = ps.Name;
            subregion.Color = ps.Color;
            subregion.Region = region;
            foreach (ProtoRoom pr in ps.ProtoRooms)
            {
                subregion.Rooms.Add(Room.FromProtoRoom(region, subregion, pr));
            }
            return subregion;
        }

        public void DrawTiles(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            foreach (Room room in Rooms)
            {
                room.DrawTiles(MapPosition, args, Statics.DrawSubregions);
            }
        }

        public void DrawRoomConnections(Vector2 MapPosition, CanvasAnimatedDrawEventArgs args)
        {
            foreach (Room room in Rooms)
            {
                room.DrawRoomConnections(MapPosition, args);
            }
        }
    }
}
