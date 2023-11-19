using System.Reflection;

namespace LTChess.Logic.Core
{
    public class UCIOption
    {
        public string Name;
        public string Type;
        public string DefaultValue;
        public int MinValue;
        public int MaxValue;
        public int ValueArrayIndex = -1;

        public FieldInfo FieldHandle;

        public UCIOption(string name, string type, string defaultValue, FieldInfo fieldHandle)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            FieldHandle = fieldHandle;
        }

        public void SetMinMax(int min, int max)
        {
            MinValue = min;
            MaxValue = max;
        }

        public override string ToString()
        {
            return "option name " + Name + " type " + Type + " default " + DefaultValue + (FieldHandle.FieldType == typeof(int) ? (" min " + MinValue + " max " + MaxValue) : string.Empty);
        }
    }
}
