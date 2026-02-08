using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class CopyCommand : EditingCommand
    {
        public override string Description { get; set; } = "Push a file from your system to the image.";

        public CopyCommand() : base("cp", "copy")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid push command, please provide an origin and a destination path.");
            }
            var originalFile = GetValidPath(args[1]);
            if (string.IsNullOrWhiteSpace(originalFile))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var destinationPath = GetValidPath(args[2]);
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Invalid command, please provide a destination path.");
            }
            var existingToCopyFile = workingNodes.FirstOrDefault(x => x.FullPath == originalFile);
            if(existingToCopyFile == null)
            {
                throw new ArgumentException("The specified original file exists in the image, please provide a valid file from your system.");
            }

            var internalTask = new List<LiveEditingTask>();
            var isDirectory = existingToCopyFile.IsDirectory();

            if (isDirectory)
            {
                var files = workingNodes.Where(x => x.FullPath.StartsWith(originalFile + "/") || x.FullPath == originalFile);
                var directoryTasks = new List<LiveEditingTask>();
                var fileTasks = new List<LiveEditingTask>(); ;

                foreach (var file in files)
                {
                    if(file.Name == "." || file.Name == "..")
                    {
                        continue;
                    }
                    
                    var relativePath = destinationPath + "/" + Path.GetRelativePath(Path.Combine(originalFile), file.FullPath).Replace("\\", "/");
                    var existingFile = workingNodes.FirstOrDefault(x => x.FullPath == relativePath);
                    var currentFileIsDirectory = file.IsDirectory();
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
                        var content = file.Read();
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
                var content = existingToCopyFile.Read();
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
