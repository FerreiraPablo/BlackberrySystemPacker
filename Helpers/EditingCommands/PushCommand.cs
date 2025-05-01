using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class PushCommand : EditingCommand
    {
        public override string Description { get; set; } = "Push a file from your system to the image.";

        public PushCommand() : base("push", "pushfile")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid push command, please provide an origin and a destination path.");
            }
            var originalFile = args[1];
            if (string.IsNullOrWhiteSpace(originalFile))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var destinationPath = GetValidPath(args[2]);

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Invalid command, please provide a destination path.");
            }

            var internalTask = new List<LiveEditingTask>();
            var isDirectory = File.GetAttributes(originalFile).HasFlag(FileAttributes.Directory);

            if (isDirectory)
            {
                var files = Directory.GetFileSystemEntries(originalFile, "*", SearchOption.AllDirectories);
                var directoryTasks = new List<LiveEditingTask>();
                var fileTasks = new List<LiveEditingTask>(); ;

                foreach (var file in files)
                {
                    var relativePath = destinationPath + "/" + Path.GetRelativePath( Path.Combine(originalFile), file).Replace("\\", "/");
                    var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == relativePath);
                    var currentFileIsDirectory = File.GetAttributes(file).HasFlag(FileAttributes.Directory);
                    if (currentFileIsDirectory)
                    {
                        if (existingFile == null)
                        {
                            directoryTasks.Add(new LiveEditingTask()
                            {
                                RelativeNodePath = relativePath,
                                Type = LiveEditingTaskType.CreateDirectory
                            });
                        }
                    }
                    else
                    {
                        var content = File.ReadAllBytes(file);
                        fileTasks.Add(new LiveEditingTask()
                        {
                            RelativeNodePath = relativePath,
                            Type = existingFile != null ? LiveEditingTaskType.Write : LiveEditingTaskType.CreateFile,
                            Data = content
                        });
                    }
                }

                internalTask.AddRange(directoryTasks);
                internalTask.AddRange(fileTasks);
            }
            else
            {
                var content = File.ReadAllBytes(originalFile);
                var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == destinationPath);
                internalTask.Add(new LiveEditingTask()
                {
                    RelativeNodePath = destinationPath,
                    Type = existingFile != null ? LiveEditingTaskType.Write : LiveEditingTaskType.CreateFile,
                    Data = content
                });
            }

            foreach(var task in internalTask)
            {
                tasks.Enqueue(task);
            }
        }
    }
}
