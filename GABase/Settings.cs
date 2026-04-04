using System.Collections.Generic;
using System.Drawing;

namespace GABase
{
    public static class Settings
    {
        #region PolygonType enum

        public enum PolygonType
        {
            Curve,
            Lines
        } ;

        #endregion

        public static string ImageLocation { get; set; }
        public static int ScreenWidth { get; set; }
        public static int ScreenHeight { get; set; }


        public static int MaxPolygonCount
        {
            get { return 5000; }
        }

        public static int MaxPolygonPointCount
        {
            get { return 5; }
        }

        public static int MinPolygonPointCount
        {
            get { return 3; }
        }

        public static PolygonType Polygon
        {
            get { return PolygonType.Lines; }
        }

        public static bool UseARGB = true;

        public static List<Rectangle> FocusAreas = new List<Rectangle>();
        public static int FocusWeight = 100;
        private static byte[] _focusWeightMap;
        private static int _lastFocusWidth;
        private static int _lastFocusHeight;

        public static byte[] FocusWeightMap
        {
            get
            {
                if (_focusWeightMap == null || _lastFocusWidth != ScreenWidth || _lastFocusHeight != ScreenHeight)
                {
                    ComputeFocusWeightMap();
                }
                return _focusWeightMap;
            }
        }

        private static void ComputeFocusWeightMap()
        {
            _lastFocusWidth = ScreenWidth;
            _lastFocusHeight = ScreenHeight;
            _focusWeightMap = new byte[ScreenWidth * ScreenHeight];

            if (FocusAreas.Count == 0)
            {
                for (int i = 0; i < _focusWeightMap.Length; i++)
                    _focusWeightMap[i] = 1;
                return;
            }

            for (int y = 0; y < ScreenHeight; y++)
            {
                for (int x = 0; x < ScreenWidth; x++)
                {
                    byte weight = 1;
                    foreach (var area in FocusAreas)
                    {
                        if (x >= area.X && x < area.X + area.Width &&
                            y >= area.Y && y < area.Y + area.Height)
                        {
                            weight = (byte)FocusWeight;
                            break;
                        }
                    }
                    _focusWeightMap[y * ScreenWidth + x] = weight;
                }
            }
        }

        public static void InvalidateFocusWeightMap()
        {
            _focusWeightMap = null;
        }
    }
}