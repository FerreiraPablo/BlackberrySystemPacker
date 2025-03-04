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
    public class RemoveLineCommand : EditingCommand
    {
        public override string Description { get; set; } = "Removes an specific line from a file.";

        public RemoveLineCommand() : base("rmline", "removeline")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid rmline command, please provide a file path, a search string and a replace string.");
            }
            var path = args[1];


            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a valid path.");
            }

            var searchString = args[2];
            var replaceString = args[3];
            var method = args.Length > 4 ? args[4] : "contains";
            var caseInsensitive = args.Length > 5 && args[5] == "true";

            var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == path);

            if (existingFile == null)
            {
                throw new FileNotFoundException($"The file {path} cannot be found.");
            }

            var content = existingFile.ReadAllText();
            var lines = content.Split("\n");
            var list = new List<string>();
            var caseSensitivity = caseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            foreach (var line in lines)
            {
                if (method == "contains" && line.Contains(searchString, caseSensitivity))
                {
                    continue;
                }
                if (method == "startswith" && line.StartsWith(searchString, caseSensitivity))
                {
                    continue;
                }
                if (method == "endswith" && line.EndsWith(searchString, caseSensitivity))
                {
                    continue;
                }
                list.Add(line);
            }

            var newContent = string.Join("\n", list);

            var task = new LiveEditingTask()
            {
                RelativeNodePath = path,
                Type = existingFile != null ? LiveEditingTaskType.Write : LiveEditingTaskType.CreateFile,
                Data = Encoding.ASCII.GetBytes(newContent)
            };
            tasks.Enqueue(task);
        }
    }
}
