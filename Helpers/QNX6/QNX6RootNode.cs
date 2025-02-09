namespace BlackberrySystemPacker.Helpers.QNX6
{
    public class QNX6RootNode
    {

        public byte[] data = new byte[0x64];

        public const int SIZE_OFFSET = 0x0;

        public const int POINTERS_OFFSET = 0x8;

        public const int LEVELS_OFFSET = 0x48;

        public const int MODE_OFFSET = 0x49;

        public int Size
        {
            get
            {
                return BitConverter.ToInt32(data, SIZE_OFFSET);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(data, SIZE_OFFSET);
            }
        }

        public byte[] Pointers
        {
            get
            {
                byte[] pointers = new byte[LEVELS_OFFSET - POINTERS_OFFSET];
                Array.Copy(data, POINTERS_OFFSET, pointers, 0, pointers.Length);
                return pointers;
            }
            set
            {
                value.CopyTo(data, POINTERS_OFFSET);
            }
        }


        public byte Levels
        {
            get
            {
                return data[LEVELS_OFFSET];
            }
            set
            {
                data[LEVELS_OFFSET] = value;
            }
        }


        public byte Mode
        {
            get
            {
                return data[MODE_OFFSET];
            }
            set
            {
                data[MODE_OFFSET] = value;
            }
        }


        public QNX6RootNode()
        {
        }

        public QNX6RootNode(byte[] data)
        {
            this.data = data;
        }

        public int[] GetAsInt()
        {
            var sectors = new List<int>();
            for (int i = 0; i < Pointers.Length / 4; i++)
            {
                var sector = BitConverter.ToInt32(Pointers, i * 4);
                sectors.Add(sector);
            }

            return sectors.ToArray();
        }

        public bool[] GetAsBitmap()
        {
            var sectors = new List<bool>();
            for (int i = 0; i < Pointers.Length; i++)
            {
                var sector = Pointers[i];
                for (int j = 0; j < 8; j++)
                {
                    sectors.Add((sector & (1 << j)) != 0);
                }
            }
            return sectors.ToArray();
        }
    }
}