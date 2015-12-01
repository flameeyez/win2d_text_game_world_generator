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

        private int TraversableTiles = 0;
        private int UntraversableTiles = 0;
        private float TraversableTilePercentage { get { return (float)TraversableTiles / TotalTiles; } }
        private float UntraversableTilePercentage { get { return (float)UntraversableTiles / TotalTiles; } }

        private HashSet<PointInt> TilesNotInMainPath;
        private HashSet<PointInt> OpenSet;
        private HashSet<PointInt> MainPath;

        private bool _aborted = false;
        public bool Aborted { get { return _aborted; } }

        #region Initialization
        public ProtoMap(int width, int height)
        {
            CalculateLayout(width, height);
            InitializeMasterTileList();
            CreateProtoRegions();
            FoldUndersizedRegions();
            MergeProtoRegions();
            ReindexSubregions();
            AssignSubregionColors();
            GenerateHeightMap();
            CalculateTraversability();
            CreateRoomConnections();
            if (!_aborted) { FixDisconnectedRooms(); }
            if (!_aborted) { FixCrossedPaths(); }
            if (!_aborted) { DebugValidation(); }
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
        private void GenerateHeightMap()
        {
            int[,] mountainMap = GenerateHeightMapMountains();
            int[,] waterMap = GenerateHeightMapWater();
            int[,] forestMap = GenerateHeightMapForest();
            // int[,] desertMap = GenerateHeightMapDesert();

            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    if (mountainMap[x, y] >= 27) { MasterTileList[x, y].Elevation = mountainMap[x, y]; }
                    else if (waterMap[x, y] == 25 || waterMap[x, y] == 26) { MasterTileList[x, y].Elevation = 1; }
                    else if (waterMap[x, y] > 26) { MasterTileList[x, y].Elevation = 0; }
                    else if (forestMap[x, y] == 30) { MasterTileList[x, y].Elevation = 13; }
                    // else if (desertMap[x, y] == 30) { MasterTileList[x, y].Elevation = 1; }
                }
            }
        }
        private void CalculateTraversability()
        {
            // could save time here by merging into last iteration of blur
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    if (MasterTileList[x, y].Elevation == 0 || MasterTileList[x, y].Elevation == 30) { UntraversableTiles++; }
                    else { TraversableTiles++; }
                }
            }
        }
        private void CreateRoomConnections()
        {
            int nNumberOfConnectionAttempts = 0;
            MainPath = new HashSet<PointInt>();
            // if fewer than 80% of land tiles are connected to main path, connect more tiles
            while (MainPath.Count < (TotalTiles * TraversableTilePercentage * 0.8f))
            {
                if (++nNumberOfConnectionAttempts == 5) { AbortConstruction(); return; }

                MainPath = new HashSet<PointInt>();

                for (int x = 0; x < WidthInTiles; x++)
                {
                    for (int y = 0; y < HeightInTiles; y++)
                    {
                        ProtoRoom currentRoom = MasterTileList[x, y];
                        if (currentRoom.Elevation == 0 || currentRoom.Elevation == 30) { continue; }

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
                    if (MasterTileList[x, y].Elevation > 0)
                    {
                        TilesNotInMainPath.Add(new PointInt(x, y));
                    }
                }
            }

            // initialize OpenSet with a random tile
            OpenSet = new HashSet<PointInt>();
            int initialX = Statics.Random.Next(WidthInTiles);
            int initialY = Statics.Random.Next(HeightInTiles);
            ProtoRoom protoRoom = MasterTileList[initialX, initialY];

            while (protoRoom.Elevation == 0)
            {
                initialX = Statics.Random.Next(WidthInTiles);
                initialY = Statics.Random.Next(HeightInTiles);
                protoRoom = MasterTileList[initialX, initialY];
            }

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
            int nFixAttempts = 0;
            while (MainPath.Count != TraversableTiles)
            {
                if (++nFixAttempts >= 5) { AbortConstruction(); return; }

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
        private void FixCrossedPaths()
        {
            for (int x = 0; x < WidthInTiles - 1; x++)
            {
                for (int y = 0; y < HeightInTiles - 1; y++)
                {
                    ProtoRoom topLeft = MasterTileList[x, y];
                    ProtoRoom topRight = MasterTileList[x + 1, y];
                    ProtoRoom bottomLeft = MasterTileList[x, y + 1];
                    ProtoRoom bottomRight = MasterTileList[x + 1, y + 1];

                    if (topLeft.DirectionalRoomConnections.Contains("se") && topRight.DirectionalRoomConnections.Contains("sw"))
                    {
                        switch (Statics.Random.Next(2))
                        {
                            case 0:
                                // remove se connection
                                topLeft.DirectionalRoomConnections.Remove("se");
                                bottomRight.DirectionalRoomConnections.Remove("nw");
                                switch (Statics.Random.Next(2))
                                {
                                    case 0:
                                        if (!topLeft.DirectionalRoomConnections.Contains("s"))
                                        {
                                            topLeft.DirectionalRoomConnections.Add("s");

                                            if (bottomLeft.DirectionalRoomConnections.Contains("n")) { throw new Exception(); }
                                            bottomLeft.DirectionalRoomConnections.Add("n");
                                        }
                                        if (!bottomLeft.DirectionalRoomConnections.Contains("e"))
                                        {
                                            bottomLeft.DirectionalRoomConnections.Add("e");

                                            if (bottomRight.DirectionalRoomConnections.Contains("w")) { throw new Exception(); }
                                            bottomRight.DirectionalRoomConnections.Add("w");
                                        }
                                        break;
                                    case 1:
                                        if (!topLeft.DirectionalRoomConnections.Contains("e"))
                                        {
                                            topLeft.DirectionalRoomConnections.Add("e");

                                            if (topRight.DirectionalRoomConnections.Contains("w")) { throw new Exception(); }
                                            topRight.DirectionalRoomConnections.Add("w");
                                        }
                                        if (!topRight.DirectionalRoomConnections.Contains("s"))
                                        {
                                            topRight.DirectionalRoomConnections.Add("s");

                                            if (bottomRight.DirectionalRoomConnections.Contains("n")) { throw new Exception(); }
                                            bottomRight.DirectionalRoomConnections.Add("n");
                                        }
                                        break;
                                }
                                break;
                            case 1:
                                // remove sw connection
                                topRight.DirectionalRoomConnections.Remove("sw");
                                bottomLeft.DirectionalRoomConnections.Remove("ne");
                                switch (Statics.Random.Next(1))
                                {
                                    case 0:
                                        if (!topRight.DirectionalRoomConnections.Contains("s"))
                                        {
                                            topRight.DirectionalRoomConnections.Add("s");

                                            if (bottomRight.DirectionalRoomConnections.Contains("n")) { throw new Exception(); }
                                            bottomRight.DirectionalRoomConnections.Add("n");
                                        }
                                        if (!bottomRight.DirectionalRoomConnections.Contains("w"))
                                        {
                                            bottomRight.DirectionalRoomConnections.Add("w");

                                            if (bottomLeft.DirectionalRoomConnections.Contains("e")) { throw new Exception(); }
                                            bottomLeft.DirectionalRoomConnections.Add("e");
                                        }
                                        break;
                                    case 1:
                                        if (!topRight.DirectionalRoomConnections.Contains("w"))
                                        {
                                            topRight.DirectionalRoomConnections.Add("w");

                                            if (topLeft.DirectionalRoomConnections.Contains("e")) { throw new Exception(); }
                                            topLeft.DirectionalRoomConnections.Add("e");
                                        }
                                        if (!topLeft.DirectionalRoomConnections.Contains("s"))
                                        {
                                            topLeft.DirectionalRoomConnections.Add("s");

                                            if (bottomLeft.DirectionalRoomConnections.Contains("n")) { throw new Exception(); }
                                            bottomLeft.DirectionalRoomConnections.Add("n");
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void DebugValidation()
        {
            // checks to ensure that rooms within a region/subregion actually point back to the containing region/subregion
            // would theoretically catch reassignment misses during merge
            if (!DebugValidateRoomOwnership()) { throw new Exception("Wut?!?"); }
            if (!DebugValidateConnectionMirroring()) { throw new Exception("For realz?!?"); }
            DebugCountConnections();
        }
        private void AbortConstruction()
        {
            _aborted = true;
        }
        #endregion

        #region Region/Room Operations
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
                if (bReindexSubregions)
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
            if (connectingRoom.Elevation == 0 || connectingRoom.Elevation == 30) { return currentRoom; }
            if (connectingRoom.DirectionalRoomConnections.Count >= Statics.MaxConnections && !bForce) { return currentRoom; }
            currentRoom.DirectionalRoomConnections.Add(strDirection);
            connectingRoom.DirectionalRoomConnections.Add(strOppositeDirection);
            return connectingRoom;
        }
        #endregion

        #region HeightMap
        private int[,] GenerateHeightMapMountains()
        {
            // mountain pass
            PerlinNoise pn = new PerlinNoise(WidthInTiles, HeightInTiles);
            Statics.fFrequency = 0.01f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            Statics.fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            Statics.fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            Statics.nOctaves = 1;// + Statics.Random.Next(5); // 5;
            int[,] mountainMap = new int[WidthInTiles, HeightInTiles];

            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
                    if (nElevation >= 27) { mountainMap[x, y] = nElevation; }
                }
            }

            Blur(mountainMap, 3);
            return mountainMap;
        }
        private int[,] GenerateHeightMapWater()
        {
            // water pass
            int[,] waterMap = new int[WidthInTiles, HeightInTiles];
            PerlinNoise pn = new PerlinNoise(WidthInTiles, HeightInTiles);
            Statics.fFrequency = 0.05f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            Statics.fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            Statics.fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            Statics.nOctaves = 1;// + Statics.Random.Next(5); // 5;
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
                    if (nElevation >= 25) { waterMap[x, y] = nElevation; }
                }
            }
            Blur(waterMap, 3);
            return waterMap;
        }
        private int[,] GenerateHeightMapForest()
        {
            // forest pass
            PerlinNoise pn = new PerlinNoise(WidthInTiles, HeightInTiles);
            Statics.fFrequency = 0.03f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            Statics.fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            Statics.fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            Statics.nOctaves = 1;// + Statics.Random.Next(5); // 5;
            int[,] forestMap = new int[WidthInTiles, HeightInTiles];

            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
                    if (nElevation >= 20) { forestMap[x, y] = 30; }
                }
            }

            Blur(forestMap, 3);
            return forestMap;
        }
        private int[,] GenerateHeightMapDesert()
        {
            // desert pass
            PerlinNoise pn = new PerlinNoise(WidthInTiles, HeightInTiles);
            Statics.fFrequency = 0.03f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            Statics.fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            Statics.fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            Statics.nOctaves = 1;// + Statics.Random.Next(5); // 5;
            int[,] desertMap = new int[WidthInTiles, HeightInTiles];

            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
                    if (nElevation >= 20) { desertMap[x, y] = 30; }
                }
            }

            Blur(desertMap, 3);
            return desertMap;
        }
        private void Blur(int[,] heightMap, int iterations)
        {
            Dictionary<int, float> FilterKernel = new Dictionary<int, float>();
            FilterKernel.Add(-3, 0.006f);
            FilterKernel.Add(-2, 0.061f);
            FilterKernel.Add(-1, 0.242f);
            FilterKernel.Add(0, 0.383f);
            FilterKernel.Add(1, 0.242f);
            FilterKernel.Add(2, 0.061f);
            FilterKernel.Add(3, 0.006f);

            for (int i = 0; i < iterations; i++)
            {
                float[,] fIntermediateBlurredHeightValues = new float[WidthInTiles, HeightInTiles];
                float[,] fFinalBlurredHeightValues = new float[WidthInTiles, HeightInTiles];

                for (int x = 0; x < WidthInTiles; x++)
                {
                    for (int y = 0; y < HeightInTiles; y++)
                    {
                        fIntermediateBlurredHeightValues[x, y] = ComputeXValue(heightMap, FilterKernel, x, y);
                    }
                }

                for (int x = 0; x < WidthInTiles; x++)
                {
                    for (int y = 0; y < HeightInTiles; y++)
                    {
                        fFinalBlurredHeightValues[x, y] = ComputeYValue(fIntermediateBlurredHeightValues, FilterKernel, x, y);
                    }
                }

                for (int x = 0; x < WidthInTiles; x++)
                {
                    for (int y = 0; y < HeightInTiles; y++)
                    {
                        heightMap[x, y] = (int)fFinalBlurredHeightValues[x, y];
                    }
                }
            }
        }
        private float ComputeXValue(int[,] heightMap, Dictionary<int, float> FilterKernel, int x, int y)
        {
            float fValue = 0.0f;

            foreach (KeyValuePair<int, float> kvp in FilterKernel)
            {
                int offset = kvp.Key;
                if (x + kvp.Key < 0) { offset = 0; }
                if (x + kvp.Key > WidthInTiles - 1) { offset = 0; }

                fValue += kvp.Value * heightMap[x + offset, y]; // MasterTileList[x + offset, y].Elevation;
            }

            return fValue;
        }
        private float ComputeYValue(float[,] fIntermediateBlurredHeightValues, Dictionary<int, float> FilterKernel, int x, int y)
        {
            float fValue = 0.0f;

            foreach (KeyValuePair<int, float> kvp in FilterKernel)
            {
                int offset = kvp.Key;
                if (y + kvp.Key < 0) { offset = 0; }
                if (y + kvp.Key > HeightInTiles - 1) { offset = 0; }

                fValue += kvp.Value * fIntermediateBlurredHeightValues[x, y + offset];
            }

            return fValue;
        }
        #endregion

        #region Debug
        private bool DebugValidateRoomOwnership()
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
        private bool DebugValidateConnectionMirroring()
        {
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    ProtoRoom currentRoom = MasterTileList[x, y];
                    ProtoRoom connectionRoom = null;

                    foreach (string strConnection in currentRoom.DirectionalRoomConnections)
                    {
                        switch (strConnection)
                        {
                            case "nw":
                                connectionRoom = MasterTileList[x - 1, y - 1];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("se")) { return false; }
                                break;
                            case "n":
                                connectionRoom = MasterTileList[x, y - 1];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("s")) { return false; }
                                break;
                            case "ne":
                                connectionRoom = MasterTileList[x + 1, y - 1];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("sw")) { return false; }
                                break;
                            case "w":
                                connectionRoom = MasterTileList[x - 1, y];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("e")) { return false; }
                                break;
                            case "e":
                                connectionRoom = MasterTileList[x + 1, y];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("w")) { return false; }
                                break;
                            case "sw":
                                connectionRoom = MasterTileList[x - 1, y + 1];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("ne")) { return false; }
                                break;
                            case "s":
                                connectionRoom = MasterTileList[x, y + 1];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("n")) { return false; }
                                break;
                            case "se":
                                connectionRoom = MasterTileList[x + 1, y + 1];
                                if (!connectionRoom.DirectionalRoomConnections.Contains("nw")) { return false; }
                                break;
                            default:
                                throw new Exception();
                        }
                    }
                }
            }
            return true;
        }
        private void DebugCountConnections()
        {
            for (int x = 0; x < WidthInTiles; x++)
            {
                for (int y = 0; y < HeightInTiles; y++)
                {
                    ProtoRoom protoRoom = MasterTileList[x, y];
                    foreach (string strConnection in protoRoom.DirectionalRoomConnections)
                    {
                        switch (strConnection)
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
        #endregion

        #region Cut Code
        //private void Agitate(ProtoRoom sourceRoom)
        //{
        //    int x = sourceRoom.Coordinates.X;
        //    int y = sourceRoom.Coordinates.Y;

        //    int nNumAttempts = 0;
        //    int nDirection = Statics.Random.Next(8);

        //    while (nNumAttempts < 8)
        //    {
        //        nNumAttempts++;
        //        switch (nDirection)
        //        {
        //            case 0:
        //                if (Agitate(sourceRoom, x - 1, y - 1)) { return; } // nw
        //                break;
        //            case 1:
        //                if (Agitate(sourceRoom, x, y - 1)) { return; }     // n
        //                break;
        //            case 2:
        //                if (Agitate(sourceRoom, x + 1, y - 1)) { return; } // ne
        //                break;
        //            case 3:
        //                if (Agitate(sourceRoom, x - 1, y)) { return; }     // w
        //                break;
        //            case 4:
        //                if (Agitate(sourceRoom, x + 1, y)) { return; }     // e
        //                break;
        //            case 5:
        //                if (Agitate(sourceRoom, x - 1, y + 1)) { return; } // sw
        //                break;
        //            case 6:
        //                if (Agitate(sourceRoom, x, y + 1)) { return; }     // s
        //                break;
        //            case 7:
        //                if (Agitate(sourceRoom, x + 1, y + 1)) { return; } // se
        //                break;
        //        }

        //        nDirection = (nDirection + 1) % 8;
        //    }
        //}
        //private bool Agitate(ProtoRoom sourceRoom, int x, int y)
        //{
        //    if (x < 0) { return false; }
        //    if (y < 0) { return false; }
        //    if (x > WidthInTiles - 1) { return false; }
        //    if (y > HeightInTiles - 1) { return false; }

        //    ProtoRoom targetRoom = MasterTileList[x, y];
        //    if (targetRoom.Elevation < sourceRoom.Elevation - Statics.HeightMapElevationFactor)
        //    {
        //        sourceRoom.Elevation -= Statics.HeightMapElevationFactor;
        //        targetRoom.Elevation += Statics.HeightMapElevationFactor;

        //        // roll the current elevation change downhill
        //        Agitate(targetRoom);
        //        return true;
        //    }

        //    return false;
        //}

        //private void BlurHeightMap(int iterations)
        //{
        //    // blurring
        //    Dictionary<int, float> FilterKernel = new Dictionary<int, float>();
        //    FilterKernel.Add(-3, 0.006f);
        //    FilterKernel.Add(-2, 0.061f);
        //    FilterKernel.Add(-1, 0.242f);
        //    FilterKernel.Add(0, 0.383f);
        //    FilterKernel.Add(1, 0.242f);
        //    FilterKernel.Add(2, 0.061f);
        //    FilterKernel.Add(3, 0.006f);

        //    for (int i = 0; i < iterations; i++)
        //    {
        //        float[,] fIntermediateBlurredHeightValues = new float[WidthInTiles, HeightInTiles];
        //        float[,] fFinalBlurredHeightValues = new float[WidthInTiles, HeightInTiles];

        //        for (int x = 0; x < WidthInTiles; x++)
        //        {
        //            for (int y = 0; y < HeightInTiles; y++)
        //            {
        //                fIntermediateBlurredHeightValues[x, y] = ComputeXValue(FilterKernel, x, y);
        //            }
        //        }

        //        for (int x = 0; x < WidthInTiles; x++)
        //        {
        //            for (int y = 0; y < HeightInTiles; y++)
        //            {
        //                fFinalBlurredHeightValues[x, y] = ComputeYValue(fIntermediateBlurredHeightValues, FilterKernel, x, y);
        //            }
        //        }

        //        for (int x = 0; x < WidthInTiles; x++)
        //        {
        //            for (int y = 0; y < HeightInTiles; y++)
        //            {
        //                MasterTileList[x, y].Elevation = (int)fFinalBlurredHeightValues[x, y];
        //            }
        //        }
        //    }
        //}
        //for (int x = 0; x < WidthInTiles; x++)
        //{
        //    for (int y = 0; y < HeightInTiles; y++)
        //    {
        //        int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
        //        if (nElevation >= 27) { MasterTileList[x, y].Elevation = nElevation; }
        //        // 0.1f, 1.2f, 0.5f, 5); // Statics.Random.Next(15);
        //        //MasterTileList[x, y].Elevation = 15 + (int)pn.GetRandomHeight(x, y, 15, 1.5f, 1.2f, 0.5f, 5);
        //    }
        //}

        //BlurHeightMap(1);



        //// forest pass
        //pn = new PerlinNoise(WidthInTiles, HeightInTiles);
        //Statics.fFrequency = 0.02f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
        //Statics.fAmplitude = 3.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
        //Statics.fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
        //Statics.nOctaves = 1;// + Statics.Random.Next(5); // 5;
        //for (int x = 0; x < WidthInTiles; x++)
        //{
        //    for (int y = 0; y < HeightInTiles; y++)
        //    {
        //        int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
        //        if (nElevation >= 27 && MasterTileList[x, y].Elevation == 3) { MasterTileList[x, y].Elevation = 50; }
        //    }
        //}

        //// final blur
        //BlurHeightMap(2);
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