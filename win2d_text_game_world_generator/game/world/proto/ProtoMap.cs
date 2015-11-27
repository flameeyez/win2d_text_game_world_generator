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
        public int TotalTiles { get { return WidthInTiles * HeightInTiles; } }

        private HashSet<PointInt> TilesNotInMainPath;
        private HashSet<PointInt> OpenSet;
        private HashSet<PointInt> MainPath;

        public int DebugFixLoopCount = 0;

        #region Initialization
        public ProtoMap(int width, int height)
        {
            CalculateLayout(width, height);
            InitializeMasterTileList();
            CreateProtoRegions();
            FoldUndersizedRegions();
            MergeProtoRegions();
            ReindexSubregions();
            DebugValidation();
            AssignSubregionColors();
            CreateRoomConnections();
            FixDisconnectedRooms();

            DebugReporting();
        }
        private void CalculateLayout(int width, int height)
        {
            Position = Statics.MapPosition;

            // stretched layout
            // WidthInPixels = Statics.CanvasWidth - Statics.Padding * 2;
            // HeightInPixels = Statics.CanvasHeight - Statics.Padding * 2;

            // parameterized layout
            WidthInPixels = width - Statics.Padding * 2;
            HeightInPixels = height - Statics.Padding * 2;
        }
        private void InitializeMasterTileList()
        {
            // initialize master array of tiles
            MasterTileList = new ProtoRoom[WidthInTiles, HeightInTiles];
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    MasterTileList[x, y] = new ProtoRoom(new PointInt(x, y));
                }
            }
        }
        private void CreateProtoRegions()
        {
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
        }
        private void FoldUndersizedRegions()
        {
            // all rooms are now unavailable
            // all regions contain one subregion that contains all region rooms
            // we're left with a swath of tiny regions

            // pass 1: fold tiny regions into neighbors
            // result is that all regions still only have one subregion, but regions are guaranteed to be a certain size
            MergeRegions(MasterTileList, 100, false);
        }
        private void MergeProtoRegions()
        {
            // pass 2: fold regions into each other (as subregions)
            // result is fewer regions that now contain multiple subregions
            MergeRegions(MasterTileList, 2000, true);
        }
        private void ReindexSubregions()
        {
            ReindexRegions(0, true);
        }
        private void DebugValidation()
        {
            // checks to ensure that rooms within a region/subregion actually point back to the containing region/subregion
            // would theoretically catch reassignment misses during merge
            if (!DebugValidateMapIntegrity()) { throw new Exception("Wut?!?"); }
        }
        private void AssignSubregionColors()
        {
            foreach (ProtoRegion pr in ProtoRegions)
            {
                foreach (ProtoSubregion ps in pr.ProtoSubregions)
                {
                    int nVariance = 20 - Statics.Random.Next(41);
                    while (Math.Abs(nVariance) < 5) { nVariance = 20 - Statics.Random.Next(41); }

                    int r = pr.Color.R + nVariance;
                    if (r < 0) { r = 0; }
                    else if (r > 255) { r = 255; }

                    int g = pr.Color.G + nVariance;
                    if (g < 0) { g = 0; }
                    else if (g > 255) { g = 255; }

                    int b = pr.Color.B + nVariance;
                    if (b < 0) { b = 0; }
                    else if (b > 255) { b = 255; }

                    ps.Color = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
                }
            }
        }
        private void CreateRoomConnections()
        {
            MainPath = new HashSet<PointInt>();
            while (MainPath.Count < ((int)TotalTiles * 0.8))
            {
                MainPath = new HashSet<PointInt>();

                for (int x = 0; x < WidthInTiles; x++)
                {
                    for (int y = 0; y < HeightInTiles; y++)
                    {
                        ProtoRoom currentRoom = MasterTileList[x, y];

                        for (int i = 0; i < 10; i++)
                        {
                            if (currentRoom.DirectionalRoomConnections.Count == Statics.MaxConnections) { break; }

                            switch (Statics.Random.Next(8))
                            {
                                case 0: currentRoom = AddRoomConnection(currentRoom, "nw"); break;
                                case 1: currentRoom = AddRoomConnection(currentRoom, "n"); break;
                                case 2: currentRoom = AddRoomConnection(currentRoom, "ne"); break;
                                case 3: currentRoom = AddRoomConnection(currentRoom, "w"); break;
                                case 4: currentRoom = AddRoomConnection(currentRoom, "e"); break;
                                case 5: currentRoom = AddRoomConnection(currentRoom, "sw"); break;
                                case 6: currentRoom = AddRoomConnection(currentRoom, "s"); break;
                                case 7: currentRoom = AddRoomConnection(currentRoom, "se"); break;
                                default: break;
                            }
                        }
                    }
                }

                AssessRoomConnectivity();
            }
        }
        private void AssessRoomConnectivity()
        {
            // initialize TilesNotInMainPath
            TilesNotInMainPath = new HashSet<PointInt>();
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    TilesNotInMainPath.Add(new PointInt(x, y));
                }
            }

            // initialize OpenSet with a random tile
            OpenSet = new HashSet<PointInt>();
            int initialX = Statics.Random.Next(WidthInTiles);
            int initialY = Statics.Random.Next(HeightInTiles);
            OpenSet.Add(new PointInt(initialX, initialY));

            while (OpenSet.Count > 0)
            {
                PointInt currentCoordinates = OpenSet.ElementAt(0);
                OpenSet.Remove(currentCoordinates);
                TilesNotInMainPath.Remove(currentCoordinates);
                MainPath.Add(currentCoordinates);

                foreach (string strConnection in MasterTileList[currentCoordinates.X, currentCoordinates.Y].DirectionalRoomConnections)
                {
                    PointInt connectingCoordinates;
                    switch (strConnection)
                    {
                        case "nw":
                            connectingCoordinates = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y - 1);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "n":
                            connectingCoordinates = new PointInt(currentCoordinates.X, currentCoordinates.Y - 1);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "ne":
                            connectingCoordinates = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y - 1);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "w":
                            connectingCoordinates = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "e":
                            connectingCoordinates = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "sw":
                            connectingCoordinates = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y + 1);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "s":
                            connectingCoordinates = new PointInt(currentCoordinates.X, currentCoordinates.Y + 1);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                        case "se":
                            connectingCoordinates = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y + 1);
                            if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                            {
                                OpenSet.Add(connectingCoordinates);
                            }
                            break;
                    }
                }
            }
        }
        private void FixDisconnectedRooms()
        {
            while (MainPath.Count != TotalTiles)
            {
                DebugFixLoopCount++;

                for (int i = TilesNotInMainPath.Count - 1; i >= 0; i--)
                {
                    PointInt currentCoordinates = TilesNotInMainPath.ElementAt(i);
                    ProtoRoom protoRoom = MasterTileList[currentCoordinates.X, currentCoordinates.Y];

                    // check neighbors to see if they're in main path
                    // nw
                    if (currentCoordinates.X > 0 && currentCoordinates.Y > 0)
                    {
                        PointInt neighborNW = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y - 1);
                        if (MainPath.Contains(neighborNW))
                        {
                            AddRoomConnection(protoRoom, "nw", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // n
                    if (currentCoordinates.Y > 0)
                    {
                        PointInt neighborN = new PointInt(currentCoordinates.X, currentCoordinates.Y - 1);
                        if (MainPath.Contains(neighborN))
                        {
                            AddRoomConnection(protoRoom, "n", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // ne
                    if (currentCoordinates.X < WidthInTiles - 1 && currentCoordinates.Y > 0)
                    {
                        PointInt neighborNE = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y - 1);
                        if (MainPath.Contains(neighborNE))
                        {
                            AddRoomConnection(protoRoom, "ne", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // w
                    if (currentCoordinates.X > 0)
                    {
                        PointInt neighborW = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y);
                        if (MainPath.Contains(neighborW))
                        {
                            AddRoomConnection(protoRoom, "w", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // e
                    if (currentCoordinates.X < WidthInTiles - 1)
                    {
                        PointInt neighborE = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y);
                        if (MainPath.Contains(neighborE))
                        {
                            AddRoomConnection(protoRoom, "e", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // sw
                    if (currentCoordinates.X > 0 && currentCoordinates.Y < HeightInTiles - 1)
                    {
                        PointInt neighborSW = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y + 1);
                        if (MainPath.Contains(neighborSW))
                        {
                            AddRoomConnection(protoRoom, "sw", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // s
                    if (currentCoordinates.Y < HeightInTiles - 1)
                    {
                        PointInt neighborS = new PointInt(currentCoordinates.X, currentCoordinates.Y + 1);
                        if (MainPath.Contains(neighborS))
                        {
                            AddRoomConnection(protoRoom, "s", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }

                    // se
                    if (currentCoordinates.X < WidthInTiles - 1 && currentCoordinates.Y < HeightInTiles - 1)
                    {
                        PointInt neighborSE = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y + 1);
                        if (MainPath.Contains(neighborSE))
                        {
                            AddRoomConnection(protoRoom, "se", true);
                            TilesNotInMainPath.Remove(currentCoordinates);
                            MainPath.Add(currentCoordinates);
                            continue;
                        }
                    }
                }
            }
        }

        private void DebugReporting()
        {
            for(int x = 0; x < WidthInTiles; x++)
            {
                for(int y = 0; y < HeightInTiles; y++)
                {
                    ProtoRoom protoRoom = MasterTileList[x, y];
                    foreach(string strConnection in protoRoom.DirectionalRoomConnections)
                    {
                        switch(strConnection)
                        {
                            case "nw": Statics.DebugNWConnectionCount++; break;
                            case "n": Statics.DebugNConnectionCount++; break;
                            case "ne": Statics.DebugNEConnectionCount++; break;
                            case "w": Statics.DebugWConnectionCount++; break;
                            case "e": Statics.DebugEConnectionCount++; break;
                            case "sw": Statics.DebugSWConnectionCount++; break;
                            case "s": Statics.DebugSConnectionCount++; break;
                            case "se": Statics.DebugSEConnectionCount++; break;
                        }
                    }
                }
            }
        }

        private ProtoRoom AddRoomConnection(ProtoRoom currentRoom, string strDirection, bool bForce = false)
        {
            if (currentRoom.DirectionalRoomConnections.Contains(strDirection)) { return currentRoom; }

            string strOppositeDirection = string.Empty;
            PointInt connectingRoomCoordinates = null;
            switch (strDirection)
            {
                case "nw":
                    strOppositeDirection = "se";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X - 1, currentRoom.Coordinates.Y - 1);
                    break;
                case "n":
                    strOppositeDirection = "s";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X, currentRoom.Coordinates.Y - 1);
                    break;
                case "ne":
                    strOppositeDirection = "sw";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X + 1, currentRoom.Coordinates.Y - 1);
                    break;
                case "w":
                    strOppositeDirection = "e";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X - 1, currentRoom.Coordinates.Y);
                    break;
                case "e":
                    strOppositeDirection = "w";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X + 1, currentRoom.Coordinates.Y);
                    break;
                case "sw":
                    strOppositeDirection = "ne";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X - 1, currentRoom.Coordinates.Y + 1);
                    break;
                case "s":
                    strOppositeDirection = "n";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X, currentRoom.Coordinates.Y + 1);
                    break;
                case "se":
                    strOppositeDirection = "nw";
                    connectingRoomCoordinates = new PointInt(currentRoom.Coordinates.X + 1, currentRoom.Coordinates.Y + 1);
                    break;
                default:
                    throw new Exception();
            }

            if (connectingRoomCoordinates.X < 0) { return currentRoom; }
            if (connectingRoomCoordinates.X > WidthInTiles - 1) { return currentRoom; }
            if (connectingRoomCoordinates.Y < 0) { return currentRoom; }
            if (connectingRoomCoordinates.Y > HeightInTiles - 1) { return currentRoom; }

            ProtoRoom connectingRoom = MasterTileList[connectingRoomCoordinates.X, connectingRoomCoordinates.Y];
            if (connectingRoom.DirectionalRoomConnections.Count >= Statics.MaxConnections && !bForce) { return currentRoom; }
            currentRoom.DirectionalRoomConnections.Add(strDirection);
            connectingRoom.DirectionalRoomConnections.Add(strOppositeDirection);
            return connectingRoom;
        }
        #endregion

        #region Region Operations
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

        private void ReindexRegions(int nStartingIndex, bool bReindexSubregions = false)
        {
            for (int i = nStartingIndex; i < ProtoRegions.Count; i++)
            {
                ProtoRegions[i].ID = i;
                if(bReindexSubregions)
                {
                    for (int j = 0; j < ProtoRegions[i].ProtoSubregions.Count; j++)
                    {
                        ProtoRegions[i].ProtoSubregions[j].ID = j;
                        ProtoRegions[i].ProtoSubregions[j].ProtoRegion = ProtoRegions[i];
                        ProtoRegions[i].ProtoSubregions[j].ReindexRooms();
                    }
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
        //private ProtoRoom AddRoomConnection(ProtoRoom sourceProtoRoom, Dictionary<PointInt, ProtoRoom> protoRoomsNeedingConnections, Dictionary<PointInt, ProtoRoom> protoRoomsWithConnections, string strDirection)
        //{
        //    if (strDirection == "o") { return sourceProtoRoom; }

        //    string strOppositeDirection = string.Empty;
        //    PointInt connectingRoomCoordinates = null;
        //    switch (strDirection)
        //    {
        //        case "nw":
        //            strOppositeDirection = "se";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X - 1, sourceProtoRoom.Coordinates.Y - 1);
        //            break;
        //        case "n":
        //            strOppositeDirection = "s";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X, sourceProtoRoom.Coordinates.Y - 1);
        //            break;
        //        case "ne":
        //            strOppositeDirection = "sw";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X + 1, sourceProtoRoom.Coordinates.Y - 1);
        //            break;
        //        case "w":
        //            strOppositeDirection = "e";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X - 1, sourceProtoRoom.Coordinates.Y);
        //            break;
        //        case "e":
        //            strOppositeDirection = "w";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X + 1, sourceProtoRoom.Coordinates.Y);
        //            break;
        //        case "sw":
        //            strOppositeDirection = "ne";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X - 1, sourceProtoRoom.Coordinates.Y + 1);
        //            break;
        //        case "s":
        //            strOppositeDirection = "n";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X, sourceProtoRoom.Coordinates.Y + 1);
        //            break;
        //        case "se":
        //            strOppositeDirection = "nw";
        //            connectingRoomCoordinates = new PointInt(sourceProtoRoom.Coordinates.X + 1, sourceProtoRoom.Coordinates.Y + 1);
        //            break;
        //        default:
        //            throw new Exception();
        //    }

        //    if (connectingRoomCoordinates.X < 0) { return sourceProtoRoom; }
        //    if (connectingRoomCoordinates.X > MasterTileList.GetLength(0) - 1) { return sourceProtoRoom; }
        //    if (connectingRoomCoordinates.Y < 0) { return sourceProtoRoom; }
        //    if (connectingRoomCoordinates.Y > MasterTileList.GetLength(1) - 1) { return sourceProtoRoom; }

        //    sourceProtoRoom.DirectionalRoomConnections.Add(strDirection);

        //    // add the corresponding opposite exit to the connecting room
        //    ProtoRoom connectingRoom = null;
        //    protoRoomsNeedingConnections.TryGetValue(connectingRoomCoordinates, out connectingRoom);
        //    if (connectingRoom != null)
        //    {
        //        protoRoomsNeedingConnections.Remove(connectingRoomCoordinates);
        //        protoRoomsWithConnections.Add(connectingRoomCoordinates, connectingRoom);
        //    }
        //    else
        //    {
        //        protoRoomsWithConnections.TryGetValue(connectingRoomCoordinates, out connectingRoom);
        //        if (connectingRoom == null)
        //        {
        //            throw new Exception("A room wasn't found in either connection dictionary!");
        //        }
        //    }

        //    // should have a connectingRoom here
        //    connectingRoom.DirectionalRoomConnections.Add(strOppositeDirection);
        //    return connectingRoom;
        //}
        // create a main path from 0,0 to max,max, adding branches along the way


        // foreach branch, keep going randomly, adding additional random branches
        // stop condition?


        //try
        //{
        //    Dictionary<PointInt, ProtoRoom> ProtoRoomsWithConnections = new Dictionary<PointInt, ProtoRoom>();
        //    Dictionary<PointInt, ProtoRoom> ProtoRoomsNeedingConnections = new Dictionary<PointInt, ProtoRoom>();

        //    // create dictionary out of mastertilelist
        //    for (int x = 0; x < MasterTileList.GetLength(0); x++)
        //    {
        //        for (int y = 0; y < MasterTileList.GetLength(1); y++)
        //        {
        //            ProtoRoomsNeedingConnections.Add(MasterTileList[x, y].Coordinates, MasterTileList[x, y]);
        //        }
        //    }

        //    ProtoRoom sourceProtoRoom = ProtoRoomsNeedingConnections.Values.ElementAt(Statics.Random.Next(ProtoRoomsNeedingConnections.Keys.Count));
        //    ProtoRoomsNeedingConnections.Remove(sourceProtoRoom.Coordinates);
        //    ProtoRoomsWithConnections.Add(sourceProtoRoom.Coordinates, sourceProtoRoom);

        //    while (ProtoRoomsNeedingConnections.Count > 0)
        //    {
        //        if(sourceProtoRoom.DirectionalRoomConnections.Count == 8)
        //        {
        //            int i = 0;
        //            i++;
        //        }

        //        // make a random connection
        //        switch (Statics.Random.Next(8))
        //        {
        //            case 0:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("nw"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "nw");
        //                }
        //                break;
        //            case 1:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("n"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "n");
        //                }
        //                break;
        //            case 2:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("ne"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "ne");
        //                }
        //                break;
        //            case 3:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("w"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "w");
        //                }
        //                break;
        //            case 4:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("e"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "e");
        //                }
        //                break;
        //            case 5:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("sw"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "sw");
        //                }
        //                break;
        //            case 6:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("s"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "s");
        //                }
        //                break;
        //            case 7:
        //                if (!sourceProtoRoom.DirectionalRoomConnections.Contains("se"))
        //                {
        //                    sourceProtoRoom = AddRoomConnection(sourceProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "se");
        //                }
        //                break;
        //        }

        //        // set the connecting room as the new source room

        //        // if (Statics.Random.Next(5) == 0) {  }
        //        // if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "n"); }
        //        // if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "ne"); }
        //        //if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "w"); }
        //        //if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "e"); }
        //        //if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "sw"); }
        //        //if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "s"); }
        //        //if (Statics.Random.Next(5) == 0) { AddRoomConnection(randomProtoRoom, ProtoRoomsNeedingConnections, ProtoRoomsWithConnections, "se"); }
        //    }
        //}
        //catch (Exception e)
        //{
        //    int i = 0;
        //    i++;
        //}
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