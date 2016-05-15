using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.Foundation;
using Microsoft.Graphics.Canvas.Text;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using System.Linq;
using Windows.System;
using System.Text;
using System.Runtime.InteropServices;

namespace win2d_text_game_world_generator
{
    public static class Statics
    {
        public static int TotalNumberOfPaths = 1000;
        public static int DesiredConnectionsPerPath = 250;

        #region Map Creation Data
        public static int MapWidthInPixels = 1920;
        public static int MapHeightInPixels = 1080;
        public static int PixelScale = 10;
        public static int Padding = 10;
        public static Vector2 MapPosition = Vector2.Zero;
        public static int MaxRoomConnections = 3;
        public static int MapResolution = 16;
        public static int MaxPathConnectionAttempts = 5;

        public static Dictionary<string, int> DirectionalStringToInt = new Dictionary<string, int>();
        public static Dictionary<int, string> IntToDirectionalString = new Dictionary<int, string>();
        public static Dictionary<int, string> IntToClockwiseDirectionalString = new Dictionary<int, string>();

        // probability that region will continue to try to expand past minimum size
        // calculated once for each tile added
        // e.g. a tile that has just met minimum size requirements has an n% chance of trying to add an additional tile (will fail if attempted add is already occupied),
        //  then an n% chance of attempting to add another tile after that, and so on
        public static int ProbabilityOfExpansion = 0;
        public static int MinimumRegionSize = 100;
        public static int MinimumCaveSize = 100;
        public static int MergeThreshold = 500;
        #endregion

        #region Mouse Position
        public static double MouseX = 0;
        public static double MouseY = 0;
        #endregion

        #region Canvas Layout
        public static int CanvasWidth;
        public static int CanvasHeight;
        #endregion

        #region Fonts / Text Formats
        public static CanvasTextFormat FontSmall = new CanvasTextFormat();
        public static CanvasTextFormat FontMedium = new CanvasTextFormat();
        public static CanvasTextFormat FontLarge = new CanvasTextFormat();
        public static CanvasTextFormat FontExtraLarge = new CanvasTextFormat();

        public static CanvasTextFormat DefaultFont;
        public static CanvasTextFormat DefaultFontNoWrap;

        public static CanvasTextLayout UpArrow;

        public static CanvasTextLayout DoubleUpArrow;
        public static CanvasTextLayout DownArrow;
        public static CanvasTextLayout DoubleDownArrow;

        private static Dictionary<char, double> CharacterWidthDictionary = new Dictionary<char, double>();
        #endregion

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

        //public static string[] CaveNames = {
        //    ""
        //};

        public static string[] CaveNameStyles =
        {
            "Cave of /1",
            "/1 Cave",
            "Caverns of /1",
            "/1 Caverns"
        };

        public static string RandomRegionName()
        {
            return RegionNames.RandomArrayItem();
        }

        public static string RandomCaveName()
        {
            // TODO: using region names for now; replace with cave names
            string strCaveName = RegionNames.RandomArrayItem();
            return CaveNameStyles.RandomArrayItem().Replace("/1", strCaveName);
        }

        public static string RandomRegionType()
        {
            string strRegionType = RegionTypes.RandomArrayItem();
            string strRegionName = RegionNames.RandomArrayItem();

            switch (Random.Next(2))
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

        #region Random
        public static Random Random = new Random(DateTime.Now.Millisecond);
        public static Color RandomColor()
        {
            int red = 20 + Random.Next(235);
            int green = 20 + Random.Next(235);
            int blue = 20 + Random.Next(235);

            return Color.FromArgb(255, (byte)red, (byte)green, (byte)blue);
        }
        internal static Color RandomCaveColor()
        {
            int red = 150 + Random.Next(75);
            int green = red;
            int blue = red;

            return Color.FromArgb(255, (byte)red, (byte)green, (byte)blue);
        }
        public static T RandomListItem<T>(this List<T> list)
        {
            return list[Random.Next(list.Count)];
        }
        public static T RandomArrayItem<T>(this T[] array)
        {
            return array[Random.Next(array.Length)];
        }
        #endregion

        #region HitTest
        public static bool HitTestRect(Rect rect, Point point)
        {
            if (point.X < rect.X) { return false; }
            if (point.X >= rect.X + rect.Width) { return false; }
            if (point.Y < rect.Y) { return false; }
            if (point.Y >= rect.Y + rect.Height) { return false; }

            return true;
        }
        #endregion

        #region VirtualKeyToString
        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode,
            byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
            StringBuilder receivingBuffer,
            int bufferSize, uint flags);

        public static string VirtualKeyToString(VirtualKey keys, bool shift = false, bool altGr = false)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shift)
                keyboardState[(int)VirtualKey.Shift] = 0xff;
            if (altGr)
            {
                keyboardState[(int)VirtualKey.Control] = 0xff;
                keyboardState[(int)VirtualKey.Menu] = 0xff;
            }
            ToUnicode((uint)keys, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }
        #endregion

        #region Initialization
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

            DefaultFont = new CanvasTextFormat();
            DefaultFont.FontFamily = "Arial";
            DefaultFont.FontSize = 14;
            DefaultFont.WordWrapping = CanvasWordWrapping.Wrap; //.NoWrap;

            DefaultFontNoWrap = new CanvasTextFormat();
            DefaultFontNoWrap.FontFamily = "Arial";
            DefaultFontNoWrap.FontSize = 14;
            DefaultFontNoWrap.WordWrapping = CanvasWordWrapping.NoWrap; //.NoWrap;

            LoadDirectionalStringToInt();
            LoadIntToDirectionalString();
            LoadIntToClockwiseDirectionalString();
        }
        private static void LoadDirectionalStringToInt()
        {
            DirectionalStringToInt.Add("nw", 0);
            DirectionalStringToInt.Add("n", 1);
            DirectionalStringToInt.Add("ne", 2);
            DirectionalStringToInt.Add("w", 3);
            DirectionalStringToInt.Add("o", 4);
            DirectionalStringToInt.Add("e", 5);
            DirectionalStringToInt.Add("sw", 6);
            DirectionalStringToInt.Add("s", 7);
            DirectionalStringToInt.Add("se", 8);
        }
        public static int SortRoomConnections(string strDirection1, string strDirection2)
        {
            int n1 = -1;
            DirectionalStringToInt.TryGetValue(strDirection1, out n1);

            int n2 = -2;
            DirectionalStringToInt.TryGetValue(strDirection2, out n2);

            // DirectionalStringToInt[x].CompareTo(Statics.DirectionalStringToInt[y]))
            return n1.CompareTo(n2);
        }
        private static void LoadIntToDirectionalString()
        {
            IntToDirectionalString.Add(0, "nw");
            IntToDirectionalString.Add(1, "n");
            IntToDirectionalString.Add(2, "ne");
            IntToDirectionalString.Add(3, "w");
            IntToDirectionalString.Add(4, "o");
            IntToDirectionalString.Add(5, "e");
            IntToDirectionalString.Add(6, "sw");
            IntToDirectionalString.Add(7, "s");
            IntToDirectionalString.Add(8, "se");
        }
        private static void LoadIntToClockwiseDirectionalString()
        {
            IntToClockwiseDirectionalString.Add(0, "nw");
            IntToClockwiseDirectionalString.Add(1, "n");
            IntToClockwiseDirectionalString.Add(2, "ne");
            IntToClockwiseDirectionalString.Add(3, "e");
            IntToClockwiseDirectionalString.Add(4, "se");
            IntToClockwiseDirectionalString.Add(5, "s");
            IntToClockwiseDirectionalString.Add(6, "sw");
            IntToClockwiseDirectionalString.Add(7, "w");
        }
        public static void Initialize(CanvasDevice device)
        {
            LoadCharacterWidths(device);

            UpArrow = new CanvasTextLayout(device, "\u2191", DefaultFontNoWrap, 0, 0);
            DoubleUpArrow = new CanvasTextLayout(device, "\u219f", DefaultFontNoWrap, 0, 0);
            DownArrow = new CanvasTextLayout(device, "\u2193", DefaultFontNoWrap, 0, 0);
            DoubleDownArrow = new CanvasTextLayout(device, "\u21a1", DefaultFontNoWrap, 0, 0);
        }
        private static void LoadCharacterWidths(CanvasDevice device)
        {
            string str = @"ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            str += @"abcdefghijklmnopqrstuvwxyz";

            str += @"1234567890";
            str += @"!@#$%^&*()";

            str += @"`~,<.>/?\|[{]}=+-_";

            foreach (char c in str)
            {
                CanvasTextLayout l = new CanvasTextLayout(device, c.ToString(), DefaultFontNoWrap, 0, 0);
                CharacterWidthDictionary.Add(c, l.LayoutBounds.Width);
            }
        }
        #endregion

        #region Character/String Width

        public static double StringWidth(string str)
        {
            double dWidth = 0;

            foreach (char c in str.Replace(' ', '.'))
            {
                dWidth += CharacterWidthDictionary[c];
            }

            return dWidth;
        }
        #endregion

        public static string GetOppositeDirection(string strDirection)
        {
            switch (strDirection)
            {
                case "nw": return "se";
                case "n": return "s";
                case "ne": return "sw";
                case "w": return "e";
                case "e": return "w";
                case "sw": return "ne";
                case "s": return "n";
                case "se": return "nw";
            }

            return string.Empty;
        }
    }
}
