namespace BlackberrySystemPacker.Decompressors
{
    public interface IDecompressor
    {
        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] data);
    }
}