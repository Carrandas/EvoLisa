# CLAUDE.md

Guidance for AI assistants working on the EvoLisa project. See also `AGENTS.md`.

## Project Overview

EvoLisa evolves a set of semi-transparent polygons to approximate a target image
using a hill-climbing genetic algorithm. A mutation is applied in-place, the
affected region is re-rendered and re-scored, and the mutation is kept only if it
lowers fitness (sum of squared per-channel pixel differences vs the target).

## Solution Layout

- `GABase/` - core GA library (.NET 9, `net9.0-windows`, `AllowUnsafeBlocks`). All business logic lives here.
- `GA/` (GAGUI) - thin Windows Forms UI. Keep logic out of this layer.
- `GaBaseTests/` - MSTest unit tests for GABase.
- `GABaseBenchmarkTests/` - MSTest throughput benchmarks; writes results to `docs/`.

## Build & Test

```pwsh
dotnet build GABase/GABase.csproj -c Release
dotnet test  GaBaseTests/GABaseTests.csproj -c Release          # unit tests
dotnet test  GABaseBenchmarkTests/GABaseBenchmarkTests.csproj -c Release `
  --filter "FullyQualifiedName~RunEvolutionBenchmark&FullyQualifiedName!~MultiIsland"
```

## Workflow

- Develop on a `feature/<feature>` branch. Do not commit; the user handles commits.
- Do not add code comments unless explicitly requested.
- Keep responses concise; follow existing conventions.

## Architecture (GABase)

- `Evolver` - owns the per-island evolution loop on a background thread. Selects a
  weighted mutation, applies it, asks `Selector` to evaluate, reverts if rejected.
  Drives progressive resize (start at `1/resizeFactor`, double resolution on plateau).
- `IslandEvolver` - runs N `Evolver` islands in parallel; periodic migration of the
  best population; stop-the-world resize across all islands.
- `Population` / `Chromosome` - the genome: a list of colored polygons. Each chromosome
  caches `PolygonArray` and `BoundingBox` (call `UpdatePolygonArray()` after edits).
- `Mutator` - 7 mutation types, each with a `*WithBackup` variant + `RevertMutation`
  so rejected mutations are undone in-place (no full clone per generation).
- `Selector` - the performance-critical core. Maintains a cached "current best"
  bitmap; renders only the mutation's dirty rectangle into a candidate bitmap and
  computes partial fitness over that region.
- `Settings` - global static config (dimensions, polygon limits, focus weight map).
- `Tools/FastBitmap` - `unsafe` LockBits pixel access. `Tools/RandomGenerator` -
  `ThreadLocal<Random>`. `Tools/DifferencePicture` - full-image diff/fitness for UI.

## Performance-Critical Hot Path

Runs every generation - keep it allocation-free and avoid redundant GDI/LockBits:

- `Selector.EvaluateMutation` -> `RenderDirtyArea` (GDI fill of the dirty rect) ->
  `ComputeBothPartialFitnesses` (single unsafe pass scoring candidate and current-best
  vs the reference) -> `CopyDirtyArea` if accepted.
- `Selector` holds persistent `Graphics` objects (`_candidateGraphics`,
  `_currentBestGraphics`) and a single reusable `SolidBrush` instead of allocating
  per generation / per chromosome. Call `Graphics.Flush(Sync)` after drawing before
  reading pixels via `LockBits`.
- The reference image is snapshotted once into a managed `Pixel[]` (`_referencePixels`)
  so the fitness loop never re-locks the reference bitmap. Read it by absolute
  coordinates: `pRefBase + (minY + y) * _referenceWidth + minX`.
- Polygon colors use alpha (`Settings.UseARGB`), so rendering must stay alpha-blended
  (`SourceOver`); do not switch to `SourceCopy`.

These changes roughly doubled fixed-resolution throughput (~1673 -> ~3299 gen/s on
the MonaLisa benchmark).

## Benchmark Notes

- `RunEvolutionBenchmark` runs at a FIXED resolution (`disableResize: true,
  resizeFactor: 4`) so gen/s is a clean per-generation throughput signal. Keep it
  this way - progressive resize makes wall-clock time RNG-dependent and unusable for
  A/B comparisons.
- Compare gen/s across multiple runs (small run-to-run variance from the 2.5s update
  cadence is expected).

## Known Follow-ups (not yet done)

- SIMD-vectorize `ComputeBothPartialFitnesses` (comment claims SIMD; loop is scalar).
- `Chromosome.GetPoint` scans the whole `differences` array O(W*H) per AddChromosome;
  use a 1D cumulative array + binary search.
- The parallel `DifferencePicture.GetDifferencePictureWithFitness` stores a per-row
  cumulative instead of a global prefix sum, which feeds `GetPoint` a wrong
  distribution (correctness/targeting bug).
