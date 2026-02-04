using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class PullCommand : EditingCommand
    {
        public override string Description { get; set; } = "Pull a file from the image to your system.";

        public PullCommand() : base("pull", "pullfile")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid pull command, please provide an origin (image) and a destination path (local).");
            }
            var originPath = GetValidPath(args[1]);
            var destinationPath = args[2];

            if (string.IsNullOrWhiteSpace(originPath))
            {
                throw new ArgumentException("Invalid command, please provide a file path.");
            }

            var node = workingNodes.FirstOrDefault(x => x.FullPath == originPath);

            if (node == null)
            {
                throw new ArgumentException($"Node not found at path: {originPath}");
            }

            if (node.IsDirectory())
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                PullRecursive(node, destinationPath);
            }
            else
            {
                if (Directory.Exists(destinationPath))
                {
                    destinationPath = Path.Combine(destinationPath, node.Name);
                }

                var dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(destinationPath, node.Read());
                Console.WriteLine($"Pulled {node.FullPath} to {destinationPath}");
            }
        }

        private void PullRecursive(FileSystemNode node, string localDestination)
        {
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var childLocalPath = Path.Combine(localDestination, child.Name);
                    if (child.IsDirectory())
                    {
                        Directory.CreateDirectory(childLocalPath);
                        PullRecursive(child, childLocalPath);
                    }
                    else
                    {
                        File.WriteAllBytes(childLocalPath, child.Read());
                        Console.WriteLine($"Pulled {child.FullPath} to {childLocalPath}");
                    }
                }
            }
        }
    }
}
