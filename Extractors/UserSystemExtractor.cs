using BlackberrySystemPacker.Helpers.QNX6;
using BlackberrySystemPacker.Nodes;
using System.Text;

namespace BlackberrySystemPacker.Extractors
{
    public class UserSystemExtractor: IExtractor
    {
        public bool AlwaysRead = false;

        private QNX6NodeStream _nodeStream;

        private BinaryReader _fileSystemBinaryReader;

        public byte[] _sharedBuffer = new byte[262144];

        public List<FileSystemNode> GetNodes(BinaryReader reader, long startOffset)
        {
            _fileSystemBinaryReader = reader;
            _nodeStream = new QNX6NodeStream(_fileSystemBinaryReader.BaseStream, startOffset);
            GetFilesFromNode(_nodeStream.GetRootNode());
            return [.. _nodeStream.Nodes];
        }

        private void GetFilesFromNode(UserSystemNode node)
        {
            using var stream = new MemoryStream();
            _nodeStream.GetNodeContent(node, stream);
            using BinaryReader binaryReader = new BinaryReader(stream);
            var innerNodes = stream.Length / 32;
            var children = new List<UserSystemNode>();
            int nodeDefinitionOffset = 0;

            for (int i = 0; i < innerNodes; i++)
            {
                nodeDefinitionOffset = i * 32;
                binaryReader.BaseStream.Seek(nodeDefinitionOffset, SeekOrigin.Begin);
                var nodeNumber = binaryReader.ReadInt32();
                if (nodeNumber == 0)
                {
                    break;
                }

                int nodeNameSize = binaryReader.ReadByte();
                var fileName = "";

                if (nodeNameSize == 255)
                {
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    int lostAndFoundNode = binaryReader.ReadInt32();
                    fileName = _nodeStream.GetLongFilename(lostAndFoundNode);
                }
                else
                {
                    fileName = Encoding.ASCII.GetString(binaryReader.ReadBytes(nodeNameSize), 0, nodeNameSize);
                }

                if (fileName == "." || fileName == "..")
                {
                    continue;
                }

                if (nodeNumber > _nodeStream.Nodes.Length)
                {
                    nodeNumber = 1;
                }

                var childNode = _nodeStream.Nodes[nodeNumber];
                if (childNode == null)
                {
                    continue;
                }

                //childNode.Name = CleanFileName(fileName);
                childNode.Path = FixPath(node.FullPath);
                childNode.Parent = node;
                children.Add(childNode);

                if (childNode.IsDirectory())
                {
                    GetFilesFromNode(childNode);
                }
                else
                {
                    if (AlwaysRead)
                    {
                        childNode.Read();
                    }
                }
            }

            node.Children = children;
        }

        private string FixPath(string path)
        {
            string cleaned = path.TrimStart('/');
            return cleaned;
        }
    }
}