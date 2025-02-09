using BlackberrySystemPacker.Helpers;
using BlackberrySystemPacker.Nodes;

public class PackageInfo
{
    public List<long> BoundaryOffset;

	public List<long> Sizes;

	public List<int> Partitions;

	public List<SectorOffset> SectorOffsets;

	public int mfcqsize;
    
    public long UserPartition { get; set; }

    public long SystemPartition;
	public long FullSize { get; set; }

    public long QCFMDebrickOSStart;

    public PackageInfo()
	{
		BoundaryOffset = new List<long>();
		Partitions = new List<int>();
		SectorOffsets = new List<SectorOffset>();
		mfcqsize = 0;
	}


    public static PackageInfo Get(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        BinaryReader binaryReader = new(stream);
        byte[] headerBytes = binaryReader.ReadBytes(8096);

        return Get(headerBytes);
    }

    public static PackageInfo Get(byte[] headerBytes)
    {
        var fileInfo = new PackageInfo();
        if (headerBytes[0] != 109 || headerBytes[1] != 102 || headerBytes[2] != 99 || headerBytes[3] != 113)
        {
            return fileInfo;
        }
        int num = (fileInfo.mfcqsize = BitConverter.ToInt32(headerBytes, 16));
        fileInfo.SystemPartition = -1L;
        int num2 = BitConverter.ToInt32(headerBytes, 24);
        BitConverter.ToInt32(headerBytes, num2 + 16);
        long num3 = 0L;
        long num4 = 0L;
        long num5 = 0L;
        fileInfo.SectorOffsets = new List<SectorOffset>();
        for (int i = num2; i < headerBytes.Length - 32; i++)
        {
            if (headerBytes[i] != 112 || headerBytes[i + 1] != 102 || headerBytes[i + 2] != 99 || headerBytes[i + 3] != 113 || headerBytes[i + 12] != 5)
            {
                continue;
            }
            if (headerBytes[i + 20] > 1)
            {
                for (int j = 0; j < headerBytes[i + 20]; j++)
                {
                    SectorOffset sectoroffsets2 = new SectorOffset();
                    int num6 = BitConverter.ToInt32(headerBytes, i + 16 * j + 52);
                    int num7 = BitConverter.ToInt32(headerBytes, i + 16 * j + 56);
                    num3 = num6 * 65536;
                    num4 = num3 - num5;
                    sectoroffsets2.Start = num3;
                    num3 += num7 * 65536;
                    sectoroffsets2.End = num3;
                    num5 += num7 * 65536;
                    sectoroffsets2.Offset = num4;
                    fileInfo.SectorOffsets.Add(sectoroffsets2);
                }
            }
            break;
        }
        fileInfo.Partitions = new List<int>();
        fileInfo.Sizes = new List<long>();
        fileInfo.BoundaryOffset = [num];
        for (int k = num2; k < headerBytes.Length - 32; k++)
        {
            if (headerBytes[k] == 112 && headerBytes[k + 1] == 102 && headerBytes[k + 2] == 99 && headerBytes[k + 3] == 113)
            {
                fileInfo.Partitions.Add(headerBytes[k + 12]);
                int num8 = 0;
                for (int l = 0; l < headerBytes[k + 20]; l++)
                {
                    num8 += BitConverter.ToInt32(headerBytes, k + 44 + l * 16 + 12);
                }
                fileInfo.Sizes.Add((long)num8 * 65536);
                var partitionIdentity = headerBytes[k + 12];
                if (partitionIdentity == 5)
                {
                    fileInfo.UserPartition = fileInfo.BoundaryOffset.Last();
                }
                if (partitionIdentity == 8 && fileInfo.SystemPartition == -1)
                {
                    fileInfo.SystemPartition = fileInfo.BoundaryOffset.Last();
                }
                fileInfo.BoundaryOffset.Add(fileInfo.BoundaryOffset.Last() + (long)num8 * 65536L);
            }
        }
        fileInfo.BoundaryOffset.Remove(fileInfo.BoundaryOffset.Last());

        List<FileSystemNode> fileNodes = new();
        if (fileInfo.Partitions.Count == 5)
        {
            fileInfo.QCFMDebrickOSStart = 0L;
        }
        return fileInfo;
    }
}
