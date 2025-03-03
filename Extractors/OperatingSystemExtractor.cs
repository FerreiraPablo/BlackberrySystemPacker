using BlackberrySystemPacker.Helpers.RCFS;
using BlackberrySystemPacker.Nodes;
using System.Text;

namespace BlackberrySystemPacker.Extractors
{
    public class OperatingSystemExtractor : IExtractor
    {
        public int _nodesOffset = 0;

        public bool AlwaysRead = false;
        
        private long _startPosition;

        private BinaryReader _fileSystemBinaryReader;
        public OperatingSystemNode VerifierFile { get; set; }

        public List<FileSystemNode> GetNodes(BinaryReader reader, long startPosition)
        {
            _startPosition = startPosition;
            _fileSystemBinaryReader = reader;

            _fileSystemBinaryReader.BaseStream.Seek(_startPosition, SeekOrigin.Begin);
            var magic = Encoding.UTF8.GetString(_fileSystemBinaryReader.ReadBytes(4));
            var version = _fileSystemBinaryReader.ReadInt32();
            var zone = Encoding.UTF8.GetString(_fileSystemBinaryReader.ReadBytes(8));
            var headerStart = _startPosition + 4096;


            _fileSystemBinaryReader.BaseStream.Seek(headerStart, SeekOrigin.Begin);
            var volumeId = new Guid(_fileSystemBinaryReader.ReadBytes(16));
            _fileSystemBinaryReader.ReadBytes(16);
            //_fileSystemBinaryReader.BaseStream.Seek(headerStart + 32, SeekOrigin.Begin);
            var magicx = Encoding.UTF8.GetString(_fileSystemBinaryReader.ReadBytes(8));
            var unk1 = _fileSystemBinaryReader.ReadInt32();
            var unk2 = _fileSystemBinaryReader.ReadInt32();

            _fileSystemBinaryReader.BaseStream.Seek(headerStart + 56, SeekOrigin.Begin);


            _nodesOffset = _fileSystemBinaryReader.ReadInt32();
            _fileSystemBinaryReader.ReadInt32();
            _fileSystemBinaryReader.ReadInt32();

            var nodes = GetFileNodes(ReadNodes(_nodesOffset, 1));

            var imageHash = GetImageHash();
            var verifierNode = new VerifierNode(VerifierFile);

            var same = imageHash == verifierNode.Hash;
            foreach (var node in nodes)
            {
                node.Verifier = verifierNode;
            }

            return [.. nodes];
        }


        public string GetImageHash()
        {
            var currentPosition = _fileSystemBinaryReader.BaseStream.Position;
            var hashPosition = _startPosition + 48;
            _fileSystemBinaryReader.BaseStream.Seek(_startPosition + 48, SeekOrigin.Begin);
            var sha256 = _fileSystemBinaryReader.ReadBytes(32);
            _fileSystemBinaryReader.BaseStream.Seek(hashPosition, SeekOrigin.Begin);
            return BitConverter.ToString(sha256).Replace("-", "").ToLower();
        }

        private List<OperatingSystemNode> GetFileNodes(OperatingSystemNode[] nodes, OperatingSystemNode parentNode = null)
        {
            List<OperatingSystemNode> files = new List<OperatingSystemNode>();
            foreach (OperatingSystemNode node in nodes)
            {
                node.Path = parentNode?.FullPath ?? "";
                if (parentNode != null)
                {
                    node.Parent = parentNode;
                }

                node.Stream = _fileSystemBinaryReader.BaseStream;
                files.Add(node);
                if (node.IsDirectory())
                {
                    var directory = node.Path + (node.Path.Length == 0 ? "" : "/") + node.Name;
                    var fileCount = node.DecompressedSize / 32;
                    var childNodes = GetFileNodes(ReadNodes(node.StartOffset, fileCount), node);
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

        private OperatingSystemNode[] ReadNodes(int offset, int nodeQuantity)
        {
            List<OperatingSystemNode> list = new List<OperatingSystemNode>();
            for (int i = 0; i < nodeQuantity; i++)
            {
                var localPosition = offset + i * 32;
                var definitionPosition = localPosition + _startPosition;
                OperatingSystemNode node = new OperatingSystemNode();
                node.NodeNumber = ((localPosition - _nodesOffset) / 32) + 1;
                node.Stream = _fileSystemBinaryReader.BaseStream;
                node.LocalPosition = localPosition;
                node.PartitionOffset = _startPosition;



                //var dasdo = _fileSystemBinaryReader.ReadUInt16();
                _fileSystemBinaryReader.BaseStream.Seek(definitionPosition, SeekOrigin.Begin);

                _fileSystemBinaryReader.ReadInt32();
                node.Mode = _fileSystemBinaryReader.ReadInt32();
                //node.ExtMode = _fileSystemBinaryReader.ReadUInt16();
                node.NameOffset = _fileSystemBinaryReader.ReadInt32();
                node.StartOffset = _fileSystemBinaryReader.ReadInt32();
                node.DecompressedSize = _fileSystemBinaryReader.ReadInt32();
                node.CreationDate = DateTime.UnixEpoch.AddSeconds(_fileSystemBinaryReader.ReadInt32()).ToLocalTime();
                node.UserId = _fileSystemBinaryReader.ReadInt32();
                node.GroupId = _fileSystemBinaryReader.ReadInt32();

                var namePosition = node.NameOffset + _startPosition;
                _fileSystemBinaryReader.BaseStream.Seek(namePosition, SeekOrigin.Begin);
                string text = "";
                for (byte charByte = _fileSystemBinaryReader.ReadByte(); charByte != 0; charByte = _fileSystemBinaryReader.ReadByte())
                {
                    text += Convert.ToChar(charByte);
                }
                node.Name = text;

                if(node.Name == "verifier.log")
                {
                    VerifierFile = node;
                }

                list.Add(node);
            }
            return list.ToArray();
        }
    }
}