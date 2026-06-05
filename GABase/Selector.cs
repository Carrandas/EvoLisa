#region

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using GABase.Rendering;

#endregion

namespace GABase
{
    public class Selector : IDisposable
    {
        private readonly FastBitmap _resizedOriginalImage;
        private readonly Bitmap _referenceBitmap;

        // Pluggable rasterization backend (GDI+ or SkiaSharp CPU raster) that owns the
        // current-best and candidate pixel buffers and renders polygons into them.
        private readonly IRasterBackend _backend;

        // Snapshot of the reference image pixels so we never have to LockBits it per evaluation.
        private readonly Pixel[] _referencePixels;
        private readonly int _referenceWidth;
        private readonly int _referenceHeight;

        // Cached per-pixel squared error (unweighted) of the current-best image vs the
        // reference. Lets the fitness comparison read the current-best contribution as a
        // plain sum instead of recomputing it from pixels every mutation. Kept in sync
        // incrementally on accept and rebuilt on full re-render.
        private readonly int[] _currentBestError;

        // Opt-in phase profiling (off by default = zero overhead in the hot path).
        // When enabled, accumulates wall-clock spent in each phase of EvaluateMutation.
        public static bool ProfilingEnabled = false;
        private long _renderTicks;
        private long _fitnessTicks;
        private long _copyTicks;
        private long _evalCount;
        private long _acceptCount;

        public Selector(FastBitmap fOriginalBitMap)
        {
            _resizedOriginalImage = fOriginalBitMap;
            _referenceBitmap = fOriginalBitMap.Bitmap.Clone(
                new Rectangle(0, 0, fOriginalBitMap.Width, fOriginalBitMap.Height),
                PixelFormat.Format32bppArgb);

            _backend = Settings.UseSkiaRenderer
                ? new SkiaRasterBackend(Settings.ScreenWidth, Settings.ScreenHeight)
                : new GdiRasterBackend(Settings.ScreenWidth, Settings.ScreenHeight);

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
        /// Render the full population to the current-best buffer. Call after initialization and resize.
        /// </summary>
        public void FullRender(Population pop)
        {
            _backend.RenderFull(pop);

            // The current-best raster changed wholesale; rebuild the cached per-pixel error.
            RebuildCurrentBestError();
        }

        /// <summary>
        /// Recompute the cached per-pixel squared error for the entire current-best image.
        /// Call after any full re-render of the current-best buffer.
        /// </summary>
        private void RebuildCurrentBestError()
        {
            var region = new Rectangle(0, 0, _referenceWidth, _referenceHeight);
            PixelView pv = _backend.LockCurrentBest(region, false);
            unsafe
            {
                fixed (Pixel* pRef = _referencePixels)
                fixed (int* pErr = _currentBestError)
                {
                    PixelMath.RebuildError(pv.Scan0, pv.Stride, pRef, _referenceWidth, pErr,
                        _referenceWidth, _referenceHeight);
                }
            }
            _backend.UnlockCurrentBest();
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

            // Clamp to actual buffer dimensions (not Settings, which can change mid-operation)
            int bmpWidth = _backend.Width;
            int bmpHeight = _backend.Height;
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            if (maxX > bmpWidth) maxX = bmpWidth;
            if (maxY > bmpHeight) maxY = bmpHeight;
            if (minX >= maxX || minY >= maxY)
            {
                newPartialFitness = long.MaxValue;
                return false;
            }

            int width = maxX - minX;
            int height = maxY - minY;
            var region = new Rectangle(minX, minY, width, height);

            bool prof = ProfilingEnabled;
            long ts0 = prof ? Stopwatch.GetTimestamp() : 0;

            // Render the mutated population in the dirty area onto the candidate buffer.
            _backend.RenderCandidateDirty(pop, region);

            long ts1 = prof ? Stopwatch.GetTimestamp() : 0;

            // Compute the mutated (candidate) fitness live; the current-best fitness for the
            // same region comes from the cached per-pixel error (no second recompute).
            long fitnessMutated, fitnessOriginal;
            var focusWeightMap = Settings.FocusWeightMap;
            bool hasFocusAreas = Settings.FocusAreas.Count > 0;

            PixelView pv = _backend.LockCandidate(region, false);
            unsafe
            {
                fixed (Pixel* pRef = _referencePixels)
                fixed (int* pErr = _currentBestError)
                {
                    PixelMath.ComputeBothPartial(pv.Scan0, pv.Stride, pRef, _referenceWidth, pErr,
                        minX, minY, width, height, focusWeightMap, _backend.Width, hasFocusAreas,
                        out fitnessMutated, out fitnessOriginal);
                }
            }
            _backend.UnlockCandidate();

            long ts2 = prof ? Stopwatch.GetTimestamp() : 0;

            newPartialFitness = fitnessMutated;

            // Parsimony pressure: a mutation must overcome its complexity cost delta.
            // Adding (costDelta > 0) requires the pixel error to drop by at least costDelta;
            // removing (costDelta < 0) tolerates the error rising by up to |costDelta|.
            bool accepted = fitnessMutated <= fitnessOriginal - costDelta;

            if (accepted)
            {
                // Copy the dirty area from candidate to current-best and refresh the cache.
                PixelView cpv = _backend.LockCandidate(region, false);
                PixelView bpv = _backend.LockCurrentBest(region, true);
                unsafe
                {
                    fixed (Pixel* pRef = _referencePixels)
                    fixed (int* pErr = _currentBestError)
                    {
                        PixelMath.CopyAndUpdateError(cpv.Scan0, cpv.Stride, bpv.Scan0, bpv.Stride,
                            pRef, _referenceWidth, pErr, minX, minY, width, height);
                    }
                }
                _backend.UnlockCurrentBest();
                _backend.UnlockCandidate();
            }

            if (prof)
            {
                long ts3 = Stopwatch.GetTimestamp();
                _renderTicks += ts1 - ts0;
                _fitnessTicks += ts2 - ts1;
                _copyTicks += ts3 - ts2; // copy phase (zero work when rejected)
                _evalCount++;
                if (accepted) _acceptCount++;
            }

            return accepted;
        }

        /// <summary>
        /// Reset the accumulated phase-timing counters.
        /// </summary>
        public void ResetPhaseTimings()
        {
            _renderTicks = 0;
            _fitnessTicks = 0;
            _copyTicks = 0;
            _evalCount = 0;
            _acceptCount = 0;
        }

        /// <summary>
        /// Human-readable breakdown of where EvaluateMutation spent its time, by phase.
        /// Requires ProfilingEnabled to have been set while running.
        /// </summary>
        public string GetPhaseTimingsReport()
        {
            double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
            double render = ToMs(_renderTicks);
            double fitness = ToMs(_fitnessTicks);
            double copy = ToMs(_copyTicks);
            double total = render + fitness + copy;
            if (total <= 0)
                return "Phase timings: (no data - ProfilingEnabled was false)";

            double acceptRate = _evalCount > 0 ? _acceptCount * 100.0 / _evalCount : 0;
            string backend = Settings.UseSkiaRenderer ? "SkiaSharp CPU" : "GDI+";
            return
                $"Backend: {backend}\n" +
                $"Phase timings over {_evalCount} evals ({acceptRate:F1}% accepted):\n" +
                $"  Render:       {render,8:F0} ms ({render / total * 100,4:F1}%)\n" +
                $"  Fitness:      {fitness,8:F0} ms ({fitness / total * 100,4:F1}%)\n" +
                $"  Copy(accept): {copy,8:F0} ms ({copy / total * 100,4:F1}%)";
        }

        /// <summary>
        /// Total squared error (unweighted) of the current-best image as rendered by this
        /// Selector's own backend. This is the fitness the GA actually optimizes, so it is
        /// the fair quality metric when comparing rendering backends (the Evolver's public
        /// "fitness" is recomputed via GDI+ and is unfair to non-GDI+ backends).
        /// </summary>
        public long GetCurrentBestErrorSum()
        {
            long sum = 0;
            var err = _currentBestError;
            for (int i = 0; i < err.Length; i++)
                sum += err[i];
            return sum;
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

        public void Dispose()
        {
            _backend?.Dispose();
            _referenceBitmap?.Dispose();
        }
    }
}
