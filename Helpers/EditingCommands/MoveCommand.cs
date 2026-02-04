using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Nodes;
using System.Collections.Concurrent;

namespace BlackberrySystemPacker.Helpers.EditingCommands
{
    public class MoveCommand : EditingCommand
    {
        public override string Description { get; set; } = "Push a file from your system to the image.";

        public MoveCommand() : base("mv", "move", "rename")
        {
        }

        public override void Execute(ConcurrentQueue<LiveEditingTask> tasks, List<FileSystemNode> workingNodes, string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Invalid move command, please provide an origin and a destination path.");
            }
            var originalFile = GetValidPath(args[1]);
            if (string.IsNullOrWhiteSpace(originalFile))
            {
                throw new ArgumentException("Invalid move command, please provide a file path.");
            }

            var destinationPath = GetValidPath(args[2]);
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Invalid move command, please provide a destination path.");
            }
            var existingToMoveFile = workingNodes.FirstOrDefault(x => x.FullPath == originalFile);
            if(existingToMoveFile == null)
            {
                throw new ArgumentException("The specified original file does not exist in the image, please provide a valid file from your system.");
            }

            var internalTask = new List<LiveEditingTask>();
            var destinationNode = workingNodes.FirstOrDefault(x => x.FullPath == destinationPath);

            // Case 1: Destination exists and is a directory -> Move source INTO destination
            if (destinationNode != null && destinationNode.IsDirectory())
            {
                internalTask.Add(new LiveEditingTask()
                {
                    RelativeNodePath = existingToMoveFile.FullPath,
                    Type = LiveEditingTaskType.ChangeParent,
                    ParentRelativePath = destinationNode.FullPath
                });
            }
            // Case 2: Destination exists and is a file -> Error (or overwrite, but safety first: Error)
            else if (destinationNode != null && !destinationNode.IsDirectory())
            {
                 throw new ArgumentException($"Destination '{destinationPath}' already exists and is a file.");
            }
            // Case 3: Destination does not exist -> Rename/Move to new path
            else
            {
                // Verify parent of destination exists
                var parentPath = Path.GetDirectoryName(destinationPath)?.Replace("\\", "/");
                var parentNode = string.IsNullOrEmpty(parentPath) ? null : workingNodes.FirstOrDefault(x => x.FullPath == parentPath);

                 // If parent path is empty or root, we assume root exists effectively if we are here (conceptually) 
                 // but checking if parent node exists in workingNodes is safer if we aren't at root.
                 // However, "root" usually isn't in workingNodes list as a single node often? 
                 // Let's rely on standard logic: if parentPath is not empty, check it.
                 
                if (!string.IsNullOrEmpty(parentPath) && parentNode == null)
                {
                     // Try finding it if maybe it's just not in the list explicitly but logic allows? 
                     // Actually, if we are moving /a/b to /c/d, and /c doesn't exist, we should fail.
                     throw new ArgumentException($"Destination directory '{parentPath}' does not exist.");
                }

                // If source and dest correspond to same node (already handled by diff check effectively above if dest existed)
                
                // We need to:
                // 1. Rename the source to the new file name
                // 2. Change parent if the directory is different

                var newFileName = Path.GetFileName(destinationPath);
                var currentParentPath = Path.GetDirectoryName(existingToMoveFile.FullPath)?.Replace("\\", "/");

                // Rename task
                if (existingToMoveFile.Name != newFileName)
                {
                     internalTask.Add(new LiveEditingTask()
                    {
                        RelativeNodePath = existingToMoveFile.FullPath,
                        Type = LiveEditingTaskType.Rename,
                        Name = newFileName
                    });
                }

                // Move task (Change Parent)
                // Note: If we renamed above, the node path in memory object might be updated by processor? 
                // LiveEditingProcessor implementation of Rename sets .Name property. 
                // ChangeParent uses .Move(parent) which likely relies on the object reference or finding it again?
                // LiveEditingProcessor.cs:411 finds node by RelativeNodePath. 
                // If we queue Rename then ChangeParent, the first task runs, updates Name. 
                // The second task tries to find node by RelativeNodePath using existingToMoveFile.FullPath.
                // existingToMoveFile.FullPath is a property that combines Path + Name. 
                // If the object instance is updated by the first task, FullPath should reflect the new name.
                // So subsequent lookups might fail if they search by OLD path provided in the task struct vs current object state?
                // LiveEditingProcessor:
                // case Rename: requiredFile.Name = currentTask.Name; requiredFile.Apply();
                // case ChangeParent: var parent = ...; requiredFile.Move(parent);
                
                // Wait, LiveEditingProcessor finds 'requiredFile' via `GetExistingParent` or direct lookup based on `currentTask.RelativeNodePath`.
                // If we queue two tasks:
                // 1. Rename: RelativeNodePath = "old/path/oldname" -> finds node -> renames to "newname".
                // 2. ChangeParent: RelativeNodePath = "old/path/oldname" -> finds node? 
                // If Rename updated the in-memory node's Name, its FullPath changed to "old/path/newname".
                // If we pass "old/path/oldname" to the second task, lookup will FAIL because node is now at "old/path/newname".
                
                // However, we can simply change parent FIRST, then rename? 
                // If we change parent: "old/path/oldname" moved to "new/parent/oldname".
                // Then rename: "new/parent/oldname" -> "new/parent/newname".
                
                // Let's see ChangeParent implementation in LiveEditingProcessor:
                // requiredFile.Move(parent);
                
                // UserSystemNode.Move: 
                // realParent.IncludeMetadata(RemoveParentMetadata());
                // This removes from old parent, adds to new parent.
                // It does NOT explicitly update the `Path` property of the `existingToMoveFile` if it's cached?
                // UserSystemNode.FullPath uses `Path` + `Name`. 
                // Warning: `Path` property on FileSystemNode is explicitly Set during Create/Load. 
                // Does Move update the `Path` property of the node itself?
                // Reading UserSystemNode.cs again... Move does NOT seem to update `.Path` on the child node recursively...
                // Wait, `FullPath` property: `System.IO.Path.Combine(Path, Name)`. 
                // If `Path` isn't updated, FullPath is wrong after Move?
                // `UserSystemNode` seems to rely on recursive structure but `Path` property might be static string?
                // Let's look at `UserSystemNode.cs` line 23: `public string FullPath { get => !string.IsNullOrWhiteSpace(Name) ? System.IO.Path.Combine(Path, Name).Replace("\\","/") : ""; }`
                // And `Path` property line 19.
                // In `Create`, `childNode.Path = FullPath;` (line 240).
                // It seems `Path` IS stored on the node.
                // `Move` (line 388) calls `RemoveParentMetadata` and `IncludeMetadata`. It touches disk structure. 
                // It does NOT appear to update the `.Path` property of the `UserSystemNode` object in memory.
                
                // This suggests that `Move` updates the persistent state, but the in-memory `FileSystemNode` object might have stale `Path`?
                // However, `LiveEditingProcessor` re-reads or similar? No, it keeps `_workingNodes` in memory.
                // This looks like a potential existing bug or limitation in `UserSystemNode` OR I am missing where `Path` is updated.
                // If `Path` is not updated, subsequent lookups by FullPath will fail if relying on old Path.
                
                // BUT, for `LiveEditingProcessor`, we usually finding nodes by iterating `_workingNodes`. 
                // If `FullPath` is wrong, we can't find it.
                
                // Strategy: 
                // If we move across directories (ChangeParent needed), we should do that.
                // If we rename (Name change needed), we should do that.
                
                // If we do both:
                // If we rename first: Node stays in old parent, Name changes. FullPath becomes `old/path/newname`.
                // Then Move: We need to target `old/path/newname` to Find it.
                // So 2nd task must use the NEW path if we proceed sequentially and rely on lookup.
                
                // HOWEVER, `LiveEditingProcessor` loop:
                // It dequeues task, finds `requiredFile`.
                // If we renamed it in step 1, step 2 must use the new path to find it.
                
                // So:
                // 1. Rename task: Path = `originalFile`. Name = `newFileName`.
                // newPathAfterRename = pathCombine(originalParent, newFileName).
                // 2. ChangeParent task: Path = `newPathAfterRename`. Parent = `destinationParent`.
                
                if (currentParentPath != parentPath && !string.IsNullOrEmpty(parentPath)) 
                {
                    // Need to change parent
                    
                    if (existingToMoveFile.Name != newFileName)
                    {
                        // Rename first
                        internalTask.Add(new LiveEditingTask()
                        {
                            RelativeNodePath = existingToMoveFile.FullPath,
                            Type = LiveEditingTaskType.Rename,
                            Name = newFileName
                        });
                        
                        // Then move, pointing to the renamed file
                        var intermediatePath = Path.Combine(currentParentPath ?? "", newFileName).Replace("\\", "/");
                         internalTask.Add(new LiveEditingTask()
                        {
                            RelativeNodePath = intermediatePath,
                            Type = LiveEditingTaskType.ChangeParent,
                            ParentRelativePath = parentPath
                        });
                    }
                    else
                    {
                        // Just move
                         internalTask.Add(new LiveEditingTask()
                        {
                            RelativeNodePath = existingToMoveFile.FullPath,
                            Type = LiveEditingTaskType.ChangeParent,
                            ParentRelativePath = parentPath
                        });
                    }
                }
                else
                {
                    // Same parent, just rename
                    if (existingToMoveFile.Name != newFileName)
                    {
                         internalTask.Add(new LiveEditingTask()
                        {
                            RelativeNodePath = existingToMoveFile.FullPath,
                            Type = LiveEditingTaskType.Rename,
                            Name = newFileName
                        });
                    }
                }
            }

            foreach(var task in internalTask)
            {
                tasks.Enqueue(task);
            }
        }
    }
}
