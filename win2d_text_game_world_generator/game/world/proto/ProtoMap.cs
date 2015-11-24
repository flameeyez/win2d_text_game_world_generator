using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    public class ProtoMap
    {
        public List<ProtoRegion> ProtoRegions = new List<ProtoRegion>();
        private ProtoRoom[,] MasterTileList;
        public Vector2 Position { get; set; }
        public int WidthInPixels { get; set; }
        public int HeightInPixels { get; set; }
        public int WidthInTiles { get { return WidthInPixels / Statics.PixelScale; } }
        public int HeightInTiles { get { return HeightInPixels / Statics.PixelScale; } }

        #region Constructor
        public ProtoMap(int width, int height)
        {
            Position = Statics.MapPosition;

            // stretched layout
            // WidthInPixels = Statics.CanvasWidth - Statics.Padding * 2;
            // HeightInPixels = Statics.CanvasHeight - Statics.Padding * 2;

            // parameterized layout
            WidthInPixels = width - Statics.Padding * 2;
            HeightInPixels = height - Statics.Padding * 2;

            // initialize master array of tiles
            MasterTileList = new ProtoRoom[WidthInTiles, HeightInTiles];
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    MasterTileList[x, y] = new ProtoRoom(new PointInt(x, y));
                }
            }

            // now we have a grid of available/unavailable [proto]rooms (tiles/pixels)
            // while we have available rooms, create regions, each with a single subregion
            int AvailableTileCount = WidthInTiles * HeightInTiles;
            int nCurrentRegionId = 0;
            while (AvailableTileCount > 0)
            {
                ProtoRegion protoRegion = new ProtoRegion(nCurrentRegionId++, MasterTileList);
                ProtoRegions.Add(protoRegion);
                AvailableTileCount -= protoRegion.RoomCount;
            }

            // all rooms are now unavailable
            // all regions contain one subregion that contains all region rooms
            // we're left with a swath of tiny regions

            // pass 1: fold tiny regions into neighbors
            // result is that all regions still only have one subregion, but regions are guaranteed to be a certain size
            MergeRegions(MasterTileList, 100, false);

            // pass 2: fold regions into each other (as subregions)
            // result is fewer regions that now contain multiple subregions
            MergeRegions(MasterTileList, 1000, true);

            // checks to ensure that rooms within a region/subregion actually point back to the containing region/subregion
            // would theoretically catch reassignment misses during merge
            if (!DebugValidateMapIntegrity()) { throw new Exception("Wut?!?"); }

            AssignColors();
        }
        #endregion

        private void AssignColors()
        {
            foreach (ProtoRegion pr in ProtoRegions)
            {
                foreach (ProtoSubregion ps in pr.ProtoSubregions)
                {
                    int r = pr.Color.R;
                    while (Math.Abs(r - pr.Color.R) < 5)
                    {
                        r = pr.Color.R + 20 - Statics.Random.Next(41);
                        if (r < 0) { r = 0; }
                        else if (r > 255) { r = 255; }
                    }

                    int g = pr.Color.G;
                    while (Math.Abs(g - pr.Color.G) < 5)
                    {
                        g = pr.Color.G + 20 - Statics.Random.Next(41);
                        if (g < 0) { g = 0; }
                        else if (g > 255) { g = 255; }
                    }

                    int b = pr.Color.B;
                    while (Math.Abs(b - pr.Color.B) < 5)
                    {
                        b = pr.Color.B + 20 - Statics.Random.Next(41);
                        if (b < 0) { b = 0; }
                        else if (b > 255) { b = 255; }
                    }

                    ps.Color = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
                }
            }
        }

        #region Region Operations
        //private void MergeRegions(ProtoRoom[,] MasterTileList)
        //{
        //    for (int i = ProtoRegions.Count - 1; i >= 0; i--)
        //    {
        //        if (ProtoRegions[i].ProtoRooms.Count <= Statics.MergeThreshold)
        //        {
        //            // pick random tile and check neighboring tiles for a new region
        //            int nMergeRegion = RandomNeighbor(i).ID;

        //            // fold ProtoRegions[i] into ProtoRegions[nMergeRegion]
        //            MergeRegions(nMergeRegion, i);

        //            // reindex regions, starting at the index where we removed the last one
        //            // NOTE: huge savings over reindexing everything every time
        //            ReindexRegions(i);
        //        }
        //    }
        //}

        private void MergeRegions(ProtoRoom[,] MasterTileList, int nMinimumSize, bool bAddAsSubregions)
        {
            for (int i = ProtoRegions.Count - 1; i >= 0; i--)
            {
                if (ProtoRegions[i].RoomCount <= nMinimumSize)
                {
                    // pick random tile and check neighboring tiles for a new region
                    int nMergeRegion = RandomNeighbor(i).ID;

                    // fold ProtoRegions[i] into ProtoRegions[nMergeRegion]
                    MergeRegions(nMergeRegion, i, bAddAsSubregions);

                    // reindex regions, starting at the index where we removed the last one
                    // NOTE: huge savings over reindexing everything every time
                    ReindexRegions(i);
                }
            }
        }

        private void MergeRegions(int nRegion1, int nRegion2, bool bAddAsSubregions)
        {
            if (bAddAsSubregions)
            {
                foreach (ProtoSubregion ps in ProtoRegions[nRegion2].ProtoSubregions)
                {
                    ps.Color = ProtoRegions[nRegion1].Color;
                    ps.ProtoRegion = ProtoRegions[nRegion1];
                    foreach (ProtoRoom pr in ps.ProtoRooms)
                    {
                        pr.ProtoRegion = ProtoRegions[nRegion1];
                    }
                    ProtoRegions[nRegion1].ProtoSubregions.Add(ps);
                }
            }
            else
            {
                // add all of nRegion2's rooms to the first subregion of nRegion1
                foreach (ProtoSubregion ps in ProtoRegions[nRegion2].ProtoSubregions)
                {
                    foreach (ProtoRoom pr in ps.ProtoRooms)
                    {
                        pr.ProtoRegion = ProtoRegions[nRegion1];
                        pr.ProtoSubregion = ProtoRegions[nRegion1].ProtoSubregions[0];
                        ProtoRegions[nRegion1].ProtoSubregions[0].ProtoRooms.Add(pr);
                    }
                }
            }

            ProtoRegions.RemoveAt(nRegion2);
        }

        private void ReindexRegions(int nStartingIndex)
        {
            for (int i = nStartingIndex; i < ProtoRegions.Count; i++)
            {
                ProtoRegions[i].ID = i;
                for(int j = 0; j < ProtoRegions[i].ProtoSubregions.Count; j++)
                {
                    ProtoRegions[i].ProtoSubregions[j].ID = j;
                    ProtoRegions[i].ProtoSubregions[j].ProtoRegion = ProtoRegions[i];
                    ProtoRegions[i].ProtoSubregions[j].ReindexRooms();
                }
            }
        }
        public ProtoRegion RandomNeighbor(int nRegionID)
        {
            ProtoRegion region = ProtoRegions[nRegionID];
            while (region == ProtoRegions[nRegionID])
            {
                ProtoSubregion subregionRandom = ProtoRegions[nRegionID].ProtoSubregions.RandomListItem();
                ProtoRoom tileRandom = subregionRandom.ProtoRooms.RandomListItem();
                int tileRandomX = (int)tileRandom.Coordinates.X;
                int tileRandomY = (int)tileRandom.Coordinates.Y;

                switch (Statics.Random.Next(4))
                {
                    case 0:
                        // left
                        if (tileRandomX > 0)
                        {
                            region = MasterTileList[tileRandomX - 1, tileRandomY].ProtoRegion;
                        }
                        break;
                    case 1:
                        // right
                        if (tileRandomX < WidthInTiles - 1)
                        {
                            region = MasterTileList[tileRandomX + 1, tileRandomY].ProtoRegion;
                        }
                        break;
                    case 2:
                        // up
                        if (tileRandomY > 0)
                        {
                            region = MasterTileList[tileRandomX, tileRandomY - 1].ProtoRegion;
                        }
                        break;
                    case 3:
                        // down
                        if (tileRandomY < HeightInTiles - 1)
                        {
                            region = MasterTileList[tileRandomX, tileRandomY + 1].ProtoRegion;
                        }
                        break;
                }
            }

            return region;
        }
        #endregion

        #region Debug
        private bool DebugValidateMapIntegrity()
        {
            foreach (ProtoRegion pr in ProtoRegions)
            {
                foreach (ProtoSubregion ps in pr.ProtoSubregions)
                {
                    if (ps.ProtoRegion != pr) { return false; }
                    foreach (ProtoRoom proom in ps.ProtoRooms)
                    {
                        if (proom.ProtoSubregion != ps) { return false; }
                        if (proom.ProtoRegion != pr) { return false; }
                    }
                }
            }

            return true;
        }
        #endregion

        #region Cut Code
        //private void MergeRegions(Region r1, Region r2)
        //{
        //    foreach (Tile tile in r2.Tiles)
        //    {
        //        r1.Tiles.Add(tile);
        //    }

        //    Regions.Remove(r2);
        //}
        //private void ReindexRegions()
        //{
        //    for (int i = 0; i < ProtoRegions.Count; i++)
        //    {
        //        ProtoRegions[i].ID = i;
        //        ProtoRegions[i].ReindexTiles();
        //    }
        //}
        //public ProtoRegion GetRegion(int x, int y)
        //{
        //    if (x < 0) { return null; }
        //    if (x >= MasterTileList.GetLength(0)) { return null; }
        //    if (y < 0) { return null; }
        //    if (y >= MasterTileList.GetLength(1)) { return null; }

        //    return MasterTileList[x, y].Region;
        //}

        // BEGIN CUT from MergeRegions
        //bool bRestart = true;
        //while (bRestart)
        //{
        //    bRestart = false;

        //    foreach (Region region in Regions)
        //    {
        //        if (region.Tiles.Count <= Statics.MergeThreshold)
        //        {
        //            Region neighbor = RandomNeighbor(region.ID);
        //            MergeCount++;
        //            MergeRegions(region, neighbor);
        //            bRestart = true;
        //        }
        //    }
        //}
        // END CUT from MergeRegions

        // BEGIN CUT debug
        // public int FailedExpansionCount { get { return ProtoRegions.Select(x => x.FailedExpansionCount).Sum(); } }
        // public int MergeCount { get; set; }
        // END CUT debug
        #endregion
    }
}