#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

#endregion

namespace GABase
{
    public class Selector
    {
        private readonly FastBitmap _resizedOriginalImage;
        private readonly Bitmap _bitmap;

        public Selector(FastBitmap fOriginalBitMap)
        {
            _resizedOriginalImage = fOriginalBitMap;
            _bitmap = fOriginalBitMap.Bitmap.Clone(new Rectangle(0, 0, fOriginalBitMap.Width, fOriginalBitMap.Height), PixelFormat.Format32bppArgb);
        }

        public Population SelectPopulation(
            Population popA,
            Population popB,
            out long fitnesse, 
            double percentageImprovement)
        {
            long fitnesseA, fitnesseB;

	        fitnesseA = CalculateFitness(
		        popA,
		        popB.DirtyArea.X,
		        popB.DirtyArea.Y,
		        popB.DirtyArea.X + popB.DirtyArea.Width,
		        popB.DirtyArea.Y + popB.DirtyArea.Height);
	        fitnesseB = CalculateFitness(
		        popB,
		        popB.DirtyArea.X,
		        popB.DirtyArea.Y,
		        popB.DirtyArea.X + popB.DirtyArea.Width,
		        popB.DirtyArea.Y + popB.DirtyArea.Height);

         //       var fitnesseA2 = CalculateFitness(popA);
         //       var fitnesseB2 = CalculateFitness(popB);
	        //if (fitnesseA < fitnesseB && fitnesseA2 > fitnesseB2) throw new ArgumentException();
	        //if (fitnesseA > fitnesseB && fitnesseA2 < fitnesseB2) throw new ArgumentException();

			if (fitnesseA < (fitnesseB * (1.0 + (percentageImprovement / 100.0))))
            {
                fitnesse = fitnesseA;
                return popA;
            }
            else
            {
                fitnesse = fitnesseB;
                return popB;
            }
        }        

        public long CalculateFitness2(Population pop)
        {
            long fitnesse = 0;
            using (FastBitmap generatedPicture = new FastBitmap(pop.GetPicture()))
            {
                for (int x = 0; x < Settings.ScreenWidth; x++)
                {
                    for (int y = 0; y < Settings.ScreenHeight; y++)
                    {
                        var originalColor = _resizedOriginalImage.GetPixelTuple(x, y);
                        var generatedColor = generatedPicture.GetPixelTuple(x, y);
                        long diffR = originalColor.red - generatedColor.red;
                        long diffB = originalColor.blue - generatedColor.blue;
                        long diffG = originalColor.green - generatedColor.green;
                        fitnesse += (diffB * diffB + diffG * diffG + diffR * diffR);
                        //fitnesse *= fitnesse;//Needed???
                    }
                }
            }

            return fitnesse;
        }

        public long CalculateFitness(Population pop)
        {
            long fitnesse = 0;

            var picture = pop.GetPicture();
            BitmapData bd = picture.LockBits(
                new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData obd = _bitmap.LockBits(
                new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unchecked
            {
                unsafe
                {
                    //Shamelessly copied from https://danbystrom.se/2008/12/14/improving-performance/
                    Pixel* p1 = (Pixel*)bd.Scan0.ToPointer();
                    Pixel* p2 = (Pixel*)obd.Scan0.ToPointer();
                    var size = Settings.ScreenWidth * Settings.ScreenHeight;
                    for (int i = size; i > 0; i--, p1++, p2++)
                    {
                        int r = p1->R - p2->R;
                        int g = p1->G - p2->G;
                        int b = p1->B - p2->B;
                        fitnesse += r * r + g * g + b * b;
                    }                    
                }
            }

            _bitmap.UnlockBits(obd);
            picture.UnlockBits(bd);

            return fitnesse;
        }

        private long CalculateFitness2(Population pop,
                                      int minX,
                                      int minY,
                                      int maxX,
                                      int maxY)
        {
            long fitnesse = 0;
            using (FastBitmap generatedPicture = new FastBitmap(pop.GetPicture(minX, minY, maxX, maxY)))
            {
                //Compare

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var originalColor = _resizedOriginalImage.GetPixelTuple(x, y);
                        var generatedColor = generatedPicture.GetPixelTuple(x, y);
                        long diffR = originalColor.red - generatedColor.red;
                        long diffG = originalColor.green - generatedColor.green;
                        long diffB = originalColor.blue - generatedColor.blue;
                        fitnesse += (diffB * diffB + diffG * diffG + diffR * diffR);
                        //fitnesse *= fitnesse;//Needed???
                    }
                }
            }

            return fitnesse;
        }

        private Bitmap _bmp;
        private long CalculateFitness(Population pop,
                              int minX,
                              int minY,
                              int maxX,
                              int maxY)
        {
            if (minX == maxX || minY == maxY) return long.MaxValue;
            long fitnesse = 0;

            var picture = pop.GetPicture(minX, minY, maxX, maxY);
            var width = maxX - minX ;
            var height = maxY - minY ;
            BitmapData bd = picture.LockBits(
                new Rectangle(minX, minY, width, height), 
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData obd = _bitmap.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            var pictureWidth = picture.Width;
            var pixelsToNextRow = pictureWidth + minX - maxX;

            unchecked
            {
                unsafe
                {
                    Pixel* p1 = (Pixel*) bd.Scan0.ToPointer();
                    Pixel* p2 = (Pixel*) obd.Scan0.ToPointer();
                    //var size = (maxX - minX) * (maxY - minY);
                    //for (int i = size; i > 0; i--, p1++, p2++)
                    //{
                    //    int r = p1->R - p2->R;
                    //    int g = p1->G - p2->G;
                    //    int b = p1->B - p2->B;
                    //    fitnesse += r * r + g * g + b * b;
                    //}

                    //Pixel* startP1 = (Pixel*)bd.Scan0.ToPointer();
                    //Pixel* startP2 = (Pixel*)obd.Scan0.ToPointer();

                    for (int y = 0; y < bd.Height; y++)
                    {
                        for (int x = 0; x < bd.Width; x++)
                        {
                            int r = p1->R - p2->R;
                            int g = p1->G - p2->G;
                            int b = p1->B - p2->B;
                            fitnesse += r * r + g * g + b * b;
                            p1++;
                            p2++;
                        }

                        //p1 = startP1 + bd.Stride * (y+1);
                        p1 += pixelsToNextRow;
                        //p2 = startP2 + obd.Stride * (y+1);
                        p2 += pixelsToNextRow;
                    }
                }
            }

            _bitmap.UnlockBits(obd);
            picture.UnlockBits(bd);

            return fitnesse;
        }

        public struct Pixel
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
        }
    }
}