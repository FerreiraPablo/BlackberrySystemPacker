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
    public class CurrentWorkDirectoryCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the node mode of a file or directory.";

        public CurrentWorkDirectoryCommand() : base("currentworkdirectory", "currentworkdir", "cwd")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            Logger.LogInformation(WorkingDirectory);
        }
    }
}
