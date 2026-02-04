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
    public class PrintWorkingDirectoryCommand : EditingCommand
    {
        public override string Description { get; set; } = "Print the current working directory.";

        public PrintWorkingDirectoryCommand() : base("pwd", "printworkingdirectory")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            var path = GetValidPath("");
            Logger.LogInformation(path);
        }
    }
}
