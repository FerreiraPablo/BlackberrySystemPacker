using BlackberrySystemPacker.Nodes;

namespace BlackberrySystemPacker.Extractors
{
    public class OperatingSystemExtractor : IExtractor
    {

        public bool AlwaysRead = false;

        private BinaryReader _fileSystemBinaryReader;

        public List<FileSystemNode> GetNodes(BinaryReader reader, long startPosition)
        {
            _fileSystemBinaryReader = reader;
            _fileSystemBinaryReader.BaseStream.Seek(4152 + startPosition, SeekOrigin.Begin);
            int offset = _fileSystemBinaryReader.ReadInt32();
            _fileSystemBinaryReader.ReadInt32();
            _fileSystemBinaryReader.ReadInt32();
            var nodes = GetFileNodes(ReadNodes(offset, 1, startPosition), startPosition);
            return [.. nodes];
        }

        private List<OperatingSystemNode> GetFileNodes(OperatingSystemNode[] nodes, long startOffset, string fileSystemDirectory = "", OperatingSystemNode parentNode = null)
        {
            List<OperatingSystemNode> files = new List<OperatingSystemNode>();
            foreach (OperatingSystemNode node in nodes)
            {
                node.Path = fileSystemDirectory;
                if (parentNode != null)
                {
                    node.Parent = parentNode;
                }

                node.Stream = _fileSystemBinaryReader.BaseStream;
                files.Add(node);
                if (node.IsDirectory())
                {
                    var directory = fileSystemDirectory + (fileSystemDirectory.Length == 0 ? "" : "/") + node.Name;
                    var childNodes = GetFileNodes(ReadNodes(node.StartOffset, node.DecompressedSize / 32, startOffset), startOffset, directory, node);
                    node.Children = childNodes;
                    files.AddRange(childNodes);
                }
                else
                {
                    if (AlwaysRead)
                    {
                        node.Read();
                    }
                }
            }
            return files;
        }

        private OperatingSystemNode[] ReadNodes(int offset, int nodeQuantity, long startOffset)
        {
            List<OperatingSystemNode> list = new List<OperatingSystemNode>();
            for (int i = 0; i < nodeQuantity; i++)
            {
                var localPosition = 4 + offset + i * 32;
                var definitionPosition = localPosition + startOffset;
                _fileSystemBinaryReader.BaseStream.Seek(definitionPosition, SeekOrigin.Begin);
                OperatingSystemNode node = new OperatingSystemNode();

                node.Stream = _fileSystemBinaryReader.BaseStream;
                node.GlobalPosition = definitionPosition;
                node.LocalPosition = localPosition;
                node.PartitionOffset = startOffset;
                
                node.Mode = (ushort)_fileSystemBinaryReader.ReadInt32();
                node.NameOffset = _fileSystemBinaryReader.ReadInt32();
                node.StartOffset = _fileSystemBinaryReader.ReadInt32();
                node.DecompressedSize = _fileSystemBinaryReader.ReadInt32();
                for (var j = 0; j < 4; j++)
                {
                    node.OtherHeaders.Add(_fileSystemBinaryReader.ReadInt32());
                }


                var namePosition = node.NameOffset + startOffset;
                _fileSystemBinaryReader.BaseStream.Seek(namePosition, SeekOrigin.Begin);
                string text = "";
                for (byte charByte = _fileSystemBinaryReader.ReadByte(); charByte != 0; charByte = _fileSystemBinaryReader.ReadByte())
                {
                    text += Convert.ToChar(charByte);
                }
                node.Name = text;
                list.Add(node);
            }
            return list.ToArray();
        }
    }
}