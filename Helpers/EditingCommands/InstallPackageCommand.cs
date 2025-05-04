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
    public class InstallPackageCommand: EditingCommand
    {
        public override string Description { get; set; } = "Installs a BAR or APK File into the OS.";


        public InstallPackageCommand() : base("install", "installpackage")
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
            if (extension != ".apk" && extension != ".bar")
            {
                throw new ArgumentException("Invalid command, the file must be a .apk or .bar file.");
            }

            using var fileStream = new FileStream(originalFile, FileMode.Open);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);


            var packageIdentity = "test";
            var packageContentLocation = $"var/android/{packageIdentity}";

            var directoryTasks = new List<LiveEditingTask>();
            var fileTasks = new List<LiveEditingTask>();

            directoryTasks.Add(new LiveEditingTask()
            {
                RelativeNodePath = packageContentLocation,
                Type = LiveEditingTaskType.CreateDirectory,
            });

            fileTasks.Add(new LiveEditingTask()
            {
                RelativeNodePath = $"var/pps/system/installer/upd/current/{packageIdentity}",
                Type = LiveEditingTaskType.CreateFile,
                Data = Encoding.ASCII.GetBytes($"@{packageIdentity}\naction::install_apk\npackage_location::{packageContentLocation}\nextras::source::apk")
            });

            foreach (var entry in archive.Entries)
            {
                var relativePath = $"{packageContentLocation}/{entry.FullName}";
                if (entry.FullName.EndsWith("/"))
                {
                    // Directory entry
                    directoryTasks.Add(new LiveEditingTask()
                    {
                        RelativeNodePath = relativePath,
                        Type = LiveEditingTaskType.CreateDirectory,
                    });
                }
                else
                {
                    using var entryStream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    entryStream.CopyTo(memoryStream);
                    fileTasks.Add(new LiveEditingTask()
                    {
                        RelativeNodePath = relativePath,
                        Type = LiveEditingTaskType.CreateFile,
                        Data = memoryStream.ToArray(),
                    });
                }
            }
            var requiredTasks = new List<LiveEditingTask>();
            requiredTasks.AddRange(directoryTasks);
            requiredTasks.AddRange(fileTasks);

            foreach (var task in requiredTasks)
            {
                tasks.Enqueue(task);
            }
        }
    }
}
