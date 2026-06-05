using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using GAgeneratedImagese.Tools;

namespace GABase
{
    public class Evolver
    {
        private class MutationWeightStats
        {
            public long Attempts;
            public long Successes;
            public double Weight;
            public double SuccessRate => Attempts > 0 ? (double)Successes / Attempts : 0.01;
        }

        private FastBitmap _originalPictureBitmap;
        private readonly Bitmap _targetImage;
        private Population _popA;
        private Selector _selector;
        private Mutator _mutator;
        private Thread _workerThread;
        private volatile bool _stopRequested;
        private readonly bool _disableResize;
        private int _resizeFactor = 4;
        private int _generation = 1;
        private long _previousFitnesse = long.MaxValue;
        private readonly Stopwatch _stopwatch;
        private long _lastUpdate;
        private long[,] _differences;

        // Island model support
        internal volatile Population _pendingMigrant;
        internal long _pendingMigrantFitness = long.MaxValue;
        public long CurrentFitness { get; internal set; } = long.MaxValue;

        private MutationWeightStats[] _mutationStats;
        private readonly object _statsLock = new object();

        public event Action<Bitmap, long, Population, int, Image, long, int, string> PopulationUpdated;

        public int TargetWidth => _targetImage.Width;
        public int TargetHeight => _targetImage.Height;

        public Evolver(Bitmap targetImage, bool disableResize = false, int resizeFactor = 4)
        {
            _targetImage = targetImage;
            _disableResize = disableResize;
            _resizeFactor = resizeFactor;
            _stopwatch = Stopwatch.StartNew();

            var resizedBitmap = new Bitmap(targetImage,
                new Size(targetImage.Width / _resizeFactor, targetImage.Height / _resizeFactor));
            Settings.ScreenWidth = resizedBitmap.Width;
            Settings.ScreenHeight = resizedBitmap.Height;

            _originalPictureBitmap = new FastBitmap((Bitmap)(resizedBitmap.Clone()));
            _selector = new Selector(_originalPictureBitmap);
            _mutator = new Mutator();

            _mutationStats = new MutationWeightStats[]
            {
                new MutationWeightStats(),
                new MutationWeightStats(),
                new MutationWeightStats(),
                new MutationWeightStats(),
                new MutationWeightStats(),
                new MutationWeightStats(),
                new MutationWeightStats()
            };
        }

        public Population CurrentPopulation => _popA;
        public int CurrentGeneration => _generation;

        public void Start()
        {
            if (_workerThread == null || !_workerThread.IsAlive)
            {
                _stopRequested = false;
                _workerThread = new Thread(RunEvolution);
                _workerThread.IsBackground = true;
                _workerThread.Start();
            }
        }

        public void Stop()
        {
            _stopRequested = true;
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(5000);
            }
        }

        public ThreadPriority Priority
        {
            get => _workerThread?.Priority ?? ThreadPriority.Normal;
            set
            {
                if (_workerThread != null)
                    _workerThread.Priority = value;
            }
        }

        public enum MutationType
        {
            Recolor,
            ChangePoint,
            AddPolygonPoint,
            RemovePolygonPoint,
            SwitchChromosomes,
            AddChromosome,
            RemoveChromosome
        }

        private void RunEvolution()
        {
            try
            {
                RunEvolutionCore();
            }
            catch (Exception)
            {
                // Prevent unhandled exceptions from crashing the process in .NET 9
            }
        }

        private void RunEvolutionCore()
        {
            _popA = new Population(Settings.MaxPolygonCount);
            _popA.GenerateRandomChromosomes();
            Chromosome chromosome = new Chromosome(Settings.MaxPolygonPointCount);
            _popA.chromosomes.Add(chromosome);
            chromosome.GenerateRandomChromosome();

            // Initial full render of the population into the cached bitmap
            _selector.FullRender(_popA);

            long currentFitness = long.MaxValue;

            while (!_stopRequested)
            {
                try
                {
                // Check for incoming migration from another island
                var migrant = System.Threading.Interlocked.Exchange(ref _pendingMigrant, null);
                if (migrant != null && _pendingMigrantFitness < CurrentFitness)
                {
                    // Only adopt if the migrant is fitter than our current solution
                    _popA = migrant;
                    _selector.FullRender(_popA);
                    currentFitness = long.MaxValue;
                    CurrentFitness = _pendingMigrantFitness;
                }

                long costDelta = 0;

                var selectedMutation = SelectWeightedMutation();
                MutationType mutationType = selectedMutation;
                MutationBackup backup;

                switch (selectedMutation)
                {
                    case MutationType.Recolor:
                        backup = _mutator.RecolorWithBackup(_popA);
                        break;
                    case MutationType.ChangePoint:
                        backup = _mutator.ChangePointWithBackup(_popA);
                        break;
                    case MutationType.AddPolygonPoint:
                        backup = _mutator.AddPolygonPointWithBackup(_popA);
                        if (backup.WasDirty)
                            costDelta = Settings.PolygonPointCost;
                        break;
                    case MutationType.RemovePolygonPoint:
                        backup = _mutator.RemovePolygonPointWithBackup(_popA);
                        if (backup.WasDirty)
                            costDelta = -Settings.PolygonPointCost;
                        break;
                    case MutationType.SwitchChromosomes:
                        backup = _mutator.SwitchChromosomesWithBackup(_popA);
                        break;
                    case MutationType.AddChromosome:
                        backup = _mutator.AddChromosomeWithBackup(_popA, _originalPictureBitmap, _differences);
                        if (backup.WasDirty)
                        {
                            int addedPoints = _popA.chromosomes[_popA.chromosomes.Count - 1].Polygon.Count;
                            costDelta = Settings.PolygonCost + Settings.PolygonPointCost * addedPoints;
                        }
                        break;
                    case MutationType.RemoveChromosome:
                        backup = _mutator.RemoveChromosomeWithBackup(_popA);
                        if (backup.WasDirty)
                        {
                            int removedPoints = backup.RemovedChromosome.Polygon.Count;
                            costDelta = -(Settings.PolygonCost + Settings.PolygonPointCost * removedPoints);
                        }
                        break;
                    default:
                        backup = new MutationBackup();
                        break;
                }

                if (_popA.IsDirty && backup.WasDirty)
                {
                    _popA.IsDirty = false;

                    bool accepted = _selector.EvaluateMutation(_popA, _popA.DirtyArea, costDelta, out var newPartialFitness);

                    if (accepted)
                    {
                        currentFitness = newPartialFitness;
                        RecordMutationResult(mutationType, true);
                    }
                    else
                    {
                        // Revert the mutation
                        _mutator.RevertMutation(_popA, backup);
                        RecordMutationResult(mutationType, false);
                    }

                    // Update CurrentFitness for island model
                    CurrentFitness = currentFitness;

                    if (_stopwatch.ElapsedMilliseconds > _lastUpdate + 2500)
                    {
                        // Skip UI update if dimensions are out of sync (another island resizing)
                        if (_originalPictureBitmap.Width != Settings.ScreenWidth ||
                            _originalPictureBitmap.Height != Settings.ScreenHeight)
                        {
                            SyncToCurrentDimensions();
                            currentFitness = long.MaxValue;
                        }
                        else
                        {
                            var (differenceImage, fitnesse, differences) = DifferencePicture.GetDifferencePictureWithFitness(_popA, _originalPictureBitmap);
                            _differences = differences;

                            var generatedImage = new Bitmap(_popA.GetPicture(), _targetImage.Width, _targetImage.Height);
                            var mutationStats = GetMutationStats();

                            PopulationUpdated?.Invoke(generatedImage, fitnesse, _popA, _generation, differenceImage, _stopwatch.ElapsedMilliseconds, _resizeFactor, mutationStats);

                            HandleResize(fitnesse);

                            _previousFitnesse = fitnesse;
                        }
                        _lastUpdate = _stopwatch.ElapsedMilliseconds;
                    }
                }

                _generation++;
                }
                catch (Exception) when (!_stopRequested)
                {
                    // Dimension mismatch — another island resized Settings.ScreenWidth/Height.
                    // Sync this island to the new dimensions.
                    SyncToCurrentDimensions();
                    currentFitness = long.MaxValue;
                }
            }
        }

        /// <summary>
        /// Sync this island's bitmaps to match the current Settings.ScreenWidth/Height
        /// after another island performed a resize.
        /// </summary>
        private void SyncToCurrentDimensions()
        {
            try
            {
                int targetWidth = Settings.ScreenWidth;
                int targetHeight = Settings.ScreenHeight;

                // Already in sync
                if (_originalPictureBitmap.Width == targetWidth && _originalPictureBitmap.Height == targetHeight)
                    return;

                // Figure out what resize factor matches the current dimensions
                _resizeFactor = Math.Max(1, _targetImage.Width / targetWidth);
                _differences = null;

                var resizedBitmap = new Bitmap(_targetImage, new Size(targetWidth, targetHeight));

                ((IDisposable)_originalPictureBitmap).Dispose();
                _originalPictureBitmap = new FastBitmap((Bitmap)(resizedBitmap.Clone()));

                _selector.Dispose();
                _selector = new Selector(new FastBitmap((Bitmap)(resizedBitmap.Clone())));

                // Scale polygon coordinates to new dimensions
                int oldWidth = targetWidth / 2; // previous was half the new
                if (oldWidth > 0)
                {
                    foreach (var c in _popA.chromosomes)
                    {
                        for (var index = 0; index < c.Polygon.Count; index++)
                        {
                            var p = c.Polygon[index];
                            c.Polygon[index] = new Point(
                                Math.Min(p.X * 2, targetWidth),
                                Math.Min(p.Y * 2, targetHeight));
                        }
                        c.UpdatePolygonArray();
                    }
                }

                _selector.FullRender(_popA);
            }
            catch (Exception)
            {
                // If sync fails, just continue — next iteration will retry
            }
        }

        // Shared lock to serialize resize operations across all islands
        private static readonly object _resizeLock = new object();

        private void HandleResize(long currentFitnesse)
        {
            if (_disableResize)
                return;

            if (_previousFitnesse > 0 &&
                (_previousFitnesse - currentFitnesse) * 1.0 / _previousFitnesse < 0.0001 &&
                _resizeFactor > 1)
            {
                lock (_resizeLock)
                {
                // Check if another island already resized
                var expectedWidth = _targetImage.Width / (_resizeFactor / 2);
                if (Settings.ScreenWidth >= expectedWidth)
                {
                    // Already resized by another island — just sync our local state
                    _resizeFactor /= 2;
                    _differences = null;
                    ((IDisposable)_originalPictureBitmap).Dispose();
                    var resized = new Bitmap(_targetImage, new Size(Settings.ScreenWidth, Settings.ScreenHeight));
                    _originalPictureBitmap = new FastBitmap((Bitmap)(resized.Clone()));
                    _selector.Dispose();
                    _selector = new Selector(new FastBitmap((Bitmap)(resized.Clone())));
                    foreach (var c in _popA.chromosomes)
                    {
                        for (var index = 0; index < c.Polygon.Count; index++)
                        {
                            var p = c.Polygon[index];
                            c.Polygon[index] = new Point(Math.Min(p.X * 2, Settings.ScreenWidth), Math.Min(p.Y * 2, Settings.ScreenHeight));
                        }
                        c.UpdatePolygonArray();
                    }
                    _selector.FullRender(_popA);
                    var (_, newDiffs) = DifferencePicture.GetDifferencePicture(_popA, _originalPictureBitmap);
                    _differences = newDiffs;
                    return;
                }

                _resizeFactor /= 2;
                _differences = null;

                Bitmap resizedBitmap;
                if (_resizeFactor > 1)
                {
                    resizedBitmap = new Bitmap(_targetImage,
                        new Size(_targetImage.Width / _resizeFactor,
                            _targetImage.Height / _resizeFactor));
                }
                else
                {
                    resizedBitmap = new Bitmap(_targetImage);
                }

                ((IDisposable)_originalPictureBitmap).Dispose();
                _originalPictureBitmap = new FastBitmap((Bitmap)(resizedBitmap.Clone()));

                Settings.ScreenWidth = resizedBitmap.Width;
                Settings.ScreenHeight = resizedBitmap.Height;

                _selector.Dispose();
                _selector = new Selector(new FastBitmap((Bitmap)(resizedBitmap.Clone())));

                foreach (var c in _popA.chromosomes)
                {
                    for (var index = 0; index < c.Polygon.Count; index++)
                    {
                        var p = c.Polygon[index];
                        c.Polygon[index] = new Point(Math.Min(p.X * 2, Settings.ScreenWidth), Math.Min(p.Y * 2, Settings.ScreenHeight));
                    }
                    c.UpdatePolygonArray();
                }

                // Full re-render at new resolution
                _selector.FullRender(_popA);

                var (_, newDifferences) = DifferencePicture.GetDifferencePicture(_popA, _originalPictureBitmap);
                _differences = newDifferences;
                } // end lock
            }
        }

        private MutationType SelectWeightedMutation()
        {
            const double baselineWeight = 1.0;
            const double successWeight = 10.0;

            double totalWeight = 0;
            for (int i = 0; i < _mutationStats.Length; i++)
            {
                double weight = baselineWeight + (_mutationStats[i].SuccessRate * successWeight);
                _mutationStats[i].Weight = weight;
                totalWeight += weight;
            }

            double randomValue = RandomGenerator.GetRandomInt(10000) / 10000.0 * totalWeight;
            double cumulative = 0;

            for (int i = 0; i < _mutationStats.Length; i++)
            {
                cumulative += _mutationStats[i].Weight;
                if (randomValue <= cumulative)
                    return (MutationType)i;
            }

            return MutationType.ChangePoint;
        }

        private void RecordMutationResult(MutationType type, bool improved)
        {
            lock (_statsLock)
            {
                _mutationStats[(int)type].Attempts++;
                if (improved)
                    _mutationStats[(int)type].Successes++;
            }
        }

        public string GetMutationStats()
        {
            const double baselineWeight = 1.0;
            const double successWeight = 10.0;

            double totalWeight = 0;
            for (int i = 0; i < _mutationStats.Length; i++)
            {
                double weight = baselineWeight + (_mutationStats[i].SuccessRate * successWeight);
                _mutationStats[i].Weight = weight;
                totalWeight += weight;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _mutationStats.Length; i++)
            {
                var type = (MutationType)i;
                var stats = _mutationStats[i];
                int percentage = totalWeight > 0 ? (int)(stats.Weight / totalWeight * 100) : 0;
                if (i > 0) sb.Append(" ");
                sb.Append($"{type}:{percentage}%");
            }
            return sb.ToString();
        }
    }
}
