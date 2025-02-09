using System;
using System.Drawing;
using System.Reflection;
using System.Text;
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
        public List<int> LostAndFound = new List<int>();
        private BinaryReader _reader;
        private BinaryWriter _writer;

        public int NodeIndex = 1;
        public UserSystemNode[] Nodes;

        private long _startOffset;
        private SectorOffset _superBlockOffset;
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
            _startOffset = startPoisition;
            _superBlockOffset = new SectorOffset()
            {
                Start = 0,
                End = 0x1000,
                Offset = _startOffset + 8192,
            };

            _superBlock = QNXSuperBlock.GetFromStream(originStream, _superBlockOffset.OffsetStart);
            _superBlock.OnChange += (sender) =>
            {
                _superBlock.WriteToStream(originStream, _superBlockOffset.OffsetStart);
            };

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
            };
            secondSystemBlockOffset.Offset = firstSystemBlockOffset.OffsetEnd + firstSystemBlockOffset.Size;

            var blocks = (_superBlock.BlockCount / 8);

            var space = ((long)blocks * (long)_superBlock.BlockSize) / 1048576;


            _originStream = originStream;
            _reader = new BinaryReader(_originStream);
            _writer = new BinaryWriter(_originStream);
            Nodes = new UserSystemNode[_superBlock.NodeCount + 1];

            Init();

            DefineLostAndFound();

            var v = GetSectorDefinitions(GetValidSectors(_superBlock.NodesBitmap.GetAsInt(), _superBlock.NodesBitmap.Levels), _superBlock.NodesBitmap.Size);
            var asdda = GetSectorOffset(v[0].Key);
            var xasd = (25455 * 4096) + _superBlockOffset.OffsetEnd;

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
                var sectorInfoLocation = i * 128 + 36;
                var sector = BitConverter.ToInt32(buffer, sectorInfoLocation) != 0;
                if (sector)
                {
                    UserSystemNode node = new UserSystemNode();
                    node.IsUnavailable = true;
                    node.Size = BitConverter.ToInt32(buffer, i * 128);
                    node.Mode = BitConverter.ToUInt16(buffer, i * 128 + 32);
                    node.Levels = buffer[i * 128 + 100];
                    node.Stream = _originStream;
                    node.NodeStream = this;
                    node.Parent = Nodes[0];
                    node.NodeNumber = NodeIndex;
                    Nodes[NodeIndex] = node;

                    for (int j = 0; j < 16; j++)
                    {

                        var sectorPosition = BitConverter.ToInt32(buffer, i * 128 + 36 + j * 4);
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
                _reader.BaseStream.Seek(lostAndFoundAddress, SeekOrigin.Begin);
                for (int j = 0; j < 1024; j++)
                {
                    int nodePosition = _reader.ReadInt32();
                    if (nodePosition > 0)
                    {
                        LostAndFound.Add(nodePosition);
                        //AllocatedBlocks.Add(nodePosition);
                    }
                }
            }
        }

        public int GetUnallocatedBlock()
        {
            if(AvailableBlocks.Count > 0)
            {
                var block = AvailableBlocks[0];

                Console.WriteLine("Space Free" + AvailableBlocks.Count * 4096);
                return block;
            }

            var sortedAllocatedBlocks = AllocatedBlocks.OrderBy(x => x).ToList();
            var freeBlocksFound = new List<int>();
            var sectorSize = _superBlock.BlockSize;
            var testBytes = 64;
            for (var i = 1; i < sortedAllocatedBlocks.Count(); i++)
            {
                var currentBlock = sortedAllocatedBlocks[i];
                var previousBlock = sortedAllocatedBlocks[i - 1];
                var difference = currentBlock - previousBlock;
                if (difference > 1)
                {
                    for (var j = 0; j < difference; j++)
                    {
                        var block = previousBlock + j + 1;
                        int existingBlock = sortedAllocatedBlocks.FirstOrDefault(x => x == block);
                        if (existingBlock != 0)
                        {
                            continue;
                        }

                        _reader.BaseStream.Seek(GetSectorOffset(block), SeekOrigin.Begin);

                        var isValidBlock = true;
                        for (var z = 0; z < 4096 / testBytes; z++)
                        {
                            var content = _reader.ReadBytes(testBytes);
                            for (var x = 0; x < content.Length; x++)
                            {
                                if (content[x] != 0x00)
                                {
                                    isValidBlock = false;
                                    break;
                                }
                            }
                            if (isValidBlock)
                            {
                                break;
                            }
                        }

                        if (isValidBlock)
                        {
                            return block;
                        }
                    }
                }
            }

            return -1;
        }

        public void AllocateBlock(int blockNumber)
        {
            AllocatedBlocks.Add(blockNumber);
            AvailableBlocks.Remove(blockNumber);
        }

        public string GetLostAndFoundName(int lostAndFoundNode)
        {
            if (lostAndFoundNode >= LostAndFound.Count)
            {
                return null;
            }
            var currentStreamPosition = _reader.BaseStream.Position;
            _reader.BaseStream.Seek(GetSectorOffset(LostAndFound[lostAndFoundNode]), SeekOrigin.Begin);
            var nameSize = _reader.ReadInt16();
            var name = Encoding.ASCII.GetString(_reader.ReadBytes(nameSize), 0, nameSize);
            _reader.BaseStream.Position = currentStreamPosition;
            return name;
        }

        public int AddLostAndFoundName(string name)
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
                        LostAndFound.Add(nameBlock);
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
            return LostAndFound.Count - 1;
        }

        public int GetFreeNodeNumber()
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i] == null)
                {
                    return i;
                }
            }

            return -1;
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

                _reader.BaseStream.Seek(GetSectorOffset(sectorPosition), SeekOrigin.Begin);
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
                        _reader.BaseStream.Seek(GetSectorOffset(sector), SeekOrigin.Begin);
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