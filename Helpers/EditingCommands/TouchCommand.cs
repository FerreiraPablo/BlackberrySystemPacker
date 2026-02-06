using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;
using System.Text;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class TouchCommand : EditingCommand
    {
        public override string Description { get; set; } = "Creates a file.";

        public TouchCommand() : base("createfile", "touch")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid touch command, please provide a file path.");
            }
            var path = GetValidPath(args[1]);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var content = args.Length > 2 ? args[2] : string.Empty;

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.CreateFile,
                Data = Encoding.ASCII.GetBytes(content)
            };
            tasks.Enqueue(task);
        }
    }
}
