using System;
using System.Drawing;

namespace GABase
{
    public class Mutator
    {
        private static readonly Random random = new Random();

        #region Original mutation methods (kept for compatibility)

        public void Recolor(Population pop)
        {
            if (pop.chromosomes.Count == 0)
                return;

            var chromosomeIndex = random.Next(pop.chromosomes.Count);
            var chromosome = pop.chromosomes[chromosomeIndex];

            pop.IsDirty = true;
	        pop.DirtyArea = GetDirtyArea(chromosome);
            chromosome.PolyColor = RandomGenerator.ChangeColor(chromosome.PolyColor);
        }

        public void ChangePoint(Population pop)
        {
            if (pop.chromosomes.Count == 0)
                return;

            var chromosomeIndex = random.Next(pop.chromosomes.Count);
            var chromosome = pop.chromosomes[chromosomeIndex];

            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome);
            var index = RandomGenerator.GetRandomInt(chromosome.Polygon.Count);

            var xMovement = RandomGenerator.GetRandomInt(50) - 25;
            var yMovement = RandomGenerator.GetRandomInt(50) - 25;
            var p = chromosome.Polygon[index];

            p.X += xMovement;
            if (p.X < 0)
                p.X = 0;
            else if (p.X >= Settings.ScreenWidth)
                p.X = Settings.ScreenWidth;

            p.Y += yMovement;
            if (p.Y < 0)
                p.Y = 0;
            else if (p.Y >= Settings.ScreenHeight)
                p.Y = Settings.ScreenHeight;

            chromosome.Polygon[index] = new Point(p.X, p.Y);

	        pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome);

			chromosome.UpdatePolygonArray();
        }

        public void SwitchChromosomes(Population pop)
        {
            var index1 = random.Next(pop.chromosomes.Count);
            var index2 = random.Next(pop.chromosomes.Count);

            if (index1 != index2)
            {
                var chromosome1 = pop.chromosomes[index1];
                var chromosome2 = pop.chromosomes[index2];

                pop.chromosomes[index1] = chromosome2;
                pop.chromosomes[index2] = chromosome1;

                pop.IsDirty = true;
                pop.DirtyArea = GetDirtyArea(chromosome1);
                pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome2);
            }
        }

        public void AddChromosome(Population pop, FastBitmap originalPictureBitmap)
        {
            if (pop.chromosomes.Count < pop.MaximumSize)
            {
                var chromosome = new Chromosome(Settings.MaxPolygonPointCount);
                chromosome.GenerateRandomSmallChromosome2(originalPictureBitmap);
                var index = random.Next(pop.chromosomes.Count);

                var chance2 = random.Next(2);
                if (chance2 == 0 && pop.chromosomes.Count > 0)
                    chromosome.PolyColor = RandomGenerator.ChangeColor(pop.chromosomes[index].PolyColor);

                pop.chromosomes.Add(chromosome);
                pop.IsDirty = true;
                pop.DirtyArea = GetDirtyArea(chromosome);
            }
        }

        public void RemoveChromosome(Population pop)
        {
            if (pop.chromosomes.Count > 0)
            {
                var index = RandomGenerator.GetRandomInt(pop.chromosomes.Count);
	            pop.IsDirty = true;
				pop.DirtyArea = GetDirtyArea(pop.chromosomes[index]);
				pop.chromosomes.RemoveAt(index);
            }
		}

        public void AddPolygonPoint(Population pop)
        {
            if (pop.chromosomes.Count > 0)
            {
                int eligibleCount = 0;
                for (int i = 0; i < pop.chromosomes.Count; i++)
                {
                    if (pop.chromosomes[i].Polygon.Count < Settings.MaxPolygonPointCount)
                        eligibleCount++;
                }

                if (eligibleCount > 0)
                {
                    int target = random.Next(eligibleCount);
                    Chromosome chromosome = null;
                    int found = 0;
                    for (int i = 0; i < pop.chromosomes.Count; i++)
                    {
                        if (pop.chromosomes[i].Polygon.Count < Settings.MaxPolygonPointCount)
                        {
                            if (found == target)
                            {
                                chromosome = pop.chromosomes[i];
                                break;
                            }
                            found++;
                        }
                    }

                    var polygonIndex = random.Next(chromosome.Polygon.Count);
                    var previousPoint = chromosome.Polygon[polygonIndex];
                    var nextPoint = chromosome.Polygon[(polygonIndex + 1) % chromosome.Polygon.Count];

                    var midX = Math.Min(previousPoint.X, nextPoint.X) + Math.Abs(previousPoint.X - nextPoint.X) / 2;
                    var midY = Math.Min(previousPoint.Y, nextPoint.Y) + Math.Abs(previousPoint.Y - nextPoint.Y) / 2;

                    var positionChance = random.Next(2);
                    var positionChange = random.Next(10) + 1;
                    Point newPoint;
                    if (positionChance == 0)
                    {
                        var newX = midX - positionChange;
                        if (newX < 0 || newX >= Settings.ScreenWidth)
                            newX += positionChange;
                        var newY = midY - positionChange;
                        if (newY < 0 || newY >= Settings.ScreenHeight)
                            newY += positionChange;
                        newPoint = new Point(newX, newY);
                    }
                    else
                    {
                        var newX = midX + positionChange;
                        if (newX < 0 || newX >= Settings.ScreenWidth)
                            newX -= positionChange;
                        var newY = midY + positionChange;
                        if (newY < 0 || newY >= Settings.ScreenHeight)
                            newY -= positionChange;
                        newPoint = new Point(newX, newY);
                    }

                    pop.IsDirty = true;
                    pop.DirtyArea = GetDirtyArea(chromosome);

                    chromosome.Polygon.Insert(polygonIndex + 1, newPoint);

                    pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome);

                    chromosome.UpdatePolygonArray();
                }
            }
        }

        public void RemovePolygonPoint(Population pop)
        {
            if (pop.chromosomes.Count > 0)
            {
                var chromosomeIndex = random.Next(pop.chromosomes.Count);
                var chromosome = pop.chromosomes[chromosomeIndex];
                if (chromosome.Polygon.Count > 3)
                {
                    pop.IsDirty = true;
                    pop.DirtyArea = GetDirtyArea(chromosome);

                    var polygonIndex = random.Next(chromosome.Polygon.Count);
                    chromosome.Polygon.RemoveAt(polygonIndex);

                    pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome);

                    chromosome.UpdatePolygonArray();
                }
            }
        }

        #endregion

        #region Mutation methods with backup (for in-place mutation with revert)

        public MutationBackup RecolorWithBackup(Population pop)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.Recolor, WasDirty = false };
            if (pop.chromosomes.Count == 0)
                return backup;

            var chromosomeIndex = random.Next(pop.chromosomes.Count);
            var chromosome = pop.chromosomes[chromosomeIndex];

            backup.ChromosomeIndex = chromosomeIndex;
            backup.OriginalColor = chromosome.PolyColor;
            backup.WasDirty = true;

            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome);
            chromosome.PolyColor = RandomGenerator.ChangeColor(chromosome.PolyColor);
            return backup;
        }

        public MutationBackup ChangePointWithBackup(Population pop)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.ChangePoint, WasDirty = false };
            if (pop.chromosomes.Count == 0)
                return backup;

            var chromosomeIndex = random.Next(pop.chromosomes.Count);
            var chromosome = pop.chromosomes[chromosomeIndex];

            backup.ChromosomeIndex = chromosomeIndex;
            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome);

            var index = RandomGenerator.GetRandomInt(chromosome.Polygon.Count);
            backup.PointIndex = index;
            backup.OriginalPoint = chromosome.Polygon[index];

            var xMovement = RandomGenerator.GetRandomInt(50) - 25;
            var yMovement = RandomGenerator.GetRandomInt(50) - 25;
            var p = chromosome.Polygon[index];

            p.X += xMovement;
            if (p.X < 0)
                p.X = 0;
            else if (p.X >= Settings.ScreenWidth)
                p.X = Settings.ScreenWidth;

            p.Y += yMovement;
            if (p.Y < 0)
                p.Y = 0;
            else if (p.Y >= Settings.ScreenHeight)
                p.Y = Settings.ScreenHeight;

            chromosome.Polygon[index] = new Point(p.X, p.Y);
            pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome);
            chromosome.UpdatePolygonArray();
            backup.WasDirty = true;
            return backup;
        }

        public MutationBackup AddPolygonPointWithBackup(Population pop)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.AddPolygonPoint, WasDirty = false };
            if (pop.chromosomes.Count == 0)
                return backup;

            int eligibleCount = 0;
            for (int i = 0; i < pop.chromosomes.Count; i++)
            {
                if (pop.chromosomes[i].Polygon.Count < Settings.MaxPolygonPointCount)
                    eligibleCount++;
            }

            if (eligibleCount == 0)
                return backup;

            int target = random.Next(eligibleCount);
            Chromosome chromosome = null;
            int chromosomeIndex = -1;
            int found = 0;
            for (int i = 0; i < pop.chromosomes.Count; i++)
            {
                if (pop.chromosomes[i].Polygon.Count < Settings.MaxPolygonPointCount)
                {
                    if (found == target)
                    {
                        chromosome = pop.chromosomes[i];
                        chromosomeIndex = i;
                        break;
                    }
                    found++;
                }
            }

            backup.ChromosomeIndex = chromosomeIndex;

            var polygonIndex = random.Next(chromosome.Polygon.Count);
            var previousPoint = chromosome.Polygon[polygonIndex];
            var nextPoint = chromosome.Polygon[(polygonIndex + 1) % chromosome.Polygon.Count];

            var midX = Math.Min(previousPoint.X, nextPoint.X) + Math.Abs(previousPoint.X - nextPoint.X) / 2;
            var midY = Math.Min(previousPoint.Y, nextPoint.Y) + Math.Abs(previousPoint.Y - nextPoint.Y) / 2;

            var positionChance = random.Next(2);
            var positionChange = random.Next(10) + 1;
            Point newPoint;
            if (positionChance == 0)
            {
                var newX = midX - positionChange;
                if (newX < 0 || newX >= Settings.ScreenWidth)
                    newX += positionChange;
                var newY = midY - positionChange;
                if (newY < 0 || newY >= Settings.ScreenHeight)
                    newY += positionChange;
                newPoint = new Point(newX, newY);
            }
            else
            {
                var newX = midX + positionChange;
                if (newX < 0 || newX >= Settings.ScreenWidth)
                    newX -= positionChange;
                var newY = midY + positionChange;
                if (newY < 0 || newY >= Settings.ScreenHeight)
                    newY -= positionChange;
                newPoint = new Point(newX, newY);
            }

            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome);

            chromosome.Polygon.Insert(polygonIndex + 1, newPoint);
            backup.InsertedPointIndex = polygonIndex + 1;

            pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome);
            chromosome.UpdatePolygonArray();
            backup.WasDirty = true;
            return backup;
        }

        public MutationBackup RemovePolygonPointWithBackup(Population pop)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.RemovePolygonPoint, WasDirty = false };
            if (pop.chromosomes.Count == 0)
                return backup;

            var chromosomeIndex = random.Next(pop.chromosomes.Count);
            var chromosome = pop.chromosomes[chromosomeIndex];
            if (chromosome.Polygon.Count <= 3)
                return backup;

            backup.ChromosomeIndex = chromosomeIndex;

            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome);

            var polygonIndex = random.Next(chromosome.Polygon.Count);
            backup.RemovedPoint = chromosome.Polygon[polygonIndex];
            backup.RemovedPointIndex = polygonIndex;

            chromosome.Polygon.RemoveAt(polygonIndex);
            pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome);
            chromosome.UpdatePolygonArray();
            backup.WasDirty = true;
            return backup;
        }

        public MutationBackup SwitchChromosomesWithBackup(Population pop)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.SwitchChromosomes, WasDirty = false };
            if (pop.chromosomes.Count < 2)
                return backup;

            var index1 = random.Next(pop.chromosomes.Count);
            var index2 = random.Next(pop.chromosomes.Count);

            if (index1 == index2)
                return backup;

            backup.SwapIndex1 = index1;
            backup.SwapIndex2 = index2;

            var chromosome1 = pop.chromosomes[index1];
            var chromosome2 = pop.chromosomes[index2];

            pop.chromosomes[index1] = chromosome2;
            pop.chromosomes[index2] = chromosome1;

            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome1);
            pop.DirtyArea = GetDirtyArea(pop.DirtyArea, chromosome2);
            backup.WasDirty = true;
            return backup;
        }

        public MutationBackup AddChromosomeWithBackup(Population pop, FastBitmap originalPictureBitmap)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.AddChromosome, WasDirty = false };
            if (pop.chromosomes.Count >= pop.MaximumSize)
                return backup;

            var chromosome = new Chromosome(Settings.MaxPolygonPointCount);
            chromosome.GenerateRandomSmallChromosome2(originalPictureBitmap);
            var index = random.Next(Math.Max(1, pop.chromosomes.Count));

            var chance2 = random.Next(2);
            if (chance2 == 0 && pop.chromosomes.Count > 0)
                chromosome.PolyColor = RandomGenerator.ChangeColor(pop.chromosomes[index].PolyColor);

            pop.chromosomes.Add(chromosome);
            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(chromosome);
            backup.WasDirty = true;
            return backup;
        }

        public MutationBackup RemoveChromosomeWithBackup(Population pop)
        {
            var backup = new MutationBackup { Type = Evolver.MutationType.RemoveChromosome, WasDirty = false };
            if (pop.chromosomes.Count == 0)
                return backup;

            var index = RandomGenerator.GetRandomInt(pop.chromosomes.Count);
            backup.RemovedChromosome = pop.chromosomes[index];
            backup.RemovedChromosomeIndex = index;

            pop.IsDirty = true;
            pop.DirtyArea = GetDirtyArea(pop.chromosomes[index]);
            pop.chromosomes.RemoveAt(index);
            backup.WasDirty = true;
            return backup;
        }

        #endregion

        #region Revert

        public void RevertMutation(Population pop, MutationBackup backup)
        {
            switch (backup.Type)
            {
                case Evolver.MutationType.Recolor:
                    pop.chromosomes[backup.ChromosomeIndex].PolyColor = backup.OriginalColor;
                    break;

                case Evolver.MutationType.ChangePoint:
                    var cpChrom = pop.chromosomes[backup.ChromosomeIndex];
                    cpChrom.Polygon[backup.PointIndex] = backup.OriginalPoint;
                    cpChrom.UpdatePolygonArray();
                    break;

                case Evolver.MutationType.AddPolygonPoint:
                    var apChrom = pop.chromosomes[backup.ChromosomeIndex];
                    apChrom.Polygon.RemoveAt(backup.InsertedPointIndex);
                    apChrom.UpdatePolygonArray();
                    break;

                case Evolver.MutationType.RemovePolygonPoint:
                    var rpChrom = pop.chromosomes[backup.ChromosomeIndex];
                    rpChrom.Polygon.Insert(backup.RemovedPointIndex, backup.RemovedPoint);
                    rpChrom.UpdatePolygonArray();
                    break;

                case Evolver.MutationType.SwitchChromosomes:
                    var temp = pop.chromosomes[backup.SwapIndex1];
                    pop.chromosomes[backup.SwapIndex1] = pop.chromosomes[backup.SwapIndex2];
                    pop.chromosomes[backup.SwapIndex2] = temp;
                    break;

                case Evolver.MutationType.AddChromosome:
                    // Was added at end
                    pop.chromosomes.RemoveAt(pop.chromosomes.Count - 1);
                    break;

                case Evolver.MutationType.RemoveChromosome:
                    pop.chromosomes.Insert(backup.RemovedChromosomeIndex, backup.RemovedChromosome);
                    break;
            }
        }

        #endregion

        #region Helpers

        private Rectangle GetDirtyArea(Chromosome chromosome)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = 0, maxY = 0;
            var polygon = chromosome.Polygon;
            for (int i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private Rectangle GetDirtyArea(Rectangle rectangle, Chromosome chromosome)
        {
            int minX = rectangle.X, minY = rectangle.Y;
            int maxX = rectangle.X + rectangle.Width, maxY = rectangle.Y + rectangle.Height;

            var polygon = chromosome.Polygon;
            for (int i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion
    }
}
