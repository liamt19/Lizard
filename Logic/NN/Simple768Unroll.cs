using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Lizard.Logic.NN
{
    public static unsafe partial class Simple768
    {
        private static void SubAdd(short* src, short* sub1, short* add1)
        {
            Avx2.Store(src + 0, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 0), Avx2.LoadVector256(add1 + 0)), Avx2.LoadVector256(sub1 + 0)));
            Avx2.Store(src + 16, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 16), Avx2.LoadVector256(add1 + 16)), Avx2.LoadVector256(sub1 + 16)));
            Avx2.Store(src + 32, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 32), Avx2.LoadVector256(add1 + 32)), Avx2.LoadVector256(sub1 + 32)));
            Avx2.Store(src + 48, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 48), Avx2.LoadVector256(add1 + 48)), Avx2.LoadVector256(sub1 + 48)));
            Avx2.Store(src + 64, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 64), Avx2.LoadVector256(add1 + 64)), Avx2.LoadVector256(sub1 + 64)));
            Avx2.Store(src + 80, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 80), Avx2.LoadVector256(add1 + 80)), Avx2.LoadVector256(sub1 + 80)));
            Avx2.Store(src + 96, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 96), Avx2.LoadVector256(add1 + 96)), Avx2.LoadVector256(sub1 + 96)));
            Avx2.Store(src + 112, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 112), Avx2.LoadVector256(add1 + 112)), Avx2.LoadVector256(sub1 + 112)));
            Avx2.Store(src + 128, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 128), Avx2.LoadVector256(add1 + 128)), Avx2.LoadVector256(sub1 + 128)));
            Avx2.Store(src + 144, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 144), Avx2.LoadVector256(add1 + 144)), Avx2.LoadVector256(sub1 + 144)));
            Avx2.Store(src + 160, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 160), Avx2.LoadVector256(add1 + 160)), Avx2.LoadVector256(sub1 + 160)));
            Avx2.Store(src + 176, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 176), Avx2.LoadVector256(add1 + 176)), Avx2.LoadVector256(sub1 + 176)));
            Avx2.Store(src + 192, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 192), Avx2.LoadVector256(add1 + 192)), Avx2.LoadVector256(sub1 + 192)));
            Avx2.Store(src + 208, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 208), Avx2.LoadVector256(add1 + 208)), Avx2.LoadVector256(sub1 + 208)));
            Avx2.Store(src + 224, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 224), Avx2.LoadVector256(add1 + 224)), Avx2.LoadVector256(sub1 + 224)));
            Avx2.Store(src + 240, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 240), Avx2.LoadVector256(add1 + 240)), Avx2.LoadVector256(sub1 + 240)));
            Avx2.Store(src + 256, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 256), Avx2.LoadVector256(add1 + 256)), Avx2.LoadVector256(sub1 + 256)));
            Avx2.Store(src + 272, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 272), Avx2.LoadVector256(add1 + 272)), Avx2.LoadVector256(sub1 + 272)));
            Avx2.Store(src + 288, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 288), Avx2.LoadVector256(add1 + 288)), Avx2.LoadVector256(sub1 + 288)));
            Avx2.Store(src + 304, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 304), Avx2.LoadVector256(add1 + 304)), Avx2.LoadVector256(sub1 + 304)));
            Avx2.Store(src + 320, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 320), Avx2.LoadVector256(add1 + 320)), Avx2.LoadVector256(sub1 + 320)));
            Avx2.Store(src + 336, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 336), Avx2.LoadVector256(add1 + 336)), Avx2.LoadVector256(sub1 + 336)));
            Avx2.Store(src + 352, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 352), Avx2.LoadVector256(add1 + 352)), Avx2.LoadVector256(sub1 + 352)));
            Avx2.Store(src + 368, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 368), Avx2.LoadVector256(add1 + 368)), Avx2.LoadVector256(sub1 + 368)));
            Avx2.Store(src + 384, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 384), Avx2.LoadVector256(add1 + 384)), Avx2.LoadVector256(sub1 + 384)));
            Avx2.Store(src + 400, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 400), Avx2.LoadVector256(add1 + 400)), Avx2.LoadVector256(sub1 + 400)));
            Avx2.Store(src + 416, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 416), Avx2.LoadVector256(add1 + 416)), Avx2.LoadVector256(sub1 + 416)));
            Avx2.Store(src + 432, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 432), Avx2.LoadVector256(add1 + 432)), Avx2.LoadVector256(sub1 + 432)));
            Avx2.Store(src + 448, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 448), Avx2.LoadVector256(add1 + 448)), Avx2.LoadVector256(sub1 + 448)));
            Avx2.Store(src + 464, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 464), Avx2.LoadVector256(add1 + 464)), Avx2.LoadVector256(sub1 + 464)));
            Avx2.Store(src + 480, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 480), Avx2.LoadVector256(add1 + 480)), Avx2.LoadVector256(sub1 + 480)));
            Avx2.Store(src + 496, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 496), Avx2.LoadVector256(add1 + 496)), Avx2.LoadVector256(sub1 + 496)));
            Avx2.Store(src + 512, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 512), Avx2.LoadVector256(add1 + 512)), Avx2.LoadVector256(sub1 + 512)));
            Avx2.Store(src + 528, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 528), Avx2.LoadVector256(add1 + 528)), Avx2.LoadVector256(sub1 + 528)));
            Avx2.Store(src + 544, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 544), Avx2.LoadVector256(add1 + 544)), Avx2.LoadVector256(sub1 + 544)));
            Avx2.Store(src + 560, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 560), Avx2.LoadVector256(add1 + 560)), Avx2.LoadVector256(sub1 + 560)));
            Avx2.Store(src + 576, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 576), Avx2.LoadVector256(add1 + 576)), Avx2.LoadVector256(sub1 + 576)));
            Avx2.Store(src + 592, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 592), Avx2.LoadVector256(add1 + 592)), Avx2.LoadVector256(sub1 + 592)));
            Avx2.Store(src + 608, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 608), Avx2.LoadVector256(add1 + 608)), Avx2.LoadVector256(sub1 + 608)));
            Avx2.Store(src + 624, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 624), Avx2.LoadVector256(add1 + 624)), Avx2.LoadVector256(sub1 + 624)));
            Avx2.Store(src + 640, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 640), Avx2.LoadVector256(add1 + 640)), Avx2.LoadVector256(sub1 + 640)));
            Avx2.Store(src + 656, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 656), Avx2.LoadVector256(add1 + 656)), Avx2.LoadVector256(sub1 + 656)));
            Avx2.Store(src + 672, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 672), Avx2.LoadVector256(add1 + 672)), Avx2.LoadVector256(sub1 + 672)));
            Avx2.Store(src + 688, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 688), Avx2.LoadVector256(add1 + 688)), Avx2.LoadVector256(sub1 + 688)));
            Avx2.Store(src + 704, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 704), Avx2.LoadVector256(add1 + 704)), Avx2.LoadVector256(sub1 + 704)));
            Avx2.Store(src + 720, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 720), Avx2.LoadVector256(add1 + 720)), Avx2.LoadVector256(sub1 + 720)));
            Avx2.Store(src + 736, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 736), Avx2.LoadVector256(add1 + 736)), Avx2.LoadVector256(sub1 + 736)));
            Avx2.Store(src + 752, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 752), Avx2.LoadVector256(add1 + 752)), Avx2.LoadVector256(sub1 + 752)));
            Avx2.Store(src + 768, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 768), Avx2.LoadVector256(add1 + 768)), Avx2.LoadVector256(sub1 + 768)));
            Avx2.Store(src + 784, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 784), Avx2.LoadVector256(add1 + 784)), Avx2.LoadVector256(sub1 + 784)));
            Avx2.Store(src + 800, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 800), Avx2.LoadVector256(add1 + 800)), Avx2.LoadVector256(sub1 + 800)));
            Avx2.Store(src + 816, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 816), Avx2.LoadVector256(add1 + 816)), Avx2.LoadVector256(sub1 + 816)));
            Avx2.Store(src + 832, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 832), Avx2.LoadVector256(add1 + 832)), Avx2.LoadVector256(sub1 + 832)));
            Avx2.Store(src + 848, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 848), Avx2.LoadVector256(add1 + 848)), Avx2.LoadVector256(sub1 + 848)));
            Avx2.Store(src + 864, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 864), Avx2.LoadVector256(add1 + 864)), Avx2.LoadVector256(sub1 + 864)));
            Avx2.Store(src + 880, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 880), Avx2.LoadVector256(add1 + 880)), Avx2.LoadVector256(sub1 + 880)));
            Avx2.Store(src + 896, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 896), Avx2.LoadVector256(add1 + 896)), Avx2.LoadVector256(sub1 + 896)));
            Avx2.Store(src + 912, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 912), Avx2.LoadVector256(add1 + 912)), Avx2.LoadVector256(sub1 + 912)));
            Avx2.Store(src + 928, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 928), Avx2.LoadVector256(add1 + 928)), Avx2.LoadVector256(sub1 + 928)));
            Avx2.Store(src + 944, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 944), Avx2.LoadVector256(add1 + 944)), Avx2.LoadVector256(sub1 + 944)));
            Avx2.Store(src + 960, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 960), Avx2.LoadVector256(add1 + 960)), Avx2.LoadVector256(sub1 + 960)));
            Avx2.Store(src + 976, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 976), Avx2.LoadVector256(add1 + 976)), Avx2.LoadVector256(sub1 + 976)));
            Avx2.Store(src + 992, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 992), Avx2.LoadVector256(add1 + 992)), Avx2.LoadVector256(sub1 + 992)));
            Avx2.Store(src + 1008, Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 1008), Avx2.LoadVector256(add1 + 1008)), Avx2.LoadVector256(sub1 + 1008)));
        }

        private static void SubSubAdd(short* src, short* sub1, short* sub2, short* add1)
        {
            Avx2.Store(src + 0, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 0), Avx2.LoadVector256(add1 + 0)), Avx2.LoadVector256(sub1 + 0)), Avx2.LoadVector256(sub2 + 0)));
            Avx2.Store(src + 16, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 16), Avx2.LoadVector256(add1 + 16)), Avx2.LoadVector256(sub1 + 16)), Avx2.LoadVector256(sub2 + 16)));
            Avx2.Store(src + 32, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 32), Avx2.LoadVector256(add1 + 32)), Avx2.LoadVector256(sub1 + 32)), Avx2.LoadVector256(sub2 + 32)));
            Avx2.Store(src + 48, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 48), Avx2.LoadVector256(add1 + 48)), Avx2.LoadVector256(sub1 + 48)), Avx2.LoadVector256(sub2 + 48)));
            Avx2.Store(src + 64, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 64), Avx2.LoadVector256(add1 + 64)), Avx2.LoadVector256(sub1 + 64)), Avx2.LoadVector256(sub2 + 64)));
            Avx2.Store(src + 80, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 80), Avx2.LoadVector256(add1 + 80)), Avx2.LoadVector256(sub1 + 80)), Avx2.LoadVector256(sub2 + 80)));
            Avx2.Store(src + 96, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 96), Avx2.LoadVector256(add1 + 96)), Avx2.LoadVector256(sub1 + 96)), Avx2.LoadVector256(sub2 + 96)));
            Avx2.Store(src + 112, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 112), Avx2.LoadVector256(add1 + 112)), Avx2.LoadVector256(sub1 + 112)), Avx2.LoadVector256(sub2 + 112)));
            Avx2.Store(src + 128, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 128), Avx2.LoadVector256(add1 + 128)), Avx2.LoadVector256(sub1 + 128)), Avx2.LoadVector256(sub2 + 128)));
            Avx2.Store(src + 144, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 144), Avx2.LoadVector256(add1 + 144)), Avx2.LoadVector256(sub1 + 144)), Avx2.LoadVector256(sub2 + 144)));
            Avx2.Store(src + 160, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 160), Avx2.LoadVector256(add1 + 160)), Avx2.LoadVector256(sub1 + 160)), Avx2.LoadVector256(sub2 + 160)));
            Avx2.Store(src + 176, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 176), Avx2.LoadVector256(add1 + 176)), Avx2.LoadVector256(sub1 + 176)), Avx2.LoadVector256(sub2 + 176)));
            Avx2.Store(src + 192, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 192), Avx2.LoadVector256(add1 + 192)), Avx2.LoadVector256(sub1 + 192)), Avx2.LoadVector256(sub2 + 192)));
            Avx2.Store(src + 208, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 208), Avx2.LoadVector256(add1 + 208)), Avx2.LoadVector256(sub1 + 208)), Avx2.LoadVector256(sub2 + 208)));
            Avx2.Store(src + 224, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 224), Avx2.LoadVector256(add1 + 224)), Avx2.LoadVector256(sub1 + 224)), Avx2.LoadVector256(sub2 + 224)));
            Avx2.Store(src + 240, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 240), Avx2.LoadVector256(add1 + 240)), Avx2.LoadVector256(sub1 + 240)), Avx2.LoadVector256(sub2 + 240)));
            Avx2.Store(src + 256, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 256), Avx2.LoadVector256(add1 + 256)), Avx2.LoadVector256(sub1 + 256)), Avx2.LoadVector256(sub2 + 256)));
            Avx2.Store(src + 272, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 272), Avx2.LoadVector256(add1 + 272)), Avx2.LoadVector256(sub1 + 272)), Avx2.LoadVector256(sub2 + 272)));
            Avx2.Store(src + 288, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 288), Avx2.LoadVector256(add1 + 288)), Avx2.LoadVector256(sub1 + 288)), Avx2.LoadVector256(sub2 + 288)));
            Avx2.Store(src + 304, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 304), Avx2.LoadVector256(add1 + 304)), Avx2.LoadVector256(sub1 + 304)), Avx2.LoadVector256(sub2 + 304)));
            Avx2.Store(src + 320, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 320), Avx2.LoadVector256(add1 + 320)), Avx2.LoadVector256(sub1 + 320)), Avx2.LoadVector256(sub2 + 320)));
            Avx2.Store(src + 336, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 336), Avx2.LoadVector256(add1 + 336)), Avx2.LoadVector256(sub1 + 336)), Avx2.LoadVector256(sub2 + 336)));
            Avx2.Store(src + 352, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 352), Avx2.LoadVector256(add1 + 352)), Avx2.LoadVector256(sub1 + 352)), Avx2.LoadVector256(sub2 + 352)));
            Avx2.Store(src + 368, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 368), Avx2.LoadVector256(add1 + 368)), Avx2.LoadVector256(sub1 + 368)), Avx2.LoadVector256(sub2 + 368)));
            Avx2.Store(src + 384, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 384), Avx2.LoadVector256(add1 + 384)), Avx2.LoadVector256(sub1 + 384)), Avx2.LoadVector256(sub2 + 384)));
            Avx2.Store(src + 400, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 400), Avx2.LoadVector256(add1 + 400)), Avx2.LoadVector256(sub1 + 400)), Avx2.LoadVector256(sub2 + 400)));
            Avx2.Store(src + 416, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 416), Avx2.LoadVector256(add1 + 416)), Avx2.LoadVector256(sub1 + 416)), Avx2.LoadVector256(sub2 + 416)));
            Avx2.Store(src + 432, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 432), Avx2.LoadVector256(add1 + 432)), Avx2.LoadVector256(sub1 + 432)), Avx2.LoadVector256(sub2 + 432)));
            Avx2.Store(src + 448, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 448), Avx2.LoadVector256(add1 + 448)), Avx2.LoadVector256(sub1 + 448)), Avx2.LoadVector256(sub2 + 448)));
            Avx2.Store(src + 464, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 464), Avx2.LoadVector256(add1 + 464)), Avx2.LoadVector256(sub1 + 464)), Avx2.LoadVector256(sub2 + 464)));
            Avx2.Store(src + 480, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 480), Avx2.LoadVector256(add1 + 480)), Avx2.LoadVector256(sub1 + 480)), Avx2.LoadVector256(sub2 + 480)));
            Avx2.Store(src + 496, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 496), Avx2.LoadVector256(add1 + 496)), Avx2.LoadVector256(sub1 + 496)), Avx2.LoadVector256(sub2 + 496)));
            Avx2.Store(src + 512, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 512), Avx2.LoadVector256(add1 + 512)), Avx2.LoadVector256(sub1 + 512)), Avx2.LoadVector256(sub2 + 512)));
            Avx2.Store(src + 528, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 528), Avx2.LoadVector256(add1 + 528)), Avx2.LoadVector256(sub1 + 528)), Avx2.LoadVector256(sub2 + 528)));
            Avx2.Store(src + 544, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 544), Avx2.LoadVector256(add1 + 544)), Avx2.LoadVector256(sub1 + 544)), Avx2.LoadVector256(sub2 + 544)));
            Avx2.Store(src + 560, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 560), Avx2.LoadVector256(add1 + 560)), Avx2.LoadVector256(sub1 + 560)), Avx2.LoadVector256(sub2 + 560)));
            Avx2.Store(src + 576, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 576), Avx2.LoadVector256(add1 + 576)), Avx2.LoadVector256(sub1 + 576)), Avx2.LoadVector256(sub2 + 576)));
            Avx2.Store(src + 592, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 592), Avx2.LoadVector256(add1 + 592)), Avx2.LoadVector256(sub1 + 592)), Avx2.LoadVector256(sub2 + 592)));
            Avx2.Store(src + 608, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 608), Avx2.LoadVector256(add1 + 608)), Avx2.LoadVector256(sub1 + 608)), Avx2.LoadVector256(sub2 + 608)));
            Avx2.Store(src + 624, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 624), Avx2.LoadVector256(add1 + 624)), Avx2.LoadVector256(sub1 + 624)), Avx2.LoadVector256(sub2 + 624)));
            Avx2.Store(src + 640, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 640), Avx2.LoadVector256(add1 + 640)), Avx2.LoadVector256(sub1 + 640)), Avx2.LoadVector256(sub2 + 640)));
            Avx2.Store(src + 656, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 656), Avx2.LoadVector256(add1 + 656)), Avx2.LoadVector256(sub1 + 656)), Avx2.LoadVector256(sub2 + 656)));
            Avx2.Store(src + 672, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 672), Avx2.LoadVector256(add1 + 672)), Avx2.LoadVector256(sub1 + 672)), Avx2.LoadVector256(sub2 + 672)));
            Avx2.Store(src + 688, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 688), Avx2.LoadVector256(add1 + 688)), Avx2.LoadVector256(sub1 + 688)), Avx2.LoadVector256(sub2 + 688)));
            Avx2.Store(src + 704, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 704), Avx2.LoadVector256(add1 + 704)), Avx2.LoadVector256(sub1 + 704)), Avx2.LoadVector256(sub2 + 704)));
            Avx2.Store(src + 720, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 720), Avx2.LoadVector256(add1 + 720)), Avx2.LoadVector256(sub1 + 720)), Avx2.LoadVector256(sub2 + 720)));
            Avx2.Store(src + 736, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 736), Avx2.LoadVector256(add1 + 736)), Avx2.LoadVector256(sub1 + 736)), Avx2.LoadVector256(sub2 + 736)));
            Avx2.Store(src + 752, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 752), Avx2.LoadVector256(add1 + 752)), Avx2.LoadVector256(sub1 + 752)), Avx2.LoadVector256(sub2 + 752)));
            Avx2.Store(src + 768, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 768), Avx2.LoadVector256(add1 + 768)), Avx2.LoadVector256(sub1 + 768)), Avx2.LoadVector256(sub2 + 768)));
            Avx2.Store(src + 784, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 784), Avx2.LoadVector256(add1 + 784)), Avx2.LoadVector256(sub1 + 784)), Avx2.LoadVector256(sub2 + 784)));
            Avx2.Store(src + 800, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 800), Avx2.LoadVector256(add1 + 800)), Avx2.LoadVector256(sub1 + 800)), Avx2.LoadVector256(sub2 + 800)));
            Avx2.Store(src + 816, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 816), Avx2.LoadVector256(add1 + 816)), Avx2.LoadVector256(sub1 + 816)), Avx2.LoadVector256(sub2 + 816)));
            Avx2.Store(src + 832, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 832), Avx2.LoadVector256(add1 + 832)), Avx2.LoadVector256(sub1 + 832)), Avx2.LoadVector256(sub2 + 832)));
            Avx2.Store(src + 848, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 848), Avx2.LoadVector256(add1 + 848)), Avx2.LoadVector256(sub1 + 848)), Avx2.LoadVector256(sub2 + 848)));
            Avx2.Store(src + 864, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 864), Avx2.LoadVector256(add1 + 864)), Avx2.LoadVector256(sub1 + 864)), Avx2.LoadVector256(sub2 + 864)));
            Avx2.Store(src + 880, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 880), Avx2.LoadVector256(add1 + 880)), Avx2.LoadVector256(sub1 + 880)), Avx2.LoadVector256(sub2 + 880)));
            Avx2.Store(src + 896, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 896), Avx2.LoadVector256(add1 + 896)), Avx2.LoadVector256(sub1 + 896)), Avx2.LoadVector256(sub2 + 896)));
            Avx2.Store(src + 912, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 912), Avx2.LoadVector256(add1 + 912)), Avx2.LoadVector256(sub1 + 912)), Avx2.LoadVector256(sub2 + 912)));
            Avx2.Store(src + 928, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 928), Avx2.LoadVector256(add1 + 928)), Avx2.LoadVector256(sub1 + 928)), Avx2.LoadVector256(sub2 + 928)));
            Avx2.Store(src + 944, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 944), Avx2.LoadVector256(add1 + 944)), Avx2.LoadVector256(sub1 + 944)), Avx2.LoadVector256(sub2 + 944)));
            Avx2.Store(src + 960, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 960), Avx2.LoadVector256(add1 + 960)), Avx2.LoadVector256(sub1 + 960)), Avx2.LoadVector256(sub2 + 960)));
            Avx2.Store(src + 976, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 976), Avx2.LoadVector256(add1 + 976)), Avx2.LoadVector256(sub1 + 976)), Avx2.LoadVector256(sub2 + 976)));
            Avx2.Store(src + 992, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 992), Avx2.LoadVector256(add1 + 992)), Avx2.LoadVector256(sub1 + 992)), Avx2.LoadVector256(sub2 + 992)));
            Avx2.Store(src + 1008, Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.LoadVector256(src + 1008), Avx2.LoadVector256(add1 + 1008)), Avx2.LoadVector256(sub1 + 1008)), Avx2.LoadVector256(sub2 + 1008)));
        }

        public static int GetEvaluationUnrolled(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            Vector256<short> ClampMax = Vector256.Create((short)QA);
            Vector256<int> normalSum = Vector256<int>.Zero;

            var ourData = (short*)(accumulator[pos.ToMove]);
            var ourWeights = (short*)(LayerWeights);
            var theirData = (short*)(accumulator[Not(pos.ToMove)]);
            var theirWeights = (short*)(LayerWeights + SIMD_CHUNKS);


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


            int output = SumVector256NoHadd(normalSum);

            return (output / QA + LayerBiases[0][0]) * OutputScale / QAB;
        }

    }
}
