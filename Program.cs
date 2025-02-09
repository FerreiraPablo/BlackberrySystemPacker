using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Helpers;
using BlackberrySystemPacker.Nodes;

internal class Program
{
    private static byte[] _sharedBuffet = new byte[262144];
    public static List<Func<string, string, Boolean>> Tests = new();

    private static void Main(string[] args)
    {
        var modifiedPath = @"C:\Users\habbo\Documents\BBDev\os\magicrate.Signed";
        var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);
        var originalPath = @"C:\Users\habbo\Documents\BBDev\os\magicmodded.Signed";
        var original = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 262144);
        modifiedPackage.SetLength(original.Length);
        original.CopyTo(modifiedPackage);
        original.Close();


        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var extractor = new SignedImageNodeUnpacker();
        var files = extractor.GetUnpackedNodes(modifiedPackage);
        stopWatch.Stop();
        Console.WriteLine("Unpacking time: " + stopWatch.Elapsed.TotalSeconds + "s");


        var directory = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system"));
        var deleteMatches = new List<string> {
            "com.twitter",
            "com.facebook",
            "com.evernote",
            "com.linkedin",
            "sys.paymentsystem",
            "sys.retaildemo",
            "sys.uri.youtube"
        };

        foreach (var match in deleteMatches)
        {
            var deletableFile = files.FirstOrDefault(x => x.FullPath.StartsWith("apps/" + match));
            if (deletableFile != null) { 
                deletableFile.Delete();
                files.Remove(deletableFile);
            }
        }

        var editableFiles = files.Where(x => !x.Name.Contains("\0") && x.IsUserNode && !x.FullPath.StartsWith("apps")).ToList();
        editableFiles = editableFiles.Where(x => x.Name != "com.rim.bb.app.sprintdss.bar").ToList();
        var sourceDir = @"C:\Users\habbo\Documents\BBDev\ClassicBB10x";
        if(Directory.Exists(sourceDir))
        {
            Directory.Delete(sourceDir, true);
        }

        var editingProcessor = new LiveEditingProcessor(editableFiles, sourceDir);
        editingProcessor.Build();
        editingProcessor.Start().Wait();
    }

    public static void Export()
    {
        var sourceDir = @"C:\Users\habbo\Documents\BBDev\ClassicBB10x";
        var modifiedPath = @"C:\Users\habbo\Documents\BBDev\os\magicrate.Signed";
        var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);
        //var originalPath = @"C:\Users\habbo\Documents\BBDev\os\magicmodded.Signed";
        //var original = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 262144);
        //modifiedPackage.SetLength(original.Length);
        //original.CopyTo(modifiedPackage);
        //original.Close();

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var extractor = new SignedImageNodeUnpacker();
        var files = extractor.GetUnpackedNodes(modifiedPackage);
        stopWatch.Stop();

        Console.WriteLine("Unpacking time: " + stopWatch.Elapsed.TotalSeconds + "s");
        var editableFiles = files.Where(x => !x.Name.Contains("\0") && x.IsUserNode && !x.FullPath.StartsWith("apps")).ToList();
        editableFiles = editableFiles.Where(x => x.Name != "com.rim.bb.app.sprintdss.bar").ToList();

        var editingProcessor = new LiveEditingProcessor(editableFiles, sourceDir);
        editingProcessor.Build();
    }

    public static void RunTests()
    {

        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Change of content");
           var result = false;
           var testText = "";
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));


               if (file != null)
               {
                   testText = file.ReadAllText().Replace("smarttriggers", "smarttriggars");
                   file.WriteAllText(testText);
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));

               if (file != null)
               {
                   var currentText = file.ReadAllText();
                   result = currentText == testText;
               }
               modifiedPackage.Close();
           }

           return result;
        });


        Tests.Add((_, modifiedPath) =>
        {

           Console.WriteLine("Test: File deletion");
           var result = false;
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));


               if (file != null)
               {
                   file.Delete();
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));

               if (file == null)
               {
                   result = true;
               }
               modifiedPackage.Close();
           }

           return result;
        });


        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Name change.");
           var result = false;
           var newName = "bslauncherback";
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));


               if (file != null)
               {
                   file.Name = newName;
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));

               if (file != null)
               {
                   result = file.Name == newName;
               }
               modifiedPackage.Close();
           }

           return result;
        });


        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Moving file.");
           var result = false;
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/bslauncher"));
               var parent = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/gfx"));

               if (file != null)
               {
                   file.Move(parent);
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/gfx/bslauncher"));

               if (file != null)
               {
                   result = true;
               }
               modifiedPackage.Close();
           }

           return result;
        });


        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Creating file.");
           var content = "Hello";
           var filename = "test.txt";

           var result = false;

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system"));

               if (file != null)
               {
                   content = "";
                   for (var i = 0; i < 4000; i++)
                   {
                       content += "A";
                   }
                   var createdFile = file.CreateFile(filename, Encoding.UTF8.GetBytes(content));
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/" + filename));

               if (file != null)
               {
                   var foundContent = file.ReadAllText();
                   result = foundContent == content;
               }
               modifiedPackage.Close();
           }

           return result;
        });


        Tests.Add((_, modifiedPath) =>
        {
            Console.WriteLine("Test: Creating file recursively.");
            var content = "Hello";
            var filename = "121/abc/ade/22po1/test.txt";

            var result = false;

            {
                var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

                var extractor = new SignedImageNodeUnpacker();
                var files = extractor.GetUnpackedNodes(modifiedPackage);
                var userFiles = files.Where(x => x.IsUserNode).ToList();
                var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system"));

                if (file != null)
                {
                    content = "";
                    for (var i = 0; i < 4000; i++)
                    {
                        content += "A";
                    }
                    var createdFile = file.CreateFile(filename, Encoding.UTF8.GetBytes(content));
                }
                modifiedPackage.Close();
            }

            {
                var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

                var extractor = new SignedImageNodeUnpacker();
                var files = extractor.GetUnpackedNodes(modifiedPackage);
                var userFiles = files.Where(x => x.IsUserNode).ToList();
                var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/" + filename));

                if (file != null)
                {
                    var foundContent = file.ReadAllText();
                    result = foundContent == content;
                }
                modifiedPackage.Close();
            }

            return result;
        });

        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Creating directory.");
           var dirname = "newdir";
           var result = false;
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system"));

               if (file != null)
               {
                   file.ReadAllText();
                   file.CreateDirectory(dirname);
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/" + dirname));

               if (file != null)
               {
                   result = file.IsDirectory();
               }
               modifiedPackage.Close();
           }

           return result;
        });


        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Creating long name directory.");
           var dirname = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
           var result = false;
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system"));

               if (file != null)
               {
                   file.ReadAllText();
                   file.CreateDirectory(dirname);
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("var/pps/system/" + dirname));

               if (file != null)
               {
                   result = file.IsDirectory();
               }
               modifiedPackage.Close();
           }

           return result;
        });



        Tests.Add((_, modifiedPath) =>
        {
           Console.WriteLine("Test: Deleting directory.");
           var result = false;
           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("accounts/1000/shared"));

               if (file != null)
               {
                   file.Delete();
               }
               modifiedPackage.Close();
           }

           {
               var modifiedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

               var extractor = new SignedImageNodeUnpacker();
               var files = extractor.GetUnpackedNodes(modifiedPackage);
               var userFiles = files.Where(x => x.IsUserNode).ToList();
               var file = files.FirstOrDefault(x => x.FullPath.StartsWith("accounts/1000/shared"));

               if (file == null)
               {
                   result = true;
               }
               modifiedPackage.Close();
           }

           return result;
        });

        var passed = 0;
        var failed = 0;
        var testCount = 0;
        foreach (var test in Tests)
        {
            var originalPath = @"C:\Users\habbo\Documents\BBDev\os\magicmodded.Signed";
            var modifiedPath = @"C:\Users\habbo\Documents\BBDev\os\magicrate.Signed";
            var moddedPackage = new FileStream(modifiedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);
            var original = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 262144);
            moddedPackage.SetLength(original.Length);
            original.CopyTo(moddedPackage);
            original.Close();
            moddedPackage.Close();

            testCount++;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = test(originalPath, modifiedPath);
            stopwatch.Stop();
            if (result)
            {
                passed++;
            }
            else
            {
                failed++;
            }


            Console.WriteLine($"Test {testCount} Status {(result ? "Passed" : "Failed")} - Time {stopwatch.Elapsed.TotalSeconds}s");
        }

        Console.WriteLine("Passed tests " + passed + ", Failed " + failed);
    }
    public static void SaveFiles(List<FileSystemNode> nodes, string outputPath, Func<FileSystemNode, bool> predicate = null)
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        List<string> AddedFiles = new List<string>();
        List<string> Duplicates = new List<string>();

        foreach (var node in nodes)
        {
            var path = Path.Combine(outputPath, node.FullPath);

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
                if(predicate != null && !predicate(node))
                {
                    continue;
                }

                node.Read();
                if(node.Data == null || node.Data.Length == 0)
                {
                    continue;
                }

                var directory = Path.GetFullPath(path.Replace("/" + node.Name, ""));
                var existingDirectory = Directory.Exists(directory);
                if (!existingDirectory)
                {
                    Directory.CreateDirectory(directory);
                }
                var expectedPath = path;
                Console.WriteLine($"Writing {node.FullPath}");
                File.WriteAllBytes(expectedPath, node.Data);
            }
        }

       Console.WriteLine("Duplicate count " + Duplicates.Count);
    }
}