using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.Data;

namespace LTChess.Logic.Core
{
    /// <summary>
    /// Used for tuning parameters. Any parameters that were changed here were hypothesized to be better than the baseline.
    /// Uses https://github.com/kiudee/chess-tuning-tools.
    /// </summary>
    public static class Tune
    {
        static Tune()
        {
            //TuneTerms();

            NormalizeTerms();
        }

        /// <summary>
        /// Selects one term to use as a baseline, and multiplies every "Scale" term by the same amount so that the baseline term
        /// becomes 1 and the other terms retain the same ratio they had to the baseline to begin with.
        /// </summary>
        public static void NormalizeTerms()
        {
            const string pivotTerm = "ScaleMaterial";

            var fields = typeof(EvaluationConstants).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => (!x.IsLiteral && x.Name.StartsWith("Scale"))).ToList();

            var baseLineField = fields.Where(x => x.Name == pivotTerm).First();
            double[] baseLine = (double[]) baseLineField.GetValue(null);

            //  The pivot term needs to be multiplied by this factor to make it ~1.0
            double scaleFactorMG = (1 / baseLine[EvaluationConstants.GamePhaseNormal]);
            double scaleFactorEG = (1 / baseLine[EvaluationConstants.GamePhaseEndgame]);

            foreach (var field in fields.Where(x => x.Name != pivotTerm))
            {
                double[] arr = ((double[])field.GetValue(null));

                //Log(field.Name + ": " + arr[EvaluationConstants.GamePhaseNormal] + " -> " + Math.Round(arr[EvaluationConstants.GamePhaseNormal] * scaleFactorMG, 2));

                field.SetValue(null, new double[] {
                    Math.Round(arr[EvaluationConstants.GamePhaseNormal] * scaleFactorMG, 2),
                    Math.Round(arr[EvaluationConstants.GamePhaseEndgame] * scaleFactorEG, 2),
                });
            }

            baseLineField.SetValue(null, new double[] {
                    Math.Round(baseLine[EvaluationConstants.GamePhaseNormal] * scaleFactorMG, 2),
                    Math.Round(baseLine[EvaluationConstants.GamePhaseEndgame] * scaleFactorEG, 2),
            });

            //Log(baseLineField.Name + ": " + baseLine[EvaluationConstants.GamePhaseNormal] + " -> " + Math.Round(baseLine[EvaluationConstants.GamePhaseNormal] * scaleFactorMG, 2));
        }

        public static void TuneTerms()
        {
            EvaluationConstants.ScaleMaterial[0] = 1.21;

            //EvaluationConstants.ScalePositional[0] = 1.75;

            //EvaluationConstants.ScalePawns[0] = 0.49;
            //EvaluationConstants.ScaleKnights[0] = 0.53;
            //EvaluationConstants.ScaleBishops[0] = 0.68;
            //EvaluationConstants.ScaleRooks[0] = 0.34;
            //EvaluationConstants.ScaleQueens[0] = 0.56;

            //EvaluationConstants.ScaleKingSafety[0] = 1.1;
            //EvaluationConstants.ScaleSpace[0] = 1.09;
            //EvaluationConstants.ScaleThreats[0] = 0.12;

        }
    }
}
