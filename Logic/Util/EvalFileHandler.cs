namespace Lizard.Logic.Util
{

    //  https://stackoverflow.com/questions/49522751/how-to-read-get-a-propertygroup-value-from-a-csproj-file-using-c-sharp-in-a-ne

    [System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    sealed class EvalFileAttribute : System.Attribute
    {
        public string EvalFile { get; }
        public EvalFileAttribute(string evalFile)
        {
            this.EvalFile = evalFile;
        }
    }
}
