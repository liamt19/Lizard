
using System.Runtime.InteropServices;

namespace Lizard.Logic.Datagen
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct BulletDataFormat : TOutputFormat
    {
        //  STM here is used to fix the game result, which is dependent on the STM:
        //  If black is to move, the result is flipped around WhiteWin <-> BlackWin.
        //  WE don't know the result when creating the entries, and STM isn't stored within them anywhere,
        //  So manually place the STM in the last byte of padding of the entries.
        [FieldOffset( 0)] BulletFormatEntry BFE;
        [FieldOffset(31)] byte STM;

        public int Score
        {
            get => BFE.score;
            set => BFE.score = (short)value;
        }

        public GameResult Result
        {
            get => (GameResult)BFE.result;
            set => BFE.result = (byte)value;
        }

        public void SetSTM(int stm) { STM = (byte)stm; }
        public void SetResult(GameResult gr)
        {
            if (STM == Black)
            {
                gr = (GameResult)(2 - gr);
            }

            Result = gr;
        }

        public string GetWritableTextData()
        {
            return "";
        }

        public byte[] GetWritableData()
        {
            int len = Marshal.SizeOf<BulletFormatEntry>();
            IntPtr ptr = Marshal.AllocHGlobal(len);
            byte[] myBuffer = new byte[len];

            Marshal.StructureToPtr(BFE, ptr, false);
            Marshal.Copy(ptr, myBuffer, 0, len);
            Marshal.FreeHGlobal(ptr);

            return myBuffer;
        }

        public void Fill(Position pos, int score)
        {
            BFE = BulletFormatEntry.FromBitboard(ref pos.bb, pos.ToMove, (short)score, GameResult.Draw);
            STM = (byte)pos.ToMove;
        }

    }
}
