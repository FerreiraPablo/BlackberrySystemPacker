using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class ChangeModeCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the node mode of a file or directory.";

        public ChangeModeCommand() : base("mode", "setmode")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid  command, please provide a mode and a file path.");
            }
            var mode = args[1];
            var path = GetValidPath(args[2]);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.SetMode,
                Mode = Convert.ToInt32(mode),
            };
            tasks.Enqueue(task);
        }
    }
}
