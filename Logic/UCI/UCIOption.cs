using System.Reflection;

namespace Lizard.Logic.UCI
{
    public class UCIOption
    {
        /// <summary>
        /// +/- 60%
        /// </summary>
        private const double AutoMinMaxMultiplier = 0.6;


        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name;

        /// <summary>
        /// Either "spin" for numerical values, or "check" for booleans.
        /// </summary>
        public string Type;

        /// <summary>
        /// The default value of the field, which should always be kept in sync with the FieldHandle's value.
        /// <para></para>
        /// TODO: this can be a setter.
        /// </summary>
        public string DefaultValue;

        /// <summary>
        /// The minimum value that this option can be set to. 
        /// Requests to set it lower than this will be ignored.
        /// </summary>
        public int MinValue;

        /// <summary>
        /// The maximum value that this option can be set to.
        /// Requests to set it higher than this will be ignored.
        /// </summary>
        public int MaxValue;

        /// <summary>
        /// The actual field that this option represents, which is in the <see cref="SearchOptions"/> class.
        /// </summary>
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

        /// <summary>
        /// Sets the <see cref="MinValue"/> and <see cref="MaxValue"/> to be a percentage of the <see cref="DefaultValue"/>, 
        /// which is based on <see cref="AutoMinMaxMultiplier"/> ( Currently <inheritdoc cref="AutoMinMaxMultiplier"/>).
        /// </summary>
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


        /// <summary>
        /// Sets the value of the <see cref="FieldHandle"/> that this option represents to <see cref="DefaultValue"/>
        /// </summary>
        public void RefreshBackingField()
        {
            if (FieldHandle == null)
            {
                return;
            }

            if (FieldHandle.FieldType == typeof(int))
            {
                FieldHandle.SetValue(null, int.Parse(DefaultValue));
            }
            else if (FieldHandle.FieldType == typeof(bool))
            {
                FieldHandle.SetValue(null, bool.Parse(DefaultValue));
            }
        }


        /// <summary>
        /// Returns a string in the formatting expected by OpenBench's SPSA tuner.
        /// <para></para>
        /// This looks like "name, int, default, min, max, step-size end, learning rate"
        /// </summary>
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
