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

        /// <summary>
        /// Select the most fit population between the two provided.
        /// PercentageImprovement indicates how much better popA has to be compared to popB to be selected.
        /// </summary>
        /// <param name="popA"></param>
        /// <param name="popB"></param>
        /// <param name="fitnesse"></param>
        /// <param name="percentageImprovement"></param>
        /// <returns></returns>
        public Population SelectPopulation(
            Population popA,
            Population popB,
            out long fitnesse, 
            double percentageImprovement)
        {
            long fitnesseA, fitnesseB;

	        fitnesseA = CalculateFitnesse(
		        popA,
		        popB.DirtyArea.X,
		        popB.DirtyArea.Y,
		        popB.DirtyArea.X + popB.DirtyArea.Width,
		        popB.DirtyArea.Y + popB.DirtyArea.Height);
	        fitnesseB = CalculateFitnesse(
		        popB,
		        popB.DirtyArea.X,
		        popB.DirtyArea.Y,
		        popB.DirtyArea.X + popB.DirtyArea.Width,
		        popB.DirtyArea.Y + popB.DirtyArea.Height);

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

        /// <summary>
        /// Calculates the fitness of the full image.
        /// </summary>
        /// <param name="pop"></param>
        /// <returns></returns>
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


        private Bitmap _bmp;
        /// <summary>
        /// Calculates the fitnesse. But only for a part of the image!
        /// </summary>
        /// <param name="pop"></param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns></returns>
        private long CalculateFitnesse(Population pop,
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