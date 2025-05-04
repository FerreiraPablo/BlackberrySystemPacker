using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class LinkCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the node mode of a file or directory.";

        public LinkCommand() : base("ln", "link")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            var sourcePath = GetValidPath(args.Length > 1 ? args[1] : "");
            var destinationPath = GetValidPath(args.Length > 2 ? args[2] : "");

            var sourceNode = workingNodes.FirstOrDefault(x => x.FullPath == sourcePath);
            if(sourceNode == null)
            {
                throw new ArgumentException("Invalid command, the specified source path does not exist.");
            }

            tasks.Enqueue(new LiveEditingTask()
            {
                RelativeNodePath = sourceNode.FullPath,
                Name = destinationPath,
                Type = LiveEditingTaskType.CreateSymlink,
            });
        }
    }
}