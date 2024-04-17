using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Lizard.Logic.NN
{
    public static unsafe class FunUnrollThings
    {

        public static void SubAdd(short* src, short* dst, short* sub1, short* add1)
        {
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 0), Vector256.Load(add1 + 0)), Vector256.Load(sub1 + 0)), dst + 0);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 16), Vector256.Load(add1 + 16)), Vector256.Load(sub1 + 16)), dst + 16);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 32), Vector256.Load(add1 + 32)), Vector256.Load(sub1 + 32)), dst + 32);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 48), Vector256.Load(add1 + 48)), Vector256.Load(sub1 + 48)), dst + 48);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 64), Vector256.Load(add1 + 64)), Vector256.Load(sub1 + 64)), dst + 64);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 80), Vector256.Load(add1 + 80)), Vector256.Load(sub1 + 80)), dst + 80);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 96), Vector256.Load(add1 + 96)), Vector256.Load(sub1 + 96)), dst + 96);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 112), Vector256.Load(add1 + 112)), Vector256.Load(sub1 + 112)), dst + 112);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 128), Vector256.Load(add1 + 128)), Vector256.Load(sub1 + 128)), dst + 128);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 144), Vector256.Load(add1 + 144)), Vector256.Load(sub1 + 144)), dst + 144);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 160), Vector256.Load(add1 + 160)), Vector256.Load(sub1 + 160)), dst + 160);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 176), Vector256.Load(add1 + 176)), Vector256.Load(sub1 + 176)), dst + 176);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 192), Vector256.Load(add1 + 192)), Vector256.Load(sub1 + 192)), dst + 192);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 208), Vector256.Load(add1 + 208)), Vector256.Load(sub1 + 208)), dst + 208);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 224), Vector256.Load(add1 + 224)), Vector256.Load(sub1 + 224)), dst + 224);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 240), Vector256.Load(add1 + 240)), Vector256.Load(sub1 + 240)), dst + 240);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 256), Vector256.Load(add1 + 256)), Vector256.Load(sub1 + 256)), dst + 256);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 272), Vector256.Load(add1 + 272)), Vector256.Load(sub1 + 272)), dst + 272);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 288), Vector256.Load(add1 + 288)), Vector256.Load(sub1 + 288)), dst + 288);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 304), Vector256.Load(add1 + 304)), Vector256.Load(sub1 + 304)), dst + 304);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 320), Vector256.Load(add1 + 320)), Vector256.Load(sub1 + 320)), dst + 320);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 336), Vector256.Load(add1 + 336)), Vector256.Load(sub1 + 336)), dst + 336);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 352), Vector256.Load(add1 + 352)), Vector256.Load(sub1 + 352)), dst + 352);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 368), Vector256.Load(add1 + 368)), Vector256.Load(sub1 + 368)), dst + 368);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 384), Vector256.Load(add1 + 384)), Vector256.Load(sub1 + 384)), dst + 384);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 400), Vector256.Load(add1 + 400)), Vector256.Load(sub1 + 400)), dst + 400);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 416), Vector256.Load(add1 + 416)), Vector256.Load(sub1 + 416)), dst + 416);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 432), Vector256.Load(add1 + 432)), Vector256.Load(sub1 + 432)), dst + 432);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 448), Vector256.Load(add1 + 448)), Vector256.Load(sub1 + 448)), dst + 448);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 464), Vector256.Load(add1 + 464)), Vector256.Load(sub1 + 464)), dst + 464);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 480), Vector256.Load(add1 + 480)), Vector256.Load(sub1 + 480)), dst + 480);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 496), Vector256.Load(add1 + 496)), Vector256.Load(sub1 + 496)), dst + 496);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 512), Vector256.Load(add1 + 512)), Vector256.Load(sub1 + 512)), dst + 512);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 528), Vector256.Load(add1 + 528)), Vector256.Load(sub1 + 528)), dst + 528);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 544), Vector256.Load(add1 + 544)), Vector256.Load(sub1 + 544)), dst + 544);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 560), Vector256.Load(add1 + 560)), Vector256.Load(sub1 + 560)), dst + 560);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 576), Vector256.Load(add1 + 576)), Vector256.Load(sub1 + 576)), dst + 576);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 592), Vector256.Load(add1 + 592)), Vector256.Load(sub1 + 592)), dst + 592);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 608), Vector256.Load(add1 + 608)), Vector256.Load(sub1 + 608)), dst + 608);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 624), Vector256.Load(add1 + 624)), Vector256.Load(sub1 + 624)), dst + 624);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 640), Vector256.Load(add1 + 640)), Vector256.Load(sub1 + 640)), dst + 640);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 656), Vector256.Load(add1 + 656)), Vector256.Load(sub1 + 656)), dst + 656);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 672), Vector256.Load(add1 + 672)), Vector256.Load(sub1 + 672)), dst + 672);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 688), Vector256.Load(add1 + 688)), Vector256.Load(sub1 + 688)), dst + 688);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 704), Vector256.Load(add1 + 704)), Vector256.Load(sub1 + 704)), dst + 704);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 720), Vector256.Load(add1 + 720)), Vector256.Load(sub1 + 720)), dst + 720);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 736), Vector256.Load(add1 + 736)), Vector256.Load(sub1 + 736)), dst + 736);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 752), Vector256.Load(add1 + 752)), Vector256.Load(sub1 + 752)), dst + 752);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 768), Vector256.Load(add1 + 768)), Vector256.Load(sub1 + 768)), dst + 768);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 784), Vector256.Load(add1 + 784)), Vector256.Load(sub1 + 784)), dst + 784);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 800), Vector256.Load(add1 + 800)), Vector256.Load(sub1 + 800)), dst + 800);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 816), Vector256.Load(add1 + 816)), Vector256.Load(sub1 + 816)), dst + 816);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 832), Vector256.Load(add1 + 832)), Vector256.Load(sub1 + 832)), dst + 832);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 848), Vector256.Load(add1 + 848)), Vector256.Load(sub1 + 848)), dst + 848);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 864), Vector256.Load(add1 + 864)), Vector256.Load(sub1 + 864)), dst + 864);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 880), Vector256.Load(add1 + 880)), Vector256.Load(sub1 + 880)), dst + 880);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 896), Vector256.Load(add1 + 896)), Vector256.Load(sub1 + 896)), dst + 896);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 912), Vector256.Load(add1 + 912)), Vector256.Load(sub1 + 912)), dst + 912);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 928), Vector256.Load(add1 + 928)), Vector256.Load(sub1 + 928)), dst + 928);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 944), Vector256.Load(add1 + 944)), Vector256.Load(sub1 + 944)), dst + 944);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 960), Vector256.Load(add1 + 960)), Vector256.Load(sub1 + 960)), dst + 960);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 976), Vector256.Load(add1 + 976)), Vector256.Load(sub1 + 976)), dst + 976);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 992), Vector256.Load(add1 + 992)), Vector256.Load(sub1 + 992)), dst + 992);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1008), Vector256.Load(add1 + 1008)), Vector256.Load(sub1 + 1008)), dst + 1008);

            if (NNUE.NetArch == NetworkArchitecture.Simple768 && Simple768.HiddenSize <= 1024 ||
                NNUE.NetArch == NetworkArchitecture.Bucketed768 && Bucketed768.HiddenSize <= 1024)
                return;

            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1024), Vector256.Load(add1 + 1024)), Vector256.Load(sub1 + 1024)), dst + 1024);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1040), Vector256.Load(add1 + 1040)), Vector256.Load(sub1 + 1040)), dst + 1040);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1056), Vector256.Load(add1 + 1056)), Vector256.Load(sub1 + 1056)), dst + 1056);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1072), Vector256.Load(add1 + 1072)), Vector256.Load(sub1 + 1072)), dst + 1072);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1088), Vector256.Load(add1 + 1088)), Vector256.Load(sub1 + 1088)), dst + 1088);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1104), Vector256.Load(add1 + 1104)), Vector256.Load(sub1 + 1104)), dst + 1104);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1120), Vector256.Load(add1 + 1120)), Vector256.Load(sub1 + 1120)), dst + 1120);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1136), Vector256.Load(add1 + 1136)), Vector256.Load(sub1 + 1136)), dst + 1136);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1152), Vector256.Load(add1 + 1152)), Vector256.Load(sub1 + 1152)), dst + 1152);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1168), Vector256.Load(add1 + 1168)), Vector256.Load(sub1 + 1168)), dst + 1168);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1184), Vector256.Load(add1 + 1184)), Vector256.Load(sub1 + 1184)), dst + 1184);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1200), Vector256.Load(add1 + 1200)), Vector256.Load(sub1 + 1200)), dst + 1200);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1216), Vector256.Load(add1 + 1216)), Vector256.Load(sub1 + 1216)), dst + 1216);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1232), Vector256.Load(add1 + 1232)), Vector256.Load(sub1 + 1232)), dst + 1232);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1248), Vector256.Load(add1 + 1248)), Vector256.Load(sub1 + 1248)), dst + 1248);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1264), Vector256.Load(add1 + 1264)), Vector256.Load(sub1 + 1264)), dst + 1264);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1280), Vector256.Load(add1 + 1280)), Vector256.Load(sub1 + 1280)), dst + 1280);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1296), Vector256.Load(add1 + 1296)), Vector256.Load(sub1 + 1296)), dst + 1296);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1312), Vector256.Load(add1 + 1312)), Vector256.Load(sub1 + 1312)), dst + 1312);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1328), Vector256.Load(add1 + 1328)), Vector256.Load(sub1 + 1328)), dst + 1328);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1344), Vector256.Load(add1 + 1344)), Vector256.Load(sub1 + 1344)), dst + 1344);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1360), Vector256.Load(add1 + 1360)), Vector256.Load(sub1 + 1360)), dst + 1360);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1376), Vector256.Load(add1 + 1376)), Vector256.Load(sub1 + 1376)), dst + 1376);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1392), Vector256.Load(add1 + 1392)), Vector256.Load(sub1 + 1392)), dst + 1392);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1408), Vector256.Load(add1 + 1408)), Vector256.Load(sub1 + 1408)), dst + 1408);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1424), Vector256.Load(add1 + 1424)), Vector256.Load(sub1 + 1424)), dst + 1424);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1440), Vector256.Load(add1 + 1440)), Vector256.Load(sub1 + 1440)), dst + 1440);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1456), Vector256.Load(add1 + 1456)), Vector256.Load(sub1 + 1456)), dst + 1456);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1472), Vector256.Load(add1 + 1472)), Vector256.Load(sub1 + 1472)), dst + 1472);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1488), Vector256.Load(add1 + 1488)), Vector256.Load(sub1 + 1488)), dst + 1488);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1504), Vector256.Load(add1 + 1504)), Vector256.Load(sub1 + 1504)), dst + 1504);
            Vector256.Store(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1520), Vector256.Load(add1 + 1520)), Vector256.Load(sub1 + 1520)), dst + 1520);
        }


        public static void SubSubAdd(short* src, short* dst, short* sub1, short* sub2, short* add1)
        {
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 0), Vector256.Load(add1 + 0)), Vector256.Load(sub1 + 0)), Vector256.Load(sub2 + 0)),  dst + 0);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 16), Vector256.Load(add1 + 16)), Vector256.Load(sub1 + 16)), Vector256.Load(sub2 + 16)),  dst + 16);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 32), Vector256.Load(add1 + 32)), Vector256.Load(sub1 + 32)), Vector256.Load(sub2 + 32)),  dst + 32);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 48), Vector256.Load(add1 + 48)), Vector256.Load(sub1 + 48)), Vector256.Load(sub2 + 48)),  dst + 48);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 64), Vector256.Load(add1 + 64)), Vector256.Load(sub1 + 64)), Vector256.Load(sub2 + 64)),  dst + 64);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 80), Vector256.Load(add1 + 80)), Vector256.Load(sub1 + 80)), Vector256.Load(sub2 + 80)),  dst + 80);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 96), Vector256.Load(add1 + 96)), Vector256.Load(sub1 + 96)), Vector256.Load(sub2 + 96)),  dst + 96);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 112), Vector256.Load(add1 + 112)), Vector256.Load(sub1 + 112)), Vector256.Load(sub2 + 112)),  dst + 112);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 128), Vector256.Load(add1 + 128)), Vector256.Load(sub1 + 128)), Vector256.Load(sub2 + 128)),  dst + 128);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 144), Vector256.Load(add1 + 144)), Vector256.Load(sub1 + 144)), Vector256.Load(sub2 + 144)),  dst + 144);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 160), Vector256.Load(add1 + 160)), Vector256.Load(sub1 + 160)), Vector256.Load(sub2 + 160)),  dst + 160);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 176), Vector256.Load(add1 + 176)), Vector256.Load(sub1 + 176)), Vector256.Load(sub2 + 176)),  dst + 176);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 192), Vector256.Load(add1 + 192)), Vector256.Load(sub1 + 192)), Vector256.Load(sub2 + 192)),  dst + 192);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 208), Vector256.Load(add1 + 208)), Vector256.Load(sub1 + 208)), Vector256.Load(sub2 + 208)),  dst + 208);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 224), Vector256.Load(add1 + 224)), Vector256.Load(sub1 + 224)), Vector256.Load(sub2 + 224)),  dst + 224);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 240), Vector256.Load(add1 + 240)), Vector256.Load(sub1 + 240)), Vector256.Load(sub2 + 240)),  dst + 240);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 256), Vector256.Load(add1 + 256)), Vector256.Load(sub1 + 256)), Vector256.Load(sub2 + 256)),  dst + 256);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 272), Vector256.Load(add1 + 272)), Vector256.Load(sub1 + 272)), Vector256.Load(sub2 + 272)),  dst + 272);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 288), Vector256.Load(add1 + 288)), Vector256.Load(sub1 + 288)), Vector256.Load(sub2 + 288)),  dst + 288);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 304), Vector256.Load(add1 + 304)), Vector256.Load(sub1 + 304)), Vector256.Load(sub2 + 304)),  dst + 304);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 320), Vector256.Load(add1 + 320)), Vector256.Load(sub1 + 320)), Vector256.Load(sub2 + 320)),  dst + 320);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 336), Vector256.Load(add1 + 336)), Vector256.Load(sub1 + 336)), Vector256.Load(sub2 + 336)),  dst + 336);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 352), Vector256.Load(add1 + 352)), Vector256.Load(sub1 + 352)), Vector256.Load(sub2 + 352)),  dst + 352);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 368), Vector256.Load(add1 + 368)), Vector256.Load(sub1 + 368)), Vector256.Load(sub2 + 368)),  dst + 368);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 384), Vector256.Load(add1 + 384)), Vector256.Load(sub1 + 384)), Vector256.Load(sub2 + 384)),  dst + 384);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 400), Vector256.Load(add1 + 400)), Vector256.Load(sub1 + 400)), Vector256.Load(sub2 + 400)),  dst + 400);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 416), Vector256.Load(add1 + 416)), Vector256.Load(sub1 + 416)), Vector256.Load(sub2 + 416)),  dst + 416);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 432), Vector256.Load(add1 + 432)), Vector256.Load(sub1 + 432)), Vector256.Load(sub2 + 432)),  dst + 432);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 448), Vector256.Load(add1 + 448)), Vector256.Load(sub1 + 448)), Vector256.Load(sub2 + 448)),  dst + 448);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 464), Vector256.Load(add1 + 464)), Vector256.Load(sub1 + 464)), Vector256.Load(sub2 + 464)),  dst + 464);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 480), Vector256.Load(add1 + 480)), Vector256.Load(sub1 + 480)), Vector256.Load(sub2 + 480)),  dst + 480);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 496), Vector256.Load(add1 + 496)), Vector256.Load(sub1 + 496)), Vector256.Load(sub2 + 496)),  dst + 496);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 512), Vector256.Load(add1 + 512)), Vector256.Load(sub1 + 512)), Vector256.Load(sub2 + 512)),  dst + 512);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 528), Vector256.Load(add1 + 528)), Vector256.Load(sub1 + 528)), Vector256.Load(sub2 + 528)),  dst + 528);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 544), Vector256.Load(add1 + 544)), Vector256.Load(sub1 + 544)), Vector256.Load(sub2 + 544)),  dst + 544);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 560), Vector256.Load(add1 + 560)), Vector256.Load(sub1 + 560)), Vector256.Load(sub2 + 560)),  dst + 560);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 576), Vector256.Load(add1 + 576)), Vector256.Load(sub1 + 576)), Vector256.Load(sub2 + 576)),  dst + 576);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 592), Vector256.Load(add1 + 592)), Vector256.Load(sub1 + 592)), Vector256.Load(sub2 + 592)),  dst + 592);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 608), Vector256.Load(add1 + 608)), Vector256.Load(sub1 + 608)), Vector256.Load(sub2 + 608)),  dst + 608);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 624), Vector256.Load(add1 + 624)), Vector256.Load(sub1 + 624)), Vector256.Load(sub2 + 624)),  dst + 624);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 640), Vector256.Load(add1 + 640)), Vector256.Load(sub1 + 640)), Vector256.Load(sub2 + 640)),  dst + 640);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 656), Vector256.Load(add1 + 656)), Vector256.Load(sub1 + 656)), Vector256.Load(sub2 + 656)),  dst + 656);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 672), Vector256.Load(add1 + 672)), Vector256.Load(sub1 + 672)), Vector256.Load(sub2 + 672)),  dst + 672);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 688), Vector256.Load(add1 + 688)), Vector256.Load(sub1 + 688)), Vector256.Load(sub2 + 688)),  dst + 688);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 704), Vector256.Load(add1 + 704)), Vector256.Load(sub1 + 704)), Vector256.Load(sub2 + 704)),  dst + 704);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 720), Vector256.Load(add1 + 720)), Vector256.Load(sub1 + 720)), Vector256.Load(sub2 + 720)),  dst + 720);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 736), Vector256.Load(add1 + 736)), Vector256.Load(sub1 + 736)), Vector256.Load(sub2 + 736)),  dst + 736);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 752), Vector256.Load(add1 + 752)), Vector256.Load(sub1 + 752)), Vector256.Load(sub2 + 752)),  dst + 752);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 768), Vector256.Load(add1 + 768)), Vector256.Load(sub1 + 768)), Vector256.Load(sub2 + 768)),  dst + 768);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 784), Vector256.Load(add1 + 784)), Vector256.Load(sub1 + 784)), Vector256.Load(sub2 + 784)),  dst + 784);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 800), Vector256.Load(add1 + 800)), Vector256.Load(sub1 + 800)), Vector256.Load(sub2 + 800)),  dst + 800);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 816), Vector256.Load(add1 + 816)), Vector256.Load(sub1 + 816)), Vector256.Load(sub2 + 816)),  dst + 816);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 832), Vector256.Load(add1 + 832)), Vector256.Load(sub1 + 832)), Vector256.Load(sub2 + 832)),  dst + 832);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 848), Vector256.Load(add1 + 848)), Vector256.Load(sub1 + 848)), Vector256.Load(sub2 + 848)),  dst + 848);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 864), Vector256.Load(add1 + 864)), Vector256.Load(sub1 + 864)), Vector256.Load(sub2 + 864)),  dst + 864);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 880), Vector256.Load(add1 + 880)), Vector256.Load(sub1 + 880)), Vector256.Load(sub2 + 880)),  dst + 880);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 896), Vector256.Load(add1 + 896)), Vector256.Load(sub1 + 896)), Vector256.Load(sub2 + 896)),  dst + 896);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 912), Vector256.Load(add1 + 912)), Vector256.Load(sub1 + 912)), Vector256.Load(sub2 + 912)),  dst + 912);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 928), Vector256.Load(add1 + 928)), Vector256.Load(sub1 + 928)), Vector256.Load(sub2 + 928)),  dst + 928);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 944), Vector256.Load(add1 + 944)), Vector256.Load(sub1 + 944)), Vector256.Load(sub2 + 944)),  dst + 944);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 960), Vector256.Load(add1 + 960)), Vector256.Load(sub1 + 960)), Vector256.Load(sub2 + 960)),  dst + 960);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 976), Vector256.Load(add1 + 976)), Vector256.Load(sub1 + 976)), Vector256.Load(sub2 + 976)),  dst + 976);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 992), Vector256.Load(add1 + 992)), Vector256.Load(sub1 + 992)), Vector256.Load(sub2 + 992)),  dst + 992);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1008), Vector256.Load(add1 + 1008)), Vector256.Load(sub1 + 1008)), Vector256.Load(sub2 + 1008)),  dst + 1008);

            if (NNUE.NetArch == NetworkArchitecture.Simple768 && Simple768.HiddenSize <= 1024 ||
                NNUE.NetArch == NetworkArchitecture.Bucketed768 && Bucketed768.HiddenSize <= 1024)
                return;

            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1024), Vector256.Load(add1 + 1024)), Vector256.Load(sub1 + 1024)), Vector256.Load(sub2 + 1024)),  dst + 1024);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1040), Vector256.Load(add1 + 1040)), Vector256.Load(sub1 + 1040)), Vector256.Load(sub2 + 1040)),  dst + 1040);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1056), Vector256.Load(add1 + 1056)), Vector256.Load(sub1 + 1056)), Vector256.Load(sub2 + 1056)),  dst + 1056);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1072), Vector256.Load(add1 + 1072)), Vector256.Load(sub1 + 1072)), Vector256.Load(sub2 + 1072)),  dst + 1072);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1088), Vector256.Load(add1 + 1088)), Vector256.Load(sub1 + 1088)), Vector256.Load(sub2 + 1088)),  dst + 1088);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1104), Vector256.Load(add1 + 1104)), Vector256.Load(sub1 + 1104)), Vector256.Load(sub2 + 1104)),  dst + 1104);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1120), Vector256.Load(add1 + 1120)), Vector256.Load(sub1 + 1120)), Vector256.Load(sub2 + 1120)),  dst + 1120);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1136), Vector256.Load(add1 + 1136)), Vector256.Load(sub1 + 1136)), Vector256.Load(sub2 + 1136)),  dst + 1136);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1152), Vector256.Load(add1 + 1152)), Vector256.Load(sub1 + 1152)), Vector256.Load(sub2 + 1152)),  dst + 1152);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1168), Vector256.Load(add1 + 1168)), Vector256.Load(sub1 + 1168)), Vector256.Load(sub2 + 1168)),  dst + 1168);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1184), Vector256.Load(add1 + 1184)), Vector256.Load(sub1 + 1184)), Vector256.Load(sub2 + 1184)),  dst + 1184);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1200), Vector256.Load(add1 + 1200)), Vector256.Load(sub1 + 1200)), Vector256.Load(sub2 + 1200)),  dst + 1200);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1216), Vector256.Load(add1 + 1216)), Vector256.Load(sub1 + 1216)), Vector256.Load(sub2 + 1216)),  dst + 1216);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1232), Vector256.Load(add1 + 1232)), Vector256.Load(sub1 + 1232)), Vector256.Load(sub2 + 1232)),  dst + 1232);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1248), Vector256.Load(add1 + 1248)), Vector256.Load(sub1 + 1248)), Vector256.Load(sub2 + 1248)),  dst + 1248);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1264), Vector256.Load(add1 + 1264)), Vector256.Load(sub1 + 1264)), Vector256.Load(sub2 + 1264)),  dst + 1264);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1280), Vector256.Load(add1 + 1280)), Vector256.Load(sub1 + 1280)), Vector256.Load(sub2 + 1280)),  dst + 1280);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1296), Vector256.Load(add1 + 1296)), Vector256.Load(sub1 + 1296)), Vector256.Load(sub2 + 1296)),  dst + 1296);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1312), Vector256.Load(add1 + 1312)), Vector256.Load(sub1 + 1312)), Vector256.Load(sub2 + 1312)),  dst + 1312);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1328), Vector256.Load(add1 + 1328)), Vector256.Load(sub1 + 1328)), Vector256.Load(sub2 + 1328)),  dst + 1328);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1344), Vector256.Load(add1 + 1344)), Vector256.Load(sub1 + 1344)), Vector256.Load(sub2 + 1344)),  dst + 1344);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1360), Vector256.Load(add1 + 1360)), Vector256.Load(sub1 + 1360)), Vector256.Load(sub2 + 1360)),  dst + 1360);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1376), Vector256.Load(add1 + 1376)), Vector256.Load(sub1 + 1376)), Vector256.Load(sub2 + 1376)),  dst + 1376);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1392), Vector256.Load(add1 + 1392)), Vector256.Load(sub1 + 1392)), Vector256.Load(sub2 + 1392)),  dst + 1392);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1408), Vector256.Load(add1 + 1408)), Vector256.Load(sub1 + 1408)), Vector256.Load(sub2 + 1408)),  dst + 1408);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1424), Vector256.Load(add1 + 1424)), Vector256.Load(sub1 + 1424)), Vector256.Load(sub2 + 1424)),  dst + 1424);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1440), Vector256.Load(add1 + 1440)), Vector256.Load(sub1 + 1440)), Vector256.Load(sub2 + 1440)),  dst + 1440);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1456), Vector256.Load(add1 + 1456)), Vector256.Load(sub1 + 1456)), Vector256.Load(sub2 + 1456)),  dst + 1456);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1472), Vector256.Load(add1 + 1472)), Vector256.Load(sub1 + 1472)), Vector256.Load(sub2 + 1472)),  dst + 1472);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1488), Vector256.Load(add1 + 1488)), Vector256.Load(sub1 + 1488)), Vector256.Load(sub2 + 1488)),  dst + 1488);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1504), Vector256.Load(add1 + 1504)), Vector256.Load(sub1 + 1504)), Vector256.Load(sub2 + 1504)),  dst + 1504);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Load(src + 1520), Vector256.Load(add1 + 1520)), Vector256.Load(sub1 + 1520)), Vector256.Load(sub2 + 1520)),  dst + 1520);
        }


        public static void SubSubAddAdd(short* src, short* dst, short* sub1, short* sub2, short* add1, short* add2)
        {
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 0), Vector256.Load(add1 + 0)), Vector256.Load(add2 + 0)), Vector256.Load(sub1 + 0)), Vector256.Load(sub2 + 0)), dst + 0);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 16), Vector256.Load(add1 + 16)), Vector256.Load(add2 + 16)), Vector256.Load(sub1 + 16)), Vector256.Load(sub2 + 16)), dst + 16);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 32), Vector256.Load(add1 + 32)), Vector256.Load(add2 + 32)), Vector256.Load(sub1 + 32)), Vector256.Load(sub2 + 32)), dst + 32);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 48), Vector256.Load(add1 + 48)), Vector256.Load(add2 + 48)), Vector256.Load(sub1 + 48)), Vector256.Load(sub2 + 48)), dst + 48);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 64), Vector256.Load(add1 + 64)), Vector256.Load(add2 + 64)), Vector256.Load(sub1 + 64)), Vector256.Load(sub2 + 64)), dst + 64);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 80), Vector256.Load(add1 + 80)), Vector256.Load(add2 + 80)), Vector256.Load(sub1 + 80)), Vector256.Load(sub2 + 80)), dst + 80);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 96), Vector256.Load(add1 + 96)), Vector256.Load(add2 + 96)), Vector256.Load(sub1 + 96)), Vector256.Load(sub2 + 96)), dst + 96);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 112), Vector256.Load(add1 + 112)), Vector256.Load(add2 + 112)), Vector256.Load(sub1 + 112)), Vector256.Load(sub2 + 112)), dst + 112);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 128), Vector256.Load(add1 + 128)), Vector256.Load(add2 + 128)), Vector256.Load(sub1 + 128)), Vector256.Load(sub2 + 128)), dst + 128);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 144), Vector256.Load(add1 + 144)), Vector256.Load(add2 + 144)), Vector256.Load(sub1 + 144)), Vector256.Load(sub2 + 144)), dst + 144);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 160), Vector256.Load(add1 + 160)), Vector256.Load(add2 + 160)), Vector256.Load(sub1 + 160)), Vector256.Load(sub2 + 160)), dst + 160);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 176), Vector256.Load(add1 + 176)), Vector256.Load(add2 + 176)), Vector256.Load(sub1 + 176)), Vector256.Load(sub2 + 176)), dst + 176);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 192), Vector256.Load(add1 + 192)), Vector256.Load(add2 + 192)), Vector256.Load(sub1 + 192)), Vector256.Load(sub2 + 192)), dst + 192);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 208), Vector256.Load(add1 + 208)), Vector256.Load(add2 + 208)), Vector256.Load(sub1 + 208)), Vector256.Load(sub2 + 208)), dst + 208);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 224), Vector256.Load(add1 + 224)), Vector256.Load(add2 + 224)), Vector256.Load(sub1 + 224)), Vector256.Load(sub2 + 224)), dst + 224);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 240), Vector256.Load(add1 + 240)), Vector256.Load(add2 + 240)), Vector256.Load(sub1 + 240)), Vector256.Load(sub2 + 240)), dst + 240);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 256), Vector256.Load(add1 + 256)), Vector256.Load(add2 + 256)), Vector256.Load(sub1 + 256)), Vector256.Load(sub2 + 256)), dst + 256);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 272), Vector256.Load(add1 + 272)), Vector256.Load(add2 + 272)), Vector256.Load(sub1 + 272)), Vector256.Load(sub2 + 272)), dst + 272);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 288), Vector256.Load(add1 + 288)), Vector256.Load(add2 + 288)), Vector256.Load(sub1 + 288)), Vector256.Load(sub2 + 288)), dst + 288);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 304), Vector256.Load(add1 + 304)), Vector256.Load(add2 + 304)), Vector256.Load(sub1 + 304)), Vector256.Load(sub2 + 304)), dst + 304);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 320), Vector256.Load(add1 + 320)), Vector256.Load(add2 + 320)), Vector256.Load(sub1 + 320)), Vector256.Load(sub2 + 320)), dst + 320);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 336), Vector256.Load(add1 + 336)), Vector256.Load(add2 + 336)), Vector256.Load(sub1 + 336)), Vector256.Load(sub2 + 336)), dst + 336);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 352), Vector256.Load(add1 + 352)), Vector256.Load(add2 + 352)), Vector256.Load(sub1 + 352)), Vector256.Load(sub2 + 352)), dst + 352);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 368), Vector256.Load(add1 + 368)), Vector256.Load(add2 + 368)), Vector256.Load(sub1 + 368)), Vector256.Load(sub2 + 368)), dst + 368);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 384), Vector256.Load(add1 + 384)), Vector256.Load(add2 + 384)), Vector256.Load(sub1 + 384)), Vector256.Load(sub2 + 384)), dst + 384);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 400), Vector256.Load(add1 + 400)), Vector256.Load(add2 + 400)), Vector256.Load(sub1 + 400)), Vector256.Load(sub2 + 400)), dst + 400);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 416), Vector256.Load(add1 + 416)), Vector256.Load(add2 + 416)), Vector256.Load(sub1 + 416)), Vector256.Load(sub2 + 416)), dst + 416);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 432), Vector256.Load(add1 + 432)), Vector256.Load(add2 + 432)), Vector256.Load(sub1 + 432)), Vector256.Load(sub2 + 432)), dst + 432);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 448), Vector256.Load(add1 + 448)), Vector256.Load(add2 + 448)), Vector256.Load(sub1 + 448)), Vector256.Load(sub2 + 448)), dst + 448);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 464), Vector256.Load(add1 + 464)), Vector256.Load(add2 + 464)), Vector256.Load(sub1 + 464)), Vector256.Load(sub2 + 464)), dst + 464);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 480), Vector256.Load(add1 + 480)), Vector256.Load(add2 + 480)), Vector256.Load(sub1 + 480)), Vector256.Load(sub2 + 480)), dst + 480);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 496), Vector256.Load(add1 + 496)), Vector256.Load(add2 + 496)), Vector256.Load(sub1 + 496)), Vector256.Load(sub2 + 496)), dst + 496);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 512), Vector256.Load(add1 + 512)), Vector256.Load(add2 + 512)), Vector256.Load(sub1 + 512)), Vector256.Load(sub2 + 512)), dst + 512);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 528), Vector256.Load(add1 + 528)), Vector256.Load(add2 + 528)), Vector256.Load(sub1 + 528)), Vector256.Load(sub2 + 528)), dst + 528);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 544), Vector256.Load(add1 + 544)), Vector256.Load(add2 + 544)), Vector256.Load(sub1 + 544)), Vector256.Load(sub2 + 544)), dst + 544);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 560), Vector256.Load(add1 + 560)), Vector256.Load(add2 + 560)), Vector256.Load(sub1 + 560)), Vector256.Load(sub2 + 560)), dst + 560);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 576), Vector256.Load(add1 + 576)), Vector256.Load(add2 + 576)), Vector256.Load(sub1 + 576)), Vector256.Load(sub2 + 576)), dst + 576);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 592), Vector256.Load(add1 + 592)), Vector256.Load(add2 + 592)), Vector256.Load(sub1 + 592)), Vector256.Load(sub2 + 592)), dst + 592);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 608), Vector256.Load(add1 + 608)), Vector256.Load(add2 + 608)), Vector256.Load(sub1 + 608)), Vector256.Load(sub2 + 608)), dst + 608);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 624), Vector256.Load(add1 + 624)), Vector256.Load(add2 + 624)), Vector256.Load(sub1 + 624)), Vector256.Load(sub2 + 624)), dst + 624);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 640), Vector256.Load(add1 + 640)), Vector256.Load(add2 + 640)), Vector256.Load(sub1 + 640)), Vector256.Load(sub2 + 640)), dst + 640);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 656), Vector256.Load(add1 + 656)), Vector256.Load(add2 + 656)), Vector256.Load(sub1 + 656)), Vector256.Load(sub2 + 656)), dst + 656);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 672), Vector256.Load(add1 + 672)), Vector256.Load(add2 + 672)), Vector256.Load(sub1 + 672)), Vector256.Load(sub2 + 672)), dst + 672);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 688), Vector256.Load(add1 + 688)), Vector256.Load(add2 + 688)), Vector256.Load(sub1 + 688)), Vector256.Load(sub2 + 688)), dst + 688);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 704), Vector256.Load(add1 + 704)), Vector256.Load(add2 + 704)), Vector256.Load(sub1 + 704)), Vector256.Load(sub2 + 704)), dst + 704);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 720), Vector256.Load(add1 + 720)), Vector256.Load(add2 + 720)), Vector256.Load(sub1 + 720)), Vector256.Load(sub2 + 720)), dst + 720);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 736), Vector256.Load(add1 + 736)), Vector256.Load(add2 + 736)), Vector256.Load(sub1 + 736)), Vector256.Load(sub2 + 736)), dst + 736);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 752), Vector256.Load(add1 + 752)), Vector256.Load(add2 + 752)), Vector256.Load(sub1 + 752)), Vector256.Load(sub2 + 752)), dst + 752);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 768), Vector256.Load(add1 + 768)), Vector256.Load(add2 + 768)), Vector256.Load(sub1 + 768)), Vector256.Load(sub2 + 768)), dst + 768);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 784), Vector256.Load(add1 + 784)), Vector256.Load(add2 + 784)), Vector256.Load(sub1 + 784)), Vector256.Load(sub2 + 784)), dst + 784);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 800), Vector256.Load(add1 + 800)), Vector256.Load(add2 + 800)), Vector256.Load(sub1 + 800)), Vector256.Load(sub2 + 800)), dst + 800);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 816), Vector256.Load(add1 + 816)), Vector256.Load(add2 + 816)), Vector256.Load(sub1 + 816)), Vector256.Load(sub2 + 816)), dst + 816);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 832), Vector256.Load(add1 + 832)), Vector256.Load(add2 + 832)), Vector256.Load(sub1 + 832)), Vector256.Load(sub2 + 832)), dst + 832);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 848), Vector256.Load(add1 + 848)), Vector256.Load(add2 + 848)), Vector256.Load(sub1 + 848)), Vector256.Load(sub2 + 848)), dst + 848);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 864), Vector256.Load(add1 + 864)), Vector256.Load(add2 + 864)), Vector256.Load(sub1 + 864)), Vector256.Load(sub2 + 864)), dst + 864);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 880), Vector256.Load(add1 + 880)), Vector256.Load(add2 + 880)), Vector256.Load(sub1 + 880)), Vector256.Load(sub2 + 880)), dst + 880);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 896), Vector256.Load(add1 + 896)), Vector256.Load(add2 + 896)), Vector256.Load(sub1 + 896)), Vector256.Load(sub2 + 896)), dst + 896);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 912), Vector256.Load(add1 + 912)), Vector256.Load(add2 + 912)), Vector256.Load(sub1 + 912)), Vector256.Load(sub2 + 912)), dst + 912);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 928), Vector256.Load(add1 + 928)), Vector256.Load(add2 + 928)), Vector256.Load(sub1 + 928)), Vector256.Load(sub2 + 928)), dst + 928);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 944), Vector256.Load(add1 + 944)), Vector256.Load(add2 + 944)), Vector256.Load(sub1 + 944)), Vector256.Load(sub2 + 944)), dst + 944);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 960), Vector256.Load(add1 + 960)), Vector256.Load(add2 + 960)), Vector256.Load(sub1 + 960)), Vector256.Load(sub2 + 960)), dst + 960);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 976), Vector256.Load(add1 + 976)), Vector256.Load(add2 + 976)), Vector256.Load(sub1 + 976)), Vector256.Load(sub2 + 976)), dst + 976);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 992), Vector256.Load(add1 + 992)), Vector256.Load(add2 + 992)), Vector256.Load(sub1 + 992)), Vector256.Load(sub2 + 992)), dst + 992);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1008), Vector256.Load(add1 + 1008)), Vector256.Load(add2 + 1008)), Vector256.Load(sub1 + 1008)), Vector256.Load(sub2 + 1008)), dst + 1008);

            if (NNUE.NetArch == NetworkArchitecture.Simple768 && Simple768.HiddenSize <= 1024 ||
                NNUE.NetArch == NetworkArchitecture.Bucketed768 && Bucketed768.HiddenSize <= 1024)
                return;

            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1024), Vector256.Load(add1 + 1024)), Vector256.Load(add2 + 1024)), Vector256.Load(sub1 + 1024)), Vector256.Load(sub2 + 1024)), dst + 1024);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1040), Vector256.Load(add1 + 1040)), Vector256.Load(add2 + 1040)), Vector256.Load(sub1 + 1040)), Vector256.Load(sub2 + 1040)), dst + 1040);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1056), Vector256.Load(add1 + 1056)), Vector256.Load(add2 + 1056)), Vector256.Load(sub1 + 1056)), Vector256.Load(sub2 + 1056)), dst + 1056);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1072), Vector256.Load(add1 + 1072)), Vector256.Load(add2 + 1072)), Vector256.Load(sub1 + 1072)), Vector256.Load(sub2 + 1072)), dst + 1072);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1088), Vector256.Load(add1 + 1088)), Vector256.Load(add2 + 1088)), Vector256.Load(sub1 + 1088)), Vector256.Load(sub2 + 1088)), dst + 1088);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1104), Vector256.Load(add1 + 1104)), Vector256.Load(add2 + 1104)), Vector256.Load(sub1 + 1104)), Vector256.Load(sub2 + 1104)), dst + 1104);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1120), Vector256.Load(add1 + 1120)), Vector256.Load(add2 + 1120)), Vector256.Load(sub1 + 1120)), Vector256.Load(sub2 + 1120)), dst + 1120);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1136), Vector256.Load(add1 + 1136)), Vector256.Load(add2 + 1136)), Vector256.Load(sub1 + 1136)), Vector256.Load(sub2 + 1136)), dst + 1136);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1152), Vector256.Load(add1 + 1152)), Vector256.Load(add2 + 1152)), Vector256.Load(sub1 + 1152)), Vector256.Load(sub2 + 1152)), dst + 1152);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1168), Vector256.Load(add1 + 1168)), Vector256.Load(add2 + 1168)), Vector256.Load(sub1 + 1168)), Vector256.Load(sub2 + 1168)), dst + 1168);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1184), Vector256.Load(add1 + 1184)), Vector256.Load(add2 + 1184)), Vector256.Load(sub1 + 1184)), Vector256.Load(sub2 + 1184)), dst + 1184);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1200), Vector256.Load(add1 + 1200)), Vector256.Load(add2 + 1200)), Vector256.Load(sub1 + 1200)), Vector256.Load(sub2 + 1200)), dst + 1200);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1216), Vector256.Load(add1 + 1216)), Vector256.Load(add2 + 1216)), Vector256.Load(sub1 + 1216)), Vector256.Load(sub2 + 1216)), dst + 1216);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1232), Vector256.Load(add1 + 1232)), Vector256.Load(add2 + 1232)), Vector256.Load(sub1 + 1232)), Vector256.Load(sub2 + 1232)), dst + 1232);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1248), Vector256.Load(add1 + 1248)), Vector256.Load(add2 + 1248)), Vector256.Load(sub1 + 1248)), Vector256.Load(sub2 + 1248)), dst + 1248);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1264), Vector256.Load(add1 + 1264)), Vector256.Load(add2 + 1264)), Vector256.Load(sub1 + 1264)), Vector256.Load(sub2 + 1264)), dst + 1264);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1280), Vector256.Load(add1 + 1280)), Vector256.Load(add2 + 1280)), Vector256.Load(sub1 + 1280)), Vector256.Load(sub2 + 1280)), dst + 1280);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1296), Vector256.Load(add1 + 1296)), Vector256.Load(add2 + 1296)), Vector256.Load(sub1 + 1296)), Vector256.Load(sub2 + 1296)), dst + 1296);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1312), Vector256.Load(add1 + 1312)), Vector256.Load(add2 + 1312)), Vector256.Load(sub1 + 1312)), Vector256.Load(sub2 + 1312)), dst + 1312);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1328), Vector256.Load(add1 + 1328)), Vector256.Load(add2 + 1328)), Vector256.Load(sub1 + 1328)), Vector256.Load(sub2 + 1328)), dst + 1328);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1344), Vector256.Load(add1 + 1344)), Vector256.Load(add2 + 1344)), Vector256.Load(sub1 + 1344)), Vector256.Load(sub2 + 1344)), dst + 1344);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1360), Vector256.Load(add1 + 1360)), Vector256.Load(add2 + 1360)), Vector256.Load(sub1 + 1360)), Vector256.Load(sub2 + 1360)), dst + 1360);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1376), Vector256.Load(add1 + 1376)), Vector256.Load(add2 + 1376)), Vector256.Load(sub1 + 1376)), Vector256.Load(sub2 + 1376)), dst + 1376);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1392), Vector256.Load(add1 + 1392)), Vector256.Load(add2 + 1392)), Vector256.Load(sub1 + 1392)), Vector256.Load(sub2 + 1392)), dst + 1392);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1408), Vector256.Load(add1 + 1408)), Vector256.Load(add2 + 1408)), Vector256.Load(sub1 + 1408)), Vector256.Load(sub2 + 1408)), dst + 1408);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1424), Vector256.Load(add1 + 1424)), Vector256.Load(add2 + 1424)), Vector256.Load(sub1 + 1424)), Vector256.Load(sub2 + 1424)), dst + 1424);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1440), Vector256.Load(add1 + 1440)), Vector256.Load(add2 + 1440)), Vector256.Load(sub1 + 1440)), Vector256.Load(sub2 + 1440)), dst + 1440);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1456), Vector256.Load(add1 + 1456)), Vector256.Load(add2 + 1456)), Vector256.Load(sub1 + 1456)), Vector256.Load(sub2 + 1456)), dst + 1456);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1472), Vector256.Load(add1 + 1472)), Vector256.Load(add2 + 1472)), Vector256.Load(sub1 + 1472)), Vector256.Load(sub2 + 1472)), dst + 1472);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1488), Vector256.Load(add1 + 1488)), Vector256.Load(add2 + 1488)), Vector256.Load(sub1 + 1488)), Vector256.Load(sub2 + 1488)), dst + 1488);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1504), Vector256.Load(add1 + 1504)), Vector256.Load(add2 + 1504)), Vector256.Load(sub1 + 1504)), Vector256.Load(sub2 + 1504)), dst + 1504);
            Vector256.Store(Vector256.Subtract(Vector256.Subtract(Vector256.Add(Vector256.Add(Vector256.Load(src + 1520), Vector256.Load(add1 + 1520)), Vector256.Load(add2 + 1520)), Vector256.Load(sub1 + 1520)), Vector256.Load(sub2 + 1520)), dst + 1520);
        }
    }
}
