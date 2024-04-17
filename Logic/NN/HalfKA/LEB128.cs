

//  https://github.com/rzubek/mini-leb128/blob/master/LEB128.cs
// This software is released under the BSD License:
// BSD Zero Clause License
// Copyright (c) 2020

// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.

// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.



using System.Text;

namespace Lizard.Logic.NN.HKA
{
    /// <summary>
    /// Single-file utility to read and write integers in the LEB128 (7-bit little endian base-128) format.
    /// See https://en.wikipedia.org/wiki/LEB128 for details.
    /// </summary>
    public static class LEB128
    {
        private const string MagicString = "COMPRESSED_LEB128";
        public const int MagicStringSize = 17;

        private const long SIGN_EXTEND_MASK = -1L;
        private const int INT64_BITSIZE = sizeof(long) * 8;

        /// <summary>
        /// Returns true if the NNUE file has a "COMPRESSED_LEB128" header, which means that the weights and biases
        /// for the FeatureTransformer are compressed.
        /// <br></br>
        /// The BinaryReader <paramref name="br"/>'s position in the stream is unchanged.
        /// </summary>
        public static unsafe bool IsCompressed(BinaryReader br)
        {

            if (br.BaseStream.CanSeek)
            {
                byte[] buff = new byte[MagicStringSize];

                int readCnt = br.BaseStream.Read(buff, 0, MagicStringSize);
                br.BaseStream.Seek(-readCnt, SeekOrigin.Current);

                if (readCnt != MagicStringSize)
                {
                    return false;
                }

                string str;
                fixed (byte* ptr = &buff[0])
                {
                    str = Encoding.UTF8.GetString(ptr, MagicStringSize);
                }

                if (str.Equals(MagicString))
                {
                    return true;
                }
            }

            return false;
        }


        public static unsafe void ReadLEBInt16(BinaryReader br, short* output, int count)
        {
            const uint BUF_SIZE = 4096 * 8;

            Stream stream = br.BaseStream;
            br.BaseStream.Position += MagicStringSize;

            byte[] buf = new byte[BUF_SIZE];
            var bytes_left = br.ReadUInt32();
            uint buf_pos = BUF_SIZE;
            for (int i = 0; i < count; ++i)
            {
                short result = 0;
                int shift = 0;
                do
                {
                    if (buf_pos == BUF_SIZE)
                    {
                        stream.Read(buf, 0, (int)Math.Min(bytes_left, BUF_SIZE));
                        buf_pos = 0;
                    }
                    byte b = buf[buf_pos++];
                    --bytes_left;
                    result |= (short)((b & 0x7f) << shift);
                    shift += 7;
                    if ((b & 0x80) == 0)
                    {
                        output[i] = ((sizeof(short) * 8 <= shift) || (b & 0x40) == 0) ? result : ((short)(result | ~((1 << shift) - 1)));
                        break;
                    }
                } while (shift < sizeof(short) * 8);
            }
        }

        public static unsafe void ReadLEBInt32(BinaryReader br, int* output, int count)
        {
            const uint BUF_SIZE = 4096;

            Stream stream = br.BaseStream;
            br.BaseStream.Position += MagicStringSize;

            byte[] buf = new byte[BUF_SIZE];
            var bytes_left = br.ReadUInt32();
            uint buf_pos = BUF_SIZE;
            for (int i = 0; i < count; ++i)
            {
                int result = 0;
                int shift = 0;
                do
                {
                    if (buf_pos == BUF_SIZE)
                    {
                        stream.Read(buf, 0, (int)Math.Min(bytes_left, BUF_SIZE));
                        buf_pos = 0;
                    }
                    byte b = buf[buf_pos++];
                    --bytes_left;
                    result |= (int)((b & 0x7f) << shift);
                    shift += 7;
                    if ((b & 0x80) == 0)
                    {
                        output[i] = ((sizeof(int) * 8 <= shift) || (b & 0x40) == 0) ? result : ((int)(result | ~((1 << shift) - 1)));
                        break;
                    }
                } while (shift < sizeof(int) * 8);
            }
        }
    }
}