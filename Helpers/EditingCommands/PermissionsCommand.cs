using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Helpers.Nodes;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class PermissionsCommand: EditingCommand
    {

        public override string Description { get; set; } = "Get the permissions of a file or directory.";

        public PermissionsCommand() : base("permissions", "perms")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }
            var path = GetValidPath(args[1]);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == path);

            if (existingFile == null)
            {
                throw new FileNotFoundException($"The file {path} cannot be found.");
            }

            var perms = FileNodeHelper.GetFilePermissionInfo(existingFile.Mode);
            Console.WriteLine($"Permissions for {path}: ({existingFile.Mode}) {perms} {FileNodeHelper.GetPermissionsOctal(existingFile.Mode)}, GID: {existingFile.GroupId}, UID: {existingFile.UserId}");
        }
    }
}
