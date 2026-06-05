# Benchmark Results

## Run Configuration
- Branch: feature/skia-pixel-align
- Renderer: SkiaSharp-CPU
- Timestamp: 2026-06-05 12:31:50 UTC
- Generations: 100000
- Target Image: MonaLisa.jpg
- Target Image Size: 402x599

## Results
- Elapsed Time: 12564 ms
- Final Generation: 107047
- Final Fitness (GDI+ recomputed): 17562795
- Final Fitness (backend-measured): 8782132

## Mutation Statistics
Recolor:18% ChangePoint:14% AddPolygonPoint:16% RemovePolygonPoint:14% SwitchChromosomes:10% AddChromosome:13% RemoveChromosome:11%

## Phase Timings
```
Backend: SkiaSharp CPU
Phase timings over 100815 evals (7,9% accepted):
  Render:          12119 ms (98,9%)
  Fitness:           121 ms ( 1,0%)
  Copy(accept):        9 ms ( 0,1%)
```
