using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.Data
{
    public interface PredicateType { }

    public struct PredicateNext : PredicateType { }
    public struct PredicateBest : PredicateType { }

}
