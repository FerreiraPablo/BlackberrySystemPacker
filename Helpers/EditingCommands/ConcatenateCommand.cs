using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class ConcatenateCommand : EditingCommand
    {
        public override string Description { get; set; } = "Displays the content of a node.";

        public ConcatenateCommand() : base("cat", "concatenate", "read")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            var path = GetValidPath(args.Length > 1 ? args[1] : "");
            var existingNode = workingNodes.FirstOrDefault(x => x.FullPath == path && !x.IsDirectory());
            if (existingNode == null)
            {
                throw new ArgumentException("Invalid command, the specified path is not a file");
            }

            try
            {
                var content = existingNode.ReadAllText();
                Console.WriteLine(content);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to read file contents.");
                throw new ArgumentException("Failed to read file contents.", ex);
            }
        }
    }
}
