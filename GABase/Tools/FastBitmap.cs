using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace GABase
{
    /// <summary>
    /// http://www.mdibb.net/net/faster_pixel_manipulation_with_getpixel_and_setpixel_in_net/
    /// </summary>
    public unsafe class FastBitmap : IDisposable
    {
        readonly Bitmap Subject;
        int SubjectWidth;
        BitmapData bitmapData;
        Byte* pBase = null;

        public struct PixelData
        {
            public byte blue;
            public byte green;
            public byte red;
        }

        public FastBitmap(Bitmap SubjectBitmap)
        {
            this.Subject = SubjectBitmap;
            try
            {
                LockBitmap();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void Release()
        {
            try
            {
                UnlockBitmap();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Bitmap Bitmap
        {
            get
            {
                return Subject;
            }
        }

        public void SetPixel(int X, int Y, Color Colour)
        {
            try
            {
                PixelData* p = PixelAt(X, Y);
                p->red = Colour.R;
                p->green = Colour.G;
                p->blue = Colour.B;
            }
            catch (AccessViolationException ave)
            {
                throw (ave);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Color GetPixel(int X, int Y)
        {
            try
            {
                PixelData* p = PixelAt(X, Y);
                return Color.FromArgb(
                    (int)p->red,
                    (int)p->green,
                    (int)p->blue);
            }
            catch (AccessViolationException ave)
            {
                throw (ave);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public PixelData GetPixelTuple(int X, int Y)
        {
            try
            {
                PixelData* p = PixelAt(X, Y);
                //return new Tuple<byte, byte, byte>
                //(   p->red,
                //    p->green,
                //    p->blue);
                return *p;
            }
            catch (AccessViolationException ave)
            {
                throw (ave);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int Width {get { return Subject.Width; }}

        public int Height{get { return Subject.Height; }}

        private void LockBitmap()
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = Subject.GetBounds(ref unit);
            Rectangle bounds = new Rectangle(
                (int)boundsF.X,
                (int)boundsF.Y,
                (int)boundsF.Width,
                    (int)boundsF.Height);
            SubjectWidth = (int)boundsF.Width * sizeof(PixelData);
            if (SubjectWidth % 4 != 0)
            {
                SubjectWidth = 4 * (SubjectWidth / 4 + 1);
            }
            bitmapData = Subject.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        private PixelData* PixelAt(int x, int y)
        {
            return (PixelData*)(pBase + y * SubjectWidth + x * sizeof(PixelData));
        }

        private void UnlockBitmap()
        {
            Subject.UnlockBits(bitmapData);
            bitmapData = null;
            pBase = null;
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            UnlockBitmap();
            //Subject.Dispose();
        }

        #endregion
    }
}