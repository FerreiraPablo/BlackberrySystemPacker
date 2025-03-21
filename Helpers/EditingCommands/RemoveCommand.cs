﻿using BlackberrySystemPacker.Core;
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
    public class RemoveCommand : EditingCommand
    {
        public override string Description { get; set; } = "Remove a file or directory.";

        public RemoveCommand() : base("rm", "remove", "delete")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid rm command, please provide a file path.");
            }
            var path = GetValidPath(args[1]);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }
            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.Delete,
            };
            tasks.Enqueue(task);
        }
    }
}
