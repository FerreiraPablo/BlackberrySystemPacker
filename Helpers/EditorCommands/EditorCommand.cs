using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public abstract class EditorCommand
    {
        public string[] Aliases { get; set; }

        public string Description { get; set; } = "No description available";

        public abstract void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> nodes, string[] args);

        public EditorCommand(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
}
