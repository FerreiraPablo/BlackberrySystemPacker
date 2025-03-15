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
    public class ChangeGroupCommand : EditingCommand
    {
        public override string Description { get; set; } = "Change the group of a file or directory.";

        public ChangeGroupCommand() : base("gid", "chgrp", "setgroup", "changegroup")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid gid command, please provide a group id and a file path.");
            }

            var gid = args[1];
            var path = GetValidPath(args[2]);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid chmod command, please provide a file path.");
            }

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.SetGroup,
                GroupId = Convert.ToInt32(gid),
            };
            tasks.Enqueue(task);
        }
    }
}
