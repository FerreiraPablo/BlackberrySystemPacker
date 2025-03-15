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
    public abstract class EditingCommand
    {
        public ILogger Logger { get; set; }

        public static string WorkingDirectory { get; set; } = "";

        public string[] Aliases { get; set; }

        public abstract string Description { get; set; }

        public abstract void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> nodes, string[] args);

        public EditingCommand(params string[] aliases)
        {
            Aliases = aliases;
        }

        public string GetValidPath(string path)
        {
            if(string.IsNullOrWhiteSpace(path) || path == ".")
            {
                return WorkingDirectory;
            }

            if(path == "..")
            {
                return WorkingDirectory;
            }

            if (path.StartsWith("/"))
            {
                return path.TrimStart('/');
            }

            return Path.Combine(WorkingDirectory, path).Replace("\\", "/").Replace("//", "/");
        }
    }
}
