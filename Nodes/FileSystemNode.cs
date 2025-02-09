using System.Text;

namespace BlackberrySystemPacker.Nodes
{
    public abstract class FileSystemNode
    {
        public Stream Stream { get; set; }

        public bool IsUserNode { get => this is UserSystemNode; }

        public abstract ushort Mode { get; set; }

        public int StartOffset;

        public abstract string Name { get; set; }

        public string Path = "";


        public abstract int Size { get; set; }

        public abstract byte[] Data { get; protected set; }

        public string FullPath { get => !string.IsNullOrWhiteSpace(Name) ? System.IO.Path.Combine(Path, Name).Replace("\\","/") : ""; }

        public FileSystemNode Parent { get; set; }
        public IEnumerable<FileSystemNode> Children { get; set; }

        public virtual bool IsDirectory()
        {
            int num = Mode;
            return ((num >>= 14) & 1) == 1;
        }

        public bool IsCompressed()
        {
            int num = Mode;
            return ((num >>= 23) & 1) == 1;
        }

        public bool IsSymLink()
        {
            int num = Mode;
            return ((num >>= 13) & 1) == 1;
        }

        public abstract byte[] Read();
        
        public abstract void Write(byte[] data);

        public abstract void Delete();

        public abstract FileSystemNode CreateFile(string name = null, byte[] data = null);

        public abstract FileSystemNode CreateDirectory(string name = null);

        public abstract void Move(FileSystemNode parent);

        public string ReadAllText()
        {
            if(Data == null)
            {
                Read();
            }

            return Encoding.UTF8.GetString(Data);
        }

        public string WriteAllText(string text)
        {
            if(Data == null)
            {
                Read();
            }

            var data = Encoding.UTF8.GetBytes(text);

            Write(data);

            return text;
        }
    }
}