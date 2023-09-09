using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.Data;
using LTChess.Properties;

namespace LTChess.Logic.NN.Simple768
{
    public static unsafe class NNUEEvaluation
    {
        public static NNUE768 Network768;
        private static bool Initialized = false;

        static NNUEEvaluation()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            if (Initialized || !UseSimple768)
            {
                return;
            }

            Initialized = true;

            Network768 = new NNUE768();

            var stream = new MemoryStream();
            var sw = new StreamWriter(stream);
            sw.Write(Resources.network);
            sw.Flush();
            stream.Position = 0;

            Network768.FromTXT(stream);
            Log("Using NNUE with Simple768 network");
        }

        [MethodImpl(Inline)]
        public static int GetEvaluation(Position pos)
        {
            return Network768.Evaluate(pos.ToMove);
        }

        [MethodImpl(Inline)]
        public static void ResetNN()
        {
            Network768.ResetAccumulator();
        }

        [MethodImpl(Inline)]
        public static void RefreshNN(Position pos)
        {
            Network768.RefreshAccumulator(pos);
        }

        [MethodImpl(Inline)]
        public static void MakeMoveNN(Position pos, Move move)
        {
            //Log("MakeMoveNN(" + move.ToString() + ")");

            Bitboard bb = pos.bb;

            int pt = bb.GetPieceAtIndex(move.From);
            int pc = bb.GetColorAtIndex(move.From);

            int theirPiece = bb.GetPieceAtIndex(move.To);

            Network768.PushAccumulator();

            if (move.Capture)
            {
                Network768.ActivateAccumulator(theirPiece, Not(pc), move.To, false);
            }

            if (move.EnPassant)
            {
                int idxPawn = (bb.Pieces[Piece.Pawn] & SquareBB[pos.EnPassantTarget - 8]) != 0 ? pos.EnPassantTarget - 8 : pos.EnPassantTarget + 8;
                Network768.ActivateAccumulator(Piece.Pawn, Not(pc), idxPawn, false);
            }

            Network768.EfficientlyUpdateAccumulator(pt, pc, move.From, move.To);

            if (move.Promotion)
            {
                Network768.ActivateAccumulator(Piece.Pawn, pc, move.To, false);
                Network768.ActivateAccumulator(move.PromotionTo, pc, move.To, true);
            }

            if (move.Castle)
            {

                int rookFrom = move.To switch
                {
                    C1 => A1,
                    G1 => H1,
                    C8 => A8,
                    G8 => H8,
                };

                int rookTo = move.To switch
                {
                    C1 => D1,
                    G1 => F1,
                    C8 => D8,
                    G8 => F8,
                };

                Network768.EfficientlyUpdateAccumulator(Piece.Rook, pc, rookFrom, rookTo);
            }
        }

        [MethodImpl(Inline)]
        public static void UnmakeMoveNN()
        {
            Network768.PullAccumulator();
        }

    }
}
