#region

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

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
            // Snapshot to handle concurrent access from migration timer.
            // If the list is modified during ToArray(), it may throw — caller should catch.
            var snapshot = chromosomes.ToArray();
            foreach (var chromosome in snapshot)
            {
                if (chromosome != null)
                    pop.chromosomes.Add(chromosome.Clone());
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
                    if (PolygonInRectangle(boundingRectangle, chromosome))
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
        /// Check if the chromosome's bounding box overlaps with the given rectangle.
        /// </summary>
        private bool PolygonInRectangle(Rectangle rectangle, Chromosome chromosome)
        {
            var bb = chromosome.BoundingBox;
            // Two rectangles do NOT overlap if one is entirely to the left/right/above/below the other
            if (rectangle.Right < bb.Left || bb.Right < rectangle.Left)
                return false;
            if (rectangle.Bottom < bb.Top || bb.Bottom < rectangle.Top)
                return false;
            return true;
        }
    }
}