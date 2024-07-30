using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
                {
                    bbs[i] = BinaryPrimitives.ReverseEndianness(bbs[i]);
                }

                (bbs[0], bbs[1]) = (bbs[1], bbs[0]);

                score = (short)-score;
                result = 1 - result;
            }

            ulong occ = bbs[0] | bbs[1];
            byte[] pieces = new byte[16];

            int idx = 0;
            ulong occ2 = occ;
            while (occ2 > 0)
            {
                int sq = BitOperations.TrailingZeroCount(occ2);
                ulong bit = 1UL << sq;
                occ2 &= occ2 - 1;

                byte colour = (byte)(((bit & bbs[1]) > 0 ? 1 : 0) << 3);
                var piece = FindIndex(bbs[2..], bb => (bit & bb) > 0);
                if (piece == -1) throw new Exception("No Piece Found!");

                byte pc = (byte)(colour | (byte)piece);

                pieces[idx / 2] |= (byte)(pc << (4 * (idx & 1)));

                idx += 1;
            }

            byte resultByte = (byte)(2 * (int)result);
            byte ksq = (byte)BitOperations.TrailingZeroCount(bbs[0] & bbs[7]);
            byte oppKsq = (byte)(BitOperations.TrailingZeroCount(bbs[1] & bbs[7]) ^ 56);

            BulletFormatEntry bfe = new BulletFormatEntry()
            {
                occ = occ,
                score = score,
                result = resultByte,
                ksq = ksq,
                opp_ksq = oppKsq,
            };

            fixed (byte* pcsPtr = pieces)
                Unsafe.CopyBlock(bfe.pcs, pcsPtr, sizeof(byte) * 16);

            return bfe;

            static int FindIndex(Span<ulong> span, Func<ulong, bool> predicate)
            {
                for (int i = 0; i < span.Length; i++)
                    if (predicate(span[i]))
                        return i;

                return -1;
            }
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
