using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class CreateDirectoryCommand : EditingCommand
    {
        public override string Description { get; set; } = "Creates a directory.";

        public CreateDirectoryCommand() : base("mkdir", "createdirectory")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid mkdir command, please provide a directory path.");
            }
            var path = GetValidPath(args[1]);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.CreateDirectory,
            };

            tasks.Enqueue(task);
        }
    }
}
