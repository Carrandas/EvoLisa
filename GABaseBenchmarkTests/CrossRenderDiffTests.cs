using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GABase;
using GABase.Rendering;

namespace GABaseBenchmarkTests
{
    /// <summary>
    /// Measures how much the GDI+ and SkiaSharp rasterizers disagree when drawing the
    /// exact same polygon set, and sweeps the Skia sub-pixel offset to find the value that
    /// best aligns them. Disagreement is the per-pixel sum of squared RGB error between the
    /// two rasters (0 = identical).
    /// </summary>
    [TestClass]
    public class CrossRenderDiffTests
    {
        private const int Width = 120;
        private const int Height = 160;
        private const int PolygonCount = 800;

        [TestMethod]
        public void CrossRenderDiffSweep()
        {
            Settings.ScreenWidth = Width;
            Settings.ScreenHeight = Height;
            Settings.Polygon = Settings.PolygonType.Lines;
            Settings.FocusAreas.Clear();

            var pop = BuildDeterministicPopulation();

            byte[] gdi;
            using (var gdiBackend = new GdiRasterBackend(Width, Height))
                gdi = RenderToBytes(gdiBackend, pop);

            var offsets = new[] { -1.0f, -0.75f, -0.5f, -0.25f, 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };

            var sb = new StringBuilder();
            sb.AppendLine("# Cross-Render Diff Sweep (GDI+ vs SkiaSharp CPU)");
            sb.AppendLine();
            sb.AppendLine($"- Image: {Width}x{Height}, Polygons: {PolygonCount}, Fill: Lines (even-odd)");
            sb.AppendLine($"- Metric: total per-pixel SSD over RGB between GDI+ and Skia rasters (lower = better aligned)");
            sb.AppendLine();
            sb.AppendLine("| Skia PixelOffset | Cross-render SSD | Mean sq err / pixel |");
            sb.AppendLine("|---|---|---|");

            float bestOffset = 0f;
            long bestSsd = long.MaxValue;
            int pixelCount = Width * Height;

            foreach (var off in offsets)
            {
                SkiaRasterBackend.PixelOffset = off;
                byte[] skia;
                using (var skiaBackend = new SkiaRasterBackend(Width, Height))
                    skia = RenderToBytes(skiaBackend, pop);

                long ssd = ComputeRgbSsd(gdi, skia);
                double meanPerPixel = (double)ssd / pixelCount;
                sb.AppendLine($"| {off,6:F2} | {ssd,14:N0} | {meanPerPixel,10:F1} |");

                if (ssd < bestSsd)
                {
                    bestSsd = ssd;
                    bestOffset = off;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"**Best offset: {bestOffset:F2}** (SSD = {bestSsd:N0})");

            Console.WriteLine(sb.ToString());

            // Restore the production default so other tests are unaffected.
            SkiaRasterBackend.PixelOffset = 0.5f;
        }

        private static long ComputeRgbSsd(byte[] a, byte[] b)
        {
            long ssd = 0;
            for (int i = 0; i < a.Length; i += 4)
            {
                int db = a[i] - b[i];
                int dg = a[i + 1] - b[i + 1];
                int dr = a[i + 2] - b[i + 2];
                ssd += db * db + dg * dg + dr * dr;
            }
            return ssd;
        }

        private static byte[] RenderToBytes(IRasterBackend backend, Population pop)
        {
            backend.RenderFull(pop);

            var region = new Rectangle(0, 0, backend.Width, backend.Height);
            PixelView pv = backend.LockCurrentBest(region, false);

            int rowBytes = backend.Width * 4;
            var buffer = new byte[backend.Height * rowBytes];
            for (int y = 0; y < backend.Height; y++)
            {
                IntPtr rowPtr = IntPtr.Add(pv.Scan0, y * pv.Stride);
                Marshal.Copy(rowPtr, buffer, y * rowBytes, rowBytes);
            }

            backend.UnlockCurrentBest();
            return buffer;
        }

        private static Population BuildDeterministicPopulation()
        {
            var rng = new Random(12345);
            var pop = new Population(PolygonCount + 16);

            for (int i = 0; i < PolygonCount; i++)
            {
                var chromosome = new Chromosome(Settings.MaxPolygonPointCount);
                int pointCount = rng.Next(3, 6);
                var points = new List<Point>(pointCount);
                for (int p = 0; p < pointCount; p++)
                    points.Add(new Point(rng.Next(Width), rng.Next(Height)));
                chromosome.Polygon = points;
                chromosome.PolyColor = Color.FromArgb(
                    rng.Next(40, 200), rng.Next(256), rng.Next(256), rng.Next(256));
                chromosome.UpdatePolygonArray();
                pop.chromosomes.Add(chromosome);
            }

            return pop;
        }
    }
}
