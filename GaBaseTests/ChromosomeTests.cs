using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GABase;

namespace GABaseTests.ChromosomeTests
{
    [TestClass]
    public class ChromosomeTests
    {
        [TestInitialize]
        public void Setup()
        {
            Settings.ScreenWidth = 100;
            Settings.ScreenHeight = 100;
        }

        [TestMethod]
        public void Constructor_WithSize_InitializesEmptyPolygon()
        {
            var chrom = new Chromosome(5);
            Assert.AreEqual(0, chrom.Polygon.Count);
        }

        [TestMethod]
        public void GenerateRandomChromosome_WithDefaultSettings_CreatesValidPolygon()
        {
            var chrom = new Chromosome(5);
            chrom.GenerateRandomChromosome();

            Assert.IsTrue(chrom.Polygon.Count >= Settings.MinPolygonPointCount);
            foreach (var point in chrom.Polygon)
            {
                Assert.IsTrue(point.X >= 0 && point.X < Settings.ScreenWidth);
                Assert.IsTrue(point.Y >= 0 && point.Y < Settings.ScreenHeight);
            }
        }

        [TestMethod]
        public void Clone_WithExistingPolygon_CreatesIndependentCopy()
        {
            var original = new Chromosome(5);
            original.PolyColor = Color.FromArgb(255, 10, 20, 30);
            original.Polygon.Add(new Point(1, 2));
            original.Polygon.Add(new Point(3, 4));

            var cloned = original.Clone();

            Assert.AreEqual(original.PolyColor, cloned.PolyColor);
            Assert.AreEqual(2, cloned.Polygon.Count);
            Assert.AreEqual(1, cloned.Polygon[0].X);
            Assert.AreEqual(2, cloned.Polygon[0].Y);

            cloned.Polygon[0] = new Point(99, 99);
            Assert.AreEqual(1, original.Polygon[0].X);
        }
    }
}