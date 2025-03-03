using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Helpers.Nodes;
using BlackberrySystemPacker.Nodes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class PushCommand : EditorCommand
    {
        new string Description { get; set; } = "Push a file to the system.";

        public PushCommand() : base("push", "pushfile")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid push command, please provide an origin and a destination path.");
            }
            var originalFile = args[1];
            if (string.IsNullOrWhiteSpace(originalFile))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var destinationPath = args[2];

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Invalid command, please provide a destination path.");
            }

            if (!File.Exists(originalFile))
            {
                throw new FileNotFoundException($"The file {originalFile} cannot be found.");
            }

            var content = File.ReadAllBytes(originalFile);

            var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == destinationPath);

            var task = new LiveEditingTask()
            {
                RelativeNodePath = destinationPath,
                Type = existingFile != null ? LiveEditingTaskType.Write : LiveEditingTaskType.CreateFile,
                Data = content
            };
            tasks.Enqueue(task);
        }
    }
}
