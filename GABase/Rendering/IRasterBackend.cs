using System;
using System.Drawing;

namespace GABase.Rendering
{
    /// <summary>
    /// A raw view onto a region of a BGRA32 pixel buffer. <see cref="Scan0"/> points at
    /// the top-left pixel of the requested region; <see cref="Stride"/> is the byte
    /// distance between consecutive rows of the underlying full-size buffer.
    /// </summary>
    public readonly struct PixelView
    {
        public readonly IntPtr Scan0;
        public readonly int Stride;

        public PixelView(IntPtr scan0, int stride)
        {
            Scan0 = scan0;
            Stride = stride;
        }
    }

    /// <summary>
    /// Abstracts the rasterization backend used by <see cref="Selector"/> so the hot
    /// evaluation loop can render with either GDI+ or SkiaSharp while sharing the exact
    /// same fitness/copy math (see <see cref="PixelMath"/>).
    ///
    /// Two persistent BGRA32 buffers are owned by the backend:
    ///  - "current best": the accepted image so far (rendered fully on FullRender).
    ///  - "candidate":    scratch buffer the mutated population is drawn into per eval.
    ///
    /// Lock* returns a <see cref="PixelView"/> for direct pixel access; for GDI+ this maps
    /// to LockBits/UnlockBits, for Skia it is a persistent pointer (Unlock is a no-op).
    /// </summary>
    public interface IRasterBackend : IDisposable
    {
        int Width { get; }
        int Height { get; }

        /// <summary>Render the whole population into the current-best buffer.</summary>
        void RenderFull(Population pop);

        /// <summary>
        /// Render the mutated population into the candidate buffer, restricted to the
        /// given clip rectangle (clears the clip to black first). Must guarantee the
        /// pixels are committed and readable once this returns.
        /// </summary>
        void RenderCandidateDirty(Population pop, Rectangle clip);

        PixelView LockCandidate(Rectangle region, bool write);
        void UnlockCandidate();

        PixelView LockCurrentBest(Rectangle region, bool write);
        void UnlockCurrentBest();
    }

    public static class RasterUtil
    {
        /// <summary>
        /// True when the chromosome's bounding box overlaps the dirty rectangle and thus
        /// could contribute pixels to the region being re-rendered.
        /// </summary>
        public static bool PolygonOverlapsDirtyArea(Rectangle rectangle, Chromosome chromosome)
        {
            var bb = chromosome.BoundingBox;
            if (rectangle.Right < bb.Left || bb.Right < rectangle.Left)
                return false;
            if (rectangle.Bottom < bb.Top || bb.Bottom < rectangle.Top)
                return false;
            return true;
        }
    }
}
