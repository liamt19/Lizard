using Lizard.Logic.NN;

namespace Lizard.Logic.Data
{
    public unsafe class AccumulatorVector(uint initialItems) : UnsafeVector<Accumulator>(initialItems)
    {
        ~AccumulatorVector()
        {
            foreach (var acc in new Span<Accumulator>(Items, (int)Size))
            {
                acc.Dispose();
            }
        }
    }
}
