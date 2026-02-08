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
            int maxDiskSpace = 0;

            if (IsDirectory())
            {
            }

            if (IsCompressed())
            {
                var reader = new BinaryReader(Stream);
                Stream.Seek(PartitionOffset + StartOffset, SeekOrigin.Begin);

                try
                {
                    var blockSize = reader.ReadInt32();
                    var blockUnits = (blockSize - 4) / 4;

                    Stream.Seek((blockUnits - 1) * 4, SeekOrigin.Current);
                    var totalDataSize = reader.ReadInt32();

                    maxDiskSpace = blockSize + totalDataSize;
                }
                catch
                {
                    if (DecompressedSize > 0) throw new InvalidOperationException("Could not determine current compressed size.");
                }
            }
            else
            {
                maxDiskSpace = DecompressedSize;
            }

            using var localStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(localStream);

            if (IsCompressed())
            {
                if (data.Length > DecompressedSize)
                {
                    throw new InvalidOperationException($"New data logical size ({data.Length}) exceeds allocated logical size ({DecompressedSize}).");
                }

                var processedBytes = 0;
                var maxChunkSize = 16384;
                var chunk = new List<byte[]>();
                var compressor = new NativeLzoDecompressor();
                var compressedDataSize = 0;

                while (processedBytes < data.Length)
                {
                    var bytesLength = Math.Min(maxChunkSize, data.Length - processedBytes);
                    var blockData = new Span<byte>(data, processedBytes, bytesLength);
                    processedBytes += bytesLength;

                    byte[] dataChunk = compressor.Compress(blockData.ToArray());
                    chunk.Add(dataChunk);

                    compressedDataSize += dataChunk.Length;
                }

                var headerSize = (chunk.Count * 4) + 4;
                var totalNewSize = headerSize + compressedDataSize;

                if (totalNewSize > maxDiskSpace)
                {
                    throw new InvalidOperationException($"New compressed size ({totalNewSize}) exceeds allocated disk space ({maxDiskSpace}).");
                }


                binaryWriter.Write(headerSize);
                var lastChunkPosition = 0;

                for (var i = 0; i < chunk.Count; i++)
                {
                    lastChunkPosition += i == 0 ? chunk[i].Length + headerSize : chunk[i].Length;
                    binaryWriter.Write(lastChunkPosition);
                }

                foreach (var c in chunk) binaryWriter.Write(c);

                CompressedSize = compressedDataSize;
            }
            else
            {
                if (data.Length > maxDiskSpace)
                {
                    throw new InvalidOperationException($"New data size ({data.Length}) exceeds allocated space ({maxDiskSpace}).");
                }

                binaryWriter.Write(data);

                if (data.Length < maxDiskSpace)
                {
                    var padding = new byte[maxDiskSpace - data.Length];
                    binaryWriter.Write(padding);
                }
            }


            var finishedData = (localStream.ToArray());
            var streamWriter = new BinaryWriter(Stream);

            var contentLocation = PartitionOffset + StartOffset;
            streamWriter.BaseStream.Seek(contentLocation, SeekOrigin.Begin);

            streamWriter.Write(finishedData);
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
            }
            else
            {
                decompressedData = binaryReader.ReadBytes(DecompressedSize);
            }

            return decompressedData;
        }


        public void WriteMetaData()
        {
            var binaryWriter = new BinaryWriter(Stream);
            var metadataPosition = PartitionOffset + LocalPosition + 4;
            Stream.Seek(metadataPosition, SeekOrigin.Begin);

            binaryWriter.Write(Mode);
            binaryWriter.Write(NameOffset);
            binaryWriter.Write(StartOffset);
            binaryWriter.Write(DecompressedSize);
            var date = (int)((DateTimeOffset)CreationDate).ToUnixTimeSeconds();
            binaryWriter.Write(date);
            binaryWriter.Write(UserId);
            binaryWriter.Write(GroupId);

            var namePosition = PartitionOffset + NameOffset;
            Stream.Seek(namePosition, SeekOrigin.Begin);
            var nameBytes = Encoding.UTF8.GetBytes(Name);
            Stream.Seek(namePosition, SeekOrigin.Begin);
            binaryWriter.Write(nameBytes);
            binaryWriter.Write((byte)0);
        }

        public override void Delete()
        {
            Mode = 0;
            WriteMetaData();
        }

        public override void Move(FileSystemNode destination)
        {
            if (destination is not OperatingSystemNode osDest)
                throw new ArgumentException("Destination is not a valid OperatingSystemNode.");

            if (!osDest.IsDirectory())
                throw new ArgumentException("Destination is not a directory.");

            var freeSlotIndex = FindFreeSlot(osDest);
            if (freeSlotIndex == -1)
            {
                throw new InvalidOperationException("Destination directory is full (no empty slots available).");
            }

            var oldMode = Mode;
            var oldStartOffset = StartOffset;
            var oldDecompressedSize = DecompressedSize;
            var oldNameOffset = NameOffset;
            var oldUserId = UserId;
            var oldGroupId = GroupId;
            var oldCreationDate = CreationDate;


            Delete();
            var newLocalPosition = osDest.StartOffset + (freeSlotIndex * 32);

            this.LocalPosition = newLocalPosition;
            this.Parent = destination;
            this.PartitionOffset = osDest.PartitionOffset;

            this.Mode = oldMode;
            this.StartOffset = oldStartOffset;
            this.DecompressedSize = oldDecompressedSize;
            this.NameOffset = oldNameOffset;
            this.UserId = oldUserId;
            this.GroupId = oldGroupId;
            this.CreationDate = oldCreationDate;

            WriteMetaData();
        }

        private int FindFreeSlot(OperatingSystemNode directoryNode)
        {
            if (!directoryNode.IsDirectory()) return -1;

            var stream = directoryNode.Stream;
            var startOfNodeList = directoryNode.PartitionOffset + directoryNode.StartOffset;
            var numberOfNodes = directoryNode.DecompressedSize / 32;

            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            for (int i = 0; i < numberOfNodes; i++)
            {
                var nodePosition = startOfNodeList + (i * 32);
                stream.Seek(nodePosition + 4, SeekOrigin.Begin);
                var mode = reader.ReadInt32();

                if (mode == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public override FileSystemNode CreateFile(string name = null, byte[] data = null)
        {
            throw new NotSupportedException("Creating new files is not supported in this packed filesystem.");
        }

        public override FileSystemNode CreateDirectory(string name = null)
        {
            throw new NotSupportedException("Creating new directories is not supported in this packed filesystem.");
        }

        public override void Apply()
        {
            WriteMetaData();
        }

        public override FileSystemNode CreateSymlink(FileSystemNode node, string name)
        {
            throw new NotSupportedException("Symlink creation is not supported.");
        }

    }
}
