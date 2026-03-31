using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using GAgeneratedImagese.Tools;

namespace GABase
{
    public class Evolver
    {
        private FastBitmap _originalPictureBitmap;
        private readonly Bitmap _targetImage;
        private Population _popA;
        private Selector _selector;
        private Mutator _mutator;
        private Thread _workerThread;
        private int _resizeFactor = 4;
        private int _generation = 1;
        private long _previousFitnesse = long.MaxValue;
        private readonly Stopwatch _stopwatch;
        private long _lastUpdate;

        public event Action<Bitmap, long, Population, int, Image, long, int> PopulationUpdated;

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
        }

        public Population CurrentPopulation => _popA;
        public int CurrentGeneration => _generation;

        public void Start()
        {
            if (_workerThread == null || !_workerThread.IsAlive)
            {
                _workerThread = new Thread(RunEvolution);
                _workerThread.Start();
            }
        }

        public void Stop()
        {
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Abort();
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

        private void RunEvolution()
        {
            _popA = new Population(Settings.MaxPolygonCount);
            _popA.GenerateRandomChromosomes();
            Chromosome chromosome = new Chromosome(Settings.MaxPolygonPointCount);
            _popA.chromosomes.Add(chromosome);
            chromosome.GenerateRandomChromosome();

            Population popB;

            while (true)
            {
                double improvedPercentage = 0.0;

                popB = _popA.Clone();

                int mutation = RandomGenerator.GetRandomInt(100);
                if (mutation < 20)
                {
                    _mutator.Recolor(popB);
                }
                else if (mutation < 89)
                {
                    _mutator.ChangePoint(popB);
                }
                else if (mutation < 92)
                {
                    _mutator.AddPolygonPoint(popB);
                }
                else if (mutation < 95)
                {
                    _mutator.RemovePolygonPoint(popB);
                }
                else if (mutation < 98)
                {
                    _mutator.SwitchChromosomes(popB);
                }
                else if (mutation < 99)
                {
                    _mutator.AddChromosome(popB, _originalPictureBitmap);
                    improvedPercentage = 1;
                }
                else if (mutation < 100)
                {
                    improvedPercentage = -1;
                    _mutator.RemoveChromosome(popB);
                }

                if (popB.IsDirty)
                {
                    popB.IsDirty = false;
                    _popA = _selector.SelectPopulation(_popA, popB, out var newPartialFitnesse, improvedPercentage);

                    if (_stopwatch.ElapsedMilliseconds > _lastUpdate + 2500)
                    {
                        var currentFitnesse = _selector.CalculateFitness(_popA);

                        var differenceImage = DifferencePicture.GetDifferencePicture(_popA, _originalPictureBitmap);

                        var generatedImage = new Bitmap(_popA.GetPicture(), _targetImage.Width, _targetImage.Height);

                        PopulationUpdated?.Invoke(generatedImage, currentFitnesse, _popA, _generation, differenceImage, _stopwatch.ElapsedMilliseconds, _resizeFactor);

                        HandleResize(currentFitnesse);

                        _previousFitnesse = currentFitnesse;
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

                _selector = new Selector(new FastBitmap((Bitmap)(resizedBitmap.Clone())));

                foreach (var c in _popA.chromosomes)
                {
                    for (var index = 0; index < c.Polygon.Count; index++)
                    {
                        var p = c.Polygon[index];
                        c.Polygon[index] = new Point(Math.Min(p.X * 2, Settings.ScreenWidth), Math.Min(p.Y * 2, Settings.ScreenHeight));
                    }
                }

                DifferencePicture.GetDifferencePicture(_popA, _originalPictureBitmap);
            }
        }
    }
}
