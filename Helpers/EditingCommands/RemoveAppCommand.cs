using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Helpers.EditingCommands;
using BlackberrySystemPacker.Nodes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class RemoveAppCommand : EditingCommand
    {
        public override string Description { get; set; } = "Removes an app or a set of apps from the image";

        public RemoveAppCommand() : base("rmapp", "removeapp")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid removeapp command, please provide the app package id or package id's splitted by comma");
            }
            var deleteMatches = args.Skip(1).Select(x => x.Trim()).ToList();


            if (!deleteMatches.Any())
            {
                throw new ArgumentException("Invalid command, please provide a package id.");
            }


            var appListFile = workingNodes.FirstOrDefault(x => x.FullPath == "var/pps/system/installer/registeredapps/applications");
            var random = new Random();
            if (appListFile != null)
            {
                var registeredAppsLines = appListFile.ReadAllText().Split('\n').ToList();
                var filesToEdit = new List<FileSystemNode>
                {
                    appListFile
                };
                filesToEdit.AddRange(workingNodes.Where(x => x.FullPath.StartsWith("var/etc/default_order")));
                filesToEdit.AddRange(workingNodes.Where(x => x.FullPath.StartsWith("var/pps/system/navigator/invokes/invoke")));
                filesToEdit.AddRange(workingNodes.Where(x => x.FullPath.StartsWith("var/pps/system/installer/appdetails/applications")));
                filesToEdit.Add(workingNodes.FirstOrDefault(x => x.FullPath == "var/pps/system/navigator/applications/applications"));
                filesToEdit.Add(workingNodes.FirstOrDefault(x => x.FullPath == "var/pps/system/navigator/urls"));
                filesToEdit.Add(workingNodes.FirstOrDefault(x => x.FullPath == "var/pps/system/bslauncher"));

                var gidFiles = workingNodes.Where(x => x.FullPath.StartsWith("apps/gid2app")).ToList();
                var gidDictionary = new Dictionary<string, FileSystemNode>();
                Stopwatch sw = new Stopwatch();
                Logger.LogInformation($"Found {gidFiles.Count} gid files.");
                sw.Start();
                foreach (var gid in gidFiles)
                {
                    var appId = gid.ReadAllText().Replace("/apps/", "");
                    gidDictionary[appId] = gid;
                }
                sw.Stop();
                Logger.LogInformation($"Gid files read in {Math.Round(sw.Elapsed.TotalSeconds, 1)}s.");

                Logger.LogInformation($"Reading app references...");
                sw.Restart();
                var filesContent = new Dictionary<string, string>();
                foreach (var file in filesToEdit)
                {
                    if (file == null)
                    {
                        continue;
                    }

                    filesContent[file.FullPath] = file.ReadAllText();
                }
                sw.Stop();
                Logger.LogInformation($"App references read in {Math.Round(sw.Elapsed.TotalSeconds, 1)}s.");


                var appsDirectory = workingNodes.First(x => x.FullPath == "apps");

                Logger.LogInformation($"Setting up the deletion pipeline...");
                sw.Restart();

                foreach (var match in deleteMatches)
                {
                    var appLine = registeredAppsLines.FirstOrDefault(x => x.StartsWith(match));
                    if (appLine == null)
                    {
                        Logger.LogWarning($"App {match} not found in the registered apps list.");
                        continue;
                    }

                    var appId = appLine.Split(',')[0];
                    appId = appId.Split("::")[0];

                    var appDirectory = appsDirectory.Children.FirstOrDefault(x => x.FullPath == "apps/" + appId);
                    var appContent = appDirectory.Children;
                    foreach (var appDir in appContent)
                    {
                        tasks.Enqueue(new LiveEditingTask()
                        {
                            RelativeNodePath = appDir.FullPath,
                            Type = LiveEditingTaskType.Delete
                        });
                    }

                    tasks.Enqueue(new LiveEditingTask()
                    {
                        RelativeNodePath = appDirectory.FullPath,
                        Type = LiveEditingTaskType.Rename,
                        Name = "apps/DELETED_" + (random.Next(999999)).ToString().PadLeft(6)
                    });

                    var gidFile = gidDictionary.Where(x => x.Key.Contains(appId)).Select(x => x.Value).FirstOrDefault();
                    if (gidFile != null)
                    {
                        tasks.Enqueue(new LiveEditingTask()
                        {
                            RelativeNodePath = gidFile.FullPath,
                            Type = LiveEditingTaskType.Delete
                        });
                    }

                    foreach (var file in filesContent)
                    {
                        var currentContent = file.Value.Split('\n').ToList();
                        var removedLines = currentContent.RemoveAll(x => x.StartsWith(match));
                        if (removedLines == 0)
                        {
                            continue;
                        }

                        var data = string.Join('\n', currentContent);
                        filesContent[file.Key] = data;
                    }
                }

                foreach (var file in filesContent)
                {
                    var currentContent = workingNodes.FirstOrDefault(x => x.FullPath == file.Key);
                    if (currentContent == null)
                    {
                        continue;
                    }

                    var data = currentContent.ReadAllText();
                    if (data == file.Value)
                    {
                        continue;
                    }

                    tasks.Enqueue(new LiveEditingTask()
                    {
                        RelativeNodePath = file.Key,
                        Type = LiveEditingTaskType.Write,
                        Data = Encoding.ASCII.GetBytes(file.Value)
                    });
                }
                sw.Stop();
                Logger.LogInformation($"Deletion pipeline set in {Math.Round(sw.Elapsed.TotalSeconds, 1)}s.");

                Logger.LogInformation($"App(s) {string.Join(", ", deleteMatches)} deletion pipeline set.");
            }
        }
    }
}
