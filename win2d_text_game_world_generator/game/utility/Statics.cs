using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.Foundation;
using Microsoft.Graphics.Canvas.Text;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using System.Linq;

namespace win2d_text_game_world_generator
{
    public enum MapDrawType
    {
        HEIGHTMAP,
        REGIONS
    }

    public static class Statics
    {
        public static float fFrequency;
        public static float fAmplitude;
        public static float fPersistence;
        public static int nOctaves;

        public static Region CurrentMouseRegion = null;
        public static Subregion CurrentMouseSubregion = null;

        public static object lockDebugLists = new object();

        public static List<double> MapCreationTimes = new List<double>();
        public static List<int> MapAbortCounts = new List<int>();
        public static bool RollingReset = false;
        public static int MapCount = 0;
        public static int HeightMapElevationFactor = 10;

        public static bool DrawDebug = true;
        public static bool DrawPaths = false;
        public static bool DrawSubregions = false;
        public static bool DrawGrid = false;

        public static MapDrawType MapDrawType = MapDrawType.REGIONS;

        public static int DebugNWConnectionCount;
        public static int DebugNConnectionCount;
        public static int DebugNEConnectionCount;
        public static int DebugWConnectionCount;
        public static int DebugEConnectionCount;
        public static int DebugSWConnectionCount;
        public static int DebugSConnectionCount;
        public static int DebugSEConnectionCount;

        public static int MaxConnections = 3;
        public static int TilesInMainPath1;
        public static int TilesInMainPath2;

        public static double MouseX = 0;
        public static double MouseY = 0;
        public static int FrameCount = 0;

        public static int CanvasWidth;
        public static int CanvasHeight;

        public static int MapWidthInPixels = 1920;
        public static int MapHeightInPixels = 1080;
        public static int PixelScale = 10;

        public static int Padding = 10;

        public static Vector2 MapPosition = Vector2.Zero;

        public static string DebugMapCreationTimeString = string.Empty;
        public static string DebugMapTotalRegionCountString = string.Empty;
        public static string DebugMapTotalTileCountString = string.Empty;
        public static string DebugHeightString = string.Empty;

        // probability that region will continue to try to expand past minimum size
        // calculated once for each tile added
        // e.g. a tile that has just met minimum size requirements has an n% chance of trying to add an additional tile (will fail if attempted add is already occupied),
        //  then an n% chance of attempting to add another tile after that, and so on
        public static int ProbabilityOfExpansion = 0;
        public static int MinimumRegionSize = 100;
        public static int MergeThreshold = 500;

        // timing
        public static int MapUpdateThreshold = 0; //500;
        public static int PauseBetweenBattlesMilliseconds = 500;

        public static Random Random = new Random(DateTime.Now.Millisecond);
        public static CanvasTextFormat FontSmall = new CanvasTextFormat();
        public static CanvasTextFormat FontMedium = new CanvasTextFormat();
        public static CanvasTextFormat FontLarge = new CanvasTextFormat();
        public static CanvasTextFormat FontExtraLarge = new CanvasTextFormat();

        #region Region Naming
        public static string[] RegionTypes = {
            "Forest",
            "Plains",
            "Desert",
            "Mountain",
            "Plateau",
            "Steppes",
            "Volcano",
            "Highlands",
            "Canyon",
            "Valley",
            "Marsh",
            "Bog",
            "Swamp",
            "Drylands",
            "Wetlands",
            "Jungle",
            "Hills"
        };

        public static string[] RegionNames = {
            "Cornelia",
            "Pravoka",
            "Elfheim",
            "Duergar",
            "Melmond",
            "Onrac",
            "Lufenia",
            "Gaia",

            "Altair",
            "Gatrea",
            "Paloom",
            "Poft",
            "Salamand",
            "Bafsk",
            "Fynn",
            "Mysidia",
            "Machanon",

            "Ur",
            "Kazus",
            "Canaan",
            "Tozus",
            "Tokkul",
            "Gysahl",
            "Amur",
            "Replito",
            "Duster",
            "Saronia",
            "Falgabard",

            "Baron",
            "Kaipo",
            "Fabul",
            "Troia",
            "Mist",
            "Mythril",
            "Agart",
            "Eblan",
            "Tomra",

            "Tule",
            "Carwen",
            "Walse",
            "Karnak",
            "Crescent",
            "Jachol",
            "Istory",
            "Lix",
            "Regole",
            "Quelb",
            "Surgate",
            "Moore",

            "Narshe",
            "Figaro",
            "Mobliz",
            "Nikeah",
            "Kohlingen",
            "Jidoor",
            "Zozo",
            "Maranda",
            "Tzen",
            "Albrook",
            "Vector",
            "Thamasa"
        };

        public static string RandomRegionName()
        {
            return RegionNames.RandomArrayItem();
        }

        public static string RandomRegionType()
        {
            string strRegionType = RegionTypes.RandomArrayItem();
            string strRegionName = RegionNames.RandomArrayItem();

            switch (Statics.Random.Next(2))
            {
                case 0:
                    // use region type as prefix
                    return strRegionType + " of " + strRegionName;
                case 1:
                    // use region type as suffix
                    return strRegionName + " " + strRegionType;
                default:
                    return string.Empty;
            }
        }
        #endregion

        #region Faction Naming
        public static string[] FactionTypes = {
            "Kingdom",
            "Empire",
            "Duchy",
            "Land",
            "Regency",
            "Sultanate",
            "Emirate",
            "Nation",
            "State",
            "Country",
            "Republic",
            "Monarchy",
            "Tribe"
        };
        #endregion

        #region Battle Terms
        public static string[] DefeatWords = {
            "defeateth",
            "obliterateth",
            "destroyeth",
            "overruneth",
            "squasheth",
            "pummeleth",
            "conquereth",
            "deraileth",
            "overthroweth",
            "ruineth",
            "thwarteth",
            "vanquisheth",
            "dismisseth",
            "beateth",
            "routeth",
            "whipeth",
            "crusheth",
            "subdueth",
            "clobbereth",
            "demolisheth",
            "skunketh",
            "slaughtereth",
            "thrasheth",
            "overpowereth"
        };
        #endregion

        #region Leader Titles
        public static string[] MaleTitles = {
            "King",
            "Emperor",
            "Duke",
            "Prince",
            "Almighty Ruler",
            "Baron",
            "Earl",
            "Jarl",
            "Lord",
            "Count",
            "Marquis",
            "Tsar",
            "Kaiser",
            "Emir",
            "Viceroy"
        };

        public static string[] FemaleTitles = {
            "Queen",
            "Empress",
            "Duchess",
            "Princess",
            "Almighty Ruler",
            "Baroness",
            "Lady",
            "Countess",
            "Marquise",
            "Tsarina",
            "Emira",
            "Vicereine"
        };
        #endregion

        static Statics()
        {
            FontSmall.FontFamily = "Old English Text MT";
            FontSmall.FontSize = 18;
            FontSmall.WordWrapping = CanvasWordWrapping.NoWrap;

            FontMedium.FontFamily = "Old English Text MT";
            FontMedium.FontSize = 24;
            FontMedium.WordWrapping = CanvasWordWrapping.NoWrap;

            FontLarge.FontFamily = "Old English Text MT";
            FontLarge.FontSize = 32;
            FontLarge.WordWrapping = CanvasWordWrapping.NoWrap;

            FontExtraLarge.FontFamily = "Old English Text MT";
            FontExtraLarge.FontSize = 48;
            FontExtraLarge.WordWrapping = CanvasWordWrapping.NoWrap;
        }

        public static Color RandomColor()
        {
            int red = 20 + Statics.Random.Next(235);
            int green = 20 + Statics.Random.Next(235);
            int blue = 20 + Statics.Random.Next(235);

            return Color.FromArgb(255, (byte)red, (byte)green, (byte)blue);
        }

        public static T RandomListItem<T>(this List<T> list)
        {
            return list[Statics.Random.Next(list.Count)];
        }

        public static T RandomArrayItem<T>(this T[] array)
        {
            return array[Statics.Random.Next(array.Length)];
        }

        //public static string RandomRegionName(Leader leader)
        //{
        //    string strRegionType = RegionTypes.RandomString();// + " of " + (Statics.Random.Next(2) == 0 ? "" : "the ") + RegionSuffixes.RandomString();
        //    string strReturn = string.Empty;

        //    switch (Statics.Random.Next(2))
        //    {
        //        case 0:
        //            // prefix
        //            strReturn = strRegionType + " of " + (Statics.Random.Next(2) == 0 ? "" : "the ") + leader.LastName;
        //            break;
        //        case 1:
        //            // suffix
        //            strReturn = leader.LastName + " " + strRegionType;
        //            break;
        //    }

        //    return strReturn;
        //}
    }
}
