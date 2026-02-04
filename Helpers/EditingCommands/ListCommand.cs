using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class ListCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the node mode of a file or directory.";

        public ListCommand() : base("list", "ls")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            var path = GetValidPath(args.Length > 1 ? args[1] : "");
            var existingNodes = workingNodes.Where(x => x.FullPath == path && x.IsDirectory());
            if (!existingNodes.Any())
            {
                throw new ArgumentException("Invalid command, the specified path is not a directory.");
            }

            var children = existingNodes.SelectMany(x => x.Children).OrderBy(x => x.Name);
            var maxNameLength = children.Any() ? children.Max(x => x.Name.Length) : 0;
            foreach (var child in children)
            {
                Logger.LogInformation(child.Name.PadRight(maxNameLength) + "\t" + (child.IsUserNode ? "UserNode" : "SystemNode"));
            }
        }
    }
}
