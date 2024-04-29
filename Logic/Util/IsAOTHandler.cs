using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lizard.Logic.Util
{

    //  https://stackoverflow.com/questions/49522751/how-to-read-get-a-propertygroup-value-from-a-csproj-file-using-c-sharp-in-a-ne

    [System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    sealed class IsAOTAttribute : System.Attribute
    {
        public bool _IsAOT { get; }
        public IsAOTAttribute(string str)
        {
            this._IsAOT = (str.EqualsIgnoreCase("true"));
        }

        public static bool IsAOT()
        {
            bool retVal = false;
            try
            {
                retVal = Assembly.GetEntryAssembly().GetCustomAttribute<IsAOTAttribute>()._IsAOT;
            }
            catch { }

            return retVal;
        }
    }
}
