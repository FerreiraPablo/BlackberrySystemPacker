using BlackberrySystemPacker.Nodes;

namespace BlackberrySystemPacker.Helpers.QNX6
{

    public class QNXSuperBlock
    {
        public static int SUPER_BLOCK_SIZE = 0x3000;

        public const int MAGIC_NUMBER_OFFSET = 0x0;

        public const int CHECKSUM_OFFSET = 0x4;

        public const int SERIAL_NUMBER_OFFSET = 0x8;

        public const int CREATION_TIMESTAMP_OFFSET = 0x10;

        public const int LAST_ACCESS_TIMESTAMP_OFFSET = 0x14;

        public const int FLAGS_OFFSET = 0x18;

        public const int VERSION_OFFSET = 0x1C;

        public const int VERSION_2_OFFSET = 0x1E;

        public const int VOLUME_ID_OFFSET = 0x20;

        public const int BLOCK_SIZE_OFFSET = 0x30;

        public const int NODE_COUNT_OFFSET = 0x34;

        public const int FREE_NODE_COUNT_OFFSET = 0x38;

        public const int BLOCK_COUNT_OFFSET = 0x3C;

        public const int FREE_BLOCK_COUNT_OFFSET = 0x40;

        public const int ALLOCATION_MAP_OFFSET = 0x44;

        public const int ROOT_NODES_OFFSET = 0x48;

        public const int ROOT_NODE_BITMAP_OFFSET = 0x98;

        public const int ROOT_LONG_FILE_NAMES_OFFSET = 0xE8;

        private byte[] _superBlockData;

        public Action<QNXSuperBlock> OnChange;

        public int BlockSize
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, BLOCK_SIZE_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, BLOCK_SIZE_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public int NodeCount
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, NODE_COUNT_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, NODE_COUNT_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public int FreeNodeCount
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, FREE_NODE_COUNT_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, FREE_NODE_COUNT_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public int BlockCount
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, BLOCK_COUNT_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, BLOCK_COUNT_OFFSET);
                OnChange?.Invoke(this);
            }
        }


        public int FreeBlockCount
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, FREE_BLOCK_COUNT_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, FREE_BLOCK_COUNT_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public int MagicNumber
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, MAGIC_NUMBER_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, MAGIC_NUMBER_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public int Checksum
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, CHECKSUM_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, CHECKSUM_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public long SerialNumber
        {
            get
            {
                return BitConverter.ToInt64(_superBlockData, SERIAL_NUMBER_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, SERIAL_NUMBER_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public DateTime CreationTimestamp
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt32(_superBlockData, CREATION_TIMESTAMP_OFFSET)).DateTime;
            }
            set
            {
                BitConverter.GetBytes(new DateTimeOffset(value).ToUnixTimeSeconds()).CopyTo(_superBlockData, CREATION_TIMESTAMP_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public DateTime LastAccessTimestamp
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt32(_superBlockData, LAST_ACCESS_TIMESTAMP_OFFSET)).DateTime;
            }
            set
            {
                BitConverter.GetBytes(new DateTimeOffset(value).ToUnixTimeSeconds()).CopyTo(_superBlockData, LAST_ACCESS_TIMESTAMP_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public int Flags
        {
            get
            {
                return BitConverter.ToInt32(_superBlockData, FLAGS_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, FLAGS_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public short Version
        {
            get
            {
                return BitConverter.ToInt16(_superBlockData, VERSION_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, VERSION_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public short Version2
        {
            get
            {
                return BitConverter.ToInt16(_superBlockData, VERSION_2_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(_superBlockData, VERSION_2_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public Guid VolumeId
        {
            get
            {
                byte[] guidData = new byte[16];
                Array.Copy(_superBlockData, VOLUME_ID_OFFSET, guidData, 0, 16);
                return new Guid(guidData);
            }
            set
            {
                value.ToByteArray().CopyTo(_superBlockData, VOLUME_ID_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public QNX6RootNode Nodes
        {
            get
            {
                byte[] rootNodeData = new byte[80];
                Array.Copy(_superBlockData, ROOT_NODES_OFFSET, rootNodeData, 0, rootNodeData.Length);
                return new QNX6RootNode(rootNodeData);
            }
            set
            {
                value.data.CopyTo(_superBlockData, ROOT_NODES_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public QNX6RootNode NodesBitmap
        {
            get
            {
                byte[] rootNodeBitmapData = new byte[80];
                Array.Copy(_superBlockData, ROOT_NODE_BITMAP_OFFSET, rootNodeBitmapData, 0, rootNodeBitmapData.Length);
                return new QNX6RootNode(rootNodeBitmapData);
            }
            set
            {
                value.data.CopyTo(_superBlockData, ROOT_NODE_BITMAP_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public QNX6RootNode LongFileNames
        {
            get
            {
                byte[] rootLongFileNamesData = new byte[80];
                Array.Copy(_superBlockData, ROOT_LONG_FILE_NAMES_OFFSET, rootLongFileNamesData, 0, rootLongFileNamesData.Length);
                return new QNX6RootNode(rootLongFileNamesData);
            }
            set
            {
                value.data.CopyTo(_superBlockData, ROOT_LONG_FILE_NAMES_OFFSET);
                OnChange?.Invoke(this);
            }
        }

        public QNXSuperBlock(byte[] superBlockData)
        {
            _superBlockData = superBlockData;
        }

        public static QNXSuperBlock GetFromStream(Stream stream, long offset)
        {
            var originalStreamPosition = stream.Position;
            byte[] superBlockData = new byte[SUPER_BLOCK_SIZE];
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(superBlockData, 0, SUPER_BLOCK_SIZE);
            stream.Seek(originalStreamPosition, SeekOrigin.Begin);
            return new QNXSuperBlock(superBlockData);
        }

        public void WriteToStream(Stream stream, long offset)
        {
            var originalStreamPosition = stream.Position;
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Write(_superBlockData, 0, SUPER_BLOCK_SIZE);
            stream.Seek(originalStreamPosition, SeekOrigin.Begin);
        }

        public UserSystemNode GetAsNode()
        {

            return new UserSystemNode()
            {
                Size = Nodes.Size,
                Sectors = Nodes.GetAsInt(),
                Levels = Nodes.Levels
            };
        }
    }
}