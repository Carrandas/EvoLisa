using System;
using System.Drawing;
using SkiaSharp;

namespace GABase.Rendering
{
    /// <summary>
    /// SkiaSharp CPU raster backend. Renders into two persistent <see cref="SKBitmap"/>
    /// buffers (BGRA32) whose pixel memory is read directly by the fitness loop, so there
    /// is no per-evaluation LockBits/Flush handshake as GDI+ requires.
    ///
    /// NOTE: Skia's blend/rounding is not bit-identical to GDI+, so absolute fitness values
    /// will differ slightly between backends. The benchmark compares throughput and
    /// convergence quality, not exact pixel equality.
    /// </summary>
    public sealed class SkiaRasterBackend : IRasterBackend
    {
        private readonly int _width;
        private readonly int _height;

        // Sub-pixel translation applied to polygon geometry (not the clip/clear) so Skia's
        // pixel-center coverage sampling aligns with GDI+. GDI+ treats integer coordinates
        // as grid intersections; shifting Skia by half a pixel makes the two rasterizers
        // agree on edge pixels. Tunable so the cross-render diff harness can sweep it.
        public static float PixelOffset = 0.5f;

        private readonly SKBitmap _currentBest;
        private readonly SKBitmap _candidate;
        private readonly SKCanvas _currentBestCanvas;
        private readonly SKCanvas _candidateCanvas;
        private readonly SKPaint _paint;
        private readonly SKPath _path;

        public SkiaRasterBackend(int width, int height)
        {
            _width = width;
            _height = height;

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _currentBest = new SKBitmap(info);
            _candidate = new SKBitmap(info);
            _currentBestCanvas = new SKCanvas(_currentBest);
            _candidateCanvas = new SKCanvas(_candidate);

            // Aliased solid fills, matching GDI+'s default SmoothingMode.None.
            _paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = false,
                BlendMode = SKBlendMode.SrcOver
            };
            _path = new SKPath();
        }

        public int Width => _width;
        public int Height => _height;

        public void RenderFull(Population pop)
        {
            var canvas = _currentBestCanvas;
            canvas.Save();
            canvas.Clear(SKColors.Black);
            canvas.Translate(PixelOffset, PixelOffset);
            FillPolygons(canvas, pop, null);
            canvas.Restore();
        }

        public void RenderCandidateDirty(Population pop, Rectangle clip)
        {
            var canvas = _candidateCanvas;
            canvas.Save();
            // Clip and clear in device space (integer pixels)...
            canvas.ClipRect(SKRect.Create(clip.X, clip.Y, clip.Width, clip.Height));
            canvas.Clear(SKColors.Black);
            // ...then shift only the polygon geometry to align edge sampling with GDI+.
            canvas.Translate(PixelOffset, PixelOffset);
            FillPolygons(canvas, pop, clip);
            canvas.Restore();
            // CPU raster writes straight to the pixel buffer; no flush needed.
        }

        private void FillPolygons(SKCanvas canvas, Population pop, Rectangle? clip)
        {
            var chromosomes = pop.chromosomes;
            int count = chromosomes.Count;
            for (int i = 0; i < count && i < chromosomes.Count; i++)
            {
                var chromosome = chromosomes[i];
                if (clip.HasValue && !RasterUtil.PolygonOverlapsDirtyArea(clip.Value, chromosome))
                    continue;

                var points = chromosome.PolygonArray;
                if (points.Length < 2)
                    continue;

                var c = chromosome.PolyColor;
                _paint.Color = new SKColor(c.R, c.G, c.B, c.A);

                _path.Reset();
                // GDI+ FillPolygon defaults to FillMode.Alternate (even-odd); match it so
                // self-intersecting polygons fill identically across backends.
                _path.FillType = SKPathFillType.EvenOdd;
                _path.MoveTo(points[0].X, points[0].Y);
                for (int p = 1; p < points.Length; p++)
                    _path.LineTo(points[p].X, points[p].Y);
                _path.Close();

                canvas.DrawPath(_path, _paint);
            }
        }

        public PixelView LockCandidate(Rectangle region, bool write)
            => RegionView(_candidate, region);

        public void UnlockCandidate() { }

        public PixelView LockCurrentBest(Rectangle region, bool write)
            => RegionView(_currentBest, region);

        public void UnlockCurrentBest() { }

        private static PixelView RegionView(SKBitmap bitmap, Rectangle region)
        {
            int stride = bitmap.RowBytes;
            IntPtr basePtr = bitmap.GetPixels();
            long offset = (long)region.Y * stride + (long)region.X * 4;
            return new PixelView(new IntPtr(basePtr.ToInt64() + offset), stride);
        }

        public void Dispose()
        {
            _path?.Dispose();
            _paint?.Dispose();
            _candidateCanvas?.Dispose();
            _currentBestCanvas?.Dispose();
            _candidate?.Dispose();
            _currentBest?.Dispose();
        }
    }
}
