using BlackberrySystemPacker.Helpers.SystemImages;
using System.Text;

public class PackedImageInfo
{
    public const string MFCQ_MAGIC = "mfcq";

    public const string PCFQ_MAGIC = "pfcq";

    public const string RRCQ_MAGIC = "rrcq";

    public const string QCFP_MAGIC = "qcfp";

    public const long BLOCK_SIZE = 65536L;

	public long FullSize { get; set; }

    public long QCFMDebrickOSStart;

    public List<PartitionDefinition> Partitions = [];

    public PackedImageInfo()
	{
	}

    public static PackedImageInfo Get(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader binaryReader = new(stream);
        binaryReader.BaseStream.Seek(16, SeekOrigin.Begin);
        var headerBytesRequired = binaryReader.ReadInt32();
        stream.Seek(0, SeekOrigin.Begin);

        byte[] headerBytes = binaryReader.ReadBytes(headerBytesRequired);
        return Get(headerBytes);
    }


    public static PackedImageInfo Get(byte[] headerBytes)
    {
        var fileInfo = new PackedImageInfo();
        var fileStartingMagic = Encoding.UTF8.GetString(headerBytes, 0, 4);
        if (fileStartingMagic != MFCQ_MAGIC)
        {
            return fileInfo;
        }


        int definitionRecords = BitConverter.ToInt32(headerBytes, 12);
        int definitionBlocks = BitConverter.ToInt32(headerBytes, 16);

        List<SparsingDefinition> sparsingDefinitions = new List<SparsingDefinition>();
        for (var i = 0; i < definitionRecords; i++)
        {
            var offset = 32 + (i * 64);
            var magic = Encoding.UTF8.GetString(headerBytes, offset, 4);
            if (magic != QCFP_MAGIC)
            {
                continue;
            }

            var sparsedParition = new SparsingDefinition();
            var mysteriousNumber1 = BitConverter.ToInt32(headerBytes, offset + magic.Length);
            var records = BitConverter.ToInt32(headerBytes, offset + 12);
            var blockSize = BitConverter.ToInt32(headerBytes, offset + 16);
            var mysteriousNumber2 = BitConverter.ToInt32(headerBytes, offset + 28);
            sparsedParition.BlockSize = blockSize;

            for (var recordIndex = 0; recordIndex < records; recordIndex++)
            {
                var recordOffset = offset + 40 + (8 * recordIndex);
                var blockPosition = BitConverter.ToInt32(headerBytes, recordOffset);
                var blockCount = BitConverter.ToInt32(headerBytes, recordOffset + 4);
                if (blockCount == 0)
                {
                    continue;
                }

                var area = new SparsingArea(blockPosition, blockCount);
                sparsedParition.Areas.Add(area);
            }

            sparsedParition.Offset = sparsingDefinitions.Count > 0 ? sparsingDefinitions.Last().End : definitionBlocks;
            sparsingDefinitions.Add(sparsedParition);
        }

        List<PartitionDefinition> partitions = new List<PartitionDefinition>();
        int startingPoint = BitConverter.ToInt32(headerBytes, 24);
        var currentPosition = startingPoint;
        var partitionsLeft = true;
        while (partitionsLeft)
        {
            if(currentPosition >= definitionBlocks)
            {
                partitionsLeft = false;
                break;
            }
            var magic = Encoding.UTF8.GetString(headerBytes, currentPosition, 4);
            if (magic == PCFQ_MAGIC || magic == RRCQ_MAGIC || magic == MFCQ_MAGIC)
            {
                if (magic == PCFQ_MAGIC)
                {
                    var partition = new PartitionDefinition();
                    partition.Type = headerBytes[currentPosition + 12];

                    int totalBlocks = 0;
                    var blockSpecificationSize = headerBytes[currentPosition + 20];
                    var blockListOffset = currentPosition + headerBytes[currentPosition + 16];
                    for (int blockSpecificationIndex = 0; blockSpecificationIndex < blockSpecificationSize; blockSpecificationIndex++)
                    {
                        var blockDefinitionOffset = blockListOffset + (blockSpecificationIndex * 16);
                        var blockCount = BitConverter.ToInt32(headerBytes, blockDefinitionOffset + 12);
                        totalBlocks += blockCount;
                    }


                    partition.Size = totalBlocks * BLOCK_SIZE;
                    var lastOffset = partitions.Count > 0 ? partitions.Last().Offset : definitionBlocks;
                    partition.Offset = lastOffset + partition.Size;

                    if (sparsingDefinitions.Count > partitions.Count)
                    {
                        var sparsing = sparsingDefinitions[partitions.Count];
                        if (sparsing.Size != partition.Size)
                        {
                            throw new Exception("Sparsing definition size does not match partition size");
                        }
                        partition.Sparsing = sparsing;
                    }

                    partitions.Add(partition);
                    currentPosition += 44;
                }
                else if (magic == RRCQ_MAGIC)
                {
                    currentPosition += 16;
                }
                else if (magic == MFCQ_MAGIC)
                {
                    currentPosition += 28;
                }
            } else
            {
                partitionsLeft = false;
                break;
            }
        }
        
        if (fileInfo.Partitions.Count == 5)
        {
            fileInfo.QCFMDebrickOSStart = 0L;
        }

        fileInfo.Partitions = partitions;
        return fileInfo;
    }
}
