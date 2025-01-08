
using System.Reflection;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace Lizard.Logic.Util
{
    public static unsafe partial class HorsieBindings
    {
        public static readonly bool HasBindings;
        private static readonly nint Handle;

        private const string DEST_NAME = "HorsieBindings.dll";

        static HorsieBindings()
        {
            HasBindings = false;
            if (!IsOSPlatform(OSPlatform.Windows) || IsOSPlatform(OSPlatform.Linux))
                return;

            string fExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dll" : "so";
            string asmName = Assembly.GetExecutingAssembly().GetName().Name;
            string resName = $"{asmName}.HorsieBindings.{fExt}";

            string exePath = Path.GetDirectoryName(AppContext.BaseDirectory);
            string absPath = Path.Combine(exePath, DEST_NAME);

            try
            {
                if (!File.Exists(absPath) && !ExtractEmbeddedLibrary(resName, DEST_NAME))
                {
                    return;
                }

                Handle = NativeLibrary.Load(absPath);
            }
            catch (Exception e)
            {
                Log("Failed loading Horsie bindings! :(");
                Log(e.Message);
                return;
            }

            HasBindings = true;
            Log("Loaded Horsie bindings!");
        }

        private static bool ExtractEmbeddedLibrary(string resName, string fileName)
        {
            var asm = Assembly.GetExecutingAssembly();
            Debug.WriteLine($"looking for {resName} in [{string.Join(", ", asm.GetManifestResourceNames())}]");
            using Stream stream = asm.GetManifestResourceStream(resName);

            if (stream == null)
            {
                Log("Running without Horsie bindings");
                return false;
            }

            string exePath = Path.GetDirectoryName(AppContext.BaseDirectory);
            string dllPath = Path.Combine(exePath, fileName);

            using FileStream fs = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);

            return true;
        }



        [LibraryImport("HorsieBindings.dll", EntryPoint = "SetupNNZ")]
        public static partial void HorsieSetupNNZ();

        [LibraryImport("HorsieBindings.dll", EntryPoint = "EvaluateBound")]
        public static partial void HorsieGetEvaluation(short* us, short* them, sbyte* L1Weights, float* L1Biases,
            float* L2Weights, float* L2Biases, float* L3weights, float L3bias, ref int L3Output);

    }
}
