namespace Lizard.Logic.Util
{
    /// <summary>
    /// Placing this attribute on a class causes the static constructor of that class to be skipped during program initialization.
    /// <para></para>
    /// This is important for the main classes of NNUE networks as well as their FeatureTransformers, as only one 
    /// architecture can be used at a time, and initializing multiple wastes memory.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class)]
    public sealed class SkipStaticConstructorAttribute : Attribute { }
}
