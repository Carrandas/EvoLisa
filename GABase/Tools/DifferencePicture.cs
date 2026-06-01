#region

using System;
using System.Drawing;
using System.Threading.Tasks;
using GABase;

#endregion

namespace GAgeneratedImagese.Tools
{
    public static class DifferencePicture
    {
        public static (Image diffImage, long fitness, long[,] differences) GetDifferencePictureWithFitness(Population pop, FastBitmap foriginalImage)
        {
            var differences = new long[Settings.ScreenWidth, Settings.ScreenHeight];

            Bitmap generatedImage = pop.GetPicture();

            if (generatedImage.Width != foriginalImage.Width || generatedImage.Height != foriginalImage.Height)
                throw new ArgumentException("Width or height are different");

            Bitmap bC = new Bitmap(generatedImage.Width, generatedImage.Height);
            long total = 0;
            int width = generatedImage.Width;
            int height = generatedImage.Height;

            using (FastBitmap fgeneratedImage = new FastBitmap(generatedImage))
            {
                FastBitmap fbC = new FastBitmap(bC);

                var rowTotals = new long[height];
                Parallel.For(0, height, y =>
                {
                    long rowTotal = 0;
                    for (int x = 0; x < width; x++)
                    {
                        var originalColor = fgeneratedImage.GetPixelTuple(x, y);
                        var generatedColor = foriginalImage.GetPixelTuple(x, y);

                        var diffR = Math.Abs(originalColor.red - generatedColor.red);
                        var diffG = Math.Abs(originalColor.green - generatedColor.green);
                        var diffB = Math.Abs(originalColor.blue - generatedColor.blue);

                        int a = diffR + diffG + diffB;
                        int diff = (int)(a / 3);
                        fbC.SetPixel(x, y, Color.FromArgb(diff, diff, diff));

                        rowTotal += a * a;
                        differences[x, y] = rowTotal;
                    }
                    rowTotals[y] = rowTotal;
                });

                foreach (var rowSum in rowTotals)
                {
                    total += rowSum;
                }

                fbC.Release();
                bC = fbC.Bitmap;
            }

            return (bC, total, differences);
        }

        public static (Image diffImage, long[,] differences) GetDifferencePicture(Population pop, FastBitmap foriginalImage)
        {
            var differences = new long[Settings.ScreenWidth, Settings.ScreenHeight];

            Bitmap generatedImage = pop.GetPicture();

            if (generatedImage.Width != foriginalImage.Width || generatedImage.Height != foriginalImage.Height)
                throw new ArgumentException("Width or height are different");

            Bitmap bC = new Bitmap(generatedImage.Width, generatedImage.Height);

            using (FastBitmap fgeneratedImage = new FastBitmap(generatedImage))
            {

                FastBitmap fbC = new FastBitmap(bC);

                long total = 0;
                for (int x = 0; x < generatedImage.Width; x++)
                {
                    for (int y = 0; y < generatedImage.Height; y++)
                    {
                        var originalColor = fgeneratedImage.GetPixelTuple(x, y);
                        var generatedColor = foriginalImage.GetPixelTuple(x, y);

                        var diffR = Math.Abs(originalColor.red - generatedColor.red);
                        var diffG = Math.Abs(originalColor.green - generatedColor.green);
                        var diffB = Math.Abs(originalColor.blue - generatedColor.blue);

                        int a = diffR + diffG + diffB;
                        int diff = (int) (a/3);
                        fbC.SetPixel(x, y, Color.FromArgb(diff, diff, diff));

                        total += a*a;
                        differences[x, y] = total;
                    }
                }
                fbC.Release();

                bC = fbC.Bitmap;
            }

            return (bC, differences);
        }
    }
}
