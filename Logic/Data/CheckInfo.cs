using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LTChess.Data
{
    /// <summary>
    /// Stores the status of the position in terms of checks and double checks.
    /// </summary>
    public struct CheckInfo
    {
        public const int NONE = 64;

        public int idxChecker = NONE;
        public int idxDoubleChecker = NONE;

        public bool InCheck = false;
        public bool InDoubleCheck = false;

        public CheckInfo()
        {

        }

        public override string ToString()
        {
            if (InCheck)
            {
                return "Check from " + IndexToString(idxChecker);
            }
            else if (InDoubleCheck)
            {
                return "Double check from " + IndexToString(idxChecker) + " and " + IndexToString(idxDoubleChecker);
            }
            else
            {
                return "NONE";
            }
        }
    }
}
