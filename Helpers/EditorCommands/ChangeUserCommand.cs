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
    public class ChangeUserCommand : EditorCommand
    {
        new string Description { get; set; } = "Change the user of a file or directory.";

        public ChangeUserCommand() : base("uid", "changeuser", "setuser", "chown")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid uid command, please provide a user id and a file path.");
            }
            var uid = args[1];
            var path = args[2];

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = LiveEditingTaskType.SetUser,
                UserId = Convert.ToInt32(uid),
            };
            tasks.Enqueue(task);
        }
    }
}
