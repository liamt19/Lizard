
using System.Reflection;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace Lizard.Logic.Util
{
    public static unsafe partial class HorsieBindings
    {
        public static readonly bool HasBindings;
        private static readonly nint Handle;

        private const string DEST_NAME = "HorsieBindings";
        private static readonly bool IsWin = IsOSPlatform(OSPlatform.Windows);

        static HorsieBindings()
        {
            HasBindings = false;
            if (!IsOSPlatform(OSPlatform.Windows) && !IsOSPlatform(OSPlatform.Linux))
                return;

            string fExt = IsWin ? "dll" : "so";
            string asmName = Assembly.GetExecutingAssembly().GetName().Name;
            string fileName = $"{DEST_NAME}.{fExt}";
            string resName = $"{asmName}.{fileName}";

            string absPath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), fileName);

            try
            {
                if (!File.Exists(absPath) && !ExtractEmbeddedLibrary(resName, fileName))
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



        public static void DoSetupNNZ()
        {
            if (IsWin)
                HorsieSetupNNZWin();
            else
                HorsieSetupNNZUnix();
        }

        public static void DoGetEvaluation(short* us, short* them, sbyte* L1Weights, float* L1Biases,
            float* L2Weights, float* L2Biases, float* L3weights, float L3bias, ref int L3Output)
        {
            if (IsWin)
                HorsieGetEvaluationWin(us, them, L1Weights, L1Biases, L2Weights, L2Biases, L3weights, L3bias, ref L3Output);
            else
                HorsieGetEvaluationUnix(us, them, L1Weights, L1Biases, L2Weights, L2Biases, L3weights, L3bias, ref L3Output);
        }



        [LibraryImport("HorsieBindings.so", EntryPoint = "SetupNNZ")]
        public static partial void HorsieSetupNNZUnix();

        [LibraryImport("HorsieBindings.so", EntryPoint = "EvaluateBound")]
        public static partial void HorsieGetEvaluationUnix(short* us, short* them, sbyte* L1Weights, float* L1Biases,
            float* L2Weights, float* L2Biases, float* L3weights, float L3bias, ref int L3Output);


        [LibraryImport("HorsieBindings.dll", EntryPoint = "SetupNNZ")]
        public static partial void HorsieSetupNNZWin();

        [LibraryImport("HorsieBindings.dll", EntryPoint = "EvaluateBound")]
        public static partial void HorsieGetEvaluationWin(short* us, short* them, sbyte* L1Weights, float* L1Biases,
            float* L2Weights, float* L2Biases, float* L3weights, float L3bias, ref int L3Output);
    }
}
