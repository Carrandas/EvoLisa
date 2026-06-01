using System;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace GABase
{
    /// <summary>
    /// Orchestrates multiple Evolver instances (islands) running in parallel.
    /// Periodically migrates the best solution between islands using a ring topology.
    /// </summary>
    public class IslandEvolver
    {
        private readonly Evolver[] _islands;
        private readonly int _islandCount;
        private readonly Bitmap _targetImage;
        private Timer _migrationTimer;
        private int _bestIslandIndex;
        private long _bestFitness = long.MaxValue;
        private readonly object _bestLock = new object();

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

            if (islandCount <= 0)
                islandCount = Math.Min(Environment.ProcessorCount, 8);

            _islandCount = islandCount;
            _islands = new Evolver[_islandCount];

            for (int i = 0; i < _islandCount; i++)
            {
                _islands[i] = new Evolver(targetImage);
                var islandIndex = i;
                _islands[i].PopulationUpdated += (img, fitnesse, pop, gen, diffImg, elapsed, zoom, stats) =>
                {
                    OnIslandUpdated(islandIndex, img, fitnesse, pop, gen, diffImg, elapsed, zoom, stats);
                };
            }

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

        /// <summary>
        /// Ring-topology migration: island[i] receives a clone from island[(i-1+N)%N].
        /// </summary>
        private void MigrationCallback(object state)
        {
            try
            {
                PerformMigration();
            }
            catch (Exception)
            {
                // Migration is best-effort; skip on error (e.g. during resize)
            }
        }

        private void PerformMigration()
        {
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
            // Each island will only adopt if the migrant is fitter than its own
            var bestPopulation = _islands[bestIdx].CurrentPopulation;
            if (bestPopulation == null)
                return;

            for (int i = 0; i < _islandCount; i++)
            {
                if (i == bestIdx)
                    continue; // Don't send to self

                var cloned = bestPopulation.Clone();
                _islands[i]._pendingMigrantFitness = bestFit;
                Interlocked.Exchange(ref _islands[i]._pendingMigrant, cloned);
            }
        }

        /// <summary>
        /// Called when any island fires its PopulationUpdated event.
        /// Forwards only the best island's update to the subscriber.
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

                // Always forward the current best island's updates (so GUI shows progress)
                // but clamp the displayed fitness to never go up
                if (islandIndex == _bestIslandIndex)
                {
                    shouldForward = true;
                }
            }

            displayFitness = _bestFitness;

            if (shouldForward)
            {
                var enrichedStats = $"[Island {islandIndex + 1}/{_islandCount}] {stats}";
                PopulationUpdated?.Invoke(img, displayFitness, pop, gen, diffImg, elapsed, zoom, enrichedStats);
            }
        }
    }
}
