using System.Text;
using BlackberrySystemPacker.Helpers.SystemImages;
using BlackberrySystemPacker.Nodes;

namespace BlackberrySystemPacker.Helpers.QNX6
{
    public class QNX6NodeStream : Stream
    {
        public List<int> AvailableBlocks = new List<int>();


        public static byte[] SharedBuffer = new byte[262144];

        public List<int> AllocatedBlocks = new List<int>();

        public int HighestSector = 0;
        public UserSystemNode HighestSectorNode = null;
        public List<int> LongFilenameSectors = new List<int>();
        private BinaryReader _reader;
        private BinaryWriter _writer;

        public int NodeIndex = 1;
        public UserSystemNode[] Nodes;

        private long _startOffset;
        private SectorOffset _superBlockOffset;
        private SectorOffset _secondSuperBlockOffset;
        private QNXSuperBlock _superBlock;
        
        private Stream _originStream;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => -1L;


        public override long Position
        {
            get
            {
                return -1L;
            }
            set
            {
            }
        }

        public QNX6NodeStream(Stream originStream = null, long startPoisition = 0)
        {
            _originStream = originStream;
            _reader = new BinaryReader(_originStream);
            _writer = new BinaryWriter(_originStream);
            _startOffset = startPoisition;
            _superBlockOffset = new SectorOffset()
            {
                Start = 0,
                End = 0x1000,
                Offset = _startOffset + 8192,
            };

            _superBlock = QNXSuperBlock.GetFromStream(originStream, _superBlockOffset.OffsetStart);
            Nodes = new UserSystemNode[_superBlock.NodeCount + 1];

            _secondSuperBlockOffset = new SectorOffset()
            {
                Start = 0,
                End = 0x1000,
                Offset = GetSectorOffset(_superBlock.NodesBitmap.GetAsInt()[0])
            };


            Init();

            //_superBlock.OnChange += (sender) =>
            //{
            //    _superBlock.WriteToStream(originStream, _superBlockOffset.OffsetStart);
            //};


            SectorOffset firstSystemBlockOffset = new SectorOffset()
            {
                Start = 0,
                End = (_superBlock.NodeCount) * 128,
                Offset = _superBlockOffset.OffsetEnd,
            };

            SectorOffset secondSystemBlockOffset = new SectorOffset()
            {
                Start = 0,
                End = _superBlock.NodesBitmap.Size,
                Offset = firstSystemBlockOffset.OffsetEnd + firstSystemBlockOffset.Size
            };


            //516510088
            DefineLostAndFound();
            //DefineBlockOcupation();
        }


        public int GetNodeMetadataLocation(int nodeNumber)
        {
            var superBlockEnd = _superBlockOffset.OffsetEnd;
            return (int)(superBlockEnd + ((nodeNumber-1) * 128));
        }

        public void Init()
        {
            var baseNode = _superBlock.GetAsNode();
            baseNode.Stream = _originStream;
            Nodes[0] = baseNode;
            baseNode.NodeStream = this;
            GetNodeContent(baseNode);

            var minBlock = GetSectorOffset(baseNode.Sectors[0]);
            var txt = _superBlockOffset.OffsetEnd + ((_superBlock.NodeCount) * 128);

        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return -1L;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int _, int count)
        {
            var sectorSize = _superBlock.BlockSize;
            for (int i = 0; i * 128 < count; i++)
            {
                var nodePosition = i * 128;
                var sectorInfoLocation = nodePosition + 36;
                var sector = BitConverter.ToInt32(buffer, sectorInfoLocation) != 0;
                if (sector)
                {
                    UserSystemNode node = new UserSystemNode();
                    
                    node.Size = BitConverter.ToInt32(buffer, nodePosition);
                    node.UserId = BitConverter.ToUInt16(buffer, nodePosition + 8);
                    node.GroupId = BitConverter.ToUInt16(buffer, nodePosition + 12);
                    node.CreationDate = DateTime.UnixEpoch.AddSeconds(BitConverter.ToInt32(buffer, nodePosition + 16));
                    node.Mode = BitConverter.ToUInt16(buffer, nodePosition + 32);
                    node.LinkNumber = BitConverter.ToUInt16(buffer, nodePosition + 34);
                    node.Levels = buffer[nodePosition + 100];
                    node.Status = buffer[nodePosition + 101];
                    var crypt = BitConverter.ToInt32(buffer, nodePosition + 104);
                    node.ExtMode = BitConverter.ToInt32(buffer, nodePosition + 108);


                    node.Stream = _originStream;
                    node.NodeStream = this;
                    node.Parent = Nodes[1];
                    node.NodeNumber = NodeIndex;
                    Nodes[NodeIndex] = node;

                    for (int j = 0; j < 16; j++)
                    {

                        var sectorPosition = BitConverter.ToInt32(buffer, sectorInfoLocation + (j * 4));
                        node.Sectors[j] = sectorPosition;
                        if (sectorPosition > -1)
                        {
                            AllocatedBlocks.Add(sectorPosition);
                        }
                        if (node.Sectors[j] > HighestSector)
                        {
                            HighestSector = node.Sectors[j];
                            HighestSectorNode = node;
                        }
                    }
                }

                NodeIndex++;
            }
        }

        private void DefineLostAndFound()
        {
            var nameBlocks = _superBlock.LongFileNames.GetAsInt().Where(x => x != -1);
            foreach (int index in nameBlocks)
            {
                if (index == -1)
                {
                    break;
                }
                var lostAndFoundAddress = GetSectorOffset(index);
                //AllocatedBlocks.Add(index);
                _reader.BaseStream.Seek(lostAndFoundAddress, SeekOrigin.Begin);
                for (int j = 0; j < 1024; j++)
                {
                    int nodePosition = _reader.ReadInt32();
                    if (nodePosition > 0)
                    {
                        LongFilenameSectors.Add(nodePosition);
                    }
                }
            }
        }

        public int GetUnallocatedBlock()
        {
            if(AvailableBlocks.Count > 0)
            {
                var block = AvailableBlocks[0];
                return block;
            }

            var allocatedBlocks = AllocatedBlocks.OrderBy(x => x).ToList();

            for (var i = 1; i < allocatedBlocks.Count; i++)
            {
                var currentBlock = allocatedBlocks[i];
                var previousBlock = allocatedBlocks[i - 1];
                if (currentBlock - previousBlock > 1)
                {
                    for (var j = previousBlock + 1; j < currentBlock; j++)
                    {
                        var isFree = true;
                        var offset = GetSectorOffset(j);
                        _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        for (var z = 0; z < _superBlock.BlockSize; z++)
                        {
                            if(_reader.ReadByte() != 0)
                            {
                                isFree = false;
                                break;
                            }
                        }

                        if (isFree)
                        {
                            return j;
                        }
                    }
                }
            }

            var sectors = _superBlock.NodesBitmap.GetAsInt();

            var bitmapSectors = GetSectorDefinitions(GetValidSectors(sectors, _superBlock.NodesBitmap.Levels), _superBlock.NodesBitmap.Size);

            var consumed = new List<bool>();

            var minBlock = AllocatedBlocks.Min();

            for (var sector = 0; sector < bitmapSectors.Count; sector++)
            {
                var sectorDefinition = bitmapSectors[sector];
                var sectorPosition = sectorDefinition.Key;
                var sectorSize = sectorDefinition.Value;
                _reader.BaseStream.Seek(GetSectorOffset(sectorPosition), SeekOrigin.Begin);
                for (int i = 0; i < sectorSize; i++)
                {
                    byte sectorByte = _reader.ReadByte();
                    for (int j = 0; j < 8; j++)
                    {

                        var block = (i * 8) + j + (sector * 8);
                        var bit = (sectorByte & (1 << j)) != 0 || block <= minBlock;
                        if (!bit)
                        {
                            if(AllocatedBlocks.Contains(block))
                            {
                                continue;
                            }

                            var offset = GetSectorOffset(block);
                            return block;
                        }

                        consumed.Add(bit);
                    }
                }
            }

            return -1;
        }


        public byte[] ReadNode(int nodeNumber)
        {
            var offset = (int)(_superBlockOffset.OffsetEnd + ((nodeNumber - 1) * 128));
            var buffer = new byte[128];
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            _reader.Read(buffer, 0, 128);

            return buffer;
        }

        public void WriteNode(UserSystemNode node)
        {
            var previousNodeData = ReadNode(node.NodeNumber);
            var existing = previousNodeData[0] != 0;
            var nodeData = new byte[128];

            if(node.Status != UserSystemNode.StatusDeleted) { 
                using var nodeDataWriter = new BinaryWriter(new MemoryStream(nodeData));
                // 0-8: Size
                nodeDataWriter.Write((long)node.Size);

                // 8-12: UserId
                nodeDataWriter.Write(node.UserId);

                // 12-16: GroupId
                nodeDataWriter.Write(node.GroupId);

                // 16-32: CreationDate
                var date = (int)((DateTimeOffset)node.CreationDate).ToUnixTimeSeconds();
                nodeDataWriter.Write(date);
                nodeDataWriter.Write(date);
                nodeDataWriter.Write(date);
                nodeDataWriter.Write(date);

                // 32-36: NodeNumber
                nodeDataWriter.Write((ushort)node.Mode);
                nodeDataWriter.Write((ushort)(node.LinkNumber));

                // 36-100 Pointers
                for (int j = 0; j < 16; j++)
                {
                    var sector = j >= node.Sectors.Length ? -1 : node.Sectors[j];
                    if (sector == -1)
                    {
                        nodeDataWriter.Write((byte)0xFF);
                        nodeDataWriter.Write((byte)0xFF);
                        nodeDataWriter.Write((byte)0xFF);
                        nodeDataWriter.Write((byte)0xFF);
                    }
                    else
                    {
                        nodeDataWriter.Write(sector);
                    }
                }

                // 100: Levels
                nodeDataWriter.Write((byte)node.Levels);

                // 101: Status
                nodeDataWriter.Write(node.Status);
                nodeDataWriter.Seek(104, SeekOrigin.Begin);
                nodeDataWriter.Write((ushort)node.ExtMode);
            }

            var firstSuperBlockOffset = (int)(_superBlockOffset.OffsetEnd + ((node.NodeNumber - 1) * 128));
            _writer.Seek(firstSuperBlockOffset, SeekOrigin.Begin);
            _writer.Write(nodeData);

            var secondSuperBlockOffset = (int)(_secondSuperBlockOffset.OffsetEnd + ((node.NodeNumber - 1) * 128));
            _writer.Seek(secondSuperBlockOffset, SeekOrigin.Begin);
            _writer.Write(nodeData);
        }

        public void AllocateBlock(int blockNumber)
        {
            AllocatedBlocks.Add(blockNumber);
            AvailableBlocks.Remove(blockNumber);
        }

        public string GetLongFilename(int longFilenameNode)
        {
            if (longFilenameNode >= LongFilenameSectors.Count)
            {
                return null;
            }
            var currentStreamPosition = _reader.BaseStream.Position;
            _reader.BaseStream.Seek(GetSectorOffset(LongFilenameSectors[longFilenameNode]), SeekOrigin.Begin);
            var nameSize = _reader.ReadInt16();
            var name = Encoding.ASCII.GetString(_reader.ReadBytes(nameSize), 0, nameSize);
            _reader.BaseStream.Position = currentStreamPosition;
            return name;
        }

        public int AddLongFilename(string name)
        {
            var currentReaderPosition = _reader.BaseStream.Position;
            var lostAndFoundMap = _superBlockOffset.OffsetStart + 240;
            var sectorSize = _superBlock.BlockSize;
            _reader.BaseStream.Seek(lostAndFoundMap, SeekOrigin.Begin);

            List<int> nodesNameIndexes = new List<int>();
            int currentIndex = 0;
            while (currentIndex != -1)
            {
                currentIndex = _reader.ReadInt32();
                if (currentIndex != -1)
                {
                    nodesNameIndexes.Add(currentIndex);
                }
            }

            var nameBlock = GetUnallocatedBlock();
            if (nameBlock == -1)
            {
                throw new Exception("No blocks available for that length");
            }

            AllocateBlock(nameBlock);
            _superBlock.FreeBlockCount--;

            var foundAvailableSpace = false;
            foreach (int index in nodesNameIndexes)
            {
                if (foundAvailableSpace)
                {
                    foundAvailableSpace = true;
                }

                if (index == -1)
                {
                    break;
                }
                var lostAndFoundAddress = GetSectorOffset(index);
                _reader.BaseStream.Seek(lostAndFoundAddress, SeekOrigin.Begin);
                for (int j = 0; j < 1024; j++)
                {
                    int nodePosition = _reader.ReadInt32();
                    if (nodePosition < 0)
                    {
                        _writer.BaseStream.Position -= 4;
                        _writer.Write(nameBlock);
                        AllocatedBlocks.Add(nameBlock);
                        LongFilenameSectors.Add(nameBlock);
                        foundAvailableSpace = true;
                        break;
                    }
                }
            }

            if (!foundAvailableSpace)
            {
                _reader.BaseStream.Position = currentReaderPosition;
                return -1;
            }

            _reader.BaseStream.Seek(GetSectorOffset(nameBlock), SeekOrigin.Begin);
            var nameBytes = Encoding.ASCII.GetBytes(name);
            _writer.Write((short)nameBytes.Length);
            _writer.Write(nameBytes);
            _writer.BaseStream.Position = currentReaderPosition;
            return LongFilenameSectors.Count - 1;
        }

        public int GetFreeNodeNumber()
        {
            for(var i = 1; i < Nodes.Length; i++)
            {
                if (Nodes[i] == null)
                {
                    return i;
                }
            }

            return (_superBlock.NodeCount - _superBlock.FreeNodeCount) + 1;
        }

        public QNXSuperBlock GetTopSuperBlock()
        {
            return _superBlock;
        }

        public long GetSectorOffset(int sectorPosition, int startPosition = 0)
        {
            var initialOffset = startPosition == 0 ? _superBlockOffset.OffsetEnd : startPosition;
            long blockPosition = sectorPosition * (long)_superBlock.BlockSize;
            var offset = blockPosition + initialOffset;
            return offset;
        }

        public UserSystemNode GetRootNode()
        {
            var rootNode = Nodes[1];
            var offset = GetSectorOffset(rootNode.Sectors.First());
            var rootFound = IsRootSector(offset);
            if (!rootFound)
            {
                throw new Exception("Root sector not found");
            }
            return rootNode;
        }

        private bool IsRootSector(long offset)
        {
            if (offset < -1)
            {
                return false;
            }

            var rootSectorSignature = new int[] { 1, 11777, 0, 0, 0, 0, 0, 0, 1, 3026434, 0 };
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            foreach (var number in rootSectorSignature)
            {
                var value = _reader.ReadInt32();
                if (value != number)
                {
                    return false;
                }
            }

            return true;
        }

        public void GetNodeContent(UserSystemNode node, Stream stream = null)
        {
            if (node.Size == 0)
            {
                return;
            }

            var blockSize = _superBlock.BlockSize;
            if (stream == null)
            {
                stream = this;
            }

            var originalStreamPosition = _reader.BaseStream.Position;
            var validSectors = GetValidSectors(node.Sectors, node.Levels);
            var sectorDefinitions = GetSectorDefinitions(validSectors, node.Size);

            var buffer = SharedBuffer;
            foreach (var sectorDefinition in sectorDefinitions)
            {
                var sectorPosition = sectorDefinition.Key;
                var sectorSize = sectorDefinition.Value;
                var sectorOffset = GetSectorOffset(sectorPosition);

                _reader.BaseStream.Seek(sectorOffset, SeekOrigin.Begin);
                do
                {
                    int contentSize = Math.Min(buffer.Length, sectorSize);
                    _reader.Read(buffer, 0, contentSize);
                    sectorSize -= contentSize;
                    stream.Write(buffer, 0, contentSize);
                }
                while (sectorSize > 0);
            }

            node.Size = (int)stream.Length;
            _reader.BaseStream.Position = originalStreamPosition;
        }

        public List<int> GetValidSectors(int[] sectors, int levels = 0)
        {
            var originalStreamPosition = _reader.BaseStream.Position;
            var validSectors = new List<int>();
            var firstSector = sectors.First();
            var sectorSize = _superBlock.BlockSize;
            if (firstSector > 0)
            {
                foreach (int sector in sectors)
                {
                    if (sector != -1)
                    {
                        validSectors.Add(sector);
                    }
                }
            }

            if (levels > 0)
            {
                for (int i = 1; i <= levels; i++)
                {
                    var levelSectors = validSectors;
                    validSectors = new List<int>();
                    foreach (var sector in levelSectors)
                    {
                        if (sector == -1)
                        {
                            break;
                        }
                        var sectorLocation = GetSectorOffset(sector);
                        _reader.BaseStream.Seek(sectorLocation, SeekOrigin.Begin);
                        for (int k = 0; k < sectorSize / 4; k++)
                        {
                            int foundSector = _reader.ReadInt32();
                            if (foundSector < 0)
                            {
                                break;
                            }
                            validSectors.Add(foundSector);
                        }
                    }
                }
            }

            _reader.BaseStream.Position = originalStreamPosition;
            return validSectors;
        }

        public List<KeyValuePair<int, int>> GetSectorDefinitions(List<int> validSectors, int fullSize)
        {
            var sectorDefinitions = new List<KeyValuePair<int, int>>();
            if (validSectors.Count == 0)
            {
                return sectorDefinitions;
            }

            var finalSector = validSectors.Last();

            for (int sectorIndex = 0; sectorIndex < validSectors.Count; sectorIndex++)
            {
                var sectorPosition = validSectors[sectorIndex];
                var isFinalSector = sectorPosition == finalSector;
                var sectorSizeLeft = (fullSize % _superBlock.BlockSize == 0) ? _superBlock.BlockSize : (fullSize % _superBlock.BlockSize);

                int sectorSize;
                if (isFinalSector)
                {
                    sectorSize = sectorSizeLeft;
                    sectorDefinitions.Add(new KeyValuePair<int, int>(sectorPosition, sectorSize));
                    break;
                }
                else
                {
                    sectorSize = _superBlock.BlockSize;
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
                            sectorSize += _superBlock.BlockSize;
                            continue;
                        }
                        sectorSize += sectorSizeLeft;
                        break;
                    }
                }

                sectorDefinitions.Add(new KeyValuePair<int, int>(sectorPosition, sectorSize));
            }

            return sectorDefinitions;
        }
    }
}