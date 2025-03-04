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
    public class ChangePermissionsCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the permissions of a file or directory.";

        public ChangePermissionsCommand() : base("chmod", "setpermissions", "changepermissions")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid chmod command, please provide a mode and a file path.");
            }
            var mode = args[1];
            var path = args[2];

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid chmod command, please provide a file path.");
            }

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.SetPermissions,
                Permissions = int.Parse(mode)
            };
            tasks.Enqueue(task);
        }
    }
}
