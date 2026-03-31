using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GABase;

namespace GABaseBenchmarkTests
{
    [TestClass]
    public class BenchmarkTests
    {
        private const int Generations = 1000;

        [TestMethod]
        public void RunEvolutionBenchmark()
        {
            var solutionDir = GetSolutionDirectory();
            var imagePath = Path.Combine(solutionDir, "GABaseBenchmarkTests", "MonaLisa.jpg");
            
            if (!File.Exists(imagePath))
            {
                Assert.Inconclusive($"Image not found at: {imagePath}");
            }
            
            using (var targetImage = new Bitmap(imagePath))
            {
                Settings.ScreenWidth = targetImage.Width;
                Settings.ScreenHeight = targetImage.Height;

                var evolver = new Evolver(targetImage);

                long finalFitnesse = 0;
                int finalGeneration = 0;

                evolver.PopulationUpdated += (img, fitnesse, pop, generation, diffImg, elapsed, zoom) =>
                {
                    finalFitnesse = fitnesse;
                    finalGeneration = generation;
                };

                var stopwatch = Stopwatch.StartNew();
                evolver.Start();

                while (finalGeneration < Generations)
                {
                    System.Threading.Thread.Sleep(100);
                    if (evolver.CurrentPopulation != null && finalGeneration >= Generations)
                        break;
                }

                evolver.Stop();
                stopwatch.Stop();

                var branchName = GetBranchName();
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                var result = new StringBuilder();
                result.AppendLine($"# Benchmark Results");
                result.AppendLine();
                result.AppendLine($"## Run Configuration");
                result.AppendLine($"- Branch: {branchName}");
                result.AppendLine($"- Timestamp: {timestamp}");
                result.AppendLine($"- Generations: {Generations}");
                result.AppendLine($"- Target Image: MonaLisa.jpg");
                result.AppendLine($"- Target Image Size: {targetImage.Width}x{targetImage.Height}");
                result.AppendLine();
                result.AppendLine($"## Results");
                result.AppendLine($"- Elapsed Time: {elapsedMs} ms");
                result.AppendLine($"- Final Generation: {finalGeneration}");
                result.AppendLine($"- Final Fitness: {finalFitnesse}");
                result.AppendLine();

                var docsPath = Path.Combine(GetSolutionDirectory(), "docs");
                Directory.CreateDirectory(docsPath);

                var fileName = $"benchmark-{branchName}-{timestamp.Replace(" ", "-").Replace(":", "-").Replace("UTC", "UTC")}.md".ToLower();
                var filePath = Path.Combine(docsPath, fileName);
                File.WriteAllText(filePath, result.ToString());

                Console.WriteLine(result.ToString());
                Console.WriteLine($"Benchmark saved to: {filePath}");
            }
        }

        private string GetBranchName()
        {
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = GetSolutionDirectory()
                };
                using (var proc = Process.Start(psi))
                {
                    return proc.StandardOutput.ReadLine().Trim();
                }
            }
            catch
            {
                return "unknown";
            }
        }

        private string GetSolutionDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.GetDirectoryName(baseDir);
            
            while (dir != null && !File.Exists(Path.Combine(dir, "GA.sln")))
            {
                dir = Path.GetDirectoryName(dir);
            }
            
            if (dir == null)
            {
                dir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            }
            
            return dir;
        }
    }
}