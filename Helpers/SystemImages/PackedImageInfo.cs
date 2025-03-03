using BlackberrySystemPacker.Helpers.SystemImages;
using System.Text;

public class PackedImageInfo
{
    public const string MFCQ_MAGIC = "mfcq";

    public const string PCFQ_MAGIC = "pfcq";

    public const string RRCQ_MAGIC = "rrcq";

    public const long BLOCK_SIZE = 65536L;

	public long FullSize { get; set; }

    public long QCFMDebrickOSStart;

    public List<PartitionDefinition> Partitions;

    public PackedImageInfo()
	{
	}

    public static PackedImageInfo Get(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader binaryReader = new(stream);
        byte[] headerBytes = binaryReader.ReadBytes(8096);

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

        int definitionHeaderSize = BitConverter.ToInt32(headerBytes, 16);
        int startingPoint = BitConverter.ToInt32(headerBytes, 24);
        
        List<PartitionDefinition> partitions = new List<PartitionDefinition>();

        var initialPartition = new PartitionDefinition()
        {
            Offset = definitionHeaderSize,
        };

        partitions.Add(initialPartition);

        var partitionsLeft = true;
        var currentPosition = startingPoint;
        while (partitionsLeft)
        {
            if(currentPosition > definitionHeaderSize)
            {
                partitionsLeft = false;
            }
            var magic = Encoding.UTF8.GetString(headerBytes, currentPosition, 4);
            if(magic == PCFQ_MAGIC || magic == RRCQ_MAGIC || magic == MFCQ_MAGIC)
            {
                if(magic == PCFQ_MAGIC)
                {
                    var partition = new PartitionDefinition();
                    partition.Type = headerBytes[currentPosition + 12];

                    int blocks = 0;
                    var blockSpecificationSize = headerBytes[currentPosition + 20];
                    for (int blockSpecificationIndex = 0; blockSpecificationIndex < blockSpecificationSize; blockSpecificationIndex++)
                    {
                        var x = currentPosition + 44 + blockSpecificationIndex * 16 + 12;
                        blocks += BitConverter.ToInt32(headerBytes, x);
                    }


                    partition.Size = blocks * BLOCK_SIZE;
                    var lastOffset = partitions.Last().Offset;
                    partition.Offset = lastOffset + partition.Size;
                    partitions.Add(partition);
                }

            }

            currentPosition += 4;
        }

        partitions.RemoveAt(partitions.Count - 1);
        fileInfo.Partitions = partitions;
        if (fileInfo.Partitions.Count == 5)
        {
            fileInfo.QCFMDebrickOSStart = 0L;
        }
        return fileInfo;
    }
}
