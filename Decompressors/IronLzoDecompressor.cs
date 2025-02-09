using IronCompress;

namespace BlackberrySystemPacker.Decompressors
{
    public class IronLzoDecompressor : IDecompressor
    {
        private Iron _iron;

        public IronLzoDecompressor()
        {
            _iron = new Iron();
        }

        public byte[] Compress(byte[] data)
        {
            using var result = _iron.Compress(Codec.LZO, data, null, System.IO.Compression.CompressionLevel.NoCompression);
            return result.AsSpan().ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            using var result = _iron.Decompress(Codec.LZO, data, data.Length);
            return result.AsSpan().ToArray();
        }
    }
}
