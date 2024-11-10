
using ZstdSharp;

namespace Lizard.Logic.Util
{
    public static class Zstd
    {
        private const int ZSTD_HEADER = -47205080;

        public static bool IsCompressed(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            int headerMaybe = br.ReadInt32();
            br.BaseStream.Seek(0, SeekOrigin.Begin);

            return (headerMaybe == ZSTD_HEADER);
        }

        public static MemoryStream Decompress(Stream stream, byte[] buff)
        {
            var zstStream = new DecompressionStream(stream);
            MemoryStream memStream = new MemoryStream(buff);
            zstStream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }
    }
}
