
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Lizard.Logic.Datagen
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public unsafe struct BulletFormatEntry
    {
        public const int Size = 32;

        [FieldOffset(0)] public ulong occ;
        [FieldOffset(8)] public fixed byte pcs[16];
        [FieldOffset(24)] public short score;
        [FieldOffset(26)] public byte result;
        [FieldOffset(27)] public byte ksq;
        [FieldOffset(28)] public byte opp_ksq;
        [FieldOffset(29)] public fixed byte _pad[3];



        public void FillBitboard(ref Bitboard bb)
        {
            bb.Reset();

            bb.Occupancy = occ;

            ulong temp = occ;
            int idx = 0;
            while (temp != 0)
            {
                int sq = poplsb(&temp);
                int piece = (pcs[idx / 2] >> (4 * (idx & 1))) & 0b1111;

                bb.AddPiece(sq, piece / 8, piece % 8);

                idx++;
            }
        }



        public static BulletFormatEntry FromBitboard(ref Bitboard bb, int stm, short score, GameResult result)
        {
            Span<ulong> bbs = [
                bb.Colors[White], bb.Colors[Black],
                bb.Pieces[0], bb.Pieces[1], bb.Pieces[2],
                bb.Pieces[3], bb.Pieces[4], bb.Pieces[5],
            ];

            if (stm == Black)
            {
                for (int i = 0; i < bbs.Length; i++)
                    bbs[i] = BinaryPrimitives.ReverseEndianness(bbs[i]);

                (bbs[White], bbs[Black]) = (bbs[Black], bbs[White]);

                score = (short)-score;
                result = 1 - result;
            }

            ulong occ = bbs[0] | bbs[1];

            BulletFormatEntry bfe = new BulletFormatEntry
            {
                score = score,
                occ = occ,
                result =  (byte)(2 * (int)result),
                ksq     = (byte) BitOperations.TrailingZeroCount(bbs[0] & bbs[7]),
                opp_ksq = (byte)(BitOperations.TrailingZeroCount(bbs[1] & bbs[7]) ^ 56)
            };

            Span<byte> pieces = stackalloc byte[16];

            int idx = 0;
            ulong occ2 = occ;
            int piece = 0;
            while (occ2 > 0)
            {
                int sq = BitOperations.TrailingZeroCount(occ2);
                ulong bit = 1UL << sq;
                occ2 &= occ2 - 1;

                byte colour = (byte)(((bit & bbs[1]) > 0 ? 1 : 0) << 3);
                for (int i = 2; i < 8; i++)
                    if ((bit & bbs[i]) > 0)
                    {
                        piece = i - 2;
                        break;
                    }

                byte pc = (byte)(colour | (byte)piece);

                bfe.pcs[idx / 2] |= (byte)(pc << (4 * (idx & 1)));

                idx += 1;
            }

            return bfe;
        }



        public void WriteToBuffer(Span<byte> buff)
        {
            fixed (void* buffPtr = &buff[0], thisPtr = &this)
            {
                Unsafe.CopyBlock(buffPtr, thisPtr, BulletFormatEntry.Size);
            }
        }
    }
}
