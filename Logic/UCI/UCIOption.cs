using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Core
{
    public struct UCIOption
    {
        public const string Opt_DefaultSearchTime = "DefaultSearchTime";
        public const string Opt_DefaultSearchDepth = "DefaultSearchDepth";


        public string Name;
        public string Type;
        public string DefaultValue;
        public string MinValue;
        public string MaxValue;

        public UCIOption(string name, string type, string defaultValue, string minValue, string maxValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override string ToString()
        {
            return "option name " + Name + " type " + Type + " default " + DefaultValue + " min " + MinValue + " max " + MaxValue;
        }
    }
}
