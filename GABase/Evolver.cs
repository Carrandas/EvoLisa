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
        private int _resizeFactor = 4;
        private int _generation = 1;
        private long _previousFitnesse = long.MaxValue;
        private readonly Stopwatch _stopwatch;
        private long _lastUpdate;

        private MutationWeightStats[] _mutationStats;
        private readonly object _statsLock = new object();

        public event Action<Bitmap, long, Population, int, Image, long, int, string> PopulationUpdated;

        public int TargetWidth => _targetImage.Width;
        public int TargetHeight => _targetImage.Height;

        public Evolver(Bitmap targetImage)
        {
            _targetImage = targetImage;
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
                double improvedPercentage = 0.0;

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
                        break;
                    case MutationType.RemovePolygonPoint:
                        backup = _mutator.RemovePolygonPointWithBackup(_popA);
                        break;
                    case MutationType.SwitchChromosomes:
                        backup = _mutator.SwitchChromosomesWithBackup(_popA);
                        break;
                    case MutationType.AddChromosome:
                        backup = _mutator.AddChromosomeWithBackup(_popA, _originalPictureBitmap);
                        improvedPercentage = 1;
                        break;
                    case MutationType.RemoveChromosome:
                        improvedPercentage = -1;
                        backup = _mutator.RemoveChromosomeWithBackup(_popA);
                        break;
                    default:
                        backup = new MutationBackup();
                        break;
                }

                if (_popA.IsDirty && backup.WasDirty)
                {
                    _popA.IsDirty = false;

                    bool accepted = _selector.EvaluateMutation(_popA, _popA.DirtyArea, improvedPercentage, out var newPartialFitness);

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

                    if (_stopwatch.ElapsedMilliseconds > _lastUpdate + 2500)
                    {
                        var (differenceImage, fitnesse) = DifferencePicture.GetDifferencePictureWithFitness(_popA, _originalPictureBitmap);

                        var generatedImage = new Bitmap(_popA.GetPicture(), _targetImage.Width, _targetImage.Height);
                        var mutationStats = GetMutationStats();

                        PopulationUpdated?.Invoke(generatedImage, fitnesse, _popA, _generation, differenceImage, _stopwatch.ElapsedMilliseconds, _resizeFactor, mutationStats);

                        HandleResize(fitnesse);

                        _previousFitnesse = fitnesse;
                        _lastUpdate = _stopwatch.ElapsedMilliseconds;
                    }
                }

                _generation++;
            }
        }

        private void HandleResize(long currentFitnesse)
        {
            if (_previousFitnesse > 0 &&
                (_previousFitnesse - currentFitnesse) * 1.0 / _previousFitnesse < 0.0001 &&
                _resizeFactor > 1)
            {
                _resizeFactor /= 2;

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

                DifferencePicture.GetDifferencePicture(_popA, _originalPictureBitmap);
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
