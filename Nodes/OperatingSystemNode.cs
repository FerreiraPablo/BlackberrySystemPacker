using BlackberrySystemPacker.Decompressors;
using BlackberrySystemPacker.Helpers.RCFS;
using System.Text;

namespace BlackberrySystemPacker.Nodes
{
    public class OperatingSystemNode : FileSystemNode
    {
        public int Flags { get; set; }

        public override int Size { get => DecompressedSize; set => DecompressedSize = value; }

        public int NodeNumber { get; set; }

        public int NameOffset { get; set; }

        public int BlockSize { get; set; }

        public int StartOffset { get; set; }

        public long PartitionOffset = 0;

        public static IDecompressor Compressor = new UnsafeLzoDecompressor();

        public long LocalPosition = 0;

        public int CompressedSize;

        public int DecompressedSize;

        public List<int> OtherHeaders = new List<int>();

        public VerifierNode Verifier { get; set; }

        public override void Write(byte[] data)
        {
            DecompressedSize = data.Length;
            using var localStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(localStream);
            if (!IsDirectory())
            {
                if (IsCompressed())
                {
                    var processedBytes = 0;
                    var maxChunkSize = 16384;
                    var compressedSize = 0;
                    var chunk = new List<byte[]>();

                    var compressor = new NativeLzoDecompressor();
                    while (processedBytes < DecompressedSize)
                    {
                        var bytesLength = Math.Min(maxChunkSize, DecompressedSize - processedBytes);
                        var blockData = new Span<byte>(data, processedBytes, bytesLength);
                        var nextChunkPosition = processedBytes + bytesLength;
                        var currentChunkSize = nextChunkPosition - processedBytes;
                        processedBytes = nextChunkPosition;

                        byte[] dataChunk = compressor.Compress(blockData.ToArray());
                        chunk.Add(dataChunk);

                        compressedSize += dataChunk.Length;
                    }

                    var blockSize = (chunk.Count * 4) + 4;
                    binaryWriter.Write(blockSize);
                    var lastChunkPosition = 0;

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        lastChunkPosition += i == 0 ? chunk[i].Length + blockSize : chunk[i].Length;
                        binaryWriter.Write(lastChunkPosition);
                    }

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        binaryWriter.Write(chunk[i]);
                    }

                    CompressedSize = compressedSize;
                }
                else
                {
                    binaryWriter.Write(data);
                }
            } else
            {
                binaryWriter.Write(data);
            }


            var finishedData = (localStream.ToArray());
            var streamWriter = new BinaryWriter(Stream);

            var contentLocation = PartitionOffset + StartOffset;
            streamWriter.BaseStream.Seek(contentLocation, SeekOrigin.Begin);
            streamWriter.Write(finishedData);
            //WriteMetaData();    

            //var metaLocation = PartitionOffset + LocalPosition;
            //streamWriter.Seek((int)metaLocation + 16, SeekOrigin.Begin);
            //streamWriter.Write(DecompressedSize);

            //Verifier.ApplyNodeChanges(this, finishedData);
        }

        public override byte[] Read()
        {
            var binaryReader = new BinaryReader(Stream);
            byte[] decompressedData = Array.Empty<byte>();

            var nodeDataLocation = PartitionOffset + StartOffset;
            Stream.Seek(nodeDataLocation, SeekOrigin.Begin);
            if (!IsDirectory())
            {
                if (IsCompressed())
                {
                    var blockSize = binaryReader.ReadInt32();
                    var blockUnits = (blockSize - 4) / 4;
                    BlockSize = blockSize;
                    int? previousValue = null;
                    List<int> blockSizes = new();
                    for (int unitIndex = 0; unitIndex < blockUnits; unitIndex++)
                    {
                        var startBlock = binaryReader.ReadInt32();
                        if (previousValue == null)
                        {
                            blockSizes.Add(startBlock - blockSize);
                        }
                        else
                        {
                            blockSizes.Add(startBlock - previousValue.Value);
                        }
                        previousValue = startBlock;
                    }

                    using var outputData = new MemoryStream();
                    List<byte> compressedData = new();
                    foreach (var size in blockSizes)
                    {
                        var src = binaryReader.ReadBytes(size);
                        compressedData.AddRange(src);
                        outputData.Write(Compressor.Decompress(src));
                        outputData.Flush();
                    }
                    CompressedSize = blockSizes.Sum();
                    decompressedData = outputData.ToArray();
                }
                else
                {
                    decompressedData = binaryReader.ReadBytes(DecompressedSize);
                }
            } else
            {
                decompressedData = binaryReader.ReadBytes(DecompressedSize);
            }

            return decompressedData;
        }

        public void WriteMetaData()
        {


            // node.Mode = _fileSystemBinaryReader.ReadUInt16();
            // var uid = _fileSystemBinaryReader.ReadUInt16();

            // node.NameOffset = _fileSystemBinaryReader.ReadInt32();
            // node.StartOffset = _fileSystemBinaryReader.ReadInt32();
            // node.DecompressedSize = _fileSystemBinaryReader.ReadInt32();
            // var date = _fileSystemBinaryReader.ReadInt32();

            // var permissions = _fileSystemBinaryReader.ReadInt32();
            // var otherPermisssion = _fileSystemBinaryReader.ReadInt32();
            // var somethinone = _fileSystemBinaryReader.ReadUInt16();
            // var something = _fileSystemBinaryReader.ReadUInt16();

            var binaryWriter = new BinaryWriter(Stream);
            var metadataPosition = PartitionOffset + LocalPosition + 4;
            Stream.Seek(metadataPosition, SeekOrigin.Begin);
            //binaryWriter.Write(0);
            binaryWriter.Write(Mode);
            //binaryWriter.Write((ushort)ExtMode);
            binaryWriter.Write(NameOffset);
            binaryWriter.Write(StartOffset);
            binaryWriter.Write(DecompressedSize);
            var date = (int)((DateTimeOffset)CreationDate).ToUnixTimeSeconds();
            binaryWriter.Write(date);
            binaryWriter.Write(UserId);
            binaryWriter.Write(GroupId);

            var nameBytes = Encoding.UTF8.GetBytes(Name);
            Stream.Seek(PartitionOffset + NameOffset, SeekOrigin.Begin);
            binaryWriter.Write(nameBytes);
            binaryWriter.Write((byte)0);
        }

        public override void Delete()
        {
            throw new NotImplementedException();
        }

        public override void Move(FileSystemNode parent)
        {

            throw new NotImplementedException();
        }

        public override FileSystemNode CreateFile(string name = null, byte[] data = null)
        {
            throw new NotImplementedException();
        }

        public override FileSystemNode CreateDirectory(string name = null)
        {
            throw new NotImplementedException();
        }

        public override void Apply()
        {
            throw new NotImplementedException();
        }

        public override FileSystemNode CreateSymlink(FileSystemNode node, string name)
        {
            throw new NotImplementedException();
        }
    }
}
