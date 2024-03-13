using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Lizard.Logic.NN
{
    public static unsafe class FunUnrollThings
    {
        public static void SubAdd(short* src, short* sub1, short* add1)
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

        public static void SubSubAdd(short* src, short* sub1, short* sub2, short* add1)
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

    }
}
