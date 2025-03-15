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

            var recursive = false;
            if (args.Length > 3)
            {
                recursive = args[1] == "-R";
                args = args.Skip(1).ToArray();
            }

            var mode = args[1];
            var path = GetValidPath(args[2]);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid chmod command, please provide a file path.");
            }

            if (recursive)
            {
                CreatePermissionTaskRecursively(workingNodes.FirstOrDefault(x => x.FullPath == path), int.Parse(mode), tasks);
            }
            else
            {
                var task = new LiveEditingTask()
                {
                    RelativeNodePath = path,
                    Type = LiveEditingTaskType.SetPermissions,
                    Permissions = int.Parse(mode)
                };

                tasks.Enqueue(task);
            }
        }

        public void CreatePermissionTaskRecursively(FileSystemNode node, int mode, ConcurrentQueue<LiveEditingTask> tasks)
        {
            var parentTask = new LiveEditingTask()
            {
                RelativeNodePath = node.FullPath,
                Type = LiveEditingTaskType.SetPermissions,
                Permissions = mode
            };

            tasks.Enqueue(parentTask);

            foreach (var child in node.Children)
            {
                if (child.IsDirectory())
                {
                    CreatePermissionTaskRecursively(child, mode, tasks);
                    continue;
                }
                var task = new LiveEditingTask()
                {
                    RelativeNodePath = child.FullPath,
                    Type = LiveEditingTaskType.SetPermissions,
                    Permissions = mode
                };
                tasks.Enqueue(task);
            }
        }
    }
}
