using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class Region
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public List<Subregion> Subregions = new List<Subregion>();
        public int RoomCount { get { return Subregions.Select(x => x.Rooms.Count).Sum(); } }

        #region Initialization
        private Region() { }
        public static Region FromProtoRegion(ProtoRegion pr)
        {
            Region region = new Region();
            region.ID = pr.ID;
            region.Name = pr.Name;
            region.Color = pr.Color;
            foreach (ProtoSubregion ps in pr.ProtoSubregions)
            {
                region.Subregions.Add(Subregion.FromProtoSubregion(region, ps));
            }
            return region;
        }
        #endregion

        #region Draw
        public void DrawRegion(Vector2 position, CanvasAnimatedDrawEventArgs args)
        {
            foreach(Subregion subregion in Subregions)
            {
                subregion.DrawSubregion(position, args);
            }
        }
        //public void DrawSubregionsWithRegionColors(Vector2 position, CanvasAnimatedDrawEventArgs args)
        //{
        //    foreach (Subregion subregion in Subregions)
        //    {
        //        subregion.DrawRoomsWithRegionColor(position, args);
        //    }
        //}
        //public void DrawSubregionsWithSubregionColors(Vector2 position, CanvasAnimatedDrawEventArgs args)
        //{
        //    foreach (Subregion subregion in Subregions)
        //    {
        //        subregion.DrawRoomsWithSubregionColor(position, args);
        //    }
        //}
        //public void DrawSubregionsWithPaths(Vector2 position, CanvasAnimatedDrawEventArgs args)
        //{
        //    foreach (Subregion subregion in Subregions)
        //    {
        //        subregion.DrawRoomsWithPaths(position, args);
        //    }
        //}
        public void DrawRoomConnections(Vector2 position, CanvasAnimatedDrawEventArgs args)
        {
            foreach (Subregion subregion in Subregions)
            {
                subregion.DrawRoomConnections(position, args);
            }
        }
        public void DrawHeightMap(Vector2 position, CanvasAnimatedDrawEventArgs args)
        {
            foreach (Subregion subregion in Subregions)
            {
                subregion.DrawHeightMap(position, args);
            }
        }
        #endregion
    }
}
