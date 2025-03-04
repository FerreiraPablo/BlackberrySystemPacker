using System.IO;
using System.Text;
using BlackberrySystemPacker.Helpers.Nodes;
using BlackberrySystemPacker.Helpers.QNX6;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlackberrySystemPacker.Nodes
{
    public class UserSystemNode : FileSystemNode
    {

        private static bool KeepPPSNodes = true;

        public static byte StatusDeleted = 2;

        public static byte StatusActive = 1;

        public byte Status = 1;

        public int NodeNumber { get; set; }

        public int LinkNumber { get; set; } = 1;

        public override int Size { get; set; }

        public int Levels { get; set; }

        public int[] Sectors = new int[16];

        public int SectorSize => NodeStream.GetTopSuperBlock().BlockSize;

        public UserSystemNode[] ParentReferences => NodeStream.Nodes.Where(x => x != null && x.Children != null && x.Children.Contains(this)).ToArray();

        public QNX6NodeStream NodeStream { get; set; }

        public byte[] _data = null;

        public override byte[] Read()
        {
            var binaryReader = new BinaryReader(Stream);
            byte[] buffer = QNX6NodeStream.SharedBuffer;
            var sectorDefinitions = NodeStream.GetSectorDefinitions(NodeStream.GetValidSectors(Sectors, Levels), Size);

            using var outputStream = new MemoryStream();
            foreach (var sectorDefinition in sectorDefinitions)
            {
                var sectorPosition = sectorDefinition.Key;
                var sectorSize = sectorDefinition.Value;

                var offset = NodeStream.GetSectorOffset(sectorPosition);
                binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

                do
                {
                    int contentSize = Math.Min(buffer.Length, sectorSize);
                    binaryReader.Read(buffer, 0, contentSize);
                    sectorSize -= contentSize;
                    outputStream.Write(buffer, 0, contentSize);
                }
                while (sectorSize > 0);
            }

            var data = outputStream.ToArray();
            Size = data.Length;
            return data;
        }

        public override void Write(byte[] data)
        {
            var binaryReader = new BinaryReader(Stream);
            var binaryWriter = new BinaryWriter(Stream);
            using var inputStream = new MemoryStream(data);

            var usableSectors = new List<KeyValuePair<int, int>>();
            var validSectors = NodeStream.GetValidSectors(Sectors, Levels);
            var existingSectorsLeft = validSectors.ToList();
            var size = data.Length;


            do
            {
                var sectorSize = Math.Min(SectorSize, size);
                if (existingSectorsLeft.Count > 0)
                {
                    usableSectors.Add(new KeyValuePair<int, int>(existingSectorsLeft[0], sectorSize));
                    existingSectorsLeft.RemoveAt(0);
                }
                else
                {
                    var nonAllocatedBlock = NodeStream.GetUnallocatedBlock();
                    if (nonAllocatedBlock == -1)
                    {
                        throw new Exception("There are no free blocks for data storage");
                    }

                    usableSectors.Add(new KeyValuePair<int, int>(nonAllocatedBlock, sectorSize));
                    NodeStream.AllocateBlock(nonAllocatedBlock);
                }

                size -= sectorSize;
            } while (size > 0);

            while (usableSectors.Count < validSectors.Count)
            {
                var lastSectorDefinition = validSectors.Last();
                var offset = NodeStream.GetSectorOffset(lastSectorDefinition);
                NodeStream.AvailableBlocks.Add(lastSectorDefinition);
                validSectors.Remove(lastSectorDefinition);
            }

            
            //if(Levels > 0)
            //{
            //    var currentWorkSectors = usableSectors.Select(x => x.Key).ToArray();
            //    for (var i = 1; i <= Levels; i++)
            //    {
            //        var currentLevelBlocks = new List<int>();
            //        var blocksRequired = (int)Math.Ceiling((double)currentWorkSectors.Length / 16);
            //        var pendingToLocate = new Queue<int>(currentWorkSectors);
            //        for (var j = 0; j < blocksRequired; j++)
            //        {
            //            var nonAllocatedBlock = NodeStream.GetUnallocatedBlock();
            //            if (nonAllocatedBlock == -1)
            //            {
            //                throw new Exception("There are no free blocks for data storage");
            //            }
            //            NodeStream.AllocateBlock(nonAllocatedBlock);
            //            currentLevelBlocks.Add(nonAllocatedBlock);

            //            var offset = NodeStream.GetSectorOffset(nonAllocatedBlock);
            //            Stream.Seek(offset, SeekOrigin.Begin);
            //            for (var z = 0; z < 1024; z++)
            //            {
            //                var sectorToWrite = pendingToLocate.Any() ? pendingToLocate.Dequeue() : -1;
            //                binaryWriter.Write(sectorToWrite);
            //            }
            //        }

            //        Sectors = currentLevelBlocks.ToArray();
            //    }
            //} else
            //{
            //}


            foreach (var sectorDefinition in usableSectors)
            {
                var sectorPosition = sectorDefinition.Key;
                var sectorSize = sectorDefinition.Value;
                var offset = NodeStream.GetSectorOffset(sectorPosition);
                Stream.Seek(offset, SeekOrigin.Begin);

                byte[] buffer = new byte[sectorSize];
                int bytesRead = inputStream.Read(buffer, 0, sectorSize);
                if (bytesRead > 0)
                {
                    Stream.Write(buffer, 0, bytesRead);
                }
            }

            double directBlockCount = (double)Size / SectorSize;
            Levels = directBlockCount <= 16 ? 0 : (int)Math.Ceiling(Math.Log(directBlockCount / 16) / Math.Log(SectorSize / 4));

            if (Levels > 0)
            {
                var currentSectors = new List<int>(usableSectors.Select(x => x.Key));
                var levelsLeft = Levels;
                while (levelsLeft > 0)
                {
                    var currentLevelBlocks = new List<int>();
                    var blocksRequired = (int)Math.Ceiling((double)currentSectors.Count / 1024);
                    var pendingToLocate = new Queue<int>(currentSectors);
                    for (var j = 0; j < blocksRequired; j++)
                    {
                        var nonAllocatedBlock = NodeStream.GetUnallocatedBlock();
                        if (nonAllocatedBlock == -1)
                        {
                            throw new Exception("There are no free blocks for data storage");
                        }
                        NodeStream.AllocateBlock(nonAllocatedBlock);
                        currentLevelBlocks.Add(nonAllocatedBlock);
                        var offset = NodeStream.GetSectorOffset(nonAllocatedBlock);
                        Stream.Seek(offset, SeekOrigin.Begin);
                        for (var z = 0; z < 1024; z++)
                        {
                            var sectorToWrite = pendingToLocate.Any() ? pendingToLocate.Dequeue() : -1;
                            binaryWriter.Write(sectorToWrite);
                        }
                    }
                    currentSectors = currentLevelBlocks;
                    levelsLeft--;
                }

                Sectors = currentSectors.ToArray();
            } else {
                Sectors = usableSectors.Select(x => x.Key).ToArray();
            }

            Size = data.Length;
            Apply();
        }

        private FileSystemNode Create(int mode, string name = null, byte[] data = null)
        {
            if (!IsDirectory())
            {
                return null;
            }

            if (data == null)
            {
                data = Array.Empty<byte>();
            }

            var randomName = name ?? "NewFile" + new Random().Next(0, 1000);
            var nodeNumber = NodeStream.GetFreeNodeNumber();


            var definition = new byte[32];
            using var binaryWriter = new BinaryWriter(new MemoryStream(definition));
            binaryWriter.Write(nodeNumber);
            IncludeMetadata(definition);

            var childNode = new UserSystemNode();
            NodeStream.Nodes[nodeNumber] = childNode;
            childNode.Stream = Stream;
            childNode.NodeStream = NodeStream;
            childNode.Parent = this;
            childNode.NodeNumber = nodeNumber;
            childNode.LinkNumber = 1;

            childNode.Size = data.Length;
            childNode.Mode = mode;
            childNode.Path = FullPath;
            childNode.Name = name;
            childNode.GroupId = GroupId;
            childNode.UserId = UserId;
            childNode.SetPermissions(GetPermissions());

            if (childNode.IsDirectory())
            {
                data = new byte[4096];
                using var structureWriter = new BinaryWriter(new MemoryStream(data));

                structureWriter.Seek(0, SeekOrigin.Begin);
                var currentDirName = ".";
                structureWriter.Write(childNode.NodeNumber);
                structureWriter.Write((byte)currentDirName.Length);
                structureWriter.Write(Encoding.ASCII.GetBytes(currentDirName));
                childNode.LinkNumber++;

                structureWriter.Seek(32, SeekOrigin.Begin);
                var parentDirName = "..";
                structureWriter.Write(NodeNumber);
                structureWriter.Write((byte)parentDirName.Length);
                structureWriter.Write(Encoding.ASCII.GetBytes(parentDirName));
                childNode.Children = new List<FileSystemNode>();
                LinkNumber++;
                Apply();
            }

            childNode.Write(data);
            Children = Children.Append(childNode).ToArray();

            NodeStream.GetTopSuperBlock().FreeNodeCount--;
            return childNode;
        }

        public override void Delete()
        {
            if (Status == StatusDeleted)
            {
                return;
            }

            if (IsDirectory() && Children != null)
            {
                var children = Children.ToList();
                foreach (var child in children)
                {
                    child.Delete();
                }
            }

            ClearData();
            NodeStream.GetTopSuperBlock().FreeNodeCount++;
            Status = StatusDeleted;
            Apply();
            var parents = ParentReferences;
            foreach (var parent in parents)
            {
                parent.RemoveChild(NodeNumber);
                if(IsDirectory())
                {
                    parent.LinkNumber--;
                    parent.Apply();
                }
            }
            Parent = null;
            return;
        }

        private (UserSystemNode, string) EnsurePathAvailability(string name)
        {
            if (string.IsNullOrWhiteSpace(FullPath))
            {
                return (this, name);
            }

            var path = name.Replace(FullPath, "").Split("/");
            var fileName = path[path.Length - 1];
            var directories = path.Take(path.Length - 1).Where(x => x != Name && !string.IsNullOrWhiteSpace(x));
            var checkedPath = "";
            var ownerDirectory = this;
            foreach (var directory in directories)
            {
                checkedPath += "/" + directory;

                var expectedFullPath = FullPath + checkedPath;
                var existingNode = NodeStream.Nodes.FirstOrDefault(x => x != null && x.FullPath == expectedFullPath);
                if (existingNode == null)
                {
                    ownerDirectory = ownerDirectory.CreateDirectory(directory) as UserSystemNode;
                }
                else
                {
                    ownerDirectory = existingNode;
                }
            }

            return (ownerDirectory, fileName);
        }

        public override FileSystemNode CreateFile(string name = null, byte[] data = null)
        {
            if (!IsDirectory() || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (NodeStream.GetUnallocatedBlock() == -1)
            {
                Console.WriteLine("No space available for data storage");
                return null;
            }

            var (ownerDirectory, fileName) = EnsurePathAvailability(name);
            var file = ownerDirectory.Create(33279, fileName, data); ;
            return file;
        }


        public override FileSystemNode CreateDirectory(string name = null)
        {

            if (!IsDirectory() || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (NodeStream.GetUnallocatedBlock() == -1)
            {
                Console.WriteLine("No space available for data storage");
                return null;
            }

            var (ownerDirectory, fileName) = EnsurePathAvailability(name);

            var node = Create(16895, fileName, null) as UserSystemNode;
            return node;
        }

        public FileSystemNode CreateSymlink(FileSystemNode node, string name)
        {
            if (!IsDirectory() || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            var (ownerDirectory, fileName) = EnsurePathAvailability(name);
            //var symlink = ownerDirectory.Create(17409, fileName, null) as UserSystemNode;
            var linkNode = (node as UserSystemNode);

            var data = new byte[32];
            using var structureWriter = new BinaryWriter(new MemoryStream(data));
            structureWriter.Seek(0, SeekOrigin.Begin);
            structureWriter.Write(linkNode.NodeNumber);
            structureWriter.Write((byte)fileName.Length);
            structureWriter.Write(Encoding.ASCII.GetBytes(fileName));
            ownerDirectory.IncludeMetadata(data);
            //symlink.Size = (byte)fileName.Length;
            //symlink.Sectors = linkNode.Sectors;
            //NodeStream.WriteNode(symlink);

            return node;
        }


        public override void Move(FileSystemNode parent)
        {
            if (!parent.IsUserNode)
            {
                return;
            }
            var realParent = parent as UserSystemNode;
            realParent.IncludeMetadata(RemoveParentMetadata());
        }

        protected void IncludeMetadata(byte[] metadata)
        {
            if (!this.IsDirectory())
            {
                return;
            }
            var currentNodes = Read();
            var nodes = currentNodes.Length / 32;
            var nodesData = new List<byte[]>();
            for (var i = 0; i < nodes; i++)
            {
                var nodePosition = i * 32;
                var currentNodeNumber = BitConverter.ToInt32(currentNodes, nodePosition);
                if (currentNodeNumber == 0)
                {
                    break;
                }


                var nodeData = new byte[32];
                Array.Copy(currentNodes, nodePosition, nodeData, 0, 32);
                nodesData.Add(nodeData);
            }

            nodesData.Add(metadata);

            for (var i = nodesData.Count(); i < nodes; i++)
            {
                var nodeData = new byte[32];
                nodesData.Add(nodeData);
            }

            var newStructure = nodesData.SelectMany(x => x).ToArray();
            Write(newStructure);
        }

        private byte[] RemoveChild(int nodeNumber)
        {
            var data = Read();
            var nodes = data.Length / 32;
            var nodesData = new List<byte[]>();
            byte[] removedNode = null;
            for (var i = 0; i < nodes; i++)
            {
                var nodePosition = i * 32;
                var currentNodeNumber = BitConverter.ToInt32(data, nodePosition);
                if (currentNodeNumber == 0)
                {
                    break;
                }

                var nodeData = new byte[32];
                Array.Copy(data, nodePosition, nodeData, 0, 32);

                if (currentNodeNumber == nodeNumber)
                {
                    removedNode = nodeData;
                    continue;
                }

                nodesData.Add(nodeData);
            }


            for (var i = nodesData.Count(); i < nodes; i++)
            {
                var nodeData = new byte[32];
                nodesData.Add(nodeData);
            }

            var newStructure = nodesData.SelectMany(x => x).ToArray();
            Write(newStructure);
            Children = Children.Where(c => (c as UserSystemNode).NodeNumber != nodeNumber).ToArray();

            return removedNode;
        }

        private byte[] RemoveParentMetadata()
        {
            return (Parent as UserSystemNode).RemoveChild(NodeNumber);
        }

        private void ClearData()
        {
            var binaryReader = new BinaryReader(Stream);
            var binaryWriter = new BinaryWriter(Stream);
            var sectorDefinitions = NodeStream.GetValidSectors(Sectors, Levels);

            foreach (var sectorDefinition in sectorDefinitions)
            {
                var sectorPosition = sectorDefinition;
                NodeStream.AvailableBlocks.Add(sectorPosition);
            }
            //ClearSectors(binaryWriter);
        }

        public long GetPositionOfOffset(int offset)
        {
            var blockIndex = Math.Floor((decimal)offset / 4096);
            var validSectors = NodeStream.GetValidSectors(Sectors, Levels);
            var sector = validSectors[(int)blockIndex];
            var blockPosition = NodeStream.GetSectorOffset(sector);
            offset = offset % 4096;
            return blockPosition + offset;
        }

        public long GetChildPosition(int nodeNumber)
        {
            var binaryReader = new BinaryReader(Stream);
            var binaryWriter = new BinaryWriter(Stream);
            var sectorDefinitions = NodeStream.GetValidSectors(Sectors, Levels);
            var data = Read();
            var nodes = data.Length / 32;
            for (var i = 0; i < nodes; i++)
            {
                var nodePosition = i * 32;
                var currentNodeNumber = BitConverter.ToInt32(data, nodePosition);
                if (currentNodeNumber == 0)
                {
                    break;
                }
                if (currentNodeNumber == nodeNumber)
                {
                    return GetPositionOfOffset(nodePosition);
                }
            }

            return -1;
        }

        private string GetRawName()
        {
            if (Parent == null)
            {
                return null;
            }

            var definitionPosition = (Parent as UserSystemNode).GetChildPosition(NodeNumber);
            if (definitionPosition == -1)
            {
                return "";
            }


            Stream.Seek(definitionPosition, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(Stream);
            var nodeNumber = binaryReader.ReadInt32();
            var nameLength = binaryReader.ReadByte();

            var name = "";
            if (nameLength == 255 || nameLength == 0)
            {
                binaryReader.ReadByte();
                binaryReader.ReadByte();
                binaryReader.ReadByte();
                int lostAndFoundNode = binaryReader.ReadInt32();
                var foundName = NodeStream.GetLongFilename(lostAndFoundNode);
                name = foundName;
            }
            else
            {

                var nameBytes = binaryReader.ReadBytes(nameLength);
                name = Encoding.ASCII.GetString(nameBytes);
            }

            return name;
        }

        public string GetName()
        {
            var name = GetRawName();

            if (!KeepPPSNodes)
            {
                if (name != null && name.Contains("@"))
                {
                    name = name.Split("@")[0];
                }
            }

            return name;
        }

        public void SetName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (Name == value)
            {
                return;
            }

            var suffix = "";


            if (!KeepPPSNodes)
            {
                var currentName = GetRawName();
                if (currentName != null && currentName.Contains("@"))
                {
                    var parts = currentName.Split("@")[1].Split(".");
                    var uid = parts[0];
                    var gid = parts[1];
                    var perms = parts[2];
                    var currentPerms = FileNodeHelper.GetPermissionsOctal(Mode);
                    suffix = $"@{UserId}.{GroupId}.{perms}";
                }
            }

            var name = value + suffix;
            var currentStreamPosition = Stream.Position;
            var definitionPosition = (Parent as UserSystemNode).GetChildPosition(NodeNumber);
            Stream.Seek(definitionPosition, SeekOrigin.Begin);
            var nameBytes = Encoding.ASCII.GetBytes(name);
            var binaryWriter = new BinaryWriter(Stream);
            binaryWriter.Write(NodeNumber);

            var isLongFileName = nameBytes.Length > 27;
            binaryWriter.Write((byte)(isLongFileName ? 255 : nameBytes.Length));

            if (isLongFileName)
            {
                var namePosition = binaryWriter.BaseStream.Position + 3;
                var foundNode = NodeStream.AddLongFilename(name);
                if (foundNode < 0)
                {
                    throw new Exception("No space available for that name length");
                }

                var nam = NodeStream.GetLongFilename(foundNode);
                binaryWriter.BaseStream.Position = namePosition;
                binaryWriter.Write(foundNode);
            }
            else
            {
                binaryWriter.Write(nameBytes);
            }

            Stream.Seek(currentStreamPosition, SeekOrigin.Begin);
        }

        public override void Apply()
        {
            SetName(Name);
            NodeStream.WriteNode(this);
        }
    }
}