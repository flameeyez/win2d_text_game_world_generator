﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace win2d_text_game_world_generator
{
    enum DIRECTIONAL_INT_CLOCKWISE { NW = 0, N = 1, NE = 2, E = 3, SE = 4, S = 5, SW = 6, W = 7 };

    public class ProtoWorld
    {
        public List<ProtoRegion> ProtoRegions = new List<ProtoRegion>();
        public List<ProtoRegion> ProtoCaves = new List<ProtoRegion>();
        public ProtoRoom[,] MasterOvergroundRoomList;
        public ProtoRoom[,] MasterUndergroundRoomList;
        public int Width { get; set; }
        public int Height { get; set; }
        public int TotalRooms { get { return Width * Height; } }
        private int nNextRegionId = 0;

        private HashSet<PointInt> RoomsNotInMainPath;
        private HashSet<PointInt> OpenSet;
        private HashSet<PointInt> MainPath;

        private HashSet<PointInt> TraversableRooms;
        private HashSet<PointInt> NonTraversableRooms;

        private HashSet<PointInt> RoomsWithMaximumConnections;

        public CanvasRenderTarget RenderTargetRegions { get; set; }
        public CanvasRenderTarget RenderTargetSubregions { get; set; }
        public CanvasRenderTarget RenderTargetPaths { get; set; }
        public CanvasRenderTarget RenderTargetHeightMap { get; set; }
        public CanvasRenderTarget RenderTargetCaves { get; set; }
        public CanvasRenderTarget RenderTargetCavePaths { get; set; }

        private bool _aborted = false;
        public bool Aborted { get { return _aborted; } }
        private void AbortConstruction() { _aborted = true; }

        #region Initialization
        public ProtoWorld(CanvasDevice device, int width, int height, IProgress<Tuple<string, float>> progress)
        {
            Debug.TotalConnectionCount = 0;

            Width = width;
            Height = height;

            progress.Report(new Tuple<string, float>("Initializing grid...", (float)1 / 16));
            InitializeMasterRoomLists();
            progress.Report(new Tuple<string, float>("Creating subregions...", (float)2 / 16));
            CreateProtoRegions();
            progress.Report(new Tuple<string, float>("Folding-in undersized subregions...", (float)3 / 16));
            FoldUndersizedRegions();
            progress.Report(new Tuple<string, float>("Merging subregions into regions...", (float)4 / 16));
            MergeProtoRegions();
            progress.Report(new Tuple<string, float>("Reindexing regions...", (float)5 / 16));
            ReindexSubregions();
            progress.Report(new Tuple<string, float>("Assigning region/subregion colors...", (float)6 / 16));
            AssignSubregionColors();
            progress.Report(new Tuple<string, float>("Generating heightmap data...", (float)7 / 16));
            GenerateHeightMap();
            progress.Report(new Tuple<string, float>("Calculating land traversability...", (float)7 / 16));
            CalculateTraversability();
            progress.Report(new Tuple<string, float>("Connecting rooms...", (float)9 / 16));

            Statics.TotalNumberOfPaths = 1000;
            Statics.DesiredConnectionsPerPath = 25;

            Stopwatch s = Stopwatch.StartNew();
            CreateRoomConnections();
            s.Stop();

            Debug.CreateRoomConnectionsTime = s.ElapsedMilliseconds;

            if (!_aborted)
            {
                progress.Report(new Tuple<string, float>("Removing disconnected paths...", (float)10 / 16));
                RemoveDisconnectedRooms();
            }
            if (!_aborted)
            {
                progress.Report(new Tuple<string, float>("Fixing overly-crossed paths...", (float)11 / 16));
                FixCrossedPaths();
            }
            //if (!_aborted)
            //{
            //    progress.Report(new Tuple<string, float>("Creating caves...", (float)12 / 16));
            //    CreateCaves();
            //}
            //if (!_aborted)
            //{
            //    progress.Report(new Tuple<string, float>("Connecting caves to overground...", (float)13 / 16));
            //    ConnectCavesToOverground();
            //}
            //if (!_aborted)
            //{
            //    progress.Report(new Tuple<string, float>("Creating cave paths...", (float)14 / 16));
            //    CreateCavePaths();
            //}
            if (!_aborted)
            {
                progress.Report(new Tuple<string, float>("Saving world images...", (float)15 / 16));
                SaveWorldImages(device);
            }

            progress.Report(new Tuple<string, float>("Done!", (float)16 / 16));
        }

        private void CalculateTraversability()
        {
            TraversableRooms = new HashSet<PointInt>();
            NonTraversableRooms = new HashSet<PointInt>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (MasterOvergroundRoomList[x, y].IsTraversable()) { TraversableRooms.Add(MasterOvergroundRoomList[x, y].CoordinatesXY); }
                    else { NonTraversableRooms.Add(MasterOvergroundRoomList[x, y].CoordinatesXY); }
                }
            }
        }

        private void InitializeMasterRoomLists()
        {
            // initialize master array of tiles
            MasterOvergroundRoomList = new ProtoRoom[Width, Height];
            MasterUndergroundRoomList = new ProtoRoom[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    MasterOvergroundRoomList[x, y] = new ProtoRoom(-1, new PointInt(x, y));
                    MasterUndergroundRoomList[x, y] = new ProtoRoom(-1, new PointInt(x, y));
                }
            }
        }

        private void CreateProtoRegions()
        {
            // now we have a grid of available/unavailable [proto]rooms (tiles/pixels)
            // while we have available rooms, create regions, each with a single subregion
            int AvailableTileCount = Width * Height;
            while (AvailableTileCount > 0)
            {
                ProtoRegion protoRegion = new ProtoRegion(nNextRegionId++, MasterOvergroundRoomList);
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
            MergeRegions(MasterOvergroundRoomList, 100, false);
        }
        private void MergeProtoRegions()
        {
            // pass 2: fold regions into each other (as subregions)
            // result is fewer regions that now contain multiple subregions
            MergeRegions(MasterOvergroundRoomList, 2000, true);
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

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (mountainMap[x, y] >= 27) { MasterOvergroundRoomList[x, y].Elevation = mountainMap[x, y]; }
                    else if (waterMap[x, y] == 25 || waterMap[x, y] == 26) { MasterOvergroundRoomList[x, y].Elevation = 1; }
                    else if (waterMap[x, y] > 26) { MasterOvergroundRoomList[x, y].Elevation = 0; }
                    else if (forestMap[x, y] == 30) { MasterOvergroundRoomList[x, y].Elevation = 13; }
                    // else if (desertMap[x, y] == 30) { MasterOvergroundRoomList[x, y].Elevation = 1; }
                }
            }
        }

        private void CreateRoomConnections()
        {
            Debug.CreateRoomConnectionsCount = 0;

            while (MainPath == null || MainPath.Count < TraversableRooms.Count * 0.4f)
            {
                Debug.CreateRoomConnectionsCount++;

                for (int i = 0; i < Statics.TotalNumberOfPaths; i++)
                {
                    ProtoRoom currentRoom = null;
                    while (currentRoom == null || !currentRoom.IsTraversable() || currentRoom.HasMaximumConnections)
                    {
                        int x = Statics.Random.Next(Width);
                        int y = Statics.Random.Next(Height);
                        currentRoom = MasterOvergroundRoomList[x, y];
                    }

                    // pick a random initial direction
                    Array directionalValues = Enum.GetValues(typeof(DIRECTIONAL_INT_CLOCKWISE));
                    DIRECTIONAL_INT_CLOCKWISE direction = (DIRECTIONAL_INT_CLOCKWISE)directionalValues.GetValue(Statics.Random.Next(directionalValues.Length));
                    string strDirection = Statics.IntToClockwiseDirectionalString[(int)direction];

                    for (int j = 0; j < Statics.DesiredConnectionsPerPath; j++)
                    {
                        // initial turning direction
                        int delta = Statics.Random.Next(2) == 0 ? 1 : -1;

                        // for each connection, add a chance that the path turns slightly
                        if (Statics.Random.Next(3) == 0)
                        {
                            int nDirection = ((int)direction + delta) % directionalValues.Length;
                            if (nDirection < 0) { nDirection += directionalValues.Length; }
                            else if (nDirection == directionalValues.Length) { nDirection = 0; }
                            direction = (DIRECTIONAL_INT_CLOCKWISE)nDirection;
                        }

                        strDirection = Statics.IntToClockwiseDirectionalString[(int)direction];
                        ProtoRoom nextRoom = AddRoomConnection(currentRoom, strDirection, MasterOvergroundRoomList);
                        int nConnectionAttempts = 0;
                        while (nextRoom.Equals(currentRoom) && nConnectionAttempts < directionalValues.Length)
                        {
                            nConnectionAttempts++;

                            // connection unavailable; turn and try again
                            int nDirection = ((int)direction + delta) % directionalValues.Length;
                            if (nDirection < 0) { nDirection += directionalValues.Length; }
                            else if (nDirection == directionalValues.Length) { nDirection = 0; }
                            direction = (DIRECTIONAL_INT_CLOCKWISE)nDirection;

                            strDirection = Statics.IntToClockwiseDirectionalString[(int)direction];
                            nextRoom = AddRoomConnection(currentRoom, strDirection, MasterOvergroundRoomList);
                        }

                        currentRoom = nextRoom;
                        int nWalkCount = 0;
                        while (currentRoom.HasMaximumConnections && nWalkCount < 10)
                        {
                            nWalkCount++;

                            // walk a random connection until we hit a room that can handle another connection
                            Tuple<int, int, int> connection = currentRoom.DirectionalRoomConnections.Values.ToList().RandomListItem();
                            currentRoom = ProtoRegions[connection.Item1].ProtoSubregions[connection.Item2].ProtoRooms[connection.Item3];
                        }

                        // abort on edge-case walking loop
                        if (nWalkCount == 10) { break; }
                    }
                }

                AssessRoomConnectivity();
            }
        }
        private void CreateRoomConnections2()
        {
            int CreateRoomConnectionsCount = 0;

            // if too few land tiles are connected to main path, attempt to connect more tiles
            while (MainPath == null || MainPath.Count < (TraversableRooms.Count * 0.7f))
            {
                if (++CreateRoomConnectionsCount == Statics.MaxPathConnectionAttempts) { AbortConstruction(); return; }

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        ProtoRoom currentRoom = MasterOvergroundRoomList[x, y];
                        if (!currentRoom.IsTraversable()) { continue; }

                        for (int i = 0; i < 1; i++)
                        {
                            if (currentRoom.HasMaximumConnections) { break; }

                            switch (Statics.Random.Next(8))
                            {
                                case 0: currentRoom = AddRoomConnection(currentRoom, "nw", MasterOvergroundRoomList); break;
                                case 1: currentRoom = AddRoomConnection(currentRoom, "n", MasterOvergroundRoomList); break;
                                case 2: currentRoom = AddRoomConnection(currentRoom, "ne", MasterOvergroundRoomList); break;
                                case 3: currentRoom = AddRoomConnection(currentRoom, "w", MasterOvergroundRoomList); break;
                                case 4: currentRoom = AddRoomConnection(currentRoom, "e", MasterOvergroundRoomList); break;
                                case 5: currentRoom = AddRoomConnection(currentRoom, "sw", MasterOvergroundRoomList); break;
                                case 6: currentRoom = AddRoomConnection(currentRoom, "s", MasterOvergroundRoomList); break;
                                case 7: currentRoom = AddRoomConnection(currentRoom, "se", MasterOvergroundRoomList); break;
                                default: break;
                            }
                        }
                    }
                }

                AssessRoomConnectivity();
            }
        }
        private void RemoveDisconnectedRooms()
        {
            foreach (PointInt pi in RoomsNotInMainPath)
            {
                ProtoRoom pr = MasterOvergroundRoomList[pi.X, pi.Y];

                // remove connections to other rooms (for a clean map draw)
                for (int i = pr.DirectionalRoomConnections.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<string, Tuple<int, int, int>> connection = pr.DirectionalRoomConnections.ElementAt(i);
                    PointInt neighborCoordinates = GetNeighborCoordinates(pi, connection.Key);
                    ProtoRoom neighborRoom = MasterOvergroundRoomList[neighborCoordinates.X, neighborCoordinates.Y];
                    bool bRemoved = neighborRoom.DirectionalRoomConnections.Remove(Statics.GetOppositeDirection(connection.Key));

                    // BEGIN DEBUG
                    if (!bRemoved) { int j = 0; j++; }
                    // END DEBUG

                    pr.DirectionalRoomConnections.Remove(connection.Key);
                }

                // remove from subregion
                // pr.ProtoSubregion.ProtoRooms.Remove(pr);
            }
            return;
        }
        private void FixCrossedPaths()
        {
            for (int x = 0; x < Width - 1; x++)
            {
                for (int y = 0; y < Height - 1; y++)
                {
                    ProtoRoom topLeft = MasterOvergroundRoomList[x, y];
                    ProtoRoom topRight = MasterOvergroundRoomList[x + 1, y];
                    ProtoRoom bottomLeft = MasterOvergroundRoomList[x, y + 1];
                    ProtoRoom bottomRight = MasterOvergroundRoomList[x + 1, y + 1];

                    Tuple<int, int, int> connectionWorldCoordinates;
                    if (topLeft.DirectionalRoomConnections.TryGetValue("se", out connectionWorldCoordinates) && topRight.DirectionalRoomConnections.TryGetValue("sw", out connectionWorldCoordinates))
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
                                        if (!topLeft.DirectionalRoomConnections.TryGetValue("s", out connectionWorldCoordinates))
                                        {
                                            topLeft.DirectionalRoomConnections.Add("s", bottomLeft.WorldCoordinatesAsTuple);

                                            if (bottomLeft.DirectionalRoomConnections.TryGetValue("n", out connectionWorldCoordinates)) { throw new Exception(); }
                                            bottomLeft.DirectionalRoomConnections.Add("n", topLeft.WorldCoordinatesAsTuple);
                                        }
                                        if (!bottomLeft.DirectionalRoomConnections.TryGetValue("e", out connectionWorldCoordinates))
                                        {
                                            bottomLeft.DirectionalRoomConnections.Add("e", bottomRight.WorldCoordinatesAsTuple);

                                            if (bottomRight.DirectionalRoomConnections.TryGetValue("w", out connectionWorldCoordinates)) { throw new Exception(); }
                                            bottomRight.DirectionalRoomConnections.Add("w", bottomLeft.WorldCoordinatesAsTuple);
                                        }
                                        break;
                                    case 1:
                                        if (!topLeft.DirectionalRoomConnections.TryGetValue("e", out connectionWorldCoordinates))
                                        {
                                            topLeft.DirectionalRoomConnections.Add("e", topRight.WorldCoordinatesAsTuple);

                                            if (topRight.DirectionalRoomConnections.TryGetValue("w", out connectionWorldCoordinates)) { throw new Exception(); }
                                            topRight.DirectionalRoomConnections.Add("w", topLeft.WorldCoordinatesAsTuple);
                                        }
                                        if (!topRight.DirectionalRoomConnections.TryGetValue("s", out connectionWorldCoordinates))
                                        {
                                            topRight.DirectionalRoomConnections.Add("s", bottomRight.WorldCoordinatesAsTuple);

                                            if (bottomRight.DirectionalRoomConnections.TryGetValue("n", out connectionWorldCoordinates)) { throw new Exception(); }
                                            bottomRight.DirectionalRoomConnections.Add("n", topRight.WorldCoordinatesAsTuple);
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
                                        if (!topRight.DirectionalRoomConnections.TryGetValue("s", out connectionWorldCoordinates))
                                        {
                                            topRight.DirectionalRoomConnections.Add("s", bottomRight.WorldCoordinatesAsTuple);

                                            if (bottomRight.DirectionalRoomConnections.TryGetValue("n", out connectionWorldCoordinates)) { throw new Exception(); }
                                            bottomRight.DirectionalRoomConnections.Add("n", topRight.WorldCoordinatesAsTuple);
                                        }
                                        if (!bottomRight.DirectionalRoomConnections.TryGetValue("w", out connectionWorldCoordinates))
                                        {
                                            bottomRight.DirectionalRoomConnections.Add("w", bottomLeft.WorldCoordinatesAsTuple);

                                            if (bottomLeft.DirectionalRoomConnections.TryGetValue("e", out connectionWorldCoordinates)) { throw new Exception(); }
                                            bottomLeft.DirectionalRoomConnections.Add("e", bottomRight.WorldCoordinatesAsTuple);
                                        }
                                        break;
                                    case 1:
                                        if (!topRight.DirectionalRoomConnections.TryGetValue("w", out connectionWorldCoordinates))
                                        {
                                            topRight.DirectionalRoomConnections.Add("w", topLeft.WorldCoordinatesAsTuple);

                                            if (topLeft.DirectionalRoomConnections.TryGetValue("e", out connectionWorldCoordinates)) { throw new Exception(); }
                                            topLeft.DirectionalRoomConnections.Add("e", topRight.WorldCoordinatesAsTuple);
                                        }
                                        if (!topLeft.DirectionalRoomConnections.TryGetValue("s", out connectionWorldCoordinates))
                                        {
                                            topLeft.DirectionalRoomConnections.Add("s", bottomLeft.WorldCoordinatesAsTuple);

                                            if (bottomLeft.DirectionalRoomConnections.TryGetValue("n", out connectionWorldCoordinates)) { throw new Exception(); }
                                            bottomLeft.DirectionalRoomConnections.Add("n", topLeft.WorldCoordinatesAsTuple);
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void CreateCaves()
        {
            int nNumberOfCaves = Width * Height / 2500; // ~100 caves at tested resolution (600 x 420)
            for (int i = 0; i < nNumberOfCaves; i++)
            {
                ProtoCaves.Add(new ProtoRegion(nNextRegionId++, MasterUndergroundRoomList, REGION_TYPE.UNDERGROUND));
            }
        }
        private void ConnectCavesToOverground()
        {
            for (int i = ProtoCaves.Count - 1; i >= 0; i--)
            {
                ProtoRoom roomUnderground = ProtoCaves[i].ProtoSubregions[0].ProtoRooms.RandomListItem();
                ProtoRoom roomOverground = MasterOvergroundRoomList[roomUnderground.CoordinatesXY.X, roomUnderground.CoordinatesXY.Y];

                int nFailCount = 0;
                while (!roomOverground.IsTraversable() && nFailCount < 10)
                {
                    nFailCount++;
                    roomUnderground = ProtoCaves[i].ProtoSubregions[0].ProtoRooms.RandomListItem();
                    roomOverground = MasterOvergroundRoomList[roomUnderground.CoordinatesXY.X, roomUnderground.CoordinatesXY.Y];
                }

                if (nFailCount == 10)
                {
                    ProtoCaves.RemoveAt(i);
                    continue;
                }
                else
                {
                    // add under to over connection
                    roomUnderground.DirectionalRoomConnections.Add("o", roomOverground.WorldCoordinatesAsTuple);
                    // add over to under connection
                    roomOverground.AddConnection("go", "cave", roomUnderground.WorldCoordinatesAsTuple);
                }
            }
        }
        private void CreateCavePaths()
        {

        }

        private void SaveWorldImages(CanvasDevice device)
        {
            // draw regions
            RenderTargetRegions = new CanvasRenderTarget(device, Width * Statics.MapResolution, Height * Statics.MapResolution, 96);
            using (CanvasDrawingSession ds = RenderTargetRegions.CreateDrawingSession())
            {
                DrawRegions(ds);
            }

            // draw subregions
            RenderTargetSubregions = new CanvasRenderTarget(device, Width * Statics.MapResolution, Height * Statics.MapResolution, 96);
            using (CanvasDrawingSession ds = RenderTargetSubregions.CreateDrawingSession())
            {
                DrawSubregions(ds);
            }

            // draw paths
            RenderTargetPaths = new CanvasRenderTarget(device, Width * Statics.MapResolution, Height * Statics.MapResolution, 96);
            using (CanvasDrawingSession ds = RenderTargetPaths.CreateDrawingSession())
            {
                DrawPaths(ds);
            }

            // draw heightmap
            RenderTargetHeightMap = new CanvasRenderTarget(device, Width * Statics.MapResolution, Height * Statics.MapResolution, 96);
            using (CanvasDrawingSession ds = RenderTargetHeightMap.CreateDrawingSession())
            {
                DrawHeightMap(ds);
            }

            // draw caves
            RenderTargetCaves = new CanvasRenderTarget(device, Width * Statics.MapResolution, Height * Statics.MapResolution, 96);
            using (CanvasDrawingSession ds = RenderTargetCaves.CreateDrawingSession())
            {
                DrawCaves(ds);
            }

            // draw cave paths
            RenderTargetCavePaths = new CanvasRenderTarget(device, Width * Statics.MapResolution, Height * Statics.MapResolution, 96);
            using (CanvasDrawingSession ds = RenderTargetCavePaths.CreateDrawingSession())
            {
                DrawCavePaths(ds);
            }
        }
        #endregion

        #region Drawing
        private void DrawHeightMap(CanvasDrawingSession ds)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    ds.FillRectangle(new Rect(x * Statics.MapResolution, y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), MasterOvergroundRoomList[x, y].ElevationColor);
                }
            }
        }
        private void DrawSubregions(CanvasDrawingSession ds)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    ds.FillRectangle(new Rect(x * Statics.MapResolution, y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), MasterOvergroundRoomList[x, y].ProtoSubregion.Color);
                }
            }
        }
        private void DrawRegions(CanvasDrawingSession ds)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    ds.FillRectangle(new Rect(x * Statics.MapResolution, y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), MasterOvergroundRoomList[x, y].ProtoRegion.Color);
                }
            }
        }
        private void DrawCaves(CanvasDrawingSession ds)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (MasterUndergroundRoomList[x, y].ProtoRegion == null) { continue; }

                    Tuple<int, int, int> debug;
                    if (MasterUndergroundRoomList[x, y].DirectionalRoomConnections.TryGetValue("o", out debug))
                    {
                        ds.FillRectangle(new Rect(x * Statics.MapResolution, y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), Colors.Red);
                    }
                    else
                    {
                        ds.FillRectangle(new Rect(x * Statics.MapResolution, y * Statics.MapResolution, Statics.MapResolution, Statics.MapResolution), MasterUndergroundRoomList[x, y].ProtoRegion.Color);
                    }
                }
            }
        }
        private void DrawPaths(CanvasDrawingSession ds)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    DrawPaths(ds, MasterOvergroundRoomList, x, y, Colors.White);
                }
            }
        }
        private void DrawCavePaths(CanvasDrawingSession ds)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (MasterUndergroundRoomList[x, y].ProtoRegion == null) { continue; }
                    DrawPaths(ds, MasterUndergroundRoomList, x, y, Colors.White);
                }
            }
        }
        private void DrawPaths(CanvasDrawingSession ds, ProtoRoom[,] roomList, int x, int y, Color color)
        {
            float fStrokeWidth = 5;

            foreach (string DirectionalRoomConnection in roomList[x, y].DirectionalRoomConnections.Keys)
            {
                switch (DirectionalRoomConnection)
                {
                    case "nw":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             ((x - 1) + 0.5f) * Statics.MapResolution,
                             ((y - 1) + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "n":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             (x + 0.5f) * Statics.MapResolution,
                             ((y - 1) + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "ne":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             ((x + 1) + 0.5f) * Statics.MapResolution,
                             ((y - 1) + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "w":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             ((x - 1) + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "o":
                        break;
                    case "e":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             ((x + 1) + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "sw":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             ((x - 1) + 0.5f) * Statics.MapResolution,
                             ((y + 1) + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "s":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             (x + 0.5f) * Statics.MapResolution,
                             ((y + 1) + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    case "se":
                        ds.DrawLine((x + 0.5f) * Statics.MapResolution,
                             (y + 0.5f) * Statics.MapResolution,
                             ((x + 1) + 0.5f) * Statics.MapResolution,
                             ((y + 1) + 0.5f) * Statics.MapResolution,
                             color, fStrokeWidth);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }
        #endregion

        #region Region/Room Operations
        private void MergeRegions(ProtoRoom[,] MasterRoomList, int nMinimumSize, bool bAddAsSubregions)
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
                int tileRandomX = (int)tileRandom.CoordinatesXY.X;
                int tileRandomY = (int)tileRandom.CoordinatesXY.Y;

                switch (Statics.Random.Next(4))
                {
                    case 0:
                        // left
                        if (tileRandomX > 0)
                        {
                            region = MasterOvergroundRoomList[tileRandomX - 1, tileRandomY].ProtoRegion;
                        }
                        break;
                    case 1:
                        // right
                        if (tileRandomX < Width - 1)
                        {
                            region = MasterOvergroundRoomList[tileRandomX + 1, tileRandomY].ProtoRegion;
                        }
                        break;
                    case 2:
                        // up
                        if (tileRandomY > 0)
                        {
                            region = MasterOvergroundRoomList[tileRandomX, tileRandomY - 1].ProtoRegion;
                        }
                        break;
                    case 3:
                        // down
                        if (tileRandomY < Height - 1)
                        {
                            region = MasterOvergroundRoomList[tileRandomX, tileRandomY + 1].ProtoRegion;
                        }
                        break;
                }
            }

            return region;
        }
        private ProtoRoom AddRoomConnection(ProtoRoom currentRoom, string strDirection, ProtoRoom[,] masterRoomList, bool bForce = false)
        {
            Tuple<int, int, int> connectionWorldCoordinates;
            if (currentRoom.DirectionalRoomConnections.TryGetValue(strDirection, out connectionWorldCoordinates)) { return currentRoom; }

            string strOppositeDirection = string.Empty;
            PointInt connectingRoomCoordinates = null;
            switch (strDirection)
            {
                case "nw":
                    strOppositeDirection = "se";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X - 1, currentRoom.CoordinatesXY.Y - 1);
                    break;
                case "n":
                    strOppositeDirection = "s";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X, currentRoom.CoordinatesXY.Y - 1);
                    break;
                case "ne":
                    strOppositeDirection = "sw";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X + 1, currentRoom.CoordinatesXY.Y - 1);
                    break;
                case "w":
                    strOppositeDirection = "e";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X - 1, currentRoom.CoordinatesXY.Y);
                    break;
                case "e":
                    strOppositeDirection = "w";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X + 1, currentRoom.CoordinatesXY.Y);
                    break;
                case "sw":
                    strOppositeDirection = "ne";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X - 1, currentRoom.CoordinatesXY.Y + 1);
                    break;
                case "s":
                    strOppositeDirection = "n";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X, currentRoom.CoordinatesXY.Y + 1);
                    break;
                case "se":
                    strOppositeDirection = "nw";
                    connectingRoomCoordinates = new PointInt(currentRoom.CoordinatesXY.X + 1, currentRoom.CoordinatesXY.Y + 1);
                    break;
                default:
                    throw new Exception();
            }

            if (connectingRoomCoordinates.X < 0) { return currentRoom; }
            if (connectingRoomCoordinates.X > Width - 1) { return currentRoom; }
            if (connectingRoomCoordinates.Y < 0) { return currentRoom; }
            if (connectingRoomCoordinates.Y > Height - 1) { return currentRoom; }

            ProtoRoom connectingRoom = masterRoomList[connectingRoomCoordinates.X, connectingRoomCoordinates.Y];
            if (connectingRoom.Elevation == 0 || connectingRoom.Elevation == 30) { return currentRoom; }
            if (connectingRoom.HasMaximumConnections && !bForce) { return currentRoom; }

            Debug.TotalConnectionCount++;

            currentRoom.DirectionalRoomConnections.Add(strDirection, connectingRoom.WorldCoordinatesAsTuple);
            connectingRoom.DirectionalRoomConnections.Add(strOppositeDirection, currentRoom.WorldCoordinatesAsTuple);
            return connectingRoom;
        }
        #endregion

        #region Pathing
        private PointInt GetNeighborCoordinates(PointInt sourceCoordinates, string strDirection)
        {
            switch (strDirection)
            {
                case "nw": return new PointInt(sourceCoordinates.X - 1, sourceCoordinates.Y - 1);
                case "n": return new PointInt(sourceCoordinates.X, sourceCoordinates.Y - 1);
                case "ne": return new PointInt(sourceCoordinates.X + 1, sourceCoordinates.Y - 1);
                case "w": return new PointInt(sourceCoordinates.X - 1, sourceCoordinates.Y);
                case "e": return new PointInt(sourceCoordinates.X + 1, sourceCoordinates.Y);
                case "sw": return new PointInt(sourceCoordinates.X - 1, sourceCoordinates.Y + 1);
                case "s": return new PointInt(sourceCoordinates.X, sourceCoordinates.Y + 1);
                case "se": return new PointInt(sourceCoordinates.X + 1, sourceCoordinates.Y + 1);
                default: return null;
            }
        }
        private void AssessRoomConnectivity()
        {
            MainPath = new HashSet<PointInt>();
            RoomsNotInMainPath = new HashSet<PointInt>(TraversableRooms);
            OpenSet = new HashSet<PointInt>();

            // initialize OpenSet with a random tile
            ProtoRoom protoRoom = null;
            int initialX = -1;
            int initialY = -1;
            while (protoRoom == null || !protoRoom.IsTraversable() || protoRoom.DirectionalRoomConnections.Count == 0)
            {
                initialX = Statics.Random.Next(Width);
                initialY = Statics.Random.Next(Height);
                protoRoom = MasterOvergroundRoomList[initialX, initialY];
            }

            OpenSet.Add(new PointInt(initialX, initialY));

            while (OpenSet.Count > 0)
            {
                PointInt currentCoordinates = OpenSet.ElementAt(0);
                if (!OpenSet.Remove(currentCoordinates)) { throw new Exception(); }
                if (!RoomsNotInMainPath.Remove(currentCoordinates)) { throw new Exception(); }
                MainPath.Add(currentCoordinates);

                foreach (string strConnection in MasterOvergroundRoomList[currentCoordinates.X, currentCoordinates.Y].DirectionalRoomConnections.Keys)
                {
                    PointInt connectingCoordinates = GetNeighborCoordinates(currentCoordinates, strConnection);
                    if (!OpenSet.Contains(connectingCoordinates) && !MainPath.Contains(connectingCoordinates))
                    {
                        OpenSet.Add(connectingCoordinates);
                    }
                }
            }
        }
        #endregion

        #region HeightMap
        private int[,] GenerateHeightMapMountains()
        {
            // mountain pass
            PerlinNoise pn = new PerlinNoise(Width, Height);
            float fFrequency = 0.02f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            float fAmplitude = 2.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            float fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            int nOctaves = 1;// + Statics.Random.Next(5); // 5;
            int[,] mountainMap = new int[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, fFrequency, fAmplitude, fPersistence, nOctaves);
                    if (nElevation >= 27) { mountainMap[x, y] = nElevation; }
                }
            }

            Blur(mountainMap, 3);
            return mountainMap;
        }
        private int[,] GenerateHeightMapWater()
        {
            // water pass
            int[,] waterMap = new int[Width, Height];
            PerlinNoise pn = new PerlinNoise(Width, Height);
            float fFrequency = 0.05f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            float fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            float fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            int nOctaves = 1;// + Statics.Random.Next(5); // 5;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, fFrequency, fAmplitude, fPersistence, nOctaves);
                    if (nElevation >= 25) { waterMap[x, y] = nElevation; }
                }
            }
            Blur(waterMap, 3);
            return waterMap;
        }
        private int[,] GenerateHeightMapForest()
        {
            // forest pass
            PerlinNoise pn = new PerlinNoise(Width, Height);
            float fFrequency = 0.03f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            float fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            float fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            int nOctaves = 1;// + Statics.Random.Next(5); // 5;
            int[,] forestMap = new int[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, fFrequency, fAmplitude, fPersistence, nOctaves);
                    if (nElevation >= 20) { forestMap[x, y] = 30; }
                }
            }

            Blur(forestMap, 3);
            return forestMap;
        }
        private int[,] GenerateHeightMapDesert()
        {
            // desert pass
            PerlinNoise pn = new PerlinNoise(Width, Height);
            float fFrequency = 0.03f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
            float fAmplitude = 1.5f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
            float fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
            int nOctaves = 1;// + Statics.Random.Next(5); // 5;
            int[,] desertMap = new int[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, fFrequency, fAmplitude, fPersistence, nOctaves);
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
                float[,] fIntermediateBlurredHeightValues = new float[Width, Height];
                float[,] fFinalBlurredHeightValues = new float[Width, Height];

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        fIntermediateBlurredHeightValues[x, y] = ComputeXValue(heightMap, FilterKernel, x, y);
                    }
                }

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        fFinalBlurredHeightValues[x, y] = ComputeYValue(fIntermediateBlurredHeightValues, FilterKernel, x, y);
                    }
                }

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
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
                if (x + kvp.Key > Width - 1) { offset = 0; }

                fValue += kvp.Value * heightMap[x + offset, y]; // MasterOvergroundRoomList[x + offset, y].Elevation;
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
                if (y + kvp.Key > Height - 1) { offset = 0; }

                fValue += kvp.Value * fIntermediateBlurredHeightValues[x, y + offset];
            }

            return fValue;
        }
        #endregion

        #region Debug
        #endregion

        #region Cut Code
        //private void DebugValidation()
        //{
        //    // checks to ensure that rooms within a region/subregion actually point back to the containing region/subregion
        //    // would theoretically catch reassignment misses during merge
        //    if (!DebugValidateRoomOwnership()) { throw new Exception("Wut?!?"); }
        //    if (!DebugValidateConnectionMirroring()) { throw new Exception("For realz?!?"); }
        //}
        //private bool DebugValidateRoomOwnership()
        //{
        //    foreach (ProtoRegion pr in ProtoRegions)
        //    {
        //        foreach (ProtoSubregion ps in pr.ProtoSubregions)
        //        {
        //            if (ps.ProtoRegion != pr) { return false; }
        //            foreach (ProtoRoom proom in ps.ProtoRooms)
        //            {
        //                if (proom.ProtoSubregion != ps) { return false; }
        //                if (proom.ProtoRegion != pr) { return false; }
        //            }
        //        }
        //    }

        //    return true;
        //}
        //private bool DebugValidateConnectionMirroring()
        //{
        //    for (int x = 0; x < Width; x++)
        //    {
        //        for (int y = 0; y < Height; y++)
        //        {
        //            ProtoRoom currentRoom = MasterOvergroundRoomList[x, y];
        //            ProtoRoom connectionRoom = null;

        //            Tuple<int, int, int> connectionWorldCoordinates;
        //            foreach (string strConnection in currentRoom.DirectionalRoomConnections.Keys)
        //            {
        //                switch (strConnection)
        //                {
        //                    case "nw":
        //                        connectionRoom = MasterOvergroundRoomList[x - 1, y - 1];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("se", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "n":
        //                        connectionRoom = MasterOvergroundRoomList[x, y - 1];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("s", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "ne":
        //                        connectionRoom = MasterOvergroundRoomList[x + 1, y - 1];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("sw", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "w":
        //                        connectionRoom = MasterOvergroundRoomList[x - 1, y];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("e", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "e":
        //                        connectionRoom = MasterOvergroundRoomList[x + 1, y];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("w", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "sw":
        //                        connectionRoom = MasterOvergroundRoomList[x - 1, y + 1];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("ne", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "s":
        //                        connectionRoom = MasterOvergroundRoomList[x, y + 1];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("n", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    case "se":
        //                        connectionRoom = MasterOvergroundRoomList[x + 1, y + 1];
        //                        if (!connectionRoom.DirectionalRoomConnections.TryGetValue("nw", out connectionWorldCoordinates)) { return false; }
        //                        break;
        //                    default:
        //                        throw new Exception();
        //                }
        //            }
        //        }
        //    }
        //    return true;
        //}
        //private void DebugCountConnections()
        //{
        //    for (int x = 0; x < Width; x++)
        //    {
        //        for (int y = 0; y < Height; y++)
        //        {
        //            ProtoRoom protoRoom = MasterOvergroundRoomList[x, y];
        //            foreach (string strConnection in protoRoom.DirectionalRoomConnections.Keys)
        //            {
        //                switch (strConnection)
        //                {
        //                    case "nw": Debug.NWConnectionCount++; break;
        //                    case "n": Debug.NConnectionCount++; break;
        //                    case "ne": Debug.NEConnectionCount++; break;
        //                    case "w": Debug.WConnectionCount++; break;
        //                    case "e": Debug.EConnectionCount++; break;
        //                    case "sw": Debug.SWConnectionCount++; break;
        //                    case "s": Debug.SConnectionCount++; break;
        //                    case "se": Debug.SEConnectionCount++; break;
        //                }
        //            }
        //        }
        //    }
        //}
        //private HashSet<PointInt> Walk(ProtoRoom prInitial)
        //{
        //    HashSet<PointInt> Path = new HashSet<PointInt>();

        //    OpenSet = new HashSet<PointInt>();
        //    OpenSet.Add(prInitial.CoordinatesXY);
        //    while (OpenSet.Count > 0)
        //    {
        //        PointInt currentCoordinates = OpenSet.ElementAt(0);
        //        OpenSet.Remove(currentCoordinates);
        //        Path.Add(currentCoordinates);

        //        foreach (string strConnection in MasterOvergroundRoomList[currentCoordinates.X, currentCoordinates.Y].DirectionalRoomConnections.Keys)
        //        {
        //            PointInt connectingCoordinates;
        //            switch (strConnection)
        //            {
        //                case "nw":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y - 1);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "n":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X, currentCoordinates.Y - 1);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "ne":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y - 1);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "w":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "e":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "sw":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X - 1, currentCoordinates.Y + 1);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "s":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X, currentCoordinates.Y + 1);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //                case "se":
        //                    connectingCoordinates = new PointInt(currentCoordinates.X + 1, currentCoordinates.Y + 1);
        //                    if (!OpenSet.Contains(connectingCoordinates) && !Path.Contains(connectingCoordinates))
        //                    {
        //                        OpenSet.Add(connectingCoordinates);
        //                    }
        //                    break;
        //            }
        //        }
        //    }

        //    return Path;
        //}
        // attempt to connect straggler rooms to main path
        //            if (MainPath.Count + TilesNotInMainPath.Count != TraversableTiles)
        //            {
        //                int i = 0;
        //        i++;
        //            }

        //            while (MainPath.Count != TraversableTiles)
        //            {
        //                if (MainPath.Count + TilesNotInMainPath.Count != TraversableTiles)
        //                {
        //                    int i = 0;
        //    i++;
        //                }

        //                if (++DebugFixConnectionsCount >= 5)
        //                {
        //                    //foreach (PointInt pi in TilesNotInMainPath)
        //                    //{
        //                    //    ProtoRoom pr = MasterOvergroundRoomList[pi.X, pi.Y];
        //                    //    pr.DirectionalRoomConnections.Clear();
        //                    //}
        //                    PointInt pi = TilesNotInMainPath.ElementAt(0);
        //ProtoRoom pr = MasterOvergroundRoomList[pi.X, pi.Y];

        //int n = Walk(pr).Count;

        ////AbortConstruction();
        //Statics.RollingReset = false;
        //                    s.Stop();
        //                    DebugFixConnectionsTime = (int)s.ElapsedMilliseconds;

        //                    return;
        //                }

        //                for (int i = TilesNotInMainPath.Count - 1; i >= 0; i--)
        //                {
        //                    PointInt currentCoordinates = TilesNotInMainPath.ElementAt(i);
        //                    if (currentCoordinates.Y == Height - 1)
        //                    {
        //                        int q = 0;
        //q++;
        //                    }
        //                    ProtoRoom protoRoom = MasterOvergroundRoomList[currentCoordinates.X, currentCoordinates.Y];

        //                    // attempt to connect current room to a neighbor that's already on the main path
        //                    if (FixDisconnectedRoom(protoRoom, "nw")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "n")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "ne")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "w")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "e")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "sw")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "s")) { continue; }
        //                    if (FixDisconnectedRoom(protoRoom, "se")) { continue; }
        //                }
        //            }

        //            s.Stop();
        //            DebugFixConnectionsTime = (int)s.ElapsedMilliseconds;
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
        //    if (x > Width - 1) { return false; }
        //    if (y > Height - 1) { return false; }

        //    ProtoRoom targetRoom = MasterOvergroundRoomList[x, y];
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
        //        float[,] fIntermediateBlurredHeightValues = new float[Width, Height];
        //        float[,] fFinalBlurredHeightValues = new float[Width, Height];

        //        for (int x = 0; x < Width; x++)
        //        {
        //            for (int y = 0; y < Height; y++)
        //            {
        //                fIntermediateBlurredHeightValues[x, y] = ComputeXValue(FilterKernel, x, y);
        //            }
        //        }

        //        for (int x = 0; x < Width; x++)
        //        {
        //            for (int y = 0; y < Height; y++)
        //            {
        //                fFinalBlurredHeightValues[x, y] = ComputeYValue(fIntermediateBlurredHeightValues, FilterKernel, x, y);
        //            }
        //        }

        //        for (int x = 0; x < Width; x++)
        //        {
        //            for (int y = 0; y < Height; y++)
        //            {
        //                MasterOvergroundRoomList[x, y].Elevation = (int)fFinalBlurredHeightValues[x, y];
        //            }
        //        }
        //    }
        //}
        //for (int x = 0; x < Width; x++)
        //{
        //    for (int y = 0; y < Height; y++)
        //    {
        //        int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
        //        if (nElevation >= 27) { MasterOvergroundRoomList[x, y].Elevation = nElevation; }
        //        // 0.1f, 1.2f, 0.5f, 5); // Statics.Random.Next(15);
        //        //MasterOvergroundRoomList[x, y].Elevation = 15 + (int)pn.GetRandomHeight(x, y, 15, 1.5f, 1.2f, 0.5f, 5);
        //    }
        //}

        //BlurHeightMap(1);



        //// forest pass
        //pn = new PerlinNoise(Width, Height);
        //Statics.fFrequency = 0.02f;// + Statics.Random.Next(30) * 0.1f; // 0.1f;
        //Statics.fAmplitude = 3.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 1.2f;
        //Statics.fPersistence = 1.0f; // 0.1f + Statics.Random.Next(30) * 0.1f; // 0.5f;
        //Statics.nOctaves = 1;// + Statics.Random.Next(5); // 5;
        //for (int x = 0; x < Width; x++)
        //{
        //    for (int y = 0; y < Height; y++)
        //    {
        //        int nElevation = 15 + (int)pn.GetRandomHeight(x, y, 15, Statics.fFrequency, Statics.fAmplitude, Statics.fPersistence, Statics.nOctaves);
        //        if (nElevation >= 27 && MasterOvergroundRoomList[x, y].Elevation == 3) { MasterOvergroundRoomList[x, y].Elevation = 50; }
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
        //    if (connectingRoomCoordinates.X > MasterOvergroundRoomList.GetLength(0) - 1) { return sourceProtoRoom; }
        //    if (connectingRoomCoordinates.Y < 0) { return sourceProtoRoom; }
        //    if (connectingRoomCoordinates.Y > MasterOvergroundRoomList.GetLength(1) - 1) { return sourceProtoRoom; }

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

        //    // create dictionary out of MasterOvergroundRoomList
        //    for (int x = 0; x < MasterOvergroundRoomList.GetLength(0); x++)
        //    {
        //        for (int y = 0; y < MasterOvergroundRoomList.GetLength(1); y++)
        //        {
        //            ProtoRoomsNeedingConnections.Add(MasterOvergroundRoomList[x, y].Coordinates, MasterOvergroundRoomList[x, y]);
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
        //private void MergeRegions(ProtoRoom[,] MasterOvergroundRoomList)
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
        //    if (x >= MasterOvergroundRoomList.GetLength(0)) { return null; }
        //    if (y < 0) { return null; }
        //    if (y >= MasterOvergroundRoomList.GetLength(1)) { return null; }

        //    return MasterOvergroundRoomList[x, y].Region;
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


        //foreach(ProtoRegion pc in ProtoCaves)
        //{
        //    int nTotal = 0;
        //    foreach (ProtoSubregion ps in pc.ProtoSubregions)
        //    {
        //        foreach(ProtoRoom pr in ps.ProtoRooms)
        //        {
        //            nTotal += pr.DirectionalRoomConnections.Keys.Where(x => x == "o").Count();
        //        }
        //    }

        //    if (nTotal == 1)
        //    {
        //        int i = 0;
        //        i++;
        //    }
        //    else
        //    {
        //        throw new Exception();
        //    }
        //}


        //private bool FixDisconnectedRoom(ProtoRoom protoRoom, string strDirection)
        //{
        //    PointInt neighborCoordinates = GetNeighborCoordinates(protoRoom.CoordinatesXY, strDirection);

        //    if (neighborCoordinates.X < 0) { return false; }
        //    if (neighborCoordinates.X > Width - 1) { return false; }
        //    if (neighborCoordinates.Y < 0) { return false; }
        //    if (neighborCoordinates.Y > Height - 1) { return false; }
        //    if (!MainPath.Contains(neighborCoordinates)) { return false; }

        //    ProtoRoom neighborRoom = MasterOvergroundRoomList[neighborCoordinates.X, neighborCoordinates.Y];
        //    if (neighborRoom.IsTraversable())
        //    {
        //        AddRoomConnection(protoRoom, strDirection, MasterOvergroundRoomList, true);
        //        TilesNotInMainPath.Remove(protoRoom.CoordinatesXY);
        //        MainPath.Add(protoRoom.CoordinatesXY);
        //        return true;
        //    }

        //    return false;
        //}
        #endregion
    }
}