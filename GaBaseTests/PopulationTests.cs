using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GABase;

namespace GABaseTests.PopulationTests
{
    [TestClass]
    public class PopulationTests
    {
        [TestInitialize]
        public void Setup()
        {
            Settings.ScreenWidth = 100;
            Settings.ScreenHeight = 100;
        }

        [TestMethod]
        public void Constructor_WithMaximumSize_SetsMaximumSizeProperty()
        {
            var pop = new Population(50);
            Assert.AreEqual(50, pop.MaximumSize);
        }

        [TestMethod]
        public void Constructor_WithAnySize_InitializesEmptyChromosomeList()
        {
            var pop = new Population(10);
            Assert.AreEqual(0, pop.chromosomes.Count);
        }

        [TestMethod]
        public void Clone_WithExistingChromosomes_CreatesIndependentCopy()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].PolyColor = Color.Red;
            pop.chromosomes[0].Polygon.Add(new Point(1, 2));

            var cloned = pop.Clone();

            Assert.AreEqual(1, cloned.chromosomes.Count);
            Assert.AreEqual(Color.Red, cloned.chromosomes[0].PolyColor);
            Assert.AreEqual(1, cloned.chromosomes[0].Polygon.Count);

            cloned.chromosomes[0].PolyColor = Color.Blue;
            Assert.AreEqual(Color.Red, pop.chromosomes[0].PolyColor);
        }

        [TestMethod]
        public void GetPicture_WithDefaultSettings_ReturnsBitmapWithCorrectSize()
        {
            var pop = new Population(10);
            var bmp = pop.GetPicture();
            Assert.AreEqual(100, bmp.Width);
            Assert.AreEqual(100, bmp.Height);
            bmp.Dispose();
        }
    }
}