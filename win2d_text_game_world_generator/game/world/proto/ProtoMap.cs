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
            // loop, creating regions, each of which will cause a group of rooms to become unavailable in the master list
            int AvailableTileCount = WidthInTiles * HeightInTiles;
            int nCurrentRegionId = 0;
            while (AvailableTileCount > 0)
            {
                ProtoRegion region = new ProtoRegion(nCurrentRegionId++, MasterTileList);
                ProtoRegions.Add(region);
                AvailableTileCount -= region.ProtoRooms.Count;
            }

            // all rooms are now unavailable, but we're left with a mess of small regions
            // merge small regions until each region is beyond a certain size (room count) threshold
            MergeRegions(MasterTileList);

            // checks to ensure that rooms within a region actually point back to the containing region
            // would theoretically catch reassignment misses during merge
            if (!DebugValidateMapIntegrity()) { throw new Exception("Wut?!?"); }
        }
        #endregion

        #region Region Operations
        private void MergeRegions(ProtoRoom[,] MasterTileList)
        {
            for (int i = ProtoRegions.Count - 1; i >= 0; i--)
            {
                if (ProtoRegions[i].ProtoRooms.Count <= Statics.MergeThreshold)
                {
                    // pick random tile and check neighboring tiles for a new region
                    int nMergeRegion = RandomNeighbor(i).ID;

                    // fold ProtoRegions[i] into ProtoRegions[nMergeRegion]
                    MergeRegions(nMergeRegion, i);

                    // reindex regions, starting at the index where we removed the last one
                    // NOTE: huge savings over reindexing everything every time
                    ReindexRegions(i);
                }
            }
        }
        private void MergeRegions(int nRegion1, int nRegion2)
        {
            foreach (ProtoRoom tile in ProtoRegions[nRegion2].ProtoRooms)
            {
                // reassign containing region
                tile.Region = ProtoRegions[nRegion1];
                ProtoRegions[nRegion1].ProtoRooms.Add(tile);
            }

            ProtoRegions.RemoveAt(nRegion2);
        }
        private void ReindexRegions(int nStartingIndex)
        {
            for (int i = nStartingIndex; i < ProtoRegions.Count; i++)
            {
                ProtoRegions[i].ID = i;
                ProtoRegions[i].ReindexRooms();
            }
        }
        public ProtoRegion RandomNeighbor(int nRegionID)
        {
            ProtoRegion region = ProtoRegions[nRegionID];
            while (region == ProtoRegions[nRegionID])
            {
                ProtoRoom tileRandom = ProtoRegions[nRegionID].ProtoRooms.RandomItem();
                int tileRandomX = (int)tileRandom.Coordinates.X;
                int tileRandomY = (int)tileRandom.Coordinates.Y;

                switch (Statics.Random.Next(4))
                {
                    case 0:
                        // left
                        if (tileRandomX > 0)
                        {
                            region = MasterTileList[tileRandomX - 1, tileRandomY].Region;
                        }
                        break;
                    case 1:
                        // right
                        if (tileRandomX < WidthInTiles - 1)
                        {
                            region = MasterTileList[tileRandomX + 1, tileRandomY].Region;
                        }
                        break;
                    case 2:
                        // up
                        if (tileRandomY > 0)
                        {
                            region = MasterTileList[tileRandomX, tileRandomY - 1].Region;
                        }
                        break;
                    case 3:
                        // down
                        if (tileRandomY < HeightInTiles - 1)
                        {
                            region = MasterTileList[tileRandomX, tileRandomY + 1].Region;
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
            foreach (ProtoRegion region in ProtoRegions)
            {
                foreach (ProtoRoom tile in region.ProtoRooms)
                {
                    if (tile.Region != region) { return false; }
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