using BlackberrySystemPacker.Helpers.Nodes;
using BlackberrySystemPacker.Helpers.RCFS;
using System.Text;

namespace BlackberrySystemPacker.Nodes
{
    public abstract class FileSystemNode
    {
        public bool IsUserNode { get => this is UserSystemNode; }

        public Stream Stream { get; set; }

        public int Mode { get; set; }

        public int ExtMode { get; set; }

        public abstract string Name { get; set; }

        public string Path { get; set; } = string.Empty;

        public abstract int Size { get; set; }

        public string FullPath { get => !string.IsNullOrWhiteSpace(Name) ? System.IO.Path.Combine(Path, Name).Replace("\\","/") : ""; }

        public int UserId { get; set; } = 0;

        public int GroupId { get; set; } = 0;

        public DateTime CreationDate { get; set; } = DateTime.Now;

        public FileSystemNode Parent { get; set; }

        public IEnumerable<FileSystemNode> Children { get; set; }

        public bool IsDirectory()
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

        public void SetPermissions(int umask)
        {
            var realUmask = Convert.ToInt32(umask.ToString(), 8);
            Mode = (Mode & 0xFE00) | (realUmask & 0x1FF);
        }

        public int GetPermissions()
        {
            return Convert.ToInt32(FileNodeHelper.GetUmaskOctal(Mode));
        }

        public abstract byte[] Read();
        
        public abstract void Write(byte[] data);

        public abstract void Delete();

        public abstract FileSystemNode CreateFile(string name = null, byte[] data = null);

        public FileSystemNode CreateFile(string name = null, string data = null)
        {
            return CreateFile(name, Encoding.ASCII.GetBytes(data));
        }

        public abstract FileSystemNode CreateDirectory(string name = null);

        public abstract void Move(FileSystemNode parent);


        public abstract void Apply();

        public string ReadAllText()
        {
            return Encoding.ASCII.GetString(Read());
        }

        public string WriteAllText(string text)
        {
            Write(Encoding.ASCII.GetBytes(text));
            return text;
        }
    }
}