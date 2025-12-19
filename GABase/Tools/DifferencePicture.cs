#region

using System;
using System.Drawing;
using GABase;

#endregion

namespace GAgeneratedImagese.Tools
{
    public static class DifferencePicture
    {
        public static long[,] Differences{ get; private set;}
        public static Image DifferenceImage{ get; private set;}

        public static Image GetDifferencePicture(Population pop, FastBitmap foriginalImage)
        {
            Differences = new long[Settings.ScreenWidth, Settings.ScreenHeight];

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
                        Differences[x, y] = total;
                    }
                }
                fbC.Release();

                bC = fbC.Bitmap;
            }

            DifferenceImage = bC;
            return bC;
        }
    }
}