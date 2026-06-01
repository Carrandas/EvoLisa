using System;
using System.Collections.Generic;
using System.Drawing;

namespace GABase
{
    public class Chromosome
    {
        public List<Point> Polygon { get; set; }
        public Point[] PolygonArray { get; private set; }
        public Color PolyColor { get; set; }
        public Rectangle BoundingBox { get; private set; }
        private readonly int _size;

        public Chromosome(int size)
        {
            _size = size;
            Polygon = new List<Point>(size);
            PolygonArray = Array.Empty<Point>();
        }

        public void UpdatePolygonArray()
        {
            PolygonArray = Polygon.ToArray();
            UpdateBoundingBox();
        }

        public void UpdateBoundingBox()
        {
            if (Polygon.Count == 0)
            {
                BoundingBox = Rectangle.Empty;
                return;
            }
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0; i < Polygon.Count; i++)
            {
                var p = Polygon[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            BoundingBox = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        public void GenerateRandomChromosome()
        {
            for (int i = 0; i < Settings.MinPolygonPointCount; i++ )
            {
                int x = RandomGenerator.GetRandomInt(Settings.ScreenWidth);
                int y = RandomGenerator.GetRandomInt(Settings.ScreenHeight);
                Polygon.Insert(i, new Point(x, y));
            }
            PolyColor = RandomGenerator.GetRandomColor();
            UpdatePolygonArray();
        }

        public void GenerateRandomSmallChromosome()
        {
            int x = RandomGenerator.GetRandomInt(Settings.ScreenWidth);
            int y = RandomGenerator.GetRandomInt(Settings.ScreenHeight);
            for (int i = 0; i < Settings.MinPolygonPointCount; i++)
            {
                int xMovement = RandomGenerator.GetRandomInt(10);
                xMovement -= 5;
                int yMovement = RandomGenerator.GetRandomInt(10);
                yMovement -= 5;

                Point p = new Point(x, y);
                p.X += xMovement;
                if (p.X < 0)
                    p.X = 0;
                else if (p.X >= Settings.ScreenWidth)
                    p.X = Settings.ScreenWidth;

                p.Y += yMovement;
                if (p.Y < 0)
                    p.Y = 0;
                else if (p.Y >= Settings.ScreenHeight)
                    p.Y = Settings.ScreenHeight;

                //Polygon[i] = p;
                Polygon.Insert(i, p);
            }
            PolyColor = RandomGenerator.GetRandomColor();
            UpdatePolygonArray();
        }

        public void GenerateRandomSmallChromosome2(FastBitmap originalPictureBitmap, long[,] differences)
        {
            // Use the bitmap's actual dimensions to avoid race with Settings during resize
            int bmpWidth = originalPictureBitmap.Width;
            int bmpHeight = originalPictureBitmap.Height;

            Point p1 = GetPoint(differences, bmpWidth, bmpHeight);
            int x = p1.X;
            int y = p1.Y;
            for (int i = 0; i < Settings.MinPolygonPointCount; i++)
            {
                int xMovement = RandomGenerator.GetRandomInt(20);
                xMovement -= 10;
                int yMovement = RandomGenerator.GetRandomInt(20);
                yMovement -= 10;

                Point p = new Point(x, y);
                p.X += xMovement;
                if (p.X < 0)
                    p.X = 0;
                else if (p.X >= bmpWidth)
                    p.X = bmpWidth - 1;

                p.Y += yMovement;
                if (p.Y < 0)
                    p.Y = 0;
                else if (p.Y >= bmpHeight)
                    p.Y = bmpHeight - 1;

                Polygon.Insert(i, p);
            }

            PolyColor = RandomGenerator.ChangeColor(originalPictureBitmap.GetPixel(p1.X, p1.Y));
            UpdatePolygonArray();
        }

        private Point GetPoint(long[,] differences, int width, int height)
        {
            if (differences != null &&
                differences.GetUpperBound(0) >= width - 1 &&
                differences.GetUpperBound(1) >= height - 1)
            {
                long maximum = differences[width - 1, height - 1];
                if (maximum > 0)
                {
                    long random = RandomGenerator.GetRandomLong(maximum);
                    long previousFitnesse = 0;
                    for (int x = 0; x <= differences.GetUpperBound(0); x++)
                    {
                        for (int y = 0; y <= differences.GetUpperBound(1); y++)
                        {
                            long currentFitnesse = differences[x, y];
                            if (currentFitnesse >= random && (previousFitnesse < random))
                            {
                                return new Point(Math.Min(x, width - 1), Math.Min(y, height - 1));
                            }
                            previousFitnesse = currentFitnesse;
                        }
                    }
                }
            }

            int px = RandomGenerator.GetRandomInt(width);
            int py = RandomGenerator.GetRandomInt(height);
            return new Point(px, py);
        }

        public Chromosome Clone()
        {
            Chromosome chromosome = new Chromosome(_size);
            chromosome.PolyColor = PolyColor;
            chromosome.Polygon.AddRange(Polygon);
            chromosome.PolygonArray = (Point[])PolygonArray.Clone();
            chromosome.BoundingBox = BoundingBox;
            return chromosome;
        }
    }
}
