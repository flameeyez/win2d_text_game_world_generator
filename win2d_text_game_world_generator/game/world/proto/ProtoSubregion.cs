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
            CheckAdjacentRooms(AvailableAdjacentRooms, AvailableAdjacentCoordinates, startingRoom, MasterRoomList);

            while (AvailableAdjacentRooms.Count > 0 && ((ProtoRooms.Count < Statics.MinimumRegionSize) || (Statics.Random.Next(100) < Statics.ProbabilityOfExpansion)))
            {
                // pick a random room from the available set
                ProtoRoom randomNeighbor = AvailableAdjacentRooms.RandomListItem();
                randomNeighbor.Available = false;
                randomNeighbor.ProtoRegion = region;
                randomNeighbor.ProtoSubregion = this;

                AvailableAdjacentRooms.Remove(randomNeighbor);
                AvailableAdjacentCoordinates.Remove(randomNeighbor.Coordinates);
                ProtoRooms.Add(randomNeighbor);

                // add new room's available neighbors to the available list
                CheckAdjacentRooms(AvailableAdjacentRooms, AvailableAdjacentCoordinates, randomNeighbor, MasterRoomList);
            }
        }
        private void CheckAdjacentRooms(List<ProtoRoom> AvailableAdjacentRooms, HashSet<PointInt> AvailableAdjacentCoordinates, ProtoRoom protoRoom, ProtoRoom[,] MasterRoomList)
        {
            // left
            if (protoRoom.Coordinates.X > 0)
            {
                ProtoRoom neighborLeft = MasterRoomList[protoRoom.Coordinates.X - 1, protoRoom.Coordinates.Y];
                if (neighborLeft.Available && !AvailableAdjacentCoordinates.Contains(neighborLeft.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborLeft);
                    AvailableAdjacentCoordinates.Add(neighborLeft.Coordinates);
                }
            }
            // right
            if (protoRoom.Coordinates.X < MasterRoomList.GetLength(0) - 1)
            {
                ProtoRoom neighborRight = MasterRoomList[protoRoom.Coordinates.X + 1, protoRoom.Coordinates.Y];
                if (neighborRight.Available && !AvailableAdjacentCoordinates.Contains(neighborRight.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborRight);
                    AvailableAdjacentCoordinates.Add(neighborRight.Coordinates);
                }
            }
            // above
            if (protoRoom.Coordinates.Y > 0)
            {
                ProtoRoom neighborAbove = MasterRoomList[protoRoom.Coordinates.X, protoRoom.Coordinates.Y - 1];
                if (neighborAbove.Available && !AvailableAdjacentCoordinates.Contains(neighborAbove.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborAbove);
                    AvailableAdjacentCoordinates.Add(neighborAbove.Coordinates);
                }
            }
            // below
            if (protoRoom.Coordinates.Y < MasterRoomList.GetLength(1) - 1)
            {
                ProtoRoom neighborBelow = MasterRoomList[protoRoom.Coordinates.X, protoRoom.Coordinates.Y + 1];
                if (neighborBelow.Available && !AvailableAdjacentCoordinates.Contains(neighborBelow.Coordinates))
                {
                    AvailableAdjacentRooms.Add(neighborBelow);
                    AvailableAdjacentCoordinates.Add(neighborBelow.Coordinates);
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
                if (room.Coordinates.X > 0 && MasterRoomList[(int)room.Coordinates.X - 1, (int)room.Coordinates.Y].Available) { return true; }

                // check right
                if (room.Coordinates.X < RoomCountX - 1 && MasterRoomList[(int)room.Coordinates.X + 1, (int)room.Coordinates.Y].Available) { return true; }

                // check up
                if (room.Coordinates.Y > 0 && MasterRoomList[(int)room.Coordinates.X, (int)room.Coordinates.Y - 1].Available) { return true; }

                // check down
                if (room.Coordinates.Y < RoomCountY - 1 && MasterRoomList[(int)room.Coordinates.X, (int)room.Coordinates.Y + 1].Available) { return true; }
            }

            return false;
        }
        public void ReindexRooms()
        {
            foreach (ProtoRoom room in ProtoRooms)
            {
                room.ProtoRegion = this.ProtoRegion;
                room.ProtoSubregion = this;
            }
        }
        #endregion

        #region Hash/Equality Overrides
        public override bool Equals(object obj)
        {
            ProtoSubregion compare = obj as ProtoSubregion;
            return compare.ID == this.ID;
        }
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
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
