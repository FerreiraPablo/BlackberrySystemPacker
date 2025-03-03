using System.Text;

namespace BlackberrySystemPacker.Helpers.Nodes
{

    public static class FileNodeHelper
    {
        // Constants for file type
        public const int S_IFDIR = 0x4000;  // Directory
        public const int S_IFREG = 0x8000;  // Regular file
        public const int S_IFLNK = 0xA000;  // Symbolic link

        // Constants for permission bits
        public const int S_IRUSR = 0x0100;  // Owner has read permission
        public const int S_IWUSR = 0x0080;  // Owner has write permission
        public const int S_IXUSR = 0x0040;  // Owner has execute permission
        public const int S_IRGRP = 0x0020;  // Group has read permission
        public const int S_IWGRP = 0x0010;  // Group has write permission
        public const int S_IXGRP = 0x0008;  // Group has execute permission
        public const int S_IROTH = 0x0004;  // Others have read permission
        public const int S_IWOTH = 0x0002;  // Others have write permission
        public const int S_IXOTH = 0x0001;  // Others have execute permission

        // Constants for special bits
        public const int S_ISUID = 0x0800;  // Set user ID on execution
        public const int S_ISGID = 0x0400;  // Set group ID on execution
        public const int S_ISVTX = 0x0200;  // Sticky bit

        // Check if the file is a directory
        public static bool IsDirectory(int mode)
        {
            return (mode & 0xF000) == S_IFDIR;
        }

        // Check if the file is a regular file
        public static bool IsRegularFile(int mode)
        {
            return (mode & 0xF000) == S_IFREG;
        }

        // Check if the file is a symbolic link
        public static bool IsSymbolicLink(int mode)
        {
            return (mode & 0xF000) == S_IFLNK;
        }

        // Get file type as a string
        public static string GetFileType(int mode)
        {
            if (IsDirectory(mode))
                return "Directory";
            else if (IsRegularFile(mode))
                return "File";
            else if (IsSymbolicLink(mode))
                return "Symbolic Link";
            else
                return "Other";
        }

        // Get permissions string in the format "rwxrwxrwx"
        public static string GetPermissionsString(int mode)
        {
            StringBuilder permissions = new StringBuilder(9);

            // Owner permissions
            permissions.Append((mode & S_IRUSR) != 0 ? 'r' : '-');
            permissions.Append((mode & S_IWUSR) != 0 ? 'w' : '-');
            permissions.Append((mode & S_IXUSR) != 0 ? 'x' : '-');

            // Group permissions
            permissions.Append((mode & S_IRGRP) != 0 ? 'r' : '-');
            permissions.Append((mode & S_IWGRP) != 0 ? 'w' : '-');
            permissions.Append((mode & S_IXGRP) != 0 ? 'x' : '-');

            // Others permissions
            permissions.Append((mode & S_IROTH) != 0 ? 'r' : '-');
            permissions.Append((mode & S_IWOTH) != 0 ? 'w' : '-');
            permissions.Append((mode & S_IXOTH) != 0 ? 'x' : '-');

            return permissions.ToString();
        }

        // Get permissions as an octal string (e.g., "0755")
        public static string GetPermissionsOctal(int mode)
        {
            int permissions = mode & 0xFFF; // Include special bits and permission bits
            return "0" + Convert.ToString(permissions, 8);
        }

        // Get just the umask (permission bits) from the mode
        public static int GetUmask(int mode)
        {
            // Extract just the permission bits (0-8)
            return mode & 0x1FF;
        }

        // Get the umask as an octal string (e.g., "0644")
        public static string GetUmaskOctal(int mode)
        {
            int umask = GetUmask(mode);
            return "0" + Convert.ToString(umask, 8).PadLeft(3, '0');
        }

        // Get special bits
        public static (bool setuid, bool setgid, bool sticky) GetSpecialBits(int mode)
        {
            return (
                (mode & S_ISUID) != 0,
                (mode & S_ISGID) != 0,
                (mode & S_ISVTX) != 0
            );
        }

        // Get detailed file mode information
        public static FilePermissionInfo GetFilePermissionInfo(int mode)
        {
            return new FilePermissionInfo
            {
                IsDirectory = IsDirectory(mode),
                IsRegularFile = IsRegularFile(mode),
                IsSymbolicLink = IsSymbolicLink(mode),
                OwnerRead = (mode & S_IRUSR) != 0,
                OwnerWrite = (mode & S_IWUSR) != 0,
                OwnerExecute = (mode & S_IXUSR) != 0,
                GroupRead = (mode & S_IRGRP) != 0,
                GroupWrite = (mode & S_IWGRP) != 0,
                GroupExecute = (mode & S_IXGRP) != 0,
                OthersRead = (mode & S_IROTH) != 0,
                OthersWrite = (mode & S_IWOTH) != 0,
                OthersExecute = (mode & S_IXOTH) != 0,
                SetUID = (mode & S_ISUID) != 0,
                SetGID = (mode & S_ISGID) != 0,
                Sticky = (mode & S_ISVTX) != 0
            };
        }

        // Create a new mode value with the specified permissions
        public static int CreateMode(bool isDirectory, bool ownerRead, bool ownerWrite, bool ownerExecute,
                                    bool groupRead, bool groupWrite, bool groupExecute,
                                    bool othersRead, bool othersWrite, bool othersExecute,
                                    bool setUID = false, bool setGID = false, bool sticky = false,
                                    bool isSymbolicLink = false)
        {
            int mode = 0;

            // Set file type
            if (isSymbolicLink)
                mode |= S_IFLNK;
            else if (isDirectory)
                mode |= S_IFDIR;
            else
                mode |= S_IFREG;

            // Set permissions
            if (ownerRead) mode |= S_IRUSR;
            if (ownerWrite) mode |= S_IWUSR;
            if (ownerExecute) mode |= S_IXUSR;
            if (groupRead) mode |= S_IRGRP;
            if (groupWrite) mode |= S_IWGRP;
            if (groupExecute) mode |= S_IXGRP;
            if (othersRead) mode |= S_IROTH;
            if (othersWrite) mode |= S_IWOTH;
            if (othersExecute) mode |= S_IXOTH;

            // Set special bits
            if (setUID) mode |= S_ISUID;
            if (setGID) mode |= S_ISGID;
            if (sticky) mode |= S_ISVTX;

            return mode;
        }

        // Create a new mode value from an octal string (e.g., "0755" or "755")
        public static int CreateModeFromOctal(string octalPermissions, bool isDirectory, bool isSymbolicLink = false)
        {
            // Remove leading '0' if present
            if (octalPermissions.StartsWith("0"))
                octalPermissions = octalPermissions.Substring(1);

            int permissionBits = Convert.ToInt32(octalPermissions, 8);

            int fileType;
            if (isSymbolicLink)
                fileType = S_IFLNK;
            else if (isDirectory)
                fileType = S_IFDIR;
            else
                fileType = S_IFREG;

            return fileType | permissionBits;
        }

        // Set permissions using a umask (e.g., 0644, 0777)
        public static int SetPermissionsByUmask(int currentMode, int umask)
        {
            // Extract the file type bits
            int fileTypeBits = currentMode & 0xF000;

            // Extract special bits if we want to preserve them
            int specialBits = currentMode & 0x0F00;

            // Apply the new permission bits from umask
            return fileTypeBits | specialBits | umask & 0x01FF;
        }

        // Set permissions using an octal string (e.g., "0644", "0777")
        public static int SetPermissionsByUmask(int currentMode, string umaskString)
        {
            // Remove leading '0' if present
            if (umaskString.StartsWith("0"))
                umaskString = umaskString.Substring(1);

            // Convert octal string to integer
            int umask = Convert.ToInt32(umaskString, 8);

            return SetPermissionsByUmask(currentMode, umask);
        }
    }

    // Class to hold detailed permission information
    public class FilePermissionInfo
    {
        public bool IsDirectory { get; set; }
        public bool IsRegularFile { get; set; }
        public bool IsSymbolicLink { get; set; }

        // Owner permissions
        public bool OwnerRead { get; set; }
        public bool OwnerWrite { get; set; }
        public bool OwnerExecute { get; set; }

        // Group permissions
        public bool GroupRead { get; set; }
        public bool GroupWrite { get; set; }
        public bool GroupExecute { get; set; }

        // Others permissions
        public bool OthersRead { get; set; }
        public bool OthersWrite { get; set; }
        public bool OthersExecute { get; set; }

        // Special bits
        public bool SetUID { get; set; }
        public bool SetGID { get; set; }
        public bool Sticky { get; set; }

        public override string ToString()
        {
            string typeStr;
            if (IsDirectory)
                typeStr = "d";
            else if (IsSymbolicLink)
                typeStr = "l";
            else if (IsRegularFile)
                typeStr = "-";
            else
                typeStr = "?";

            string specialBits = (SetUID ? "u" : "-") + (SetGID ? "g" : "-") + (Sticky ? "t" : "-");

            string permissions =
                (OwnerRead ? "r" : "-") +
                (OwnerWrite ? "w" : "-") +
                (OwnerExecute ? "x" : "-") +
                (GroupRead ? "r" : "-") +
                (GroupWrite ? "w" : "-") +
                (GroupExecute ? "x" : "-") +
                (OthersRead ? "r" : "-") +
                (OthersWrite ? "w" : "-") +
                (OthersExecute ? "x" : "-");

            return $"{typeStr}{permissions} (Special: {specialBits})";
        }
    }
}
