using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.Graphics.Canvas;

namespace win2d_text_game_world_generator
{
    public class ProtoSubregion
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public ProtoRegion ProtoRegion { get; set; }
        public List<ProtoRoom> ProtoRooms = new List<ProtoRoom>();

        #region Initialization
        public ProtoSubregion(int id, ProtoRegion region, ProtoRoom[,] MasterRoomList)
        {
            ID = id;
            ProtoRegion = region;
            Color = region.Color;
            Name = Statics.RandomRegionType();

            int RoomCountX = MasterRoomList.GetLength(0);
            int RoomCountY = MasterRoomList.GetLength(1);

            // grab random point as starting room
            int x = Statics.Random.Next(RoomCountX);
            int y = Statics.Random.Next(RoomCountY);
            while (!MasterRoomList[x, y].Available)
            {
                x = Statics.Random.Next(RoomCountX);
                y = Statics.Random.Next(RoomCountY);
            }

            // keep track of available adjacent roomss
            // list for random access; hashset for lookup by coordinates
            List<ProtoRoom> AvailableAdjacentRooms = new List<ProtoRoom>();
            HashSet<PointInt> AvailableAdjacentCoordinates = new HashSet<PointInt>();

            // create starting room
            ProtoRoom startingRoom = MasterRoomList[x, y];
            ProtoRooms.Add(startingRoom);
            startingRoom.Available = false;
            startingRoom.ProtoRegion = region;
            startingRoom.ProtoSubregion = this;
            UpdateAdjacentRooms(AvailableAdjacentRooms, AvailableAdjacentCoordinates, startingRoom, MasterRoomList);

            while (AvailableAdjacentRooms.Count > 0 && ((ProtoRooms.Count < Statics.MinimumRegionSize) || (Statics.Random.Next(100) < Statics.ProbabilityOfExpansion)))
            {
                // pick a random room from the available set
                ProtoRoom randomNeighbor = AvailableAdjacentRooms.RandomListItem();
                randomNeighbor.Available = false;
                randomNeighbor.ProtoRegion = region;
                randomNeighbor.ProtoSubregion = this;

                AvailableAdjacentRooms.Remove(randomNeighbor);
                AvailableAdjacentCoordinates.Remove(randomNeighbor.CoordinatesXY);
                ProtoRooms.Add(randomNeighbor);

                // add new room's available neighbors to the available list
                UpdateAdjacentRooms(AvailableAdjacentRooms, AvailableAdjacentCoordinates, randomNeighbor, MasterRoomList);
            }
        }

        public void DrawRegions(CanvasDrawingSession ds)
        {
            foreach (ProtoRoom pr in ProtoRooms)
            {
                pr.DrawRegions(ds);
            }
        }

        public void DrawSubregions(CanvasDrawingSession ds)
        {
            foreach (ProtoRoom pr in ProtoRooms)
            {
                pr.DrawSubregions(ds);
            }
        }

        public void DrawPaths(CanvasDrawingSession ds)
        {
            foreach (ProtoRoom pr in ProtoRooms)
            {
                pr.DrawPaths(ds);
            }
        }

        public void DrawHeightMap(CanvasDrawingSession ds)
        {
            foreach (ProtoRoom pr in ProtoRooms)
            {
                pr.DrawHeightMap(ds);
            }
        }

        private void UpdateAdjacentRooms(List<ProtoRoom> AvailableAdjacentRooms, HashSet<PointInt> AvailableAdjacentCoordinates, ProtoRoom protoRoom, ProtoRoom[,] MasterRoomList)
        {
            // left
            if (protoRoom.CoordinatesXY.X > 0)
            {
                ProtoRoom neighborLeft = MasterRoomList[protoRoom.CoordinatesXY.X - 1, protoRoom.CoordinatesXY.Y];
                if (neighborLeft.Available && !AvailableAdjacentCoordinates.Contains(neighborLeft.CoordinatesXY))
                {
                    AvailableAdjacentRooms.Add(neighborLeft);
                    AvailableAdjacentCoordinates.Add(neighborLeft.CoordinatesXY);
                }
            }
            // right
            if (protoRoom.CoordinatesXY.X < MasterRoomList.GetLength(0) - 1)
            {
                ProtoRoom neighborRight = MasterRoomList[protoRoom.CoordinatesXY.X + 1, protoRoom.CoordinatesXY.Y];
                if (neighborRight.Available && !AvailableAdjacentCoordinates.Contains(neighborRight.CoordinatesXY))
                {
                    AvailableAdjacentRooms.Add(neighborRight);
                    AvailableAdjacentCoordinates.Add(neighborRight.CoordinatesXY);
                }
            }
            // above
            if (protoRoom.CoordinatesXY.Y > 0)
            {
                ProtoRoom neighborAbove = MasterRoomList[protoRoom.CoordinatesXY.X, protoRoom.CoordinatesXY.Y - 1];
                if (neighborAbove.Available && !AvailableAdjacentCoordinates.Contains(neighborAbove.CoordinatesXY))
                {
                    AvailableAdjacentRooms.Add(neighborAbove);
                    AvailableAdjacentCoordinates.Add(neighborAbove.CoordinatesXY);
                }
            }
            // below
            if (protoRoom.CoordinatesXY.Y < MasterRoomList.GetLength(1) - 1)
            {
                ProtoRoom neighborBelow = MasterRoomList[protoRoom.CoordinatesXY.X, protoRoom.CoordinatesXY.Y + 1];
                if (neighborBelow.Available && !AvailableAdjacentCoordinates.Contains(neighborBelow.CoordinatesXY))
                {
                    AvailableAdjacentRooms.Add(neighborBelow);
                    AvailableAdjacentCoordinates.Add(neighborBelow.CoordinatesXY);
                }
            }
        }
        private bool HasAvailableNeighboringRoom(ProtoRoom[,] MasterRoomList)
        {
            int RoomCountX = MasterRoomList.GetLength(0);
            int RoomCountY = MasterRoomList.GetLength(1);

            foreach (ProtoRoom room in ProtoRooms)
            {
                // check left
                if (room.CoordinatesXY.X > 0 && MasterRoomList[(int)room.CoordinatesXY.X - 1, (int)room.CoordinatesXY.Y].Available) { return true; }
                // check right
                if (room.CoordinatesXY.X < RoomCountX - 1 && MasterRoomList[(int)room.CoordinatesXY.X + 1, (int)room.CoordinatesXY.Y].Available) { return true; }
                // check up
                if (room.CoordinatesXY.Y > 0 && MasterRoomList[(int)room.CoordinatesXY.X, (int)room.CoordinatesXY.Y - 1].Available) { return true; }
                // check down
                if (room.CoordinatesXY.Y < RoomCountY - 1 && MasterRoomList[(int)room.CoordinatesXY.X, (int)room.CoordinatesXY.Y + 1].Available) { return true; }
            }

            return false;
        }
        public void ReindexRooms()
        {
            for (int i = 0; i < ProtoRooms.Count; i++)
            {
                ProtoRooms[i].ID = i;
                ProtoRooms[i].ProtoRegion = ProtoRegion;
                ProtoRooms[i].ProtoSubregion = this;
            }
        }
        #endregion

        #region Hash/Equality Overrides
        public override bool Equals(object obj)
        {
            ProtoSubregion compare = obj as ProtoSubregion;
            return compare.ID == ID;
        }
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        #endregion

        #region Cut Code
        //public bool Contains(int x, int y)
        //{
        //    foreach (Tile tile in Tiles)
        //    {
        //        if (tile.Coordinates.X == x && tile.Coordinates.Y == y) { return true; }
        //    }

        //    return false;
        //}
        //public void FindNeighbors(Tile[,] MasterTileList)
        //{
        //    int TileCountX = MasterTileList.GetLength(0);
        //    int TileCountY = MasterTileList.GetLength(1);

        //    foreach (Tile tile in Tiles)
        //    {
        //        int x = (int)tile.Coordinates.X;
        //        int y = (int)tile.Coordinates.Y;

        //        if (x > 0)
        //        {
        //            // analyze left neighbor
        //            Tile neighbor = MasterTileList[x - 1, y];
        //            if (neighbor.Region != null && neighbor.Region.ID != ID)
        //            {
        //                NeighboringRegions.Add(neighbor.Region);
        //            }
        //        }

        //        if (x < TileCountX - 1)
        //        {
        //            // analyze right neighbor
        //            Tile neighbor = MasterTileList[x + 1, y];
        //            if (neighbor.Region != null && neighbor.Region.ID != ID)
        //            {
        //                NeighboringRegions.Add(neighbor.Region);
        //            }
        //        }

        //        if (y > 0)
        //        {
        //            // analyze up neighbor
        //            Tile neighbor = MasterTileList[x, y - 1];
        //            if (neighbor.Region != null && neighbor.Region.ID != ID)
        //            {
        //                NeighboringRegions.Add(neighbor.Region);
        //            }
        //        }

        //        if (y < TileCountY - 1)
        //        {
        //            // analyze down neighbor
        //            Tile neighbor = MasterTileList[x, y + 1];
        //            if (neighbor.Region != null && neighbor.Region.ID != ID)
        //            {
        //                NeighboringRegions.Add(neighbor.Region);
        //            }
        //        }
        //    }
        //}

        // BEGIN CUT crawling expansion
        // if able, grow until minimum size reached, then possibly grow more
        //while (((Tiles.Count < Statics.MinimumRegionSize) || (Statics.Random.Next(100) < Statics.ProbabilityOfExpansion)) && HasAvailableNeighboringTile(MasterTileList))
        //{
        //    // pick a random tile from the region set and attempt to grow
        //    Tile currentTile = Tiles.RandomItem();
        //    x = (int)currentTile.Coordinates.X;
        //    y = (int)currentTile.Coordinates.Y;

        //    int nExpansionDirection = Statics.Random.Next(4);
        //    int nExpansionAttempts = 0;

        //    Tile expansionTile = null;
        //    while (expansionTile == null && nExpansionAttempts < 4)
        //    {
        //        nExpansionAttempts++;
        //        nExpansionDirection = (nExpansionDirection + 1) % 4;
        //        switch (nExpansionDirection)
        //        {
        //            case 0:
        //                // try to grow left
        //                if (x > 0 && MasterTileList[x - 1, y].Available)
        //                {
        //                    expansionTile = MasterTileList[x - 1, y];
        //                }
        //                break;
        //            case 1:
        //                // try to grow right
        //                if (x < TileCountX - 1 && MasterTileList[x + 1, y].Available)
        //                {
        //                    expansionTile = MasterTileList[x + 1, y];
        //                }
        //                break;
        //            case 2:
        //                // try to grow up
        //                if (y > 0 && MasterTileList[x, y - 1].Available)
        //                {
        //                    expansionTile = MasterTileList[x, y - 1];
        //                }
        //                break;
        //            case 3:
        //                // try to grow down
        //                if (y < TileCountY - 1 && MasterTileList[x, y + 1].Available)
        //                {
        //                    expansionTile = MasterTileList[x, y + 1];
        //                }
        //                break;
        //        }
        //    }

        //    if (expansionTile != null)
        //    {
        //        Tiles.Add(expansionTile);
        //        expansionTile.Available = false;
        //        expansionTile.Region = this;
        //    }
        //    else
        //    {
        //        FailedExpansionCount++;
        //    }
        //}
        // END CUT crawling expansion

        // BEGIN CUT debug
        // public int FailedExpansionCount { get; set; }
        // END CUT debug

        // BEGIN CUT neighbor tracking
        // public List<int> Neighbors = new List<int>();
        //else if ((neighborLeft.Region.ID != this.ID) && !Neighbors.Contains(neighborLeft.Region.ID))
        //{
        //    Neighbors.Add(neighborLeft.Region.ID);
        //    if (!neighborLeft.Region.Neighbors.Contains(this.ID))
        //    {
        //        neighborLeft.Region.Neighbors.Add(this.ID);
        //    }
        //}
        // END CUT neighbor tracking
        #endregion
    }
}
