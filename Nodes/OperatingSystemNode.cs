using BlackberrySystemPacker.Decompressors;
using IronCompress;
using System.Text;

namespace BlackberrySystemPacker.Nodes
{
    public class OperatingSystemNode : FileSystemNode
    {

        public bool IsUnavailable = true;
        public override string Name { get => GetName(); set => SetName(value); }

        public long PartitionOffset = 0;

        public static IDecompressor Compressor = new UnsafeLzoDecompressor();

        private int _size = -1;

        private ushort _mode;

        public long LocalPosition = 0;

        public long GlobalPosition = 0;

        public override int Size
        {
            get
            {
                if (IsUnavailable)
                {
                    return _size;
                }

                var streamPosition = Stream.Position;
                var binaryReader = new BinaryReader(Stream);
                Stream.Seek(PartitionOffset + LocalPosition + 12, SeekOrigin.Begin);
                var value = binaryReader.ReadInt32();
                Stream.Seek(streamPosition, SeekOrigin.Begin);
                return value;
            }
            set
            {
                if (IsUnavailable)
                {
                    _size = value;
                    return;
                }

                if (value < 0)
                {
                    return;
                }

                if (value == Size)
                {
                    return;
                }

                var streamPosition = Stream.Position;
                var binaryWriter = new BinaryWriter(Stream);
                Stream.Seek(PartitionOffset + LocalPosition + 12, SeekOrigin.Begin);
                binaryWriter.Write(value);
                Stream.Seek(streamPosition, SeekOrigin.Begin);
            }
        }


        public override ushort Mode
        {
            get
            {
                if (IsUnavailable)
                {
                    return _mode;
                }

                var streamPosition = Stream.Position;
                var binaryReader = new BinaryReader(Stream);
                Stream.Seek(PartitionOffset + LocalPosition, SeekOrigin.Begin);
                var value = binaryReader.ReadUInt16();
                Stream.Seek(streamPosition, SeekOrigin.Begin);
                return value;
            }
            set
            {
                if (IsUnavailable)
                {
                    _mode = value;
                    return;
                }


                if (value < 0)
                {
                    return;
                }

                if (value == Mode)
                {
                    return;
                }

                var streamPosition = Stream.Position;
                var binaryWriter = new BinaryWriter(Stream);
                Stream.Seek(PartitionOffset + LocalPosition, SeekOrigin.Begin);
                binaryWriter.Write(value);
                Stream.Seek(streamPosition, SeekOrigin.Begin);
            }
        }


        public override byte[] Data { get => DecompressedData; protected set => DecompressedData = value; }

        public int CompressedSize;

        public byte[] DecompressedData = null;

        public int DecompressedSize;

        public List<int> OtherHeaders = new List<int>();

        public List<int> CompressedChunks = new List<int>();

        public int NameOffset;

        public int BlockSize;

        public override void Write(byte[] data)
        {
            DecompressedSize = data.Length;
            var binaryWriter = new BinaryWriter(Stream);
            if (!IsDirectory())
            {
                Stream.Seek(PartitionOffset + StartOffset, SeekOrigin.Begin);
                if (IsCompressed())
                {
                    Stream.Seek(PartitionOffset + StartOffset + BlockSize, SeekOrigin.Begin);
                    var iron = new Iron();
                    var processedBytes = 0;
                    var chunkSize = 16384;
                    var compressedSize = 0;
                    var chunk = new List<byte[]>();

                    var ironDecompressor = new NativeLzoDecompressor();
                    while (processedBytes < DecompressedSize)
                    {
                        var bytesLength = Math.Min(chunkSize, DecompressedSize - processedBytes);
                        var blockData = new Span<byte>(data, processedBytes, bytesLength);
                        var nextChunkPosition = processedBytes + bytesLength;
                        var currentChunkSize = nextChunkPosition - processedBytes;
                        processedBytes = nextChunkPosition;

                        byte[] dataChunk = ironDecompressor.Compress(blockData.ToArray());
                        chunk.Add(dataChunk);

                        compressedSize += dataChunk.Length;
                    }

                    Stream.Seek(PartitionOffset + StartOffset, SeekOrigin.Begin);
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
            }
        }

        public override byte[] Read()
        {
            if (Data != null && Data.Length > 0)
            {
                return Data;
            }

            var binaryReader = new BinaryReader(Stream);
            if (!IsDirectory())
            {
                Stream.Seek(PartitionOffset + StartOffset, SeekOrigin.Begin);
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
                    DecompressedData = outputData.ToArray();
                }
                else
                {
                    DecompressedData = binaryReader.ReadBytes(DecompressedSize);
                }
            }

            return DecompressedData;
        }


        public string GetName()
        {
            Stream.Seek(PartitionOffset + LocalPosition, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(Stream);
            binaryReader.ReadInt32();
            var nameOffset = binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            binaryReader.ReadInt32();

            var name = new StringBuilder();
            Stream.Seek(PartitionOffset + nameOffset, SeekOrigin.Begin);
            for (byte b = binaryReader.ReadByte(); b != 0; b = binaryReader.ReadByte())
            {
                name.Append(Convert.ToChar(b));
            }

            return name.ToString();
        }

        public void SetName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var previousName = GetName();
            if (previousName == name)
            {
                return;
            }

            Stream.Seek(PartitionOffset + LocalPosition, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(Stream);
            binaryReader.ReadInt32();
            var nameOffset = binaryReader.ReadInt32();
            binaryReader.ReadInt32();
            binaryReader.ReadInt32();

            var nameBytes = Encoding.UTF8.GetBytes(name);
            Stream.Seek(PartitionOffset + nameOffset, SeekOrigin.Begin);


            var binaryWriter = new BinaryWriter(Stream);
            if (nameBytes.Length > previousName.Length)
            {
                var difference = nameBytes.Length - previousName.Length;
                Stream.Seek(PartitionOffset + nameOffset + previousName.Length, SeekOrigin.Begin);
                for (var i = 0; i < difference; i++)
                {
                    binaryWriter.Write((byte)0);
                }
            }

            Stream.Seek(PartitionOffset + nameOffset, SeekOrigin.Begin);
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
    }
}
