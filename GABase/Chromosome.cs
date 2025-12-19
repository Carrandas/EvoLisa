using System;
using System.Collections.Generic;
using System.Drawing;
using GAgeneratedImagese.Tools;

namespace GABase
{
    public class Chromosome
    {
        //public Point[] Polygon { get; set; }
        public List<Point> Polygon { get; set; }
        public Color PolyColor { get; set; }
        private readonly int _size;

        public Chromosome(int size)
        {
            _size = size;
            Polygon = new List<Point>(size);
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
        }

        public void GenerateRandomSmallChromosome2(FastBitmap originalPictureBitmap)
        {
            //int x = RandomGenerator.GetRandomInt(Settings.ScreenWidth);
            //int y = RandomGenerator.GetRandomInt(Settings.ScreenHeight);
            Point p1 = GetPoint();
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
                else if (p.X >= Settings.ScreenWidth)
                    p.X = Settings.ScreenWidth;

                p.Y += yMovement;
                if (p.Y < 0)
                    p.Y = 0;
                else if (p.Y >= Settings.ScreenHeight)
                    p.Y = Settings.ScreenHeight;

                Polygon.Insert(i, p);
            }

            //if (DifferencePicture.DifferenceImage != null)
            //{
                //PolyColor = (DifferencePicture.DifferenceImage as Bitmap).GetPixel(x, y);
                //PolyColor = Color.FromArgb(RandomGenerator.GetRandomInt(30) + 30, PolyColor.R, PolyColor.G, PolyColor.B);
                //PolyColor = Color.FromArgb(PolyColor.A, PolyColor.R, PolyColor.G, PolyColor.B);
            //}
            //else
            {
                //PolyColor = RandomGenerator.GetRandomColor();
                PolyColor = RandomGenerator.ChangeColor(originalPictureBitmap.GetPixel(p1.X, p1.Y));
            }
    }

        private Point GetPoint()
        {
            if (DifferencePicture.Differences != null)
            {
                long maximum = DifferencePicture.Differences[Settings.ScreenWidth - 1, Settings.ScreenHeight - 1];
                long random = RandomGenerator.GetRandomLong(maximum);
                long previousFitnesse = 0;
                for (int x = 0; x <= DifferencePicture.Differences.GetUpperBound(0); x++)
                {
                    for (int y = 0; y <= DifferencePicture.Differences.GetUpperBound(1); y++)
                    {
                        long currentFitnesse = DifferencePicture.Differences[x, y];
                        if (currentFitnesse >= random && (previousFitnesse < random))
                        {
                            return new Point(x, y);
                        }
                        previousFitnesse = currentFitnesse;
                    }
                }
            }

            int px = RandomGenerator.GetRandomInt(Settings.ScreenWidth);
            int py = RandomGenerator.GetRandomInt(Settings.ScreenHeight);
            return new Point(px,py);
        }

        public Chromosome Clone()
        {
            Chromosome chromosome = new Chromosome(_size);
            chromosome.PolyColor = PolyColor;
            chromosome.Polygon.AddRange(Polygon);
            return chromosome;
        }
    }
}
