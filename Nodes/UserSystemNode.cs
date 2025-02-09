using System.Drawing;
using System.IO;
using System.Text;
using System.Xml.Linq;
using BlackberrySystemPacker.Helpers.QNX6;

namespace BlackberrySystemPacker.Nodes
{
    public class UserSystemNode : FileSystemNode
    {
        public bool IsUnavailable { get; set; } = true;

        public override string Name
        {
            get
            {
                if (Parent == null)
                {
                    return null;
                }

                var definitionPosition = (Parent as UserSystemNode).GetPositionOfOffset(NameOffset);
                Stream.Seek(definitionPosition, SeekOrigin.Begin);
                var binaryReader = new BinaryReader(Stream);
                binaryReader.ReadInt32();
                var nameLength = binaryReader.ReadByte();

                var name = "";
                if (nameLength == 255)
                {
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    int lostAndFoundNode = binaryReader.ReadInt32();
                    var foundName = NodeStream.GetLostAndFoundName(lostAndFoundNode);
                    name = foundName;
                }
                else
                {

                    var nameBytes = binaryReader.ReadBytes(nameLength);
                    name = Encoding.ASCII.GetString(nameBytes);
                }

                return name;
            }
            set
            {

                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                if (Name == value)
                {
                    return;
                }

                var name = value;
                var currentStreamPosition = Stream.Position;
                var definitionPosition = (Parent as UserSystemNode).GetPositionOfOffset(NameOffset);
                Stream.Seek(definitionPosition, SeekOrigin.Begin);
                var nameBytes = Encoding.ASCII.GetBytes(name);
                var binaryWriter = new BinaryWriter(Stream);
                binaryWriter.Write(NodeNumber);

                var isLongFileName = nameBytes.Length > 27;
                binaryWriter.Write((byte)(isLongFileName ? 255 : nameBytes.Length));

                if (isLongFileName)
                {
                    var namePosition = binaryWriter.BaseStream.Position + 3;
                    var foundNode = NodeStream.AddLostAndFoundName(name);
                    if (foundNode < 0)
                    {
                        throw new Exception("No space available for that name length");
                    }

                    var nam = NodeStream.GetLostAndFoundName(foundNode);
                    binaryWriter.BaseStream.Position = namePosition;
                    binaryWriter.Write(foundNode);
                }
                else
                {
                    binaryWriter.Write(nameBytes);
                }

                Stream.Seek(currentStreamPosition, SeekOrigin.Begin);

            }
        }

        public override byte[] Data { get => _data; protected set => _data = value; }

        private int _size = -1;

        private ushort _mode = 0;
        
        private int _levels = -1;
        public int NodeNumber { get; set; }

        public override int Size
        {
            get
            {
                if (IsUnavailable)
                {
                    return _size;
                }

                var streamPosition = Stream.Position;
                var binaryReader = new BinaryReader(Stream);
                binaryReader.BaseStream.Seek(MetadataPosition, SeekOrigin.Begin);
                var value = (int)binaryReader.ReadInt64();
                Stream.Seek(streamPosition, SeekOrigin.Begin);
                return value;
            }
            set
            {
                if (IsUnavailable)
                {
                    _size = value;
                    return;
                }

                if (value < 0)
                {
                    return;
                }

                if (value == Size)
                {
                    return;
                }

                var streamPosition = Stream.Position;
                var binaryWriter = new BinaryWriter(Stream);
                binaryWriter.BaseStream.Seek(MetadataPosition, SeekOrigin.Begin);
                binaryWriter.Write((long)value);
                Stream.Seek(streamPosition, SeekOrigin.Begin);
            }
        }

        public override ushort Mode
        {
            get
            {
                if (IsUnavailable)
                {
                    return _mode;
                }

                var streamPosition = Stream.Position;
                var binaryReader = new BinaryReader(Stream);
                binaryReader.BaseStream.Seek(MetadataPosition + 32, SeekOrigin.Begin);
                var data = binaryReader.ReadUInt16();
                var result = data;
                Stream.Seek(streamPosition, SeekOrigin.Begin);
                return result;
            }
            set
            {
                if (IsUnavailable)
                {
                    _mode = value;
                    return;
                }


                if (value < 0)
                {
                    return;
                }

                if (value == Mode)
                {
                    return;
                }

                var streamPosition = Stream.Position;
                var binaryWriter = new BinaryWriter(Stream);
                binaryWriter.BaseStream.Seek(MetadataPosition + 32, SeekOrigin.Begin);
                binaryWriter.Write((ushort)value);
                Stream.Seek(streamPosition, SeekOrigin.Begin);
            }
        }

        public int Levels
        {
            get
            {
                if (IsUnavailable)
                {
                    return _levels;
                }

                var streamPosition = Stream.Position;
                var binaryReader = new BinaryReader(Stream);
                binaryReader.BaseStream.Seek(MetadataPosition + 100, SeekOrigin.Begin);
                var result = binaryReader.ReadByte();
                Stream.Seek(streamPosition, SeekOrigin.Begin);
                return result;
            }
            set
            {
                if (IsUnavailable)
                {
                    _levels = value;
                    return;
                }

                if (value < 0 || value > 255)
                {
                    return;
                }

                if (value == Levels)
                {
                    return;
                }

                var streamPosition = Stream.Position;
                var binaryWriter = new BinaryWriter(Stream);
                binaryWriter.BaseStream.Seek(MetadataPosition + 100, SeekOrigin.Begin);
                binaryWriter.Write((byte)value);
                Stream.Seek(streamPosition, SeekOrigin.Begin);
            }
        }

        public int MetadataPosition => NodeStream.GetNodeMetadataLocation(NodeNumber);

        public int NameOffset { get; set; }


        public int NextNodeDefinitionOffset = 0;


        public int[] Sectors = new int[16];

        public int SectorSize => NodeStream.GetTopSuperBlock().BlockSize;

        public QNX6NodeStream NodeStream { get; set; }

        public byte[] _data = null;


        public override byte[] Read()
        {
            //if (Data != null && Data.Length > 0)
            //{
            //    return Data;
            //}

            var binaryReader = new BinaryReader(Stream);
            byte[] buffer = QNX6NodeStream.SharedBuffer;
            var sectorDefinitions = GetSectorDefinitions(binaryReader);

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

            Data = outputStream.ToArray();
            Size = Data.Length;
            return Data;
        }

        public override void Write(byte[] data)
        {
            var binaryReader = new BinaryReader(Stream);
            var binaryWriter = new BinaryWriter(Stream);
            using var inputStream = new MemoryStream(data);

            var sectorQueue = new Queue<KeyValuePair<int, int>>();
            var validSectors = NodeStream.GetValidSectors(Sectors, Levels);
            var existingSectorsLeft = validSectors.ToList();
            var size = data.Length;

            do
            {
                var sectorSize = Math.Min(SectorSize, size);
                if (existingSectorsLeft.Count > 0)
                {
                    sectorQueue.Enqueue(new KeyValuePair<int, int>(existingSectorsLeft[0], sectorSize));
                    existingSectorsLeft.RemoveAt(0);
                }
                else
                {
                    var nonAllocatedBlock = NodeStream.GetUnallocatedBlock();
                    if (nonAllocatedBlock == -1)
                    {
                        throw new Exception("There are no free blocks for data storage");
                    }

                    sectorQueue.Enqueue(new KeyValuePair<int, int>(nonAllocatedBlock, sectorSize));
                    NodeStream.AllocateBlock(nonAllocatedBlock);
                }

                size -= sectorSize;
            } while (size > 0);


            while (sectorQueue.Count < validSectors.Count)
            {
                var lastSectorDefinition = validSectors.Last();
                var offset = NodeStream.GetSectorOffset(lastSectorDefinition);
                var emptyData = new byte[SectorSize];
                Stream.Write(emptyData, 0, emptyData.Length);
                NodeStream.AvailableBlocks.Add(lastSectorDefinition);
                Console.WriteLine("Space Free" + NodeStream.AvailableBlocks.Count * 4096);
                validSectors.Remove(lastSectorDefinition);
            }

            var usableSectors = sectorQueue.ToList();
            binaryWriter.Seek(MetadataPosition + 36, SeekOrigin.Begin);
            for (int j = 0; j < 16; j++)
            {
                if (!sectorQueue.Any())
                {
                    binaryWriter.Write((byte)0xFF);
                    binaryWriter.Write((byte)0xFF);
                    binaryWriter.Write((byte)0xFF);
                    binaryWriter.Write((byte)0xFF);
                }
                else
                {
                    binaryWriter.Write(sectorQueue.Dequeue().Key);
                }
            }

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

            var sectors = usableSectors.Select(x => x.Key);
            var sectorArray = new int[16];

            for (int i = 0; i < 16; i++)
            {
                if (i < sectors.Count())
                {
                    sectorArray[i] = sectors.ElementAt(i);
                }
                else
                {
                    sectorArray[i] = -1;
                }
            }
            Sectors = sectorArray;
            Data = data;
            Size = data.Length;
        }

        private FileSystemNode Create(int mode, string name = null, byte[] data = null)
        {
            if (!IsDirectory())
            {
                return null;
            }

            if(!NodeStream.AvailableBlocks.Any())
            {
                Console.WriteLine("No space available for data storage");
                return null;
            }


            if (data == null)
            {
                data = Array.Empty<byte>();
            }
            var randomName = name ?? "NewFile" + new Random().Next(0, 1000);
            var nodeNumber = NodeStream.GetFreeNodeNumber();
            var nameOffset = NextNodeDefinitionOffset;


            var definition = new byte[32];
            using var binaryWriter = new BinaryWriter(new MemoryStream(definition));
            binaryWriter.Write(nodeNumber);
            IncludeMetadata(definition);

            var childNode = new UserSystemNode();
            NodeStream.Nodes[nodeNumber] = childNode;
            childNode.IsUnavailable = false;
            childNode.Stream = Stream;
            childNode.NodeStream = NodeStream;
            childNode.Parent = this;
            childNode.NameOffset = nameOffset;
            childNode.NodeNumber = nodeNumber;

            if (mode == 16893)
            {
                data = new byte[4096];
                using var structureWriter = new BinaryWriter(new MemoryStream(data));

                structureWriter.Seek(0, SeekOrigin.Begin);
                var currentDirName = ".";
                structureWriter.Write(childNode.NodeNumber);
                structureWriter.Write((byte)currentDirName.Length);
                structureWriter.Write(Encoding.ASCII.GetBytes(currentDirName));

                structureWriter.Seek(32, SeekOrigin.Begin);
                var parentDirName = "..";
                structureWriter.Write(NodeNumber);
                structureWriter.Write((byte)parentDirName.Length);
                structureWriter.Write(Encoding.ASCII.GetBytes(parentDirName));
                childNode.Children = new List<FileSystemNode>();
            }

            childNode.Size = data.Length;
            childNode.Mode = (ushort)mode;
            childNode.Path = FullPath;
            childNode.Name = name;


            var contentWriter = new BinaryWriter(Stream);
            //NodeStream.GetTopSuperBlock().FreeBlockCount--;
            contentWriter.Seek(childNode.MetadataPosition + 8, SeekOrigin.Begin);
            contentWriter.Write(88);
            contentWriter.Write(88);

            contentWriter.Seek(childNode.MetadataPosition + 101, SeekOrigin.Begin);
            contentWriter.Write((byte)1);

            contentWriter.Seek(childNode.MetadataPosition + 16, SeekOrigin.Begin);
            var date = 1519233000; //(int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            contentWriter.Write(date);
            contentWriter.Write(date);
            contentWriter.Write(date);
            contentWriter.Write(date);
            childNode.Write(data);


            
            NextNodeDefinitionOffset += 32;
            Children = Children.Append(childNode);
            return childNode;
        }

        public override void Delete()
        {
            if (string.IsNullOrEmpty(Name))
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
            var writer = new BinaryWriter(Stream);
            var statusPosition = MetadataPosition + 101;
            writer.Seek(statusPosition, SeekOrigin.Begin);
            writer.Write((byte)2);
            RemoveParentMetadata();
            Parent.Children = Parent.Children.Where(c => c != this);
            (Parent as UserSystemNode).NextNodeDefinitionOffset -= 32;
            return;
        }


        private (UserSystemNode, string) EnsurePathAvailability(string name)
        {
            var path = name.Replace(FullPath, "").Split("/");
            var fileName = path[path.Length - 1];
            var directories = path.Take(path.Length - 1).Where(x => x != Name && !string.IsNullOrWhiteSpace(x));
            var checkedPath = "";
            var ownerDirectory = this;
            foreach (var directory in directories)
            {
                checkedPath += "/" + directory;

                var expectedFullPath = FullPath + checkedPath;
                var existingNode = ownerDirectory.Children.FirstOrDefault(x => x != null && x.FullPath == expectedFullPath);
                if (existingNode == null)
                {
                    ownerDirectory = ownerDirectory.CreateDirectory(directory) as UserSystemNode;
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
            var (ownerDirectory, fileName) = EnsurePathAvailability(name);
            var file = ownerDirectory.Create(33184, fileName, data); ;
            return file;
        }

        public override FileSystemNode CreateDirectory(string name = null)
        {

            if (!IsDirectory() || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            var (ownerDirectory, fileName) = EnsurePathAvailability(name);

            var node = Create(16893, fileName, null) as UserSystemNode;
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

        public void OverwriteEntityData(byte[] data)
        {
            Data = data;
        }

        protected List<KeyValuePair<int, int>> GetSectorDefinitions(BinaryReader binaryReader)
        {
            var sectorDefinitions = new List<KeyValuePair<int, int>>();
            var validSectors = NodeStream.GetValidSectors(Sectors, Levels);
            if (validSectors.Count == 0)
            {
                return sectorDefinitions;
            }

            var finalSector = validSectors.Last();

            for (int sectorIndex = 0; sectorIndex < validSectors.Count; sectorIndex++)
            {
                var sectorPosition = validSectors[sectorIndex];
                var isFinalSector = sectorPosition == finalSector;
                var SectorSizeLeft = (Size % SectorSize == 0) ? SectorSize : (Size % SectorSize);

                int sectorSize;
                if (isFinalSector)
                {
                    sectorSize = SectorSizeLeft;
                    sectorDefinitions.Add(new KeyValuePair<int, int>(sectorPosition, sectorSize));
                    break;
                }
                else
                {
                    sectorSize = SectorSize;
                    var startingPoint = sectorIndex;
                    var nextValidSectorPosition = validSectors[sectorIndex + 1];
                    var missingSectorsBetween = nextValidSectorPosition - sectorPosition;
                    while (missingSectorsBetween == sectorIndex + 1 - startingPoint)
                    {
                        sectorIndex++;
                        var currentSector = validSectors[sectorIndex];
                        isFinalSector = currentSector != finalSector;
                        if (isFinalSector)
                        {
                            sectorSize += SectorSize;
                            continue;
                        }
                        sectorSize += SectorSizeLeft;
                        break;
                    }
                }

                sectorDefinitions.Add(new KeyValuePair<int, int>(sectorPosition, sectorSize));
            }

            return sectorDefinitions;
        }

        protected void IncludeMetadata(byte[] metadata)
        {
            if (!this.IsDirectory())
            {
                return;
            }
            var definitionPosition = GetPositionOfOffset(NextNodeDefinitionOffset);
            var contentWriter = new BinaryWriter(Stream);
            contentWriter.BaseStream.Seek(definitionPosition, SeekOrigin.Begin);
            contentWriter.Write(metadata);
            NextNodeDefinitionOffset += 32;
        }

        protected void ClearSectors(BinaryWriter binaryWriter)
        {
            var validSectors = new List<int>();
            var firstSector = Sectors.First();
            if (firstSector > 0)
            {
                foreach (int sector in Sectors)
                {
                    if (sector != -1)
                    {
                        validSectors.Add(sector);
                    }
                }
            }

            if (Levels > 0)
            {
                for (int i = 1; i <= Levels; i++)
                {
                    var levelSectors = validSectors;
                    validSectors = new List<int>();
                    foreach (var sector in levelSectors)
                    {
                        if (sector == -1)
                        {
                            break;
                        }
                        binaryWriter.BaseStream.Seek(NodeStream.GetSectorOffset(sector), SeekOrigin.Begin);
                        for (int k = 0; k < SectorSize / 4; k++)
                        {
                            binaryWriter.Write(-1);
                        }
                    }
                }
            }
        }

        private byte[] RemoveParentMetadata()
        {
            var binaryReader = new BinaryReader(Stream);
            var definitionStart = NameOffset;
            var definitionEnd = definitionStart + 32;

            var parent = Parent as UserSystemNode;
            var parentDefinitionStart = parent.GetPositionOfOffset(0);
            Stream.Seek(parentDefinitionStart, SeekOrigin.Begin);
            var modifiedParentDefinition = new List<byte>();
            var removedMetaData = new List<byte>();

            var validSectors = NodeStream.GetValidSectors(parent.Sectors, parent.Levels);
            var blockSize = NodeStream.GetTopSuperBlock().BlockSize;
            for (var i = 0; i < validSectors.Count * blockSize; i++)
            {
                var foundData = binaryReader.ReadByte();
                if (i >= definitionStart && i < definitionEnd)
                {

                    removedMetaData.Add(foundData);
                    continue;
                }

                modifiedParentDefinition.Add(foundData);
            }

            for (var i = 0; i < 32; i++)
            {
                modifiedParentDefinition.Add((byte)0);
            }

            var data = Encoding.UTF8.GetString(modifiedParentDefinition.ToArray());
            Parent.Write(modifiedParentDefinition.ToArray());

            return removedMetaData.ToArray();
        }

        public static uint Qnx6LfileChecksum(string name, uint size)
        {
            uint crc = 0;
            int end = (int)size; // Cast size to int for indexing

            for (int i = 0; i < end; i++)
            {
                crc = ((crc >> 1) + name[i]) ^
                      ((crc & 0x00000001) != 0 ? 0x80000000 : 0);
            }
            return crc;
        }

        private void ClearData()
        {

            var binaryReader = new BinaryReader(Stream);
            var binaryWriter = new BinaryWriter(Stream);
            var sectorDefinitions = NodeStream.GetValidSectors(Sectors, Levels);

            foreach (var sectorDefinition in sectorDefinitions)
            {
                var sectorPosition = sectorDefinition;
                var sectorSize = 4096;
                NodeStream.AvailableBlocks.Add(sectorPosition);

                Console.WriteLine("Space Free" + NodeStream.AvailableBlocks.Count * 4096);

                var offset = NodeStream.GetSectorOffset(sectorPosition);
                binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

                do
                {
                    int contentSize = Math.Min(262144, sectorSize);
                    binaryWriter.Write(new byte[contentSize]);
                    sectorSize -= contentSize;
                }
                while (sectorSize > 0);
            }
            ClearSectors(binaryWriter);
        }

        public long GetDefinitionStart()
        {
            var sectors = GetSectorDefinitions(new BinaryReader(Stream));
            var sectorDefinition = sectors.First();
            var sectorPosition = sectorDefinition.Key;
            return NodeStream.GetSectorOffset(sectorPosition);
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
    }
}