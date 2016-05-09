using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class Cave
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public List<Room> Rooms = new List<Room>();

        #region Initialization
        private Cave() { }
        public static Cave FromProtoCave(ProtoCave pc)
        {
            Cave cave = new Cave();
            cave.ID = pc.ID;
            cave.Name = pc.Name;
            cave.Color = pc.Color;
            foreach (ProtoRoom pr in pc.ProtoRooms)
            {
                cave.Rooms.Add(Room.FromProtoRoom(null, null, pr));
            }
            return cave;
        }
        #endregion
    }
}
