using System.Text;
using BlackberrySystemPacker.Extractors;
using BlackberrySystemPacker.Nodes;

namespace BlackberrySystemPacker.Core
{
    public class SignedImageNodeUnpacker
    {
        public List<FileSystemNode> GetUnpackedNodes(Stream signedFileStream)
        {

            BinaryReader binaryReader = new(signedFileStream);
            var fileInfo = PackageInfo.Get(signedFileStream);
            fileInfo.FullSize = signedFileStream.Length;
            List<FileSystemNode> fileNodes = new List<FileSystemNode>();

            foreach (long boundaryOffset in fileInfo.BoundaryOffset)
            {
                if (boundaryOffset >= 0)
                {
                    var systemFiles = GetOperatingSystemFileNodes(binaryReader, boundaryOffset, fileInfo);
                    fileNodes.AddRange(systemFiles);
                }
            }

            fileNodes.AddRange(GetUserFileNodes(binaryReader, fileInfo.UserPartition, fileInfo));

            return fileNodes.Where(x => x != null && !string.IsNullOrEmpty(x.FullPath)).ToList();
        }

        private List<FileSystemNode> GetOperatingSystemFileNodes(BinaryReader binaryReader, long startOffset, PackageInfo fileInfo)
        {

            if (ValidateFileSystem(binaryReader, startOffset))
            {
                OperatingSystemExtractor extractor = new();
                var nodeList = extractor.GetNodes(binaryReader, startOffset);
                return nodeList;
            }

            return new List<FileSystemNode>();
        }

        private List<FileSystemNode> GetUserFileNodes(BinaryReader binaryReader, long startOffset, PackageInfo fileInfo)
        {
            var nodeList = new List<FileSystemNode>();

            var hasSignature = ValidateUserFileSystem(binaryReader, startOffset, fileInfo);
            if (!hasSignature)
            {
                return nodeList;
            }


            var userSystemExtractor = new UserSystemExtractor();
            nodeList.AddRange(userSystemExtractor.GetNodes(binaryReader, fileInfo.UserPartition));
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

        private bool ValidateUserFileSystem(BinaryReader binaryReader, long startOffset, PackageInfo fileInfo)
        {
            binaryReader.BaseStream.Seek(fileInfo.QCFMDebrickOSStart, SeekOrigin.Begin);
            byte[] header = binaryReader.ReadBytes(8000);
            if (binaryReader.ReadByte() == 235 && binaryReader.ReadByte() == 16 && binaryReader.ReadByte() == 144 && binaryReader.ReadByte() == 0 && binaryReader.ReadByte() == 0)
            {
                binaryReader.BaseStream.Seek(8187L, SeekOrigin.Current);
                if (binaryReader.ReadByte() == 221 && binaryReader.ReadByte() == 238 && binaryReader.ReadByte() == 230 && binaryReader.ReadByte() == 151)
                {
                    fileInfo.UserPartition = binaryReader.BaseStream.Position - 8196;
                    return true;
                }
            }

            long offset = 0L;
            binaryReader.BaseStream.Seek(fileInfo.UserPartition, SeekOrigin.Begin);
            while (fileInfo.UserPartition + 65536 * offset < binaryReader.BaseStream.Length - 200000)
            {
                try
                {
                    if (binaryReader.BaseStream.Position + 100000 >= fileInfo.FullSize)
                    {
                        return false;
                    }
                    binaryReader.BaseStream.Seek(fileInfo.UserPartition + 65536 * offset++, SeekOrigin.Begin);
                    if (binaryReader.ReadByte() == 235 && binaryReader.ReadByte() == 16 && binaryReader.ReadByte() == 144 && binaryReader.ReadByte() == 0 && binaryReader.ReadByte() == 0)
                    {
                        binaryReader.BaseStream.Seek(8187L, SeekOrigin.Current);
                        if (binaryReader.ReadByte() == 221 && binaryReader.ReadByte() == 238 && binaryReader.ReadByte() == 230 && binaryReader.ReadByte() == 151)
                        {
                            fileInfo.UserPartition = binaryReader.BaseStream.Position - 8196;
                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            binaryReader.BaseStream.Seek(fileInfo.QCFMDebrickOSStart, SeekOrigin.Begin);

            while (binaryReader.BaseStream.Position + 100000 <= fileInfo.FullSize)
            {
                if (binaryReader.ReadByte() == 235 && binaryReader.ReadByte() == 16 && binaryReader.ReadByte() == 144 && binaryReader.ReadByte() == 0 && binaryReader.ReadByte() == 0)
                {
                    binaryReader.BaseStream.Seek(8187L, SeekOrigin.Current);
                    if (binaryReader.ReadByte() == 221 && binaryReader.ReadByte() == 238 && binaryReader.ReadByte() == 230 && binaryReader.ReadByte() == 151)
                    {
                        fileInfo.UserPartition = binaryReader.BaseStream.Position - 8196;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}