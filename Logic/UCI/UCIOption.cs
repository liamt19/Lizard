using System.Reflection;

namespace Lizard.Logic.UCI
{
    public class UCIOption
    {
        private const double AutoMinMaxMultiplier = 0.6;

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

        public void AutoMinMax()
        {
            if (FieldHandle.FieldType != typeof(int))
            {
                Log($"AutoMinMax was called on {FieldHandle.Name}, which is a {FieldHandle.FieldType}, not an int!");
                return;
            }

            int v = int.Parse(DefaultValue);
            MinValue = (int)(v * (1 - AutoMinMaxMultiplier));
            MaxValue = (int)(v * (1 + AutoMinMaxMultiplier));
        }


        public string GetSPSAFormat()
        {
            const int minStepSize = 1;
            int stepSize = Math.Max(minStepSize, (MaxValue - MinValue) / 10);

            //  name, int, default, min, max, step-size end, learning rate
            return $"{FieldHandle.Name}, int, {DefaultValue}, {MinValue}, {MaxValue}, {stepSize}, 0.002";
        }

        public override string ToString()
        {
            return "option name " + Name + " type " + Type + " default " + DefaultValue + (FieldHandle.FieldType == typeof(int) ? (" min " + MinValue + " max " + MaxValue) : string.Empty);
        }
    }
}
