using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;
using System.Text;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class WriteCommand : EditingCommand
    {
        public override string Description { get; set; } = "Write content to a file. Usage: write <file> <content>";

        public WriteCommand() : base("write")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> nodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid write command. Usage: write <file> <content>");
            }

            var filePath = GetValidPath(args[1]);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid write command, please provide a valid file path.");
            }

            var contentParts = args.Skip(2);
            var content = string.Join(" ", contentParts);
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var existingFile = nodes.FirstOrDefault(x => x.FullPath == filePath);

            var task = new LiveEditingTask()
            {
                RelativeNodePath = filePath,
                Type = existingFile != null ? LiveEditingTaskType.Write : LiveEditingTaskType.CreateFile,
                Data = contentBytes
            };

            tasks.Enqueue(task);
        }
    }
}
