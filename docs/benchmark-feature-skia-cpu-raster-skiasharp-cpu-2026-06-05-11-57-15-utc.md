# Benchmark Results

## Run Configuration
- Branch: feature/skia-cpu-raster
- Renderer: SkiaSharp-CPU
- Timestamp: 2026-06-05 11:57:15 UTC
- Generations: 100000
- Target Image: MonaLisa.jpg
- Target Image Size: 402x599

## Results
- Elapsed Time: 12572 ms
- Final Generation: 107287
- Final Fitness (GDI+ recomputed): 14250442
- Final Fitness (backend-measured): 3715126

## Mutation Statistics
Recolor:21% ChangePoint:14% AddPolygonPoint:16% RemovePolygonPoint:14% SwitchChromosomes:10% AddChromosome:12% RemoveChromosome:10%

## Phase Timings
```
Backend: SkiaSharp CPU
Phase timings over 103033 evals (10,0% accepted):
  Render:          11989 ms (98,1%)
  Fitness:           220 ms ( 1,8%)
  Copy(accept):       14 ms ( 0,1%)
```
