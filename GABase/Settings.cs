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
    }
}