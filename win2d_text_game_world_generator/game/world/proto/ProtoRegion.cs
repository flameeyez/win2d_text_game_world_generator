using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class ProtoRegion
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public List<ProtoSubregion> ProtoSubregions = new List<ProtoSubregion>();

        public ProtoRegion(int id, ProtoRoom[,] MasterRoomList)
        {
            ID = id;
            Name = Statics.RandomRegionName();
            Color = Statics.RandomColor();

            // initialize with a single subregion
            ProtoSubregions.Add(new ProtoSubregion(0, this, MasterRoomList));
        }

        public int RoomCount
        {
            get
            {
                return ProtoSubregions.Select(x => x.ProtoRooms.Count).Sum();
            }
        }

        public void DrawHeightMap(CanvasDrawingSession ds)
        {
            foreach (ProtoSubregion ps in ProtoSubregions)
            {
                ps.DrawHeightMap(ds);
            }
        }

        public void DrawPaths(CanvasDrawingSession ds)
        {
            foreach (ProtoSubregion ps in ProtoSubregions)
            {
                ps.DrawPaths(ds);
            }
        }

        public void DrawRegions(CanvasDrawingSession ds)
        {
            foreach (ProtoSubregion ps in ProtoSubregions)
            {
                ps.DrawRegions(ds);
            }
        }

        public void DrawSubregions(CanvasDrawingSession ds)
        {
            foreach (ProtoSubregion ps in ProtoSubregions)
            {
                ps.DrawSubregions(ds);
            }
        }
    }
}
