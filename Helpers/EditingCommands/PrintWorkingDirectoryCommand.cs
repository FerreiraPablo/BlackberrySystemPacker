using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

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
