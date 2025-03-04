using BlackberrySystemPacker.Helpers.EditingCommands;
using BlackberrySystemPacker.Nodes;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Core
{
    public enum LiveEditingTaskType
    {
        Write,
        Delete,
        CreateFile,
        CreateDirectory,
        SetPermissions,
        SetGroup,
        SetUser,
        SetMode,
        Rename,
        ChangeParent
    }

    public class LiveEditingTask
    {
        public string Name = null;

        public string RelativeNodePath = "";

        public byte[] Data;

        public int Permissions = 0;

        public int GroupId = 0;

        public int UserId = 0;

        public int Mode = 0;

        public string ParentRelativePath = "";

        public LiveEditingTaskType Type;
    }

    public class LiveEditingProcessor
    {
        private List<EditorCommand> commands = new List<EditorCommand>()
        {
            new ChangeGroupCommand(),
            new ChangeModeCommand(),
            new ChangePermissionsCommand(),
            new ChangeUserCommand(),
            new ContentReplaceCommand(),
            new CreateDirectoryCommand(),
            new PermissionsCommand(),
            new PushCommand(),
            new RemoveCommand(),
            new RemoveLineCommand(),
            new TouchCommand(),
        };

        public bool KeepRunning = true;

        private ConcurrentQueue<LiveEditingTask> _tasks = new();
        private List<FileSystemNode> _workingNodes;
        private string _sourceDirectory;
        private readonly ILogger _logger;

        public LiveEditingProcessor(List<FileSystemNode> workingNodes, string sourceDirectory, ILogger logger)
        {
            _workingNodes = workingNodes;
            _sourceDirectory = sourceDirectory;
            _logger = logger;
        }

        public async Task Start()
        {
            _logger.LogInformation("Started live editing processor.");
            _logger.LogInformation($"You can go to the workspace in {_sourceDirectory} and create, delete or edit any file.");
            _logger.LogInformation("Write 'help' or enter for additional commands.");
            _logger.LogInformation("Write 'quit' then [ENTER] to stop the processor, and end the live editing session...");

            await Task.WhenAll(
            ApplyChanges(),
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }


                    var parts = input.Split(" ");
                    var commandAlias = parts[0].ToLower();

                    if (commandAlias == "quit")
                    {
                        break;
                    }

                    if(commandAlias == "help")
                    {
                        _logger.LogInformation("Available commands:");
                        foreach (var commandDef in commands)
                        {
                            _logger.LogInformation($"{commandDef.Aliases[0]}: {commandDef.Description}");
                        }
                        continue;
                    }

                    var existingCommand = commands.FirstOrDefault(x => x.Aliases.Contains(commandAlias));
                    if (existingCommand != null)
                    {
                        try {
                            existingCommand.Execute(_tasks, _workingNodes, parts);
                        } catch(Exception e)
                        {
                            _logger.LogError(e.Message);
                        }
                    }
                    else
                    {
                        _logger.LogError($"Invalid command: {commandAlias}");
                    }
                }
                Console.WriteLine("");
                _logger.LogInformation("Stopping live editing processor.");
                KeepRunning = false;
            }),
            Task.Factory.StartNew(() =>
            {
                while (KeepRunning)
                {
                    RunTasks();
                }
            }));
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

        
        public void Build()
        {
            _logger.LogInformation("Building workspace, creating files that don't exist.");
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            if (!Directory.Exists(_sourceDirectory))
            {
                Directory.CreateDirectory(_sourceDirectory);
            }

            _workingNodes = _workingNodes.OrderBy(x => x.IsDirectory()).ToList();
            foreach (var node in _workingNodes)
            {
                var path = Path.Combine(_sourceDirectory, node.FullPath);
                if (node.IsDirectory())
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        Directory.SetLastWriteTime(path, node.CreationDate);
                    }
                }
                else
                {
                    var data = node.Read();
                    var directory = Path.GetDirectoryName(path); ;
                    var existingDirectory = Directory.Exists(directory);

                    if (!existingDirectory)
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (File.Exists(path))
                    {
                        continue;
                    }

                    var expectedPath = path;
                    File.WriteAllBytes(expectedPath, data);
                    File.SetLastWriteTime(expectedPath, node.CreationDate);
                }
            }

            stopWatch.Stop();
            _logger.LogInformation($"Created workspace. {Math.Round(stopWatch.Elapsed.TotalSeconds, 1)}s");
        }


        private void RunTasks()
        {
            var existingTask = _tasks.TryDequeue(out var currentTask);
            if (!existingTask)
            {
                return;
            }

            var isCreationTask = currentTask.Type == LiveEditingTaskType.CreateFile || currentTask.Type == LiveEditingTaskType.CreateDirectory;
            FileSystemNode requiredFile;
            if (isCreationTask)
            {
                var existingNode = _workingNodes.FirstOrDefault(x => x != null && x.FullPath == currentTask.RelativeNodePath);
                if (existingNode != null)
                {
                    if (currentTask.Type == LiveEditingTaskType.CreateFile)
                    {

                        requiredFile = existingNode;
                        currentTask.Type = LiveEditingTaskType.Write;
                        _logger.LogWarning("File already existed, writing on it instead...");
                    }
                    else
                    {
                        _logger.LogError("The directory already exists, skipping directory creation task...");
                        return;
                    }
                }
                else
                {
                    requiredFile = GetExistingParent(currentTask.RelativeNodePath);
                }
            }
            else
            {
                requiredFile = _workingNodes.FirstOrDefault(x => x != null && x.FullPath == currentTask.RelativeNodePath);
            }


            if (requiredFile == null)
            {
                return;
            }

            _logger.LogInformation($"Executing task for {currentTask.RelativeNodePath} {currentTask.Type}");
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
                    if (createdNode != null)
                        _workingNodes.Add(createdNode);
                    break;
                case LiveEditingTaskType.CreateDirectory:
                    var createdDirectoryNode = requiredFile.CreateDirectory(currentTask.RelativeNodePath);
                    if (createdDirectoryNode != null)
                        _workingNodes.Add(createdDirectoryNode);
                    break;
                case LiveEditingTaskType.SetPermissions:
                    requiredFile.SetPermissions(currentTask.Permissions);
                    requiredFile.Write(requiredFile.Read());
                    break;
                case LiveEditingTaskType.SetUser:
                    requiredFile.UserId = currentTask.UserId;
                    requiredFile.Write(requiredFile.Read());
                    break;
                case LiveEditingTaskType.SetGroup:
                    requiredFile.GroupId = currentTask.GroupId;
                    requiredFile.Write(requiredFile.Read());
                    break;
                case LiveEditingTaskType.SetMode:
                    requiredFile.Mode = currentTask.Mode;
                    requiredFile.Write(requiredFile.Read());
                    break;
                case LiveEditingTaskType.ChangeParent:
                    var parent = _workingNodes.FirstOrDefault(x => x.FullPath == currentTask.ParentRelativePath);
                    if (parent != null)
                    {
                        requiredFile.Move(parent);
                    }
                    else
                    {
                        _logger.LogError($"Parent {currentTask.ParentRelativePath} not found for {currentTask.RelativeNodePath}");
                    }
                    break;
                case LiveEditingTaskType.Rename:
                    if (string.IsNullOrWhiteSpace(currentTask.Name))
                    {
                        _logger.LogError("No new name provided for rename task.");
                        break;
                    }
                    else
                    {
                        requiredFile.Name = currentTask.Name;
                        requiredFile.Write(requiredFile.Read());
                    }
                    break;
            }

            stopWatch.Stop();
            _logger.LogInformation($"Task for {currentTask.RelativeNodePath} {currentTask.Type} took {Math.Round(stopWatch.Elapsed.TotalSeconds, 1)}s");
        }

        private async Task ApplyChanges()
        {

            var dirSeparator = Path.DirectorySeparatorChar;
            var files = _workingNodes.Select(x => Path.Combine(_sourceDirectory, x.FullPath.Replace('/', dirSeparator).Replace('\\', dirSeparator))).ToList(); /// Directory.GetFileSystemEntries(_sourceDirectory, "*", SearchOption.AllDirectories);
            var localFiles = new ConcurrentBag<string>(files);
            var startingModificationTimes = new ConcurrentDictionary<string, DateTime>();


            while (KeepRunning)
            {
                var currentFiles = new ConcurrentBag<string>(Directory.GetFileSystemEntries(_sourceDirectory, "*", SearchOption.AllDirectories));
                var deletedEntries = localFiles.Except(currentFiles).OrderBy(x => x.Length).ToList();
                var toDelete = new List<string>();
                foreach (var parent in deletedEntries)
                {
                    var containerParentExist = toDelete.Any(x => parent.StartsWith(x));
                    if (containerParentExist)
                    {
                        continue;
                    }
                    toDelete.Add(parent);
                }

                if (OperatingSystem.IsWindows())
                {
                    var deleteList = new List<string>(toDelete);
                    foreach (var deletedFile in deleteList)
                    {
                        var existingCaseInsensitive = currentFiles.FirstOrDefault(x => x.Equals(deletedFile, StringComparison.CurrentCultureIgnoreCase));
                        if (existingCaseInsensitive != null)
                        {
                            toDelete.Remove(deletedFile);
                        }
                    }
                }

                var removedFiles = new ConcurrentBag<string>(toDelete);
                var addedFiles = new ConcurrentBag<string>(currentFiles.Except(localFiles));
                localFiles = currentFiles;
                await Parallel.ForEachAsync(removedFiles, (file, _) =>
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

                    var task = new LiveEditingTask()
                    {
                        RelativeNodePath = relativePath,
                        Type = LiveEditingTaskType.Delete,
                    };
                    _tasks.Enqueue(task);

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
                    _tasks.Enqueue(task);
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
                    _tasks.Enqueue(task);

                    return new ValueTask();
                });
            }
        }
    }
}
