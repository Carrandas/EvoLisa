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

        public int MaximumSize { get; }
        public bool IsDirty { get; set; }
		public Rectangle DirtyArea { get; set; }

        public void GenerateRandomChromosomes()
        {
            for (int i = 0; i < chromosomes.Count; i++)
            {
                chromosomes[i] = new Chromosome(Settings.MaxPolygonPointCount);
                chromosomes[i].GenerateRandomChromosome();
            }
        }

        public Population Clone()
        {
            Population pop = new Population(MaximumSize);
            for (int i = 0; i < chromosomes.Count; i++)
            {
                pop.chromosomes.Insert(i, chromosomes[i].Clone());
            }
            return pop;
        }

        public Bitmap GetPicture()
        {
            Bitmap bitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);
                
                var colorBrushes = new Dictionary<Color, SolidBrush>();
                
                foreach (Chromosome chromosome in chromosomes)
                {
                    if (!colorBrushes.TryGetValue(chromosome.PolyColor, out var brush))
                    {
                        brush = new SolidBrush(chromosome.PolyColor);
                        colorBrushes[chromosome.PolyColor] = brush;
                    }
                    
                    var points = chromosome.PolygonArray;
                    if (Settings.Polygon == Settings.PolygonType.Lines)
                        graphics.FillPolygon(brush, points);
                    else
                        graphics.FillClosedCurve(brush, points);
                }
                
                foreach (var cachedBrush in colorBrushes.Values)
                {
                    cachedBrush.Dispose();
                }
            }
            return bitmap;
        }

        public Bitmap GetPicture(int minX, int minY, int maxX, int maxY)
        {
            Bitmap bitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                var boundingRectangle = new Rectangle(minX, minY, maxX-minX, maxY-minY);
                graphics.SetClip(boundingRectangle);
                graphics.Clear(Color.Black);
                
                var colorBrushes = new Dictionary<Color, SolidBrush>();
                
                foreach (Chromosome chromosome in chromosomes)
                {
                    if (PolygonInRectangle(boundingRectangle, chromosome.Polygon))
                    {
                        if (!colorBrushes.TryGetValue(chromosome.PolyColor, out var brush))
                        {
                            brush = new SolidBrush(chromosome.PolyColor);
                            colorBrushes[chromosome.PolyColor] = brush;
                        }
                        
                        var points = chromosome.PolygonArray;
                        if (Settings.Polygon == Settings.PolygonType.Lines)
                            graphics.FillPolygon(brush, points);
                        else
                            graphics.FillClosedCurve(brush, points);
                    }
                }
                
                foreach (var cachedBrush in colorBrushes.Values)
                {
                    cachedBrush.Dispose();
                }
            }
            return bitmap;
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