#region

using System;
using System.Drawing;
using System.Drawing.Imaging;

#endregion

namespace GABase
{
    public class Selector : IDisposable
    {
        private readonly FastBitmap _resizedOriginalImage;
        private readonly Bitmap _referenceBitmap;
        private Bitmap _currentBestBitmap;
        private Bitmap _candidateBitmap;

        // Persistent GDI resources reused across every mutation to avoid per-generation allocations.
        private readonly Graphics _currentBestGraphics;
        private readonly Graphics _candidateGraphics;
        private readonly SolidBrush _brush;

        // Snapshot of the reference image pixels so we never have to LockBits it per evaluation.
        private readonly Pixel[] _referencePixels;
        private readonly int _referenceWidth;
        private readonly int _referenceHeight;

        // Cached per-pixel squared error (unweighted) of the current-best image vs the
        // reference. Lets the fitness comparison read the current-best contribution as a
        // plain sum instead of recomputing it from pixels (and locking the bitmap) every
        // mutation. Kept in sync incrementally on accept and rebuilt on full re-render.
        private readonly int[] _currentBestError;

        public Selector(FastBitmap fOriginalBitMap)
        {
            _resizedOriginalImage = fOriginalBitMap;
            _referenceBitmap = fOriginalBitMap.Bitmap.Clone(
                new Rectangle(0, 0, fOriginalBitMap.Width, fOriginalBitMap.Height),
                PixelFormat.Format32bppArgb);
            _currentBestBitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
            _candidateBitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);

            _currentBestGraphics = Graphics.FromImage(_currentBestBitmap);
            _candidateGraphics = Graphics.FromImage(_candidateBitmap);
            _brush = new SolidBrush(Color.Black);

            _referenceWidth = _referenceBitmap.Width;
            _referenceHeight = _referenceBitmap.Height;
            _referencePixels = SnapshotReferencePixels(_referenceBitmap, _referenceWidth, _referenceHeight);

            _currentBestError = new int[_referenceWidth * _referenceHeight];
            RebuildCurrentBestError();
        }

        /// <summary>
        /// Copy the reference image into a managed Pixel array once so the per-mutation
        /// fitness loop never needs to LockBits/UnlockBits the reference bitmap.
        /// </summary>
        private static Pixel[] SnapshotReferencePixels(Bitmap reference, int width, int height)
        {
            var pixels = new Pixel[width * height];
            BitmapData bd = reference.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* rowPtr = (byte*)bd.Scan0.ToPointer();
                fixed (Pixel* dstBase = pixels)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Pixel* src = (Pixel*)rowPtr;
                        Pixel* dst = dstBase + y * width;
                        for (int x = 0; x < width; x++)
                            dst[x] = src[x];
                        rowPtr += bd.Stride;
                    }
                }
            }
            reference.UnlockBits(bd);
            return pixels;
        }

        /// <summary>
        /// Render the full population to the currentBestBitmap cache. Call after initialization and resize.
        /// </summary>
        public void FullRender(Population pop)
        {
            Graphics g = _currentBestGraphics;
            g.ResetClip();
            g.Clear(Color.Black);
            var chromosomes = pop.chromosomes;
            int count = chromosomes.Count;
            for (int i = 0; i < count && i < chromosomes.Count; i++)
            {
                var chromosome = chromosomes[i];
                _brush.Color = chromosome.PolyColor;
                var points = chromosome.PolygonArray;
                if (Settings.Polygon == Settings.PolygonType.Lines)
                    g.FillPolygon(_brush, points);
                else
                    g.FillClosedCurve(_brush, points);
            }

            // The current-best raster changed wholesale; rebuild the cached per-pixel error.
            RebuildCurrentBestError();
        }

        /// <summary>
        /// Recompute the cached per-pixel squared error for the entire current-best image.
        /// Call after any full re-render of the current-best bitmap.
        /// </summary>
        private void RebuildCurrentBestError()
        {
            int width = _currentBestBitmap.Width;
            int height = _currentBestBitmap.Height;
            int refWidth = _referenceWidth;

            BitmapData bd = _currentBestBitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unchecked
            {
                unsafe
                {
                    byte* rowPtr = (byte*)bd.Scan0.ToPointer();
                    fixed (Pixel* pRefBase = _referencePixels)
                    fixed (int* pErrBase = _currentBestError)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Pixel* pCur = (Pixel*)rowPtr;
                            Pixel* pRef = pRefBase + y * refWidth;
                            int* pErr = pErrBase + y * refWidth;
                            for (int x = 0; x < width; x++)
                            {
                                int r = pCur->R - pRef->R;
                                int g = pCur->G - pRef->G;
                                int b = pCur->B - pRef->B;
                                *pErr = r * r + g * g + b * b;
                                pCur++;
                                pRef++;
                                pErr++;
                            }
                            rowPtr += bd.Stride;
                        }
                    }
                }
            }

            _currentBestBitmap.UnlockBits(bd);
        }

        /// <summary>
        /// Evaluate a mutation that has been applied in-place to the population.
        /// Returns true if the mutation is accepted (better fitness), false if rejected.
        /// <paramref name="costDelta"/> is the change in complexity cost (parsimony
        /// penalty) caused by this mutation, in squared-error units: positive when the
        /// mutation adds polygons/points, negative when it removes them, zero otherwise.
        /// The mutation must improve pixel error by at least costDelta to be accepted.
        /// </summary>
        public bool EvaluateMutation(Population pop, Rectangle dirtyArea, long costDelta, out long newPartialFitness)
        {
            int minX = dirtyArea.X;
            int minY = dirtyArea.Y;
            int maxX = dirtyArea.X + dirtyArea.Width;
            int maxY = dirtyArea.Y + dirtyArea.Height;

            // Clamp to actual bitmap dimensions (not Settings, which can change mid-operation)
            int bmpWidth = _candidateBitmap.Width;
            int bmpHeight = _candidateBitmap.Height;
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            if (maxX > bmpWidth) maxX = bmpWidth;
            if (maxY > bmpHeight) maxY = bmpHeight;
            if (minX >= maxX || minY >= maxY)
            {
                newPartialFitness = long.MaxValue;
                return false;
            }

            // Render the mutated population in the dirty area onto the candidate bitmap
            RenderDirtyArea(pop, _candidateBitmap, minX, minY, maxX, maxY);
            // Ensure all GDI+ drawing is committed before we read raw pixels via LockBits.
            _candidateGraphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);

            // Compute the mutated (candidate) fitness live; the current-best fitness for
            // the same region comes from the cached per-pixel error (no second lock/recompute).
            ComputeBothPartialFitnesses(_candidateBitmap, minX, minY, maxX, maxY,
                out long fitnessMutated, out long fitnessOriginal);

            newPartialFitness = fitnessMutated;

            // Parsimony pressure: a mutation must overcome its complexity cost delta.
            // Adding (costDelta > 0) requires the pixel error to drop by at least costDelta;
            // removing (costDelta < 0) tolerates the error rising by up to |costDelta|.
            bool accepted = fitnessMutated <= fitnessOriginal - costDelta;

            if (accepted)
            {
                // Copy the dirty area from candidate to currentBest
                CopyDirtyArea(_candidateBitmap, _currentBestBitmap, minX, minY, maxX, maxY);
            }

            return accepted;
        }

        /// <summary>
        /// Render only the dirty area of the population onto a target bitmap.
        /// </summary>
        private void RenderDirtyArea(Population pop, Bitmap target, int minX, int minY, int maxX, int maxY)
        {
            var clipRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
            Graphics g = _candidateGraphics;
            g.SetClip(clipRect);
            g.Clear(Color.Black);

            var chromosomes = pop.chromosomes;
            int count = chromosomes.Count;
            for (int i = 0; i < count && i < chromosomes.Count; i++)
            {
                var chromosome = chromosomes[i];
                if (PolygonOverlapsDirtyArea(clipRect, chromosome))
                {
                    _brush.Color = chromosome.PolyColor;
                    var points = chromosome.PolygonArray;
                    if (Settings.Polygon == Settings.PolygonType.Lines)
                        g.FillPolygon(_brush, points);
                    else
                        g.FillClosedCurve(_brush, points);
                }
            }
        }

        /// <summary>
        /// Compute partial fitness for the candidate live against the reference, and the
        /// current-best partial fitness from the cached per-pixel error buffer (no lock
        /// or pixel recompute for the current-best image).
        /// </summary>
        private void ComputeBothPartialFitnesses(Bitmap candidate,
            int minX, int minY, int maxX, int maxY,
            out long fitnessCand, out long fitnessCurr)
        {
            fitnessCand = 0;
            fitnessCurr = 0;
            int width = maxX - minX;
            int height = maxY - minY;

            BitmapData cdBd = candidate.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var focusWeightMap = Settings.FocusWeightMap;
            int screenWidth = candidate.Width; // Use bitmap width for map indexing (matches bitmap dimensions)
            int bitmapWidth = candidate.Width;
            int pixelsToNextRow = bitmapWidth - width;
            int refWidth = _referenceWidth;
            bool hasFocusAreas = Settings.FocusAreas.Count > 0;

            unchecked
            {
                unsafe
                {
                    Pixel* pCand = (Pixel*)cdBd.Scan0.ToPointer();

                    fixed (Pixel* pRefBase = _referencePixels)
                    fixed (int* pErrBase = _currentBestError)
                    {
                        if (!hasFocusAreas)
                        {
                            // Fast path: no focus weight lookup needed (all weights = 1)
                            for (int y = 0; y < height; y++)
                            {
                                Pixel* pRef = pRefBase + (minY + y) * refWidth + minX;
                                int* pErr = pErrBase + (minY + y) * refWidth + minX;
                                for (int x = 0; x < width; x++)
                                {
                                    int rc = pCand->R - pRef->R;
                                    int gc = pCand->G - pRef->G;
                                    int bc = pCand->B - pRef->B;
                                    fitnessCand += rc * rc + gc * gc + bc * bc;

                                    fitnessCurr += *pErr;

                                    pCand++;
                                    pRef++;
                                    pErr++;
                                }

                                pCand += pixelsToNextRow;
                            }
                        }
                        else
                        {
                            // Scalar fallback (with focus weight support)
                            for (int y = 0; y < height; y++)
                            {
                                int mapIndex = (minY + y) * screenWidth + minX;
                                Pixel* pRef = pRefBase + (minY + y) * refWidth + minX;
                                int* pErr = pErrBase + (minY + y) * refWidth + minX;
                                for (int x = 0; x < width; x++)
                                {
                                    int weight = focusWeightMap[mapIndex];

                                    int rc = pCand->R - pRef->R;
                                    int gc = pCand->G - pRef->G;
                                    int bc = pCand->B - pRef->B;
                                    fitnessCand += (rc * rc + gc * gc + bc * bc) * weight;

                                    fitnessCurr += (long)(*pErr) * weight;

                                    pCand++;
                                    pRef++;
                                    pErr++;
                                    mapIndex++;
                                }

                                pCand += pixelsToNextRow;
                            }
                        }
                    }
                }
            }

            candidate.UnlockBits(cdBd);
        }

        /// <summary>
        /// Compute partial fitness for a bitmap in the given region vs the reference image.
        /// </summary>
        private long ComputePartialFitness(Bitmap picture, int minX, int minY, int maxX, int maxY)
        {
            long fitness = 0;
            int width = maxX - minX;
            int height = maxY - minY;

            BitmapData bd = picture.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData obd = _referenceBitmap.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var focusWeightMap = Settings.FocusWeightMap;
            int screenWidth = Settings.ScreenWidth;
            int pictureWidth = picture.Width;
            int pixelsToNextRow = pictureWidth - width;

            unchecked
            {
                unsafe
                {
                    Pixel* p1 = (Pixel*)bd.Scan0.ToPointer();
                    Pixel* p2 = (Pixel*)obd.Scan0.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        int mapIndex = (minY + y) * screenWidth + minX;
                        for (int x = 0; x < width; x++)
                        {
                            int r = p1->R - p2->R;
                            int g = p1->G - p2->G;
                            int b = p1->B - p2->B;
                            int diff = r * r + g * g + b * b;
                            fitness += diff * focusWeightMap[mapIndex];
                            p1++;
                            p2++;
                            mapIndex++;
                        }

                        p1 += pixelsToNextRow;
                        p2 += pixelsToNextRow;
                    }
                }
            }

            _referenceBitmap.UnlockBits(obd);
            picture.UnlockBits(bd);

            return fitness;
        }

        /// <summary>
        /// Copy a dirty area from source bitmap to destination bitmap, and refresh the
        /// cached per-pixel error for that region (the source is the newly accepted best).
        /// </summary>
        private void CopyDirtyArea(Bitmap source, Bitmap dest, int minX, int minY, int maxX, int maxY)
        {
            int width = maxX - minX;
            int height = maxY - minY;
            int refWidth = _referenceWidth;

            BitmapData srcData = source.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData dstData = dest.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unchecked
            {
                unsafe
                {
                    byte* srcRow = (byte*)srcData.Scan0.ToPointer();
                    byte* dstRow = (byte*)dstData.Scan0.ToPointer();
                    fixed (Pixel* pRefBase = _referencePixels)
                    fixed (int* pErrBase = _currentBestError)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Pixel* pSrc = (Pixel*)srcRow;
                            Pixel* pDst = (Pixel*)dstRow;
                            Pixel* pRef = pRefBase + (minY + y) * refWidth + minX;
                            int* pErr = pErrBase + (minY + y) * refWidth + minX;
                            for (int x = 0; x < width; x++)
                            {
                                *pDst = *pSrc;

                                int r = pSrc->R - pRef->R;
                                int g = pSrc->G - pRef->G;
                                int b = pSrc->B - pRef->B;
                                *pErr = r * r + g * g + b * b;

                                pSrc++;
                                pDst++;
                                pRef++;
                                pErr++;
                            }
                            srcRow += srcData.Stride;
                            dstRow += dstData.Stride;
                        }
                    }
                }
            }

            source.UnlockBits(srcData);
            dest.UnlockBits(dstData);
        }

        private bool PolygonOverlapsDirtyArea(Rectangle rectangle, Chromosome chromosome)
        {
            var bb = chromosome.BoundingBox;
            if (rectangle.Right < bb.Left || bb.Right < rectangle.Left)
                return false;
            if (rectangle.Bottom < bb.Top || bb.Bottom < rectangle.Top)
                return false;
            return true;
        }

        #region Legacy methods (kept for DifferencePicture and full-image fitness)

        /// <summary>
        /// Select the most fit population between the two provided (legacy method).
        /// </summary>
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
        public long CalculateFitness(Population pop)
        {
            long fitnesse = 0;

            var picture = pop.GetPicture();
            BitmapData bd = picture.LockBits(
                new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData obd = _referenceBitmap.LockBits(
                new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var focusWeightMap = Settings.FocusWeightMap;
            int width = Settings.ScreenWidth;
            int height = Settings.ScreenHeight;

            unchecked
            {
                unsafe
                {
                    Pixel* p1 = (Pixel*)bd.Scan0.ToPointer();
                    Pixel* p2 = (Pixel*)obd.Scan0.ToPointer();
                    for (int i = 0; i < width * height; i++, p1++, p2++)
                    {
                        int r = p1->R - p2->R;
                        int g = p1->G - p2->G;
                        int b = p1->B - p2->B;
                        int diff = r * r + g * g + b * b;
                        fitnesse += diff * focusWeightMap[i];
                    }
                }
            }

            _referenceBitmap.UnlockBits(obd);
            picture.UnlockBits(bd);
            picture.Dispose();

            return fitnesse;
        }

        private long CalculateFitnesse(Population pop,
                              int minX,
                              int minY,
                              int maxX,
                              int maxY)
        {
            if (minX == maxX || minY == maxY) return long.MaxValue;
            long fitnesse = 0;

            var picture = pop.GetPicture(minX, minY, maxX, maxY);
            var width = maxX - minX;
            var height = maxY - minY;
            BitmapData bd = picture.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData obd = _referenceBitmap.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            var pictureWidth = picture.Width;
            var pixelsToNextRow = pictureWidth + minX - maxX;

            var focusWeightMap = Settings.FocusWeightMap;
            int screenWidth = Settings.ScreenWidth;

            unchecked
            {
                unsafe
                {
                    Pixel* p1 = (Pixel*) bd.Scan0.ToPointer();
                    Pixel* p2 = (Pixel*) obd.Scan0.ToPointer();

                    for (int y = 0; y < bd.Height; y++)
                    {
                        int mapIndex = (minY + y) * screenWidth + minX;
                        for (int x = 0; x < bd.Width; x++)
                        {
                            int r = p1->R - p2->R;
                            int g = p1->G - p2->G;
                            int b = p1->B - p2->B;
                            int diff = r * r + g * g + b * b;
                            fitnesse += diff * focusWeightMap[mapIndex];
                            p1++;
                            p2++;
                            mapIndex++;
                        }

                        p1 += pixelsToNextRow;
                        p2 += pixelsToNextRow;
                    }
                }
            }

            _referenceBitmap.UnlockBits(obd);
            picture.UnlockBits(bd);
            picture.Dispose();

            return fitnesse;
        }

        #endregion

        public struct Pixel
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
        }

        public void Dispose()
        {
            _candidateGraphics?.Dispose();
            _currentBestGraphics?.Dispose();
            _brush?.Dispose();
            _currentBestBitmap?.Dispose();
            _candidateBitmap?.Dispose();
            _referenceBitmap?.Dispose();
        }
    }
}
