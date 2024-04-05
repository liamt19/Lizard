using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lizard.Logic.Data
{
    public enum FeatureUpdateType : byte
    {
        Normal,
        Capture,
        Castle,
        None,
    }

    public unsafe struct FeatureUpdate
    {
        public fixed int Add[2];
        public fixed int Sub[2];

        public FeatureUpdateType UpdateType;
    }

    [InlineArray(2)]
    public unsafe struct FeatureUpdatePair
    {
        public FeatureUpdate PerspectiveUpdate;
    }
}
