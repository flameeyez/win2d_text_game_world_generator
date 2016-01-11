using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace win2d_text_game_world_generator
{
    public static class Debug
    {
        public static int FrameCount = 0;
        public static bool RollingReset = false;
        public static Region CurrentMouseRegion = null;
        public static Subregion CurrentMouseSubregion = null;
        public static int NWConnectionCount;
        public static int NConnectionCount;
        public static int NEConnectionCount;
        public static int WConnectionCount;
        public static int EConnectionCount;
        public static int SWConnectionCount;
        public static int SConnectionCount;
        public static int SEConnectionCount;
        public static List<double> MapCreationTimes = new List<double>();
        public static List<int> MapAbortCounts = new List<int>();
        public static List<int> CreateRoomConnectionsCounts = new List<int>();
        public static List<int> FixRoomConnectionsCounts = new List<int>();
        public static int MapCount = 0;
        public static string MapCreationTimeString = string.Empty;
        public static string MapTotalRegionCountString = string.Empty;
        public static string MapTotalTileCountString = string.Empty;
        public static string HeightString = string.Empty;
        public static object lockLists = new object();
        public static byte HeightMapOpacity = 75;

        public static void SetMapCreationMetadata(Map map)
        {
            lock (Debug.lockLists)
            {
                Debug.MapCreationTimes.Add(map.DebugCreationTime.TotalMilliseconds);
                Debug.MapAbortCounts.Add(map.DebugAbortedCount);
                Debug.CreateRoomConnectionsCounts.Add(map.DebugCreateRoomConnectionsCount);
                Debug.FixRoomConnectionsCounts.Add(map.DebugFixConnectionsCount);
            }

            Debug.MapCreationTimeString = "Map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms";
            Debug.MapTotalRegionCountString = "Total regions: " + map.Regions.Count.ToString();
            Debug.MapTotalTileCountString = "Total tiles: " + (map.WidthInTiles * map.HeightInTiles).ToString();
        }
    }
}
