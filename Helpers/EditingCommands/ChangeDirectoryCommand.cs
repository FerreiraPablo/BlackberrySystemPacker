using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class ChangeDirectoryCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the node mode of a file or directory.";

        public ChangeDirectoryCommand() : base("changedirectory", "changedir", "cd")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid  command, please provide a mode and a file path.");
            }
            
            var path = GetValidPath(args[1]);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var existingNode = workingNodes.FirstOrDefault(x => x.FullPath == path && x.IsDirectory());
            if (existingNode == null)
            {
                throw new ArgumentException("Invalid command, the specified path is not a directory.");
            }

            WorkingDirectory = args[1] == ".." ? existingNode.Parent.FullPath : existingNode.FullPath;
        }
    }
}
