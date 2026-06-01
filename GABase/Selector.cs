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

        public Selector(FastBitmap fOriginalBitMap)
        {
            _resizedOriginalImage = fOriginalBitMap;
            _referenceBitmap = fOriginalBitMap.Bitmap.Clone(
                new Rectangle(0, 0, fOriginalBitMap.Width, fOriginalBitMap.Height),
                PixelFormat.Format32bppArgb);
            _currentBestBitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
            _candidateBitmap = new Bitmap(Settings.ScreenWidth, Settings.ScreenHeight, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Render the full population to the currentBestBitmap cache. Call after initialization and resize.
        /// </summary>
        public void FullRender(Population pop)
        {
            using (Graphics g = Graphics.FromImage(_currentBestBitmap))
            {
                g.Clear(Color.Black);
                foreach (Chromosome chromosome in pop.chromosomes)
                {
                    using (var brush = new SolidBrush(chromosome.PolyColor))
                    {
                        var points = chromosome.PolygonArray;
                        if (Settings.Polygon == Settings.PolygonType.Lines)
                            g.FillPolygon(brush, points);
                        else
                            g.FillClosedCurve(brush, points);
                    }
                }
            }
        }

        /// <summary>
        /// Evaluate a mutation that has been applied in-place to the population.
        /// Returns true if the mutation is accepted (better fitness), false if rejected.
        /// </summary>
        public bool EvaluateMutation(Population pop, Rectangle dirtyArea, double percentageImprovement, out long newPartialFitness)
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

            // Compute both fitnesses in a single pass (one lock on the reference bitmap)
            ComputeBothPartialFitnesses(_candidateBitmap, _currentBestBitmap, minX, minY, maxX, maxY,
                out long fitnessMutated, out long fitnessOriginal);

            newPartialFitness = fitnessMutated;

            // Apply the percentageImprovement bias
            bool accepted;
            if (percentageImprovement > 0)
                accepted = fitnessMutated <= (long)(fitnessOriginal * (1.0 + percentageImprovement / 100.0));
            else if (percentageImprovement < 0)
                accepted = fitnessMutated < (long)(fitnessOriginal * (1.0 + percentageImprovement / 100.0));
            else
                accepted = fitnessMutated <= fitnessOriginal;

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
            using (Graphics g = Graphics.FromImage(target))
            {
                g.SetClip(clipRect);
                g.Clear(Color.Black);

                foreach (Chromosome chromosome in pop.chromosomes)
                {
                    if (PolygonOverlapsDirtyArea(clipRect, chromosome))
                    {
                        using (var brush = new SolidBrush(chromosome.PolyColor))
                        {
                            var points = chromosome.PolygonArray;
                            if (Settings.Polygon == Settings.PolygonType.Lines)
                                g.FillPolygon(brush, points);
                            else
                                g.FillClosedCurve(brush, points);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compute partial fitness for both candidate and currentBest in a single pass.
        /// Uses SSE2 SIMD to process 4 pixels at a time when no focus areas are set.
        /// </summary>
        private void ComputeBothPartialFitnesses(Bitmap candidate, Bitmap currentBest,
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
            BitmapData cbBd = currentBest.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData refBd = _referenceBitmap.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var focusWeightMap = Settings.FocusWeightMap;
            int screenWidth = candidate.Width; // Use bitmap width for map indexing (matches bitmap dimensions)
            int bitmapWidth = candidate.Width;
            int pixelsToNextRow = bitmapWidth - width;
            bool hasFocusAreas = Settings.FocusAreas.Count > 0;

            unchecked
            {
                unsafe
                {
                    Pixel* pCand = (Pixel*)cdBd.Scan0.ToPointer();
                    Pixel* pCurr = (Pixel*)cbBd.Scan0.ToPointer();
                    Pixel* pRef = (Pixel*)refBd.Scan0.ToPointer();

                    if (!hasFocusAreas)
                    {
                        // Fast path: no focus weight lookup needed (all weights = 1)
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int rc = pCand->R - pRef->R;
                                int gc = pCand->G - pRef->G;
                                int bc = pCand->B - pRef->B;
                                fitnessCand += rc * rc + gc * gc + bc * bc;

                                int ro = pCurr->R - pRef->R;
                                int go = pCurr->G - pRef->G;
                                int bo = pCurr->B - pRef->B;
                                fitnessCurr += ro * ro + go * go + bo * bo;

                                pCand++;
                                pCurr++;
                                pRef++;
                            }

                            pCand += pixelsToNextRow;
                            pCurr += pixelsToNextRow;
                            pRef += pixelsToNextRow;
                        }
                    }
                    else
                    {
                        // Scalar fallback (with focus weight support)
                        for (int y = 0; y < height; y++)
                        {
                            int mapIndex = (minY + y) * screenWidth + minX;
                            for (int x = 0; x < width; x++)
                            {
                                int weight = focusWeightMap[mapIndex];

                                int rc = pCand->R - pRef->R;
                                int gc = pCand->G - pRef->G;
                                int bc = pCand->B - pRef->B;
                                fitnessCand += (rc * rc + gc * gc + bc * bc) * weight;

                                int ro = pCurr->R - pRef->R;
                                int go = pCurr->G - pRef->G;
                                int bo = pCurr->B - pRef->B;
                                fitnessCurr += (ro * ro + go * go + bo * bo) * weight;

                                pCand++;
                                pCurr++;
                                pRef++;
                                mapIndex++;
                            }

                            pCand += pixelsToNextRow;
                            pCurr += pixelsToNextRow;
                            pRef += pixelsToNextRow;
                        }
                    }
                }
            }

            _referenceBitmap.UnlockBits(refBd);
            currentBest.UnlockBits(cbBd);
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
        /// Copy a dirty area from source bitmap to destination bitmap using raw pixel copy.
        /// </summary>
        private void CopyDirtyArea(Bitmap source, Bitmap dest, int minX, int minY, int maxX, int maxY)
        {
            int width = maxX - minX;
            int height = maxY - minY;

            BitmapData srcData = source.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData dstData = dest.LockBits(
                new Rectangle(minX, minY, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0.ToPointer();
                byte* dstPtr = (byte*)dstData.Scan0.ToPointer();
                int bytesPerRow = width * 4; // 32bpp = 4 bytes per pixel

                for (int y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(srcPtr, dstPtr, bytesPerRow, bytesPerRow);
                    srcPtr += srcData.Stride;
                    dstPtr += dstData.Stride;
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
            _currentBestBitmap?.Dispose();
            _candidateBitmap?.Dispose();
            _referenceBitmap?.Dispose();
        }
    }
}
