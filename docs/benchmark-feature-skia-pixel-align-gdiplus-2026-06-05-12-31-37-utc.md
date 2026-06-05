# Benchmark Results

## Run Configuration
- Branch: feature/skia-pixel-align
- Renderer: GDI+
- Timestamp: 2026-06-05 12:31:37 UTC
- Generations: 100000
- Target Image: MonaLisa.jpg
- Target Image Size: 402x599

## Results
- Elapsed Time: 15128 ms
- Final Generation: 103410
- Final Fitness (GDI+ recomputed): 7540207
- Final Fitness (backend-measured): 3061169

## Mutation Statistics
Recolor:24% ChangePoint:13% AddPolygonPoint:14% RemovePolygonPoint:13% SwitchChromosomes:13% AddChromosome:10% RemoveChromosome:9%

## Phase Timings
```
Backend: GDI+
Phase timings over 98277 evals (14,4% accepted):
  Render:          14561 ms (98,5%)
  Fitness:           178 ms ( 1,2%)
  Copy(accept):       43 ms ( 0,3%)
```
