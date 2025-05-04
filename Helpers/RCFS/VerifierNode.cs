using BlackberrySystemPacker.Nodes;
using System.Security.Cryptography;
using System.Text;

namespace BlackberrySystemPacker.Helpers.RCFS
{
    public class VerifierNode
    {
        public OperatingSystemNode OriginalNode { get; set; }

        public string Magic { get; set; }

        public int FileCount { get; set; }

        public int LinkCount { get; set; }

        public string Hash { get; set; }

        public List<VerifierRecord> Records { get; set; } = new List<VerifierRecord>();

        public VerifierNode(OperatingSystemNode node)
        {
            OriginalNode = node;
            Read();
        }

        public void Read()
        {
            if (OriginalNode == null)
            {
                return;
            }

            var fileData = OriginalNode.Read();
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(fileData);
            Hash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            
            using var binaryReader = new BinaryReader(new MemoryStream(fileData));
            Magic = Encoding.UTF8.GetString(binaryReader.ReadBytes(4));
            FileCount = binaryReader.ReadInt32();
            LinkCount = binaryReader.ReadInt32();

            Records.Clear();
            var totalRecords = FileCount + LinkCount;
            for (int i = 0; i < totalRecords; i++)
            {
                var record = new VerifierRecord();
                record.Position = i * 48;
                record.NodeNumber = binaryReader.ReadInt32();
                binaryReader.ReadInt32();
                record.Size = binaryReader.ReadInt32();
                record.Hash = BitConverter.ToString(binaryReader.ReadBytes(32)).Replace("-", "").ToLower();
                record.Type = binaryReader.ReadInt32();
                Records.Add(record);
            }
        }

        public void Write()
        {
            if (OriginalNode == null)
            {
                return;
            }

            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            var magicByte = Encoding.UTF8.GetBytes(Magic);
            writer.Write(magicByte);
            writer.Write(FileCount);
            writer.Write(LinkCount);    
            for (var i = 0; i < Records.Count; i++)
            {
                var record = Records[i];
                writer.Write(record.NodeNumber);
                writer.Write(0);
                writer.Write(record.Size);
                var hashByte = Enumerable.Range(0, record.Hash.Length)
                                     .Where(x => x % 2 == 0)
                                     .Select(x => Convert.ToByte(record.Hash.Substring(x, 2), 16))
                                     .ToArray();

                writer.Write(hashByte);
                writer.Write(Records[i].Type);
            }

            var verifiedContent = memoryStream.ToArray();
            OriginalNode.Write(verifiedContent);

            var fileData = OriginalNode.Read();
            var currentPosition = writer.BaseStream.Position;
            var sha256 = SHA256.Create().ComputeHash(fileData);
            var hashString = BitConverter.ToString(sha256).Replace("-", "").ToLower();

            var streamWriter = new BinaryWriter(OriginalNode.Stream);
            streamWriter.BaseStream.Seek(OriginalNode.PartitionOffset + 48, SeekOrigin.Begin);
            streamWriter.Write(sha256);
            streamWriter.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
            Read();
        }


        public void ApplyNodeChanges(OperatingSystemNode node, byte[] data)
        {
            if(node == OriginalNode)
            {
                return;
            }

            var record = Records.FirstOrDefault(x => x.NodeNumber == node.NodeNumber);
            if(record == null)
            {
                record = new VerifierRecord();
                Records.Add(record);
            }

            var hash = SHA256.Create().ComputeHash(data);
            record.Hash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            record.Size = data.Length;
            //record.Type = node.IsSymLink() ? 0 : 128;
            Write();
        }
    }
}
