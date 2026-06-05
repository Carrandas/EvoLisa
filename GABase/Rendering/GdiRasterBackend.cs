using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GABase.Rendering
{
    /// <summary>
    /// GDI+ (System.Drawing) rasterization backend. This is the original rendering path:
    /// two <see cref="Bitmap"/>s drawn via persistent <see cref="Graphics"/> objects, with
    /// pixel access through LockBits/UnlockBits.
    /// </summary>
    public sealed class GdiRasterBackend : IRasterBackend
    {
        private readonly int _width;
        private readonly int _height;

        private readonly Bitmap _currentBest;
        private readonly Bitmap _candidate;
        private readonly Graphics _currentBestGraphics;
        private readonly Graphics _candidateGraphics;
        private readonly SolidBrush _brush;

        private BitmapData _candidateLock;
        private BitmapData _currentBestLock;

        public GdiRasterBackend(int width, int height)
        {
            _width = width;
            _height = height;

            _currentBest = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _candidate = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _currentBestGraphics = Graphics.FromImage(_currentBest);
            _candidateGraphics = Graphics.FromImage(_candidate);
            _brush = new SolidBrush(Color.Black);
        }

        public int Width => _width;
        public int Height => _height;

        public void RenderFull(Population pop)
        {
            Graphics g = _currentBestGraphics;
            g.ResetClip();
            g.Clear(Color.Black);
            FillPolygons(g, pop, null);
        }

        public void RenderCandidateDirty(Population pop, Rectangle clip)
        {
            Graphics g = _candidateGraphics;
            g.SetClip(clip);
            g.Clear(Color.Black);
            FillPolygons(g, pop, clip);
            // Commit GDI+ drawing before the caller reads raw pixels via LockBits.
            g.Flush(FlushIntention.Sync);
        }

        private void FillPolygons(Graphics g, Population pop, Rectangle? clip)
        {
            var chromosomes = pop.chromosomes;
            int count = chromosomes.Count;
            bool lines = Settings.Polygon == Settings.PolygonType.Lines;
            for (int i = 0; i < count && i < chromosomes.Count; i++)
            {
                var chromosome = chromosomes[i];
                if (clip.HasValue && !RasterUtil.PolygonOverlapsDirtyArea(clip.Value, chromosome))
                    continue;
                _brush.Color = chromosome.PolyColor;
                var points = chromosome.PolygonArray;
                if (lines)
                    g.FillPolygon(_brush, points);
                else
                    g.FillClosedCurve(_brush, points);
            }
        }

        public PixelView LockCandidate(Rectangle region, bool write)
        {
            _candidateLock = _candidate.LockBits(region,
                write ? ImageLockMode.WriteOnly : ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            return new PixelView(_candidateLock.Scan0, _candidateLock.Stride);
        }

        public void UnlockCandidate()
        {
            if (_candidateLock != null)
            {
                _candidate.UnlockBits(_candidateLock);
                _candidateLock = null;
            }
        }

        public PixelView LockCurrentBest(Rectangle region, bool write)
        {
            _currentBestLock = _currentBest.LockBits(region,
                write ? ImageLockMode.WriteOnly : ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            return new PixelView(_currentBestLock.Scan0, _currentBestLock.Stride);
        }

        public void UnlockCurrentBest()
        {
            if (_currentBestLock != null)
            {
                _currentBest.UnlockBits(_currentBestLock);
                _currentBestLock = null;
            }
        }

        public void Dispose()
        {
            _candidateGraphics?.Dispose();
            _currentBestGraphics?.Dispose();
            _brush?.Dispose();
            _candidate?.Dispose();
            _currentBest?.Dispose();
        }
    }
}
