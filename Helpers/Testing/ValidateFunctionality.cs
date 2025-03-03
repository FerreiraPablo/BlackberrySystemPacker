using BlackberrySystemPacker.Core;
using System.Diagnostics;
using System.Text;

namespace BlackberrySystemPacker.Helpers.Testing
{
    internal class ValidateFunctionality
    {
        public static List<Func<string, string, bool>> Tests = new();

        public static void RunTests(string originalPath)
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
                        result = foundContent == content && !file.IsDirectory();
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
                        result = foundContent == content && !file.IsDirectory();
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
                var modifiedPath = Path.GetTempFileName();
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
    }
}
