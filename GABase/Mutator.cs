using System;
using System.Drawing;
using System.Linq;

namespace GABase
{
    public class Mutator
    {
        private static readonly Random random = new Random();

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

	    private Rectangle GetDirtyArea(Chromosome chromosome)
	    {
		    int minX = Int32.MaxValue, minY = int.MaxValue;
		    int maxX = 0, maxY = 0;
			foreach (var p in chromosome.Polygon)
		    {
			    if (p.X < minX)
			    {
				    minX = p.X;
			    }
			    if (p.X > maxX)
			    {
				    maxX = p.X;
			    }
			    if (p.Y < minY)
			    {
				    minY = p.Y;
			    }
			    if (p.Y > maxY)
			    {
				    maxY = p.Y;
			    }
		    }
		    return new Rectangle(minX, minY, maxX - minX, maxY - minY);
	    }

	    private Rectangle GetDirtyArea(Rectangle rectangle, Chromosome chromosome)
	    {
		    int minX = rectangle.X, minY = rectangle.Y;
		    int maxX = rectangle.X + rectangle.Width, maxY = rectangle.Y + rectangle.Height;
		
		    foreach (var p in chromosome.Polygon)
		    {
			    if (p.X < minX)
			    {
				    minX = p.X;
			    }
			    if (p.X > maxX)
			    {
				    maxX = p.X;
			    }

			    if (p.Y < minY)
			    {
				    minY = p.Y;
			    }
				if (p.Y > maxY)
			    {
				    maxY = p.Y;
			    }
		    }

		    return new Rectangle(minX, minY, maxX - minX, maxY - minY);
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
        }

	    public void SwitchChromosomes(Population pop)
	    {
		    var index1 = RandomGenerator.GetRandomInt(pop.chromosomes.Count);
		    var index2 = RandomGenerator.GetRandomInt(pop.chromosomes.Count);

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
                //chromosome.GenerateRandomChromosome();
                //chromosome.GenerateRandomSmallChromosome();
                chromosome.GenerateRandomSmallChromosome2(originalPictureBitmap);
                var index = random.Next(pop.chromosomes.Count);

                var chance2 = random.Next(2);
                if (chance2 == 0 && pop.chromosomes.Count > 0)
                    chromosome.PolyColor = RandomGenerator.ChangeColor(pop.chromosomes[index].PolyColor);
                //int index = RandomGenerator.GetRandomInt(pop.chromosomes.Count);
                //pop.chromosomes.Insert(index, chromosome);
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
                var chromosomesWithoutMaxPolygons = pop.chromosomes.Where(x => x.Polygon.Count < Settings.MaxPolygonPointCount).ToList();

                if (chromosomesWithoutMaxPolygons.Count > 0)
                {
                    //pop.chromosomes.Add(chromosome);
                    var chromosomeIndex = random.Next(chromosomesWithoutMaxPolygons.Count);
                    var chromosome = chromosomesWithoutMaxPolygons[chromosomeIndex];

                    var polygonIndex = random.Next(chromosome.Polygon.Count);
                    var previousPoint = chromosome.Polygon[polygonIndex];
                    var nextPoint = chromosome.Polygon[(polygonIndex + 1) % chromosome.Polygon.Count];

                    var midX = Math.Min(previousPoint.X, nextPoint.X) + Math.Abs(previousPoint.X - nextPoint.X) / 2;
                    var midY = Math.Min(previousPoint.Y, nextPoint.Y) + Math.Abs(previousPoint.Y - nextPoint.Y) / 2;

                    var positionChance = random.Next(2);
                    var positionChange = random.Next(10) + 1;//1 > 11
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
				}
            }
        }
    }
}