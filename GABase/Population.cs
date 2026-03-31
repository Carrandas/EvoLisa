#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Microsoft.Win32.SafeHandles;

#endregion

namespace GABase
{
    public class Population
    {
        public List<Chromosome> chromosomes;

        public Population(int maximumSize)
        {
            MaximumSize = maximumSize;
            chromosomes = new List<Chromosome>();
        }

        private Bitmap _cachedPicture;
        private Rectangle _cachedDirtyArea;
        private bool _isDirty = true;
        private int _cachedWidth;
        private int _cachedHeight;

        public int MaximumSize { get; }
        public bool IsDirty { get; set; }
		public Rectangle DirtyArea { get; set; }

        public void MarkDirty()
        {
            _isDirty = true;
            _cachedPicture?.Dispose();
            _cachedPicture = null;
        }

        public void InvalidateCache()
        {
            _cachedPicture?.Dispose();
            _cachedPicture = null;
            _isDirty = true;
        }

        public Bitmap GetPicture()
        {
            if (_cachedPicture == null || _isDirty || _cachedWidth != Settings.ScreenWidth || _cachedHeight != Settings.ScreenHeight)
            {
                _cachedPicture?.Dispose();
                _cachedPicture = CreateBitmap();
                _cachedDirtyArea = new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight);
                _cachedWidth = Settings.ScreenWidth;
                _cachedHeight = Settings.ScreenHeight;
                _isDirty = false;
            }
            return new Bitmap(_cachedPicture);
        }

        public Bitmap GetPicture(int minX, int minY, int maxX, int maxY)
        {
            if (_cachedPicture == null || _isDirty || _cachedWidth != Settings.ScreenWidth || _cachedHeight != Settings.ScreenHeight)
            {
                _cachedPicture?.Dispose();
                _cachedPicture = CreateBitmap();
                _cachedDirtyArea = new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight);
                _cachedWidth = Settings.ScreenWidth;
                _cachedHeight = Settings.ScreenHeight;
                _isDirty = false;
            }

            if (minX == 0 && minY == 0 && maxX == Settings.ScreenWidth && maxY == Settings.ScreenHeight)
            {
                return new Bitmap(_cachedPicture);
            }

            Bitmap bitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(_cachedPicture, 0, 0);
            }
            return bitmap;
        }

        private Bitmap CreateBitmap()
        {
            Bitmap bitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);
                foreach (Chromosome chromosome in chromosomes)
                {
                    using (var brush = new SolidBrush(chromosome.PolyColor))
                    {
                        if (Settings.Polygon == Settings.PolygonType.Lines)
                            graphics.FillPolygon(brush, chromosome.Polygon.ToArray());
                        else
                            graphics.FillClosedCurve(brush, chromosome.Polygon.ToArray());
                    }
                }
            }
            return bitmap;
        }

        public void GenerateRandomChromosomes()
        {
            for (int i = 0; i < chromosomes.Count; i++)
            {
                chromosomes[i] = new Chromosome(Settings.MaxPolygonPointCount);
                chromosomes[i].GenerateRandomChromosome();
            }
            MarkDirty();
        }

        public Population Clone()
        {
            Population pop = new Population(MaximumSize);
            for (int i = 0; i < chromosomes.Count; i++)
            {
                pop.chromosomes.Insert(i, chromosomes[i].Clone());
            }
            pop.MarkDirty();
            return pop;
        }

        /// <summary>
        /// Simplified implementation: check if the bounding box of the polygon overlap.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private bool PolygonInRectangle(Rectangle rectangle, List<Point> polygon)
        {
            var minX = polygon.Min(x => x.X);
            var maxX = polygon.Max(x => x.X);
            var minY = polygon.Min(x => x.Y);
            var maxY = polygon.Max(x => x.Y);
            var boundingRectangle = new Rectangle(minX, minY, maxX - minX, maxY - minY);
            return doOverlap(
                new Point(rectangle.Left, rectangle.Bottom), new Point(rectangle.Right, rectangle.Top),
                new Point(boundingRectangle.Left, boundingRectangle.Bottom), new Point(boundingRectangle.Right, boundingRectangle.Top)
            );
        }

        bool doOverlap(Point l1, Point r1, Point l2, Point r2)
        {
            // If one rectangle is on left side of other
            if (l1.X > r2.X || l2.X > r1.X)
                return false;

            // If one rectangle is above other
            if (l1.Y < r2.Y || l2.Y < r1.Y)
                return false;

            return true;
        }
    }
}