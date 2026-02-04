using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class InstallBarCommand: EditingCommand
    {
        public override string Description { get; set; } = "Installs a BAR or APK File into the OS.";


        public InstallBarCommand() : base("installbar")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> nodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid command, include the package path to be installed.");
            }

            var originalFile = args[1];
            if (string.IsNullOrWhiteSpace(originalFile))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }


            var extension = Path.GetExtension(originalFile);
            if (extension != ".bar")
            {
                throw new ArgumentException("Invalid command, the file must be a .bar file.");
            }

            var fixedFileName = Path.GetFileNameWithoutExtension(originalFile).Replace(" ", "").Replace(".", "").ToLower();
            var packageContentLocation = $"var/android/{fixedFileName}.bar";

            var fileTasks = new List<LiveEditingTask>
            {
                new LiveEditingTask()
                {
                    RelativeNodePath = packageContentLocation,
                    Type = LiveEditingTaskType.CreateFile,
                    Data = File.ReadAllBytes(originalFile)
                },
                new LiveEditingTask()
                {
                    RelativeNodePath = $"var/pps/system/installer/upd/current/{fixedFileName}",
                    Type = LiveEditingTaskType.CreateFile,
                    Data = Encoding.ASCII.GetBytes($"@{fixedFileName}\naction::install\npackage_location::/{packageContentLocation}\nextras::\n")
                }
            };

            foreach (var task in fileTasks)
            {
                tasks.Enqueue(task);
            }
        }
    }
}
