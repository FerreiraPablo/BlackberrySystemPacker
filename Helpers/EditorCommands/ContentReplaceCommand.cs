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
    public class ContentReplaceCommand : EditorCommand
    {
        new string Description { get; set; } = "Replaces specific content on a file.";

        public ContentReplaceCommand() : base("contentreplace", "replace")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid contentreplace command, please provide a file path, a search string and a replace string.");
            }
            var path = args[1];

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Invalid command, please provide a valid path.");
            }

            var searchString = args[2];
            var replaceString = args[3];

            var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == path);

            if (existingFile == null)
            {
                throw new FileNotFoundException($"The file {path} cannot be found.");
            }

            var content = existingFile.ReadAllText();
            var newContent = content.Replace(searchString, replaceString);

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
