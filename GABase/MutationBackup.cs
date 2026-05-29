using System.Drawing;

namespace GABase
{
    /// <summary>
    /// Stores minimal undo state for a single mutation, enabling in-place mutation with revert.
    /// </summary>
    public struct MutationBackup
    {
        public Evolver.MutationType Type;
        public int ChromosomeIndex;
        public bool WasDirty;

        // For Recolor
        public Color OriginalColor;

        // For ChangePoint
        public int PointIndex;
        public Point OriginalPoint;

        // For AddPolygonPoint
        public int InsertedPointIndex;

        // For RemovePolygonPoint
        public Point RemovedPoint;
        public int RemovedPointIndex;

        // For SwitchChromosomes
        public int SwapIndex1;
        public int SwapIndex2;

        // For AddChromosome (chromosome added at end of list)

        // For RemoveChromosome
        public Chromosome RemovedChromosome;
        public int RemovedChromosomeIndex;
    }
}
