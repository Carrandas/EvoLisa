# Benchmark Results

## Run Configuration
- Branch: feature/skia-cpu-raster
- Renderer: GDI+
- Timestamp: 2026-06-05 11:57:02 UTC
- Generations: 100000
- Target Image: MonaLisa.jpg
- Target Image Size: 402x599

## Results
- Elapsed Time: 17644 ms
- Final Generation: 105996
- Final Fitness (GDI+ recomputed): 7063322
- Final Fitness (backend-measured): 2863681

## Mutation Statistics
Recolor:24% ChangePoint:14% AddPolygonPoint:14% RemovePolygonPoint:13% SwitchChromosomes:13% AddChromosome:10% RemoveChromosome:9%

## Phase Timings
```
Backend: GDI+
Phase timings over 101268 evals (15,0% accepted):
  Render:          16903 ms (98,3%)
  Fitness:           249 ms ( 1,4%)
  Copy(accept):       44 ms ( 0,3%)
```
