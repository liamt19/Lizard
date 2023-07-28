using System.Reflection;

namespace LTChess.Core
{
    public struct UCIOption
    {
        public string Name;
        public string Type;
        public string DefaultValue;
        public string MinValue;
        public string MaxValue;
        public int ValueArrayIndex = -1;

        public FieldInfo FieldHandle;

        public UCIOption(string name, string type, string defaultValue, FieldInfo fieldHandle)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            FieldHandle = fieldHandle;
        }

        public override string ToString()
        {
            return "option name " + Name + " type " + Type + " default " + DefaultValue + ((MinValue == null || MinValue.Length == 0) ? string.Empty : (" min " + MinValue + " max " + MaxValue));
        }
    }
}
