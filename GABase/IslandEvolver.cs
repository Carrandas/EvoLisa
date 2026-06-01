using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace GABase
{
    /// <summary>
    /// Orchestrates multiple Evolver instances (islands) running in parallel.
    /// Periodically migrates the best solution between islands.
    /// Handles progressive resize by stopping all islands, resizing, and restarting.
    /// </summary>
    public class IslandEvolver
    {
        private Evolver[] _islands;
        private readonly int _islandCount;
        private readonly Bitmap _targetImage;
        private Timer _migrationTimer;
        private int _bestIslandIndex;
        private long _bestFitness = long.MaxValue;
        private readonly object _bestLock = new object();
        private int _resizeFactor = 4;
        private long _previousFitnesse = long.MaxValue;
        private readonly Stopwatch _stopwatch;
        private volatile bool _stopped;

        /// <summary>
        /// Fired when the best island updates. Same signature as Evolver.PopulationUpdated.
        /// </summary>
        public event Action<Bitmap, long, Population, int, Image, long, int, string> PopulationUpdated;

        /// <summary>
        /// Number of islands running in parallel.
        /// </summary>
        public int IslandCount => _islandCount;

        /// <summary>
        /// Total generations across all islands.
        /// </summary>
        public int TotalGenerations => _islands.Sum(e => e.CurrentGeneration);

        /// <summary>
        /// The current best fitness across all islands.
        /// </summary>
        public long BestFitness => _bestFitness;

        /// <summary>
        /// Index of the island with the best fitness.
        /// </summary>
        public int BestIslandIndex => _bestIslandIndex;

        public int TargetWidth => _targetImage.Width;
        public int TargetHeight => _targetImage.Height;

        /// <summary>
        /// Create a multi-island evolver.
        /// </summary>
        /// <param name="targetImage">The target image to approximate.</param>
        /// <param name="islandCount">Number of parallel islands. 0 = auto (ProcessorCount, max 8).</param>
        /// <param name="migrationIntervalMs">Milliseconds between migrations. Default 10000 (10s).</param>
        public IslandEvolver(Bitmap targetImage, int islandCount = 0, int migrationIntervalMs = 10000)
        {
            _targetImage = targetImage;
            _stopwatch = Stopwatch.StartNew();

            if (islandCount <= 0)
                islandCount = Math.Min(Environment.ProcessorCount, 8);

            _islandCount = islandCount;
            _islands = CreateIslands();

            if (migrationIntervalMs > 0 && _islandCount > 1)
            {
                _migrationTimer = new Timer(MigrationCallback, null, migrationIntervalMs, migrationIntervalMs);
            }
        }

        /// <summary>
        /// Start all islands.
        /// </summary>
        public void Start()
        {
            _stopped = false;
            for (int i = 0; i < _islandCount; i++)
            {
                _islands[i].Start();
            }
        }

        /// <summary>
        /// Stop all islands.
        /// </summary>
        public void Stop()
        {
            _stopped = true;
            _migrationTimer?.Dispose();
            _migrationTimer = null;

            for (int i = 0; i < _islandCount; i++)
            {
                _islands[i].Stop();
            }
        }

        /// <summary>
        /// Get mutation stats from the best island.
        /// </summary>
        public string GetMutationStats()
        {
            return _islands[_bestIslandIndex].GetMutationStats();
        }

        /// <summary>
        /// Get generation count from the best island.
        /// </summary>
        public int CurrentGeneration => _islands[_bestIslandIndex].CurrentGeneration;

        /// <summary>
        /// Thread priority for all island threads.
        /// </summary>
        public ThreadPriority Priority
        {
            set
            {
                for (int i = 0; i < _islandCount; i++)
                    _islands[i].Priority = value;
            }
        }

        private Evolver[] CreateIslands()
        {
            var islands = new Evolver[_islandCount];
            for (int i = 0; i < _islandCount; i++)
            {
                islands[i] = new Evolver(_targetImage, disableResize: true, resizeFactor: _resizeFactor);
                var islandIndex = i;
                islands[i].PopulationUpdated += (img, fitnesse, pop, gen, diffImg, elapsed, zoom, stats) =>
                {
                    OnIslandUpdated(islandIndex, img, fitnesse, pop, gen, diffImg, elapsed, zoom, stats);
                };
            }
            return islands;
        }

        /// <summary>
        /// Stop-the-world resize: stop all islands, increase resolution, restart with best population.
        /// </summary>
        private void PerformResize()
        {
            if (_resizeFactor <= 1)
                return;

            _resizeFactor /= 2;

            // Stop all islands
            for (int i = 0; i < _islandCount; i++)
                _islands[i].Stop();

            // Get the best population and scale its coordinates up
            var bestPop = _islands[_bestIslandIndex].CurrentPopulation;
            if (bestPop == null)
                return;

            var scaledPop = bestPop.Clone();
            int newWidth = _resizeFactor > 1
                ? _targetImage.Width / _resizeFactor
                : _targetImage.Width;
            int newHeight = _resizeFactor > 1
                ? _targetImage.Height / _resizeFactor
                : _targetImage.Height;

            // Scale polygon coordinates to new resolution
            foreach (var c in scaledPop.chromosomes)
            {
                for (var index = 0; index < c.Polygon.Count; index++)
                {
                    var p = c.Polygon[index];
                    c.Polygon[index] = new Point(
                        Math.Min(p.X * 2, newWidth),
                        Math.Min(p.Y * 2, newHeight));
                }
                c.UpdatePolygonArray();
            }

            // Update global dimensions
            Settings.ScreenWidth = newWidth;
            Settings.ScreenHeight = newHeight;

            // Create fresh islands at new resolution
            _islands = CreateIslands();

            // Seed all islands with the scaled best population
            for (int i = 0; i < _islandCount; i++)
            {
                _islands[i]._pendingMigrant = scaledPop.Clone();
                _islands[i]._pendingMigrantFitness = 0; // Force adoption
            }

            // Reset fitness tracking (new resolution = different scale)
            _bestFitness = long.MaxValue;
            _previousFitnesse = long.MaxValue;

            // Restart
            if (!_stopped)
                Start();
        }

        private void MigrationCallback(object state)
        {
            try
            {
                PerformMigration();
                CheckForResize();
            }
            catch (Exception)
            {
                // Migration/resize is best-effort; skip on error
            }
        }

        /// <summary>
        /// Check if the best island has plateaued and trigger a resize.
        /// </summary>
        private void CheckForResize()
        {
            if (_resizeFactor <= 1 || _stopped)
                return;

            long currentFitness = _bestFitness;
            if (currentFitness == long.MaxValue)
                return;

            if (_previousFitnesse > 0 && _previousFitnesse != long.MaxValue)
            {
                double improvement = (_previousFitnesse - currentFitness) * 1.0 / _previousFitnesse;
                if (improvement < 0.0001)
                {
                    PerformResize();
                }
            }

            _previousFitnesse = currentFitness;
        }

        private void PerformMigration()
        {
            if (_stopped)
                return;

            // Only migrate if all islands have been running for a while
            for (int i = 0; i < _islandCount; i++)
            {
                if (_islands[i].CurrentGeneration < 1000 || _islands[i].CurrentPopulation == null)
                    return;
            }

            // Find the best island (lowest fitness)
            int bestIdx = 0;
            long bestFit = _islands[0].CurrentFitness;
            for (int i = 1; i < _islandCount; i++)
            {
                if (_islands[i].CurrentFitness < bestFit)
                {
                    bestFit = _islands[i].CurrentFitness;
                    bestIdx = i;
                }
            }

            // Broadcast best: send the best island's population to all others
            var bestPopulation = _islands[bestIdx].CurrentPopulation;
            if (bestPopulation == null)
                return;

            for (int i = 0; i < _islandCount; i++)
            {
                if (i == bestIdx)
                    continue;

                var cloned = bestPopulation.Clone();
                _islands[i]._pendingMigrantFitness = bestFit;
                Interlocked.Exchange(ref _islands[i]._pendingMigrant, cloned);
            }
        }

        /// <summary>
        /// Called when any island fires its PopulationUpdated event.
        /// Forwards the best island's updates to the subscriber.
        /// </summary>
        private void OnIslandUpdated(int islandIndex, Bitmap img, long fitnesse, Population pop,
            int gen, Image diffImg, long elapsed, int zoom, string stats)
        {
            bool shouldForward = false;
            long displayFitness;

            lock (_bestLock)
            {
                if (fitnesse < _bestFitness)
                {
                    _bestFitness = fitnesse;
                    _bestIslandIndex = islandIndex;
                }

                if (islandIndex == _bestIslandIndex)
                {
                    shouldForward = true;
                }
            }

            displayFitness = _bestFitness;

            if (shouldForward)
            {
                var enrichedStats = $"[Island {islandIndex + 1}/{_islandCount} Zoom:{_resizeFactor}] {stats}";
                PopulationUpdated?.Invoke(img, displayFitness, pop, gen, diffImg, _stopwatch.ElapsedMilliseconds, _resizeFactor, enrichedStats);
            }
        }
    }
}
