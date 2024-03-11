using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using Lizard.Properties;

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {
        public static int GetEvaluationUnrolled(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;

            if (accumulator.NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, White);
            }

            if (accumulator.NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, Black);
            }

            Vector256<short> ClampMax = Vector256.Create((short)QA);
            Vector256<int> normalSum = Vector256<int>.Zero;

            int outputBucket = (int)((popcount(pos.bb.Occupancy) - 2) / 4);

            var ourData = (short*)(accumulator[pos.ToMove]);
            var ourWeights = (short*)(LayerWeights + (outputBucket * (SIMD_CHUNKS * 2)));
            var theirData = (short*)(accumulator[Not(pos.ToMove)]);
            var theirWeights = (short*)(LayerWeights + (outputBucket * (SIMD_CHUNKS * 2)) + SIMD_CHUNKS);


            Vector256<short> clamp_us_0 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 0)));
            Vector256<short> clamp_us_16 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 16)));
            Vector256<short> clamp_us_32 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 32)));
            Vector256<short> clamp_us_48 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 48)));
            Vector256<short> clamp_us_64 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 64)));
            Vector256<short> clamp_us_80 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 80)));
            Vector256<short> clamp_us_96 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 96)));
            Vector256<short> clamp_us_112 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 112)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_0, Avx2.MultiplyLow(clamp_us_0, Avx2.LoadAlignedVector256(ourWeights + 0))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_16, Avx2.MultiplyLow(clamp_us_16, Avx2.LoadAlignedVector256(ourWeights + 16))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_32, Avx2.MultiplyLow(clamp_us_32, Avx2.LoadAlignedVector256(ourWeights + 32))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_48, Avx2.MultiplyLow(clamp_us_48, Avx2.LoadAlignedVector256(ourWeights + 48))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_64, Avx2.MultiplyLow(clamp_us_64, Avx2.LoadAlignedVector256(ourWeights + 64))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_80, Avx2.MultiplyLow(clamp_us_80, Avx2.LoadAlignedVector256(ourWeights + 80))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_96, Avx2.MultiplyLow(clamp_us_96, Avx2.LoadAlignedVector256(ourWeights + 96))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_112, Avx2.MultiplyLow(clamp_us_112, Avx2.LoadAlignedVector256(ourWeights + 112))));

            Vector256<short> clamp_us_128 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 128)));
            Vector256<short> clamp_us_144 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 144)));
            Vector256<short> clamp_us_160 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 160)));
            Vector256<short> clamp_us_176 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 176)));
            Vector256<short> clamp_us_192 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 192)));
            Vector256<short> clamp_us_208 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 208)));
            Vector256<short> clamp_us_224 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 224)));
            Vector256<short> clamp_us_240 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 240)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_128, Avx2.MultiplyLow(clamp_us_128, Avx2.LoadAlignedVector256(ourWeights + 128))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_144, Avx2.MultiplyLow(clamp_us_144, Avx2.LoadAlignedVector256(ourWeights + 144))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_160, Avx2.MultiplyLow(clamp_us_160, Avx2.LoadAlignedVector256(ourWeights + 160))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_176, Avx2.MultiplyLow(clamp_us_176, Avx2.LoadAlignedVector256(ourWeights + 176))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_192, Avx2.MultiplyLow(clamp_us_192, Avx2.LoadAlignedVector256(ourWeights + 192))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_208, Avx2.MultiplyLow(clamp_us_208, Avx2.LoadAlignedVector256(ourWeights + 208))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_224, Avx2.MultiplyLow(clamp_us_224, Avx2.LoadAlignedVector256(ourWeights + 224))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_240, Avx2.MultiplyLow(clamp_us_240, Avx2.LoadAlignedVector256(ourWeights + 240))));

            Vector256<short> clamp_us_256 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 256)));
            Vector256<short> clamp_us_272 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 272)));
            Vector256<short> clamp_us_288 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 288)));
            Vector256<short> clamp_us_304 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 304)));
            Vector256<short> clamp_us_320 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 320)));
            Vector256<short> clamp_us_336 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 336)));
            Vector256<short> clamp_us_352 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 352)));
            Vector256<short> clamp_us_368 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 368)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_256, Avx2.MultiplyLow(clamp_us_256, Avx2.LoadAlignedVector256(ourWeights + 256))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_272, Avx2.MultiplyLow(clamp_us_272, Avx2.LoadAlignedVector256(ourWeights + 272))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_288, Avx2.MultiplyLow(clamp_us_288, Avx2.LoadAlignedVector256(ourWeights + 288))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_304, Avx2.MultiplyLow(clamp_us_304, Avx2.LoadAlignedVector256(ourWeights + 304))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_320, Avx2.MultiplyLow(clamp_us_320, Avx2.LoadAlignedVector256(ourWeights + 320))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_336, Avx2.MultiplyLow(clamp_us_336, Avx2.LoadAlignedVector256(ourWeights + 336))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_352, Avx2.MultiplyLow(clamp_us_352, Avx2.LoadAlignedVector256(ourWeights + 352))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_368, Avx2.MultiplyLow(clamp_us_368, Avx2.LoadAlignedVector256(ourWeights + 368))));

            Vector256<short> clamp_us_384 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 384)));
            Vector256<short> clamp_us_400 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 400)));
            Vector256<short> clamp_us_416 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 416)));
            Vector256<short> clamp_us_432 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 432)));
            Vector256<short> clamp_us_448 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 448)));
            Vector256<short> clamp_us_464 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 464)));
            Vector256<short> clamp_us_480 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 480)));
            Vector256<short> clamp_us_496 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 496)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_384, Avx2.MultiplyLow(clamp_us_384, Avx2.LoadAlignedVector256(ourWeights + 384))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_400, Avx2.MultiplyLow(clamp_us_400, Avx2.LoadAlignedVector256(ourWeights + 400))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_416, Avx2.MultiplyLow(clamp_us_416, Avx2.LoadAlignedVector256(ourWeights + 416))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_432, Avx2.MultiplyLow(clamp_us_432, Avx2.LoadAlignedVector256(ourWeights + 432))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_448, Avx2.MultiplyLow(clamp_us_448, Avx2.LoadAlignedVector256(ourWeights + 448))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_464, Avx2.MultiplyLow(clamp_us_464, Avx2.LoadAlignedVector256(ourWeights + 464))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_480, Avx2.MultiplyLow(clamp_us_480, Avx2.LoadAlignedVector256(ourWeights + 480))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_496, Avx2.MultiplyLow(clamp_us_496, Avx2.LoadAlignedVector256(ourWeights + 496))));

            Vector256<short> clamp_us_512 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 512)));
            Vector256<short> clamp_us_528 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 528)));
            Vector256<short> clamp_us_544 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 544)));
            Vector256<short> clamp_us_560 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 560)));
            Vector256<short> clamp_us_576 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 576)));
            Vector256<short> clamp_us_592 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 592)));
            Vector256<short> clamp_us_608 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 608)));
            Vector256<short> clamp_us_624 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 624)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_512, Avx2.MultiplyLow(clamp_us_512, Avx2.LoadAlignedVector256(ourWeights + 512))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_528, Avx2.MultiplyLow(clamp_us_528, Avx2.LoadAlignedVector256(ourWeights + 528))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_544, Avx2.MultiplyLow(clamp_us_544, Avx2.LoadAlignedVector256(ourWeights + 544))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_560, Avx2.MultiplyLow(clamp_us_560, Avx2.LoadAlignedVector256(ourWeights + 560))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_576, Avx2.MultiplyLow(clamp_us_576, Avx2.LoadAlignedVector256(ourWeights + 576))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_592, Avx2.MultiplyLow(clamp_us_592, Avx2.LoadAlignedVector256(ourWeights + 592))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_608, Avx2.MultiplyLow(clamp_us_608, Avx2.LoadAlignedVector256(ourWeights + 608))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_624, Avx2.MultiplyLow(clamp_us_624, Avx2.LoadAlignedVector256(ourWeights + 624))));

            Vector256<short> clamp_us_640 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 640)));
            Vector256<short> clamp_us_656 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 656)));
            Vector256<short> clamp_us_672 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 672)));
            Vector256<short> clamp_us_688 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 688)));
            Vector256<short> clamp_us_704 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 704)));
            Vector256<short> clamp_us_720 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 720)));
            Vector256<short> clamp_us_736 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 736)));
            Vector256<short> clamp_us_752 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 752)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_640, Avx2.MultiplyLow(clamp_us_640, Avx2.LoadAlignedVector256(ourWeights + 640))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_656, Avx2.MultiplyLow(clamp_us_656, Avx2.LoadAlignedVector256(ourWeights + 656))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_672, Avx2.MultiplyLow(clamp_us_672, Avx2.LoadAlignedVector256(ourWeights + 672))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_688, Avx2.MultiplyLow(clamp_us_688, Avx2.LoadAlignedVector256(ourWeights + 688))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_704, Avx2.MultiplyLow(clamp_us_704, Avx2.LoadAlignedVector256(ourWeights + 704))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_720, Avx2.MultiplyLow(clamp_us_720, Avx2.LoadAlignedVector256(ourWeights + 720))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_736, Avx2.MultiplyLow(clamp_us_736, Avx2.LoadAlignedVector256(ourWeights + 736))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_752, Avx2.MultiplyLow(clamp_us_752, Avx2.LoadAlignedVector256(ourWeights + 752))));

            Vector256<short> clamp_us_768 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 768)));
            Vector256<short> clamp_us_784 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 784)));
            Vector256<short> clamp_us_800 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 800)));
            Vector256<short> clamp_us_816 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 816)));
            Vector256<short> clamp_us_832 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 832)));
            Vector256<short> clamp_us_848 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 848)));
            Vector256<short> clamp_us_864 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 864)));
            Vector256<short> clamp_us_880 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 880)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_768, Avx2.MultiplyLow(clamp_us_768, Avx2.LoadAlignedVector256(ourWeights + 768))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_784, Avx2.MultiplyLow(clamp_us_784, Avx2.LoadAlignedVector256(ourWeights + 784))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_800, Avx2.MultiplyLow(clamp_us_800, Avx2.LoadAlignedVector256(ourWeights + 800))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_816, Avx2.MultiplyLow(clamp_us_816, Avx2.LoadAlignedVector256(ourWeights + 816))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_832, Avx2.MultiplyLow(clamp_us_832, Avx2.LoadAlignedVector256(ourWeights + 832))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_848, Avx2.MultiplyLow(clamp_us_848, Avx2.LoadAlignedVector256(ourWeights + 848))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_864, Avx2.MultiplyLow(clamp_us_864, Avx2.LoadAlignedVector256(ourWeights + 864))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_880, Avx2.MultiplyLow(clamp_us_880, Avx2.LoadAlignedVector256(ourWeights + 880))));

            Vector256<short> clamp_us_896 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 896)));
            Vector256<short> clamp_us_912 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 912)));
            Vector256<short> clamp_us_928 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 928)));
            Vector256<short> clamp_us_944 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 944)));
            Vector256<short> clamp_us_960 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 960)));
            Vector256<short> clamp_us_976 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 976)));
            Vector256<short> clamp_us_992 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 992)));
            Vector256<short> clamp_us_1008 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1008)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_896, Avx2.MultiplyLow(clamp_us_896, Avx2.LoadAlignedVector256(ourWeights + 896))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_912, Avx2.MultiplyLow(clamp_us_912, Avx2.LoadAlignedVector256(ourWeights + 912))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_928, Avx2.MultiplyLow(clamp_us_928, Avx2.LoadAlignedVector256(ourWeights + 928))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_944, Avx2.MultiplyLow(clamp_us_944, Avx2.LoadAlignedVector256(ourWeights + 944))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_960, Avx2.MultiplyLow(clamp_us_960, Avx2.LoadAlignedVector256(ourWeights + 960))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_976, Avx2.MultiplyLow(clamp_us_976, Avx2.LoadAlignedVector256(ourWeights + 976))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_992, Avx2.MultiplyLow(clamp_us_992, Avx2.LoadAlignedVector256(ourWeights + 992))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1008, Avx2.MultiplyLow(clamp_us_1008, Avx2.LoadAlignedVector256(ourWeights + 1008))));

            Vector256<short> clamp_us_1024 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1024)));
            Vector256<short> clamp_us_1040 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1040)));
            Vector256<short> clamp_us_1056 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1056)));
            Vector256<short> clamp_us_1072 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1072)));
            Vector256<short> clamp_us_1088 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1088)));
            Vector256<short> clamp_us_1104 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1104)));
            Vector256<short> clamp_us_1120 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1120)));
            Vector256<short> clamp_us_1136 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1136)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1024, Avx2.MultiplyLow(clamp_us_1024, Avx2.LoadAlignedVector256(ourWeights + 1024))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1040, Avx2.MultiplyLow(clamp_us_1040, Avx2.LoadAlignedVector256(ourWeights + 1040))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1056, Avx2.MultiplyLow(clamp_us_1056, Avx2.LoadAlignedVector256(ourWeights + 1056))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1072, Avx2.MultiplyLow(clamp_us_1072, Avx2.LoadAlignedVector256(ourWeights + 1072))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1088, Avx2.MultiplyLow(clamp_us_1088, Avx2.LoadAlignedVector256(ourWeights + 1088))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1104, Avx2.MultiplyLow(clamp_us_1104, Avx2.LoadAlignedVector256(ourWeights + 1104))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1120, Avx2.MultiplyLow(clamp_us_1120, Avx2.LoadAlignedVector256(ourWeights + 1120))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1136, Avx2.MultiplyLow(clamp_us_1136, Avx2.LoadAlignedVector256(ourWeights + 1136))));

            Vector256<short> clamp_us_1152 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1152)));
            Vector256<short> clamp_us_1168 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1168)));
            Vector256<short> clamp_us_1184 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1184)));
            Vector256<short> clamp_us_1200 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1200)));
            Vector256<short> clamp_us_1216 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1216)));
            Vector256<short> clamp_us_1232 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1232)));
            Vector256<short> clamp_us_1248 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1248)));
            Vector256<short> clamp_us_1264 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1264)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1152, Avx2.MultiplyLow(clamp_us_1152, Avx2.LoadAlignedVector256(ourWeights + 1152))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1168, Avx2.MultiplyLow(clamp_us_1168, Avx2.LoadAlignedVector256(ourWeights + 1168))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1184, Avx2.MultiplyLow(clamp_us_1184, Avx2.LoadAlignedVector256(ourWeights + 1184))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1200, Avx2.MultiplyLow(clamp_us_1200, Avx2.LoadAlignedVector256(ourWeights + 1200))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1216, Avx2.MultiplyLow(clamp_us_1216, Avx2.LoadAlignedVector256(ourWeights + 1216))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1232, Avx2.MultiplyLow(clamp_us_1232, Avx2.LoadAlignedVector256(ourWeights + 1232))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1248, Avx2.MultiplyLow(clamp_us_1248, Avx2.LoadAlignedVector256(ourWeights + 1248))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1264, Avx2.MultiplyLow(clamp_us_1264, Avx2.LoadAlignedVector256(ourWeights + 1264))));

            Vector256<short> clamp_us_1280 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1280)));
            Vector256<short> clamp_us_1296 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1296)));
            Vector256<short> clamp_us_1312 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1312)));
            Vector256<short> clamp_us_1328 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1328)));
            Vector256<short> clamp_us_1344 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1344)));
            Vector256<short> clamp_us_1360 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1360)));
            Vector256<short> clamp_us_1376 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1376)));
            Vector256<short> clamp_us_1392 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1392)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1280, Avx2.MultiplyLow(clamp_us_1280, Avx2.LoadAlignedVector256(ourWeights + 1280))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1296, Avx2.MultiplyLow(clamp_us_1296, Avx2.LoadAlignedVector256(ourWeights + 1296))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1312, Avx2.MultiplyLow(clamp_us_1312, Avx2.LoadAlignedVector256(ourWeights + 1312))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1328, Avx2.MultiplyLow(clamp_us_1328, Avx2.LoadAlignedVector256(ourWeights + 1328))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1344, Avx2.MultiplyLow(clamp_us_1344, Avx2.LoadAlignedVector256(ourWeights + 1344))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1360, Avx2.MultiplyLow(clamp_us_1360, Avx2.LoadAlignedVector256(ourWeights + 1360))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1376, Avx2.MultiplyLow(clamp_us_1376, Avx2.LoadAlignedVector256(ourWeights + 1376))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1392, Avx2.MultiplyLow(clamp_us_1392, Avx2.LoadAlignedVector256(ourWeights + 1392))));

            Vector256<short> clamp_us_1408 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1408)));
            Vector256<short> clamp_us_1424 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1424)));
            Vector256<short> clamp_us_1440 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1440)));
            Vector256<short> clamp_us_1456 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1456)));
            Vector256<short> clamp_us_1472 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1472)));
            Vector256<short> clamp_us_1488 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1488)));
            Vector256<short> clamp_us_1504 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1504)));
            Vector256<short> clamp_us_1520 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1520)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1408, Avx2.MultiplyLow(clamp_us_1408, Avx2.LoadAlignedVector256(ourWeights + 1408))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1424, Avx2.MultiplyLow(clamp_us_1424, Avx2.LoadAlignedVector256(ourWeights + 1424))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1440, Avx2.MultiplyLow(clamp_us_1440, Avx2.LoadAlignedVector256(ourWeights + 1440))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1456, Avx2.MultiplyLow(clamp_us_1456, Avx2.LoadAlignedVector256(ourWeights + 1456))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1472, Avx2.MultiplyLow(clamp_us_1472, Avx2.LoadAlignedVector256(ourWeights + 1472))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1488, Avx2.MultiplyLow(clamp_us_1488, Avx2.LoadAlignedVector256(ourWeights + 1488))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1504, Avx2.MultiplyLow(clamp_us_1504, Avx2.LoadAlignedVector256(ourWeights + 1504))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1520, Avx2.MultiplyLow(clamp_us_1520, Avx2.LoadAlignedVector256(ourWeights + 1520))));






            Vector256<short> clamp_them_0 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 0)));
            Vector256<short> clamp_them_16 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 16)));
            Vector256<short> clamp_them_32 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 32)));
            Vector256<short> clamp_them_48 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 48)));
            Vector256<short> clamp_them_64 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 64)));
            Vector256<short> clamp_them_80 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 80)));
            Vector256<short> clamp_them_96 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 96)));
            Vector256<short> clamp_them_112 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 112)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_0, Avx2.MultiplyLow(clamp_them_0, Avx2.LoadAlignedVector256(theirWeights + 0))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_16, Avx2.MultiplyLow(clamp_them_16, Avx2.LoadAlignedVector256(theirWeights + 16))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_32, Avx2.MultiplyLow(clamp_them_32, Avx2.LoadAlignedVector256(theirWeights + 32))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_48, Avx2.MultiplyLow(clamp_them_48, Avx2.LoadAlignedVector256(theirWeights + 48))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_64, Avx2.MultiplyLow(clamp_them_64, Avx2.LoadAlignedVector256(theirWeights + 64))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_80, Avx2.MultiplyLow(clamp_them_80, Avx2.LoadAlignedVector256(theirWeights + 80))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_96, Avx2.MultiplyLow(clamp_them_96, Avx2.LoadAlignedVector256(theirWeights + 96))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_112, Avx2.MultiplyLow(clamp_them_112, Avx2.LoadAlignedVector256(theirWeights + 112))));

            Vector256<short> clamp_them_128 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 128)));
            Vector256<short> clamp_them_144 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 144)));
            Vector256<short> clamp_them_160 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 160)));
            Vector256<short> clamp_them_176 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 176)));
            Vector256<short> clamp_them_192 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 192)));
            Vector256<short> clamp_them_208 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 208)));
            Vector256<short> clamp_them_224 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 224)));
            Vector256<short> clamp_them_240 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 240)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_128, Avx2.MultiplyLow(clamp_them_128, Avx2.LoadAlignedVector256(theirWeights + 128))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_144, Avx2.MultiplyLow(clamp_them_144, Avx2.LoadAlignedVector256(theirWeights + 144))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_160, Avx2.MultiplyLow(clamp_them_160, Avx2.LoadAlignedVector256(theirWeights + 160))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_176, Avx2.MultiplyLow(clamp_them_176, Avx2.LoadAlignedVector256(theirWeights + 176))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_192, Avx2.MultiplyLow(clamp_them_192, Avx2.LoadAlignedVector256(theirWeights + 192))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_208, Avx2.MultiplyLow(clamp_them_208, Avx2.LoadAlignedVector256(theirWeights + 208))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_224, Avx2.MultiplyLow(clamp_them_224, Avx2.LoadAlignedVector256(theirWeights + 224))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_240, Avx2.MultiplyLow(clamp_them_240, Avx2.LoadAlignedVector256(theirWeights + 240))));

            Vector256<short> clamp_them_256 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 256)));
            Vector256<short> clamp_them_272 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 272)));
            Vector256<short> clamp_them_288 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 288)));
            Vector256<short> clamp_them_304 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 304)));
            Vector256<short> clamp_them_320 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 320)));
            Vector256<short> clamp_them_336 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 336)));
            Vector256<short> clamp_them_352 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 352)));
            Vector256<short> clamp_them_368 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 368)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_256, Avx2.MultiplyLow(clamp_them_256, Avx2.LoadAlignedVector256(theirWeights + 256))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_272, Avx2.MultiplyLow(clamp_them_272, Avx2.LoadAlignedVector256(theirWeights + 272))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_288, Avx2.MultiplyLow(clamp_them_288, Avx2.LoadAlignedVector256(theirWeights + 288))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_304, Avx2.MultiplyLow(clamp_them_304, Avx2.LoadAlignedVector256(theirWeights + 304))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_320, Avx2.MultiplyLow(clamp_them_320, Avx2.LoadAlignedVector256(theirWeights + 320))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_336, Avx2.MultiplyLow(clamp_them_336, Avx2.LoadAlignedVector256(theirWeights + 336))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_352, Avx2.MultiplyLow(clamp_them_352, Avx2.LoadAlignedVector256(theirWeights + 352))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_368, Avx2.MultiplyLow(clamp_them_368, Avx2.LoadAlignedVector256(theirWeights + 368))));

            Vector256<short> clamp_them_384 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 384)));
            Vector256<short> clamp_them_400 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 400)));
            Vector256<short> clamp_them_416 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 416)));
            Vector256<short> clamp_them_432 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 432)));
            Vector256<short> clamp_them_448 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 448)));
            Vector256<short> clamp_them_464 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 464)));
            Vector256<short> clamp_them_480 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 480)));
            Vector256<short> clamp_them_496 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 496)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_384, Avx2.MultiplyLow(clamp_them_384, Avx2.LoadAlignedVector256(theirWeights + 384))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_400, Avx2.MultiplyLow(clamp_them_400, Avx2.LoadAlignedVector256(theirWeights + 400))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_416, Avx2.MultiplyLow(clamp_them_416, Avx2.LoadAlignedVector256(theirWeights + 416))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_432, Avx2.MultiplyLow(clamp_them_432, Avx2.LoadAlignedVector256(theirWeights + 432))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_448, Avx2.MultiplyLow(clamp_them_448, Avx2.LoadAlignedVector256(theirWeights + 448))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_464, Avx2.MultiplyLow(clamp_them_464, Avx2.LoadAlignedVector256(theirWeights + 464))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_480, Avx2.MultiplyLow(clamp_them_480, Avx2.LoadAlignedVector256(theirWeights + 480))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_496, Avx2.MultiplyLow(clamp_them_496, Avx2.LoadAlignedVector256(theirWeights + 496))));

            Vector256<short> clamp_them_512 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 512)));
            Vector256<short> clamp_them_528 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 528)));
            Vector256<short> clamp_them_544 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 544)));
            Vector256<short> clamp_them_560 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 560)));
            Vector256<short> clamp_them_576 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 576)));
            Vector256<short> clamp_them_592 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 592)));
            Vector256<short> clamp_them_608 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 608)));
            Vector256<short> clamp_them_624 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 624)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_512, Avx2.MultiplyLow(clamp_them_512, Avx2.LoadAlignedVector256(theirWeights + 512))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_528, Avx2.MultiplyLow(clamp_them_528, Avx2.LoadAlignedVector256(theirWeights + 528))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_544, Avx2.MultiplyLow(clamp_them_544, Avx2.LoadAlignedVector256(theirWeights + 544))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_560, Avx2.MultiplyLow(clamp_them_560, Avx2.LoadAlignedVector256(theirWeights + 560))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_576, Avx2.MultiplyLow(clamp_them_576, Avx2.LoadAlignedVector256(theirWeights + 576))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_592, Avx2.MultiplyLow(clamp_them_592, Avx2.LoadAlignedVector256(theirWeights + 592))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_608, Avx2.MultiplyLow(clamp_them_608, Avx2.LoadAlignedVector256(theirWeights + 608))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_624, Avx2.MultiplyLow(clamp_them_624, Avx2.LoadAlignedVector256(theirWeights + 624))));

            Vector256<short> clamp_them_640 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 640)));
            Vector256<short> clamp_them_656 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 656)));
            Vector256<short> clamp_them_672 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 672)));
            Vector256<short> clamp_them_688 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 688)));
            Vector256<short> clamp_them_704 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 704)));
            Vector256<short> clamp_them_720 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 720)));
            Vector256<short> clamp_them_736 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 736)));
            Vector256<short> clamp_them_752 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 752)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_640, Avx2.MultiplyLow(clamp_them_640, Avx2.LoadAlignedVector256(theirWeights + 640))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_656, Avx2.MultiplyLow(clamp_them_656, Avx2.LoadAlignedVector256(theirWeights + 656))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_672, Avx2.MultiplyLow(clamp_them_672, Avx2.LoadAlignedVector256(theirWeights + 672))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_688, Avx2.MultiplyLow(clamp_them_688, Avx2.LoadAlignedVector256(theirWeights + 688))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_704, Avx2.MultiplyLow(clamp_them_704, Avx2.LoadAlignedVector256(theirWeights + 704))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_720, Avx2.MultiplyLow(clamp_them_720, Avx2.LoadAlignedVector256(theirWeights + 720))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_736, Avx2.MultiplyLow(clamp_them_736, Avx2.LoadAlignedVector256(theirWeights + 736))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_752, Avx2.MultiplyLow(clamp_them_752, Avx2.LoadAlignedVector256(theirWeights + 752))));

            Vector256<short> clamp_them_768 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 768)));
            Vector256<short> clamp_them_784 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 784)));
            Vector256<short> clamp_them_800 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 800)));
            Vector256<short> clamp_them_816 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 816)));
            Vector256<short> clamp_them_832 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 832)));
            Vector256<short> clamp_them_848 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 848)));
            Vector256<short> clamp_them_864 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 864)));
            Vector256<short> clamp_them_880 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 880)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_768, Avx2.MultiplyLow(clamp_them_768, Avx2.LoadAlignedVector256(theirWeights + 768))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_784, Avx2.MultiplyLow(clamp_them_784, Avx2.LoadAlignedVector256(theirWeights + 784))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_800, Avx2.MultiplyLow(clamp_them_800, Avx2.LoadAlignedVector256(theirWeights + 800))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_816, Avx2.MultiplyLow(clamp_them_816, Avx2.LoadAlignedVector256(theirWeights + 816))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_832, Avx2.MultiplyLow(clamp_them_832, Avx2.LoadAlignedVector256(theirWeights + 832))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_848, Avx2.MultiplyLow(clamp_them_848, Avx2.LoadAlignedVector256(theirWeights + 848))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_864, Avx2.MultiplyLow(clamp_them_864, Avx2.LoadAlignedVector256(theirWeights + 864))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_880, Avx2.MultiplyLow(clamp_them_880, Avx2.LoadAlignedVector256(theirWeights + 880))));

            Vector256<short> clamp_them_896 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 896)));
            Vector256<short> clamp_them_912 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 912)));
            Vector256<short> clamp_them_928 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 928)));
            Vector256<short> clamp_them_944 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 944)));
            Vector256<short> clamp_them_960 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 960)));
            Vector256<short> clamp_them_976 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 976)));
            Vector256<short> clamp_them_992 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 992)));
            Vector256<short> clamp_them_1008 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1008)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_896, Avx2.MultiplyLow(clamp_them_896, Avx2.LoadAlignedVector256(theirWeights + 896))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_912, Avx2.MultiplyLow(clamp_them_912, Avx2.LoadAlignedVector256(theirWeights + 912))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_928, Avx2.MultiplyLow(clamp_them_928, Avx2.LoadAlignedVector256(theirWeights + 928))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_944, Avx2.MultiplyLow(clamp_them_944, Avx2.LoadAlignedVector256(theirWeights + 944))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_960, Avx2.MultiplyLow(clamp_them_960, Avx2.LoadAlignedVector256(theirWeights + 960))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_976, Avx2.MultiplyLow(clamp_them_976, Avx2.LoadAlignedVector256(theirWeights + 976))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_992, Avx2.MultiplyLow(clamp_them_992, Avx2.LoadAlignedVector256(theirWeights + 992))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1008, Avx2.MultiplyLow(clamp_them_1008, Avx2.LoadAlignedVector256(theirWeights + 1008))));

            Vector256<short> clamp_them_1024 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1024)));
            Vector256<short> clamp_them_1040 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1040)));
            Vector256<short> clamp_them_1056 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1056)));
            Vector256<short> clamp_them_1072 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1072)));
            Vector256<short> clamp_them_1088 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1088)));
            Vector256<short> clamp_them_1104 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1104)));
            Vector256<short> clamp_them_1120 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1120)));
            Vector256<short> clamp_them_1136 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1136)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1024, Avx2.MultiplyLow(clamp_them_1024, Avx2.LoadAlignedVector256(theirWeights + 1024))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1040, Avx2.MultiplyLow(clamp_them_1040, Avx2.LoadAlignedVector256(theirWeights + 1040))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1056, Avx2.MultiplyLow(clamp_them_1056, Avx2.LoadAlignedVector256(theirWeights + 1056))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1072, Avx2.MultiplyLow(clamp_them_1072, Avx2.LoadAlignedVector256(theirWeights + 1072))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1088, Avx2.MultiplyLow(clamp_them_1088, Avx2.LoadAlignedVector256(theirWeights + 1088))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1104, Avx2.MultiplyLow(clamp_them_1104, Avx2.LoadAlignedVector256(theirWeights + 1104))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1120, Avx2.MultiplyLow(clamp_them_1120, Avx2.LoadAlignedVector256(theirWeights + 1120))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1136, Avx2.MultiplyLow(clamp_them_1136, Avx2.LoadAlignedVector256(theirWeights + 1136))));

            Vector256<short> clamp_them_1152 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1152)));
            Vector256<short> clamp_them_1168 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1168)));
            Vector256<short> clamp_them_1184 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1184)));
            Vector256<short> clamp_them_1200 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1200)));
            Vector256<short> clamp_them_1216 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1216)));
            Vector256<short> clamp_them_1232 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1232)));
            Vector256<short> clamp_them_1248 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1248)));
            Vector256<short> clamp_them_1264 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1264)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1152, Avx2.MultiplyLow(clamp_them_1152, Avx2.LoadAlignedVector256(theirWeights + 1152))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1168, Avx2.MultiplyLow(clamp_them_1168, Avx2.LoadAlignedVector256(theirWeights + 1168))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1184, Avx2.MultiplyLow(clamp_them_1184, Avx2.LoadAlignedVector256(theirWeights + 1184))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1200, Avx2.MultiplyLow(clamp_them_1200, Avx2.LoadAlignedVector256(theirWeights + 1200))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1216, Avx2.MultiplyLow(clamp_them_1216, Avx2.LoadAlignedVector256(theirWeights + 1216))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1232, Avx2.MultiplyLow(clamp_them_1232, Avx2.LoadAlignedVector256(theirWeights + 1232))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1248, Avx2.MultiplyLow(clamp_them_1248, Avx2.LoadAlignedVector256(theirWeights + 1248))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1264, Avx2.MultiplyLow(clamp_them_1264, Avx2.LoadAlignedVector256(theirWeights + 1264))));

            Vector256<short> clamp_them_1280 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1280)));
            Vector256<short> clamp_them_1296 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1296)));
            Vector256<short> clamp_them_1312 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1312)));
            Vector256<short> clamp_them_1328 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1328)));
            Vector256<short> clamp_them_1344 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1344)));
            Vector256<short> clamp_them_1360 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1360)));
            Vector256<short> clamp_them_1376 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1376)));
            Vector256<short> clamp_them_1392 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1392)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1280, Avx2.MultiplyLow(clamp_them_1280, Avx2.LoadAlignedVector256(theirWeights + 1280))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1296, Avx2.MultiplyLow(clamp_them_1296, Avx2.LoadAlignedVector256(theirWeights + 1296))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1312, Avx2.MultiplyLow(clamp_them_1312, Avx2.LoadAlignedVector256(theirWeights + 1312))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1328, Avx2.MultiplyLow(clamp_them_1328, Avx2.LoadAlignedVector256(theirWeights + 1328))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1344, Avx2.MultiplyLow(clamp_them_1344, Avx2.LoadAlignedVector256(theirWeights + 1344))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1360, Avx2.MultiplyLow(clamp_them_1360, Avx2.LoadAlignedVector256(theirWeights + 1360))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1376, Avx2.MultiplyLow(clamp_them_1376, Avx2.LoadAlignedVector256(theirWeights + 1376))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1392, Avx2.MultiplyLow(clamp_them_1392, Avx2.LoadAlignedVector256(theirWeights + 1392))));

            Vector256<short> clamp_them_1408 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1408)));
            Vector256<short> clamp_them_1424 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1424)));
            Vector256<short> clamp_them_1440 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1440)));
            Vector256<short> clamp_them_1456 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1456)));
            Vector256<short> clamp_them_1472 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1472)));
            Vector256<short> clamp_them_1488 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1488)));
            Vector256<short> clamp_them_1504 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1504)));
            Vector256<short> clamp_them_1520 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1520)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1408, Avx2.MultiplyLow(clamp_them_1408, Avx2.LoadAlignedVector256(theirWeights + 1408))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1424, Avx2.MultiplyLow(clamp_them_1424, Avx2.LoadAlignedVector256(theirWeights + 1424))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1440, Avx2.MultiplyLow(clamp_them_1440, Avx2.LoadAlignedVector256(theirWeights + 1440))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1456, Avx2.MultiplyLow(clamp_them_1456, Avx2.LoadAlignedVector256(theirWeights + 1456))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1472, Avx2.MultiplyLow(clamp_them_1472, Avx2.LoadAlignedVector256(theirWeights + 1472))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1488, Avx2.MultiplyLow(clamp_them_1488, Avx2.LoadAlignedVector256(theirWeights + 1488))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1504, Avx2.MultiplyLow(clamp_them_1504, Avx2.LoadAlignedVector256(theirWeights + 1504))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1520, Avx2.MultiplyLow(clamp_them_1520, Avx2.LoadAlignedVector256(theirWeights + 1520))));

            int output = SumVector256NoHadd(normalSum);

            return (output / QA + LayerBiases[0][outputBucket]) * OutputScale / QAB;
        }

    }
}
