using BlackberrySystemPacker.Nodes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlackberrySystemPacker.Core
{
    enum LiveEditingTaskType
    {
        Write,
        Delete,
        CreateFile,
        CreateDirectory,
    }

    class LiveEditingTask
    {
        public string RelativeNodePath = "";

        public byte[] Data;

        public LiveEditingTaskType Type;
    }

    public class LiveEditingProcessor
    {
        private ConcurrentQueue<LiveEditingTask> Tasks = new();
        private List<FileSystemNode> _workingNodes;
        private string _sourceDirectory;

        public LiveEditingProcessor(List<FileSystemNode> workingNodes, string sourceDirectory)
        {
            _workingNodes = workingNodes;
            _sourceDirectory = sourceDirectory;

        }

        public async Task Start()
        {
            Console.WriteLine("Started live editing processor.");
            ;

            Task.WhenAll(ApplyChanges());
            while(true) {
                RunTasks();
            }
        }


        private FileSystemNode GetExistingParent(string path)
        {
            var partParts = path.Split("/");
            var testPath = string.Join("/", partParts.Take(partParts.Length - 1));
            while (partParts.Length > 0)
            {
                var existingParent = _workingNodes.FirstOrDefault(x => x != null && x.FullPath == testPath);
                if (existingParent != null)
                {
                    return existingParent;
                }
                partParts = partParts.Take(partParts.Length - 1).ToArray();
                testPath = string.Join("/", partParts);
            }

            return null;
        }


        private void RunTasks()
        {
            var existingTask = Tasks.TryDequeue(out var currentTask);
            if (!existingTask)
            {
                return;
            }


            var isCreationTask = currentTask.Type == LiveEditingTaskType.CreateFile || currentTask.Type == LiveEditingTaskType.CreateDirectory;
            FileSystemNode requiredFile;
            if (isCreationTask)
            {
                requiredFile = GetExistingParent(currentTask.RelativeNodePath);
            } else
            {
                requiredFile = _workingNodes.FirstOrDefault(x => x != null && x.FullPath == currentTask.RelativeNodePath);
            }


            if (requiredFile == null)
            {
                return;
            }

            Console.WriteLine($"Executing task for {currentTask.RelativeNodePath} {currentTask.Type}");
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            switch (currentTask.Type)
            {
                case LiveEditingTaskType.Write:
                    requiredFile.Write(currentTask.Data);
                    break;
                case LiveEditingTaskType.Delete:
                    requiredFile.Delete();
                    _workingNodes.Remove(requiredFile);
                    break;
                case LiveEditingTaskType.CreateFile:
                    var createdNode = requiredFile.CreateFile(currentTask.RelativeNodePath, currentTask.Data);
                    _workingNodes.Add(createdNode);
                    break;
                case LiveEditingTaskType.CreateDirectory:
                    var createdDirectoryNode = requiredFile.CreateDirectory(currentTask.RelativeNodePath);
                    _workingNodes.Add(createdDirectoryNode);
                    break;
            }

            stopWatch.Stop();
            Console.WriteLine($"Task for {currentTask.RelativeNodePath} {currentTask.Type} took {stopWatch.Elapsed.TotalSeconds}");
        }

        public void Build()
        {
            if (!Directory.Exists(_sourceDirectory))
            {
                Directory.CreateDirectory(_sourceDirectory);
            }

            List<string> AddedFiles = new List<string>();
            List<string> Duplicates = new List<string>();

            foreach (var node in _workingNodes)
            {
                var path = Path.Combine(_sourceDirectory, node.FullPath);

                if (AddedFiles.Contains(node.FullPath))
                {
                    Duplicates.Add(node.FullPath);
                    continue;
                }
                else
                {
                    AddedFiles.Add(node.FullPath);
                }

                if (node.IsDirectory())
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }
                else
                {
                    node.Read();
                    if (node.Data == null || node.Data.Length == 0)
                    {
                        continue;
                    }

                    var directory = Path.GetFullPath(path.Replace(node.Name, ""));
                    var existingDirectory = Directory.Exists(directory);
                    if (!existingDirectory)
                    {
                        Directory.CreateDirectory(directory);
                    }
                    var expectedPath = path;
                    File.WriteAllBytes(expectedPath, node.Data);
                }
            }

            Console.WriteLine("Created workspace.");
        }

        private async Task ApplyChanges()
        {
            var localFiles = new ConcurrentBag<string>(Directory.GetFileSystemEntries(_sourceDirectory, "*", SearchOption.AllDirectories));
            var startingModificationTimes = new ConcurrentDictionary<string, DateTime>();

            while (true)
            {

                var currentFiles = new ConcurrentBag<string>(Directory.GetFileSystemEntries(_sourceDirectory, "*", SearchOption.AllDirectories));
                var deletedEntries = localFiles.Except(currentFiles).OrderBy(x => x.Length).ToList();
                var parentsToBeDeleted = new List<string>();
                foreach (var parent in deletedEntries)
                {
                    var containerParentExist = parentsToBeDeleted.Any(x => parent.StartsWith(x));
                    if (containerParentExist)
                    {
                        continue;
                    }
                    parentsToBeDeleted.Add(parent);
                }

                var removedFiles = new ConcurrentBag<string>(parentsToBeDeleted);
                


                var addedFiles = new ConcurrentBag<string>(currentFiles.Except(localFiles));
                localFiles = currentFiles;
                await Parallel.ForEachAsync(removedFiles, (file , _) =>
                {
                    if(file == null)
                    {
                        return new ValueTask();
                    }

                    var relativePath = Path.GetRelativePath(_sourceDirectory, file).Replace("\\", "/");
                    if (relativePath.StartsWith("."))
                    {
                        return new ValueTask();
                    }

                    var task = new LiveEditingTask()
                    {
                        RelativeNodePath = relativePath,
                        Type = LiveEditingTaskType.Delete,
                    };
                    Tasks.Enqueue(task);

                    return new ValueTask();
                });

                await Parallel.ForEachAsync(addedFiles, (file, _) =>
                {
                    if (file == null)
                    {
                        return new ValueTask();
                    }

                    var relativePath = Path.GetRelativePath(_sourceDirectory, file).Replace("\\", "/");
                    if (relativePath.StartsWith("."))
                    {
                        return new ValueTask();
                    }

                    var isDirectory = File.GetAttributes(file).HasFlag(FileAttributes.Directory);

                    var task = new LiveEditingTask()
                    {
                        RelativeNodePath = relativePath,
                        Data = isDirectory ? null : File.ReadAllBytes(file),
                        Type = isDirectory ? LiveEditingTaskType.CreateDirectory : LiveEditingTaskType.CreateFile,
                    };
                    Tasks.Enqueue(task);
                    return new ValueTask();
                });

                await Parallel.ForEachAsync(currentFiles, (file, _) =>
                {
                    if (file == null)
                    {
                        return new ValueTask();
                    }


                    var isDirectory = Directory.Exists(file);
                    if (isDirectory)
                    {
                        return new ValueTask();
                    }

                    if (!startingModificationTimes.ContainsKey(file))
                    {
                        startingModificationTimes.TryAdd(file, File.GetLastWriteTime(file));
                    }

                    var relativePath = Path.GetRelativePath(_sourceDirectory, file).Replace("\\", "/");
                    if (relativePath.StartsWith("."))
                    {
                        return new ValueTask();
                    }

                    var currentModificationTime = File.GetLastWriteTime(file);
                    var previousModificationTime = startingModificationTimes[file];

                    if (currentModificationTime <= previousModificationTime)
                    {
                        return new ValueTask();
                    }

                    startingModificationTimes[file] = currentModificationTime;
                    var data = File.ReadAllBytes(file);
                    var task = new LiveEditingTask()
                    {
                        RelativeNodePath = relativePath,
                        Data = data,
                        Type = LiveEditingTaskType.Write,
                    };
                    Tasks.Enqueue(task);

                    return new ValueTask();
                });

                Thread.Sleep(1000);
            }
        }


    }
}
