using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GABase;
using GAgeneratedImagese.Tools;

namespace GABaseTests.MutatorTests
{
    [TestClass]
    public class MutatorTests
    {
        [TestInitialize]
        public void Setup()
        {
            Settings.ScreenWidth = 100;
            Settings.ScreenHeight = 100;
        }

        [TestMethod]
        public void Recolor_WithPopulatedPopulation_MarksPopulationDirty()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].Polygon.Add(new Point(10, 10));
            pop.chromosomes[0].Polygon.Add(new Point(20, 20));
            pop.chromosomes[0].Polygon.Add(new Point(30, 30));

            var mutator = new Mutator();
            mutator.Recolor(pop);

            Assert.IsTrue(pop.IsDirty);
        }

        [TestMethod]
        public void ChangePoint_WithPopulatedPopulation_MarksPopulationDirty()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].Polygon.Add(new Point(10, 10));
            pop.chromosomes[0].Polygon.Add(new Point(20, 20));
            pop.chromosomes[0].Polygon.Add(new Point(30, 30));

            var mutator = new Mutator();
            mutator.ChangePoint(pop);

            Assert.IsTrue(pop.IsDirty);
        }

        [TestMethod]
        public void AddChromosome_WithSpaceAvailable_AddsNewChromosome()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].GenerateRandomChromosome();

            using (var bmp = new Bitmap(10, 10))
            using (var fb = new FastBitmap(bmp))
            {
                var mutator = new Mutator();
                mutator.AddChromosome(pop, fb);

                Assert.AreEqual(2, pop.chromosomes.Count);
                Assert.IsTrue(pop.IsDirty);
            }
        }

        [TestMethod]
        public void AddChromosome_WhenAtMaximum_DoesNotExceedMaximumSize()
        {
            var pop = new Population(2);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].GenerateRandomChromosome();

            using (var bmp = new Bitmap(10, 10))
            using (var fb = new FastBitmap(bmp))
            {
                var mutator = new Mutator();
                mutator.AddChromosome(pop, fb);
                mutator.AddChromosome(pop, fb);
                mutator.AddChromosome(pop, fb);

                Assert.AreEqual(2, pop.chromosomes.Count);
            }
        }

        [TestMethod]
        public void RemoveChromosome_WithPopulatedPopulation_RemovesChromosome()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].GenerateRandomChromosome();

            var mutator = new Mutator();
            mutator.RemoveChromosome(pop);

            Assert.AreEqual(0, pop.chromosomes.Count);
            Assert.IsTrue(pop.IsDirty);
        }

        [TestMethod]
        public void RemoveChromosome_WithEmptyPopulation_DoesNotRemoveAnything()
        {
            var pop = new Population(10);
            var mutator = new Mutator();
            mutator.RemoveChromosome(pop);

            Assert.AreEqual(0, pop.chromosomes.Count);
        }

        [TestMethod]
        public void AddPolygonPoint_WithPopulatedChromosome_AddsPointToChromosome()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].Polygon.Add(new Point(10, 10));
            pop.chromosomes[0].Polygon.Add(new Point(20, 20));
            pop.chromosomes[0].Polygon.Add(new Point(30, 30));

            var mutator = new Mutator();
            mutator.AddPolygonPoint(pop);

            Assert.IsTrue(pop.chromosomes[0].Polygon.Count >= 3);
            Assert.IsTrue(pop.IsDirty);
        }

        [TestMethod]
        public void RemovePolygonPoint_WithSufficientPoints_RemovesPointFromChromosome()
        {
            var pop = new Population(10);
            pop.chromosomes.Add(new Chromosome(5));
            pop.chromosomes[0].Polygon.Add(new Point(10, 10));
            pop.chromosomes[0].Polygon.Add(new Point(20, 20));
            pop.chromosomes[0].Polygon.Add(new Point(30, 30));
            pop.chromosomes[0].Polygon.Add(new Point(40, 40));

            var initialCount = pop.chromosomes[0].Polygon.Count;
            var mutator = new Mutator();
            mutator.RemovePolygonPoint(pop);

            Assert.AreEqual(initialCount - 1, pop.chromosomes[0].Polygon.Count);
            Assert.IsTrue(pop.IsDirty);
        }

        [TestMethod]
        public void SwitchChromosomes_WithMultipleChromosomes_DoesNotThrow()
        {
            var pop = new Population(10);
            var chrom1 = new Chromosome(5);
            chrom1.PolyColor = Color.Red;
            var chrom2 = new Chromosome(5);
            chrom2.PolyColor = Color.Blue;
            pop.chromosomes.Add(chrom1);
            pop.chromosomes.Add(chrom2);

            var mutator = new Mutator();
            mutator.SwitchChromosomes(pop);

            Assert.AreEqual(2, pop.chromosomes.Count);
        }
    }
}