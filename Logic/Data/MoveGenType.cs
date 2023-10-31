using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.Data
{
    public interface MoveGenerationType { }

    public struct GenLoud : MoveGenerationType { }


    public struct GenQuiets : MoveGenerationType { }


    public struct GenEvasions : MoveGenerationType { }


    public struct GenQChecks : MoveGenerationType { }


    public struct GenNonEvasions : MoveGenerationType { }


}
