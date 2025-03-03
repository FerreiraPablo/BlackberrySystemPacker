using System.Text;
using BlackberrySystemPacker.Extractors;
using BlackberrySystemPacker.Nodes;
using DiscUtils;

namespace BlackberrySystemPacker.Core
{
    public class SignedImageNodeUnpacker
    {
        public List<FileSystemNode> GetUnpackedNodes(Stream signedFileStream)
        {

            List<FileSystemNode> fileNodes = new List<FileSystemNode>();
            BinaryReader binaryReader = new(signedFileStream);
            signedFileStream.Seek(0, SeekOrigin.Begin);
            var headerBytes = binaryReader.ReadBytes(4);
            var magic = Encoding.UTF8.GetString(headerBytes).ToLower();

            if (headerBytes[0] == 0xEB && headerBytes[1] == 0x10 && headerBytes[2] == 0x90)
            {
                fileNodes.AddRange(GetUserFileNodes(binaryReader, 0));
            }
            else if (magic == "rimh")
            {
                fileNodes.AddRange(GetOperatingSystemFileNodes(binaryReader, 0));
            }
            else if (magic == "kdmv")
            {
                VirtualDisk disk = new DiscUtils.Vmdk.Disk(signedFileStream, DiscUtils.Streams.Ownership.None);
                var systemPartition = disk.Partitions[1].Open();
                fileNodes.AddRange(GetOperatingSystemFileNodes(new BinaryReader(systemPartition), 0));

                
                var userPartition = disk.Partitions[3].Open();
                fileNodes.AddRange(GetUserFileNodes(new BinaryReader(userPartition), 0));
            }
            else
            {
                var fileInfo = PackedImageInfo.Get(signedFileStream);
                fileInfo.FullSize = signedFileStream.Length;
                
                var systemPartition = fileInfo.Partitions.FirstOrDefault(x => x.Type == 8);
                fileNodes.AddRange(GetOperatingSystemFileNodes(binaryReader, systemPartition.Offset));

                var userPartition = fileInfo.Partitions.FirstOrDefault(x => x.Type == 6);
                fileNodes.AddRange(GetUserFileNodes(binaryReader, userPartition.Offset));
            }

            return fileNodes.Where(x => x != null && !string.IsNullOrEmpty(x.FullPath)).ToList();
        }

        private List<FileSystemNode> GetOperatingSystemFileNodes(BinaryReader binaryReader, long startOffset)
        {

            if (ValidateFileSystem(binaryReader, startOffset))
            {
                OperatingSystemExtractor extractor = new();
                var nodeList = extractor.GetNodes(binaryReader, startOffset);
                return nodeList;
            }

            return new List<FileSystemNode>();
        }

        private List<FileSystemNode> GetUserFileNodes(BinaryReader binaryReader, long startOffset)
        {
            var nodeList = new List<FileSystemNode>();
            var userSystemExtractor = new UserSystemExtractor();
            nodeList.AddRange(userSystemExtractor.GetNodes(binaryReader, startOffset));
            return nodeList;
        }

        private bool ValidateFileSystem(BinaryReader binaryReader, long startOffset)
        {
            binaryReader.BaseStream.Seek(startOffset, SeekOrigin.Begin);
            byte[] header = binaryReader.ReadBytes(8000);

            return header[0] == 102 && header[1] == 115 && header[2] == 45
                && header[3] == 111 && header[4] == 115 && header[5] == 32 && header[6] == 32 && header[7] == 32
                || header[0] == 102 && header[1] == 115 && header[2] == 45 && header[3] == 114
                && header[4] == 97 && header[5] == 100 && header[6] == 105 && header[7] == 111
                || header[4128] == 114 && header[4129] == 45 && header[4130] == 99 && header[4131] == 45 && header[4132] == 102 && header[4133] == 45 && header[4134] == 115 && Encoding.ASCII.GetString(header, 8, 8).Trim() == "fs-os";
        }
    }
}