#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
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

            Bitmap originalImage = (Bitmap)foriginalImage.Bitmap.Clone();
            Bitmap bC = new Bitmap(generatedImage.Width, generatedImage.Height, PixelFormat.Format32bppArgb);

            BitmapData genData = generatedImage.LockBits(
                new Rectangle(0, 0, generatedImage.Width, generatedImage.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData origData = originalImage.LockBits(
                new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData diffData = bC.LockBits(
                new Rectangle(0, 0, bC.Width, bC.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            unchecked
            {
                unsafe
                {
                    FastBitmap.PixelData* pGen = (FastBitmap.PixelData*)genData.Scan0.ToPointer();
                    FastBitmap.PixelData* pOrig = (FastBitmap.PixelData*)origData.Scan0.ToPointer();
                    FastBitmap.PixelData* pDiff = (FastBitmap.PixelData*)diffData.Scan0.ToPointer();

                    int width = generatedImage.Width;
                    int height = generatedImage.Height;
                    int stride = genData.Stride / sizeof(FastBitmap.PixelData);
                    int origStride = origData.Stride / sizeof(FastBitmap.PixelData);
                    int diffStride = diffData.Stride / sizeof(FastBitmap.PixelData);

                    long total = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int genIdx = y * stride + x;
                            int origIdx = y * origStride + x;
                            int diffIdx = y * diffStride + x;

                            int diffR = Math.Abs(pGen[genIdx].R - pOrig[origIdx].R);
                            int diffG = Math.Abs(pGen[genIdx].G - pOrig[origIdx].G);
                            int diffB = Math.Abs(pGen[genIdx].B - pOrig[origIdx].B);

                            byte avg = (byte)((diffR + diffG + diffB) / 3);
                            pDiff[diffIdx].R = avg;
                            pDiff[diffIdx].G = avg;
                            pDiff[diffIdx].B = avg;
                            pDiff[diffIdx].A = 255;

                            int a = diffR + diffG + diffB;
                            total += a * a;
                            Differences[x, y] = total;
                        }
                    }
                }
            }

            generatedImage.UnlockBits(genData);
            originalImage.UnlockBits(origData);
            bC.UnlockBits(diffData);
            originalImage.Dispose();

            DifferenceImage = bC;
            return bC;
        }
    }
}