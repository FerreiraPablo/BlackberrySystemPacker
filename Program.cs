using BlackberrySystemPacker.Core;
using BlackberrySystemPacker.Helpers.Autoloaders;
using BlackberrySystemPacker.Helpers.Debugging;
using BlackberrySystemPacker.Nodes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

internal class Program
{

    private static bool _keepSystemNodes = false;

    private static ILogger _logger;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Program))]
    private static void Main(string[] args)
    {

        _logger = new CustomLogger("BlackberrySystemPacker", LogLevel.Information);

        Console.WriteLine("Blackberry Signed Image Patcher V0.0.1 BETA - By Pablo Ferreira");
        Console.WriteLine("Currently only edits the User Image (QNX6 FS) of the OS.");
        Console.WriteLine("This program is not responsible for any damage caused to your device, use at your own risk.");
        Console.WriteLine("");


        var options = GetOptions(args);
        var procedure = args.Length > 0 ? args[0].ToUpper() : null;
        if (procedure != null)
        {
            options["procedure"] = procedure;
        }
        else
        {
            var tasksFile = options.GetValueOrDefault("tasksFile") ?? options.GetValueOrDefault("tf");
            if (tasksFile != null)
            {
                if (File.Exists(tasksFile))
                {
                    var tasks = File.ReadAllText(tasksFile);
                    var taskList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(tasks);
                    foreach (var task in taskList)
                    {
                        RunProcedure(task);
                    }
                }
            }
        }

        RunProcedure(options);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Program))]
    public static void RunProcedure(Dictionary<string, string> options)
    {

        var config = options.GetValueOrDefault("config") ?? options.GetValueOrDefault("c");
        if (config != null)
        {
            if (File.Exists(config))
            {
                try
                {
                    _logger.LogInformation("Loading config file " + config);
                    var configFile = File.ReadAllText(config);
                    var newOptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(configFile);
                    foreach (var newOption in newOptions)
                    {
                        options[newOption.Key] = newOption.Value;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("The config file provided is not valid. {0}", e.Message);
                }
            }
            else
            {
                _logger.LogError("The config file provided does not exist.");
            }
        }

        string[] validProcedures = ["AUTOPATCH", "EDIT", "HELP"];
        var procedure = options.GetValueOrDefault("procedure") ?? options.GetValueOrDefault("p") ?? "HELP";
        procedure = validProcedures.Contains(procedure.ToUpper()) ? procedure.ToUpper() : "HELP";

        var radioFile = options.GetValueOrDefault("radio") ?? options.GetValueOrDefault("r");
        var signedFile = options.GetValueOrDefault("os");
        var outputDirectory = options.GetValueOrDefault("outputdir") ?? options.GetValueOrDefault("od") ?? Directory.GetCurrentDirectory();
        var outputFile = options.GetValueOrDefault("outputfile") ?? options.GetValueOrDefault("of");
        var workspaceDir = options.GetValueOrDefault("workspace") ?? options.GetValueOrDefault("w") ?? Path.Combine(outputDirectory, "Workspace");
        var autoloader = options.GetValueOrDefault("autoloader") ?? options.GetValueOrDefault("al");
        var writeInput = options.GetValueOrDefault("writeinput") ?? options.GetValueOrDefault("wi");
        var systemNodes = options.GetValueOrDefault("systemnodes") ?? options.GetValueOrDefault("sn");
        if (writeInput != null)
        {
            outputFile = signedFile;
        }

        if (outputFile == null && outputDirectory != null)
        {
            var extension = Path.GetExtension(signedFile);
            outputFile = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(signedFile) + "-MOD" + extension);
        }

        var validArguments = true;
        if (procedure != "HELP")
        {
            if (outputFile == null)
            {
                _logger.LogError("Output file or output directory is required for all operations.");
                validArguments = false;
            }

            if (signedFile == null)
            {
                _logger.LogError("OS file is required for all operations.");
                validArguments = false;
            }
            else if (!File.Exists(signedFile))
            {
                _logger.LogError("OS file does not exist.");
                validArguments = false;
            }

            if (autoloader != null)
            {
                if (outputDirectory == null)
                {
                    _logger.LogError("Output directory is required.");
                    validArguments = false;
                }

                if (radioFile == null)
                {
                    _logger.LogError("Radio file is required for autoloader generation.");
                    validArguments = false;
                }
                else if (!File.Exists(radioFile))
                {
                    _logger.LogError("Radio file does not exist.");
                    validArguments = false;
                }
            }

            if (procedure == "EDIT")
            {
                if (workspaceDir == null)
                {
                    _logger.LogError("Workspace directory is required for editing.");
                    validArguments = false;
                }
                else if (!Directory.Exists(workspaceDir))
                {
                    Directory.CreateDirectory(workspaceDir);
                }
            }
        }

        if (validArguments == false)
        {
            return;
        }

        var modifiedFile = "";
        string executableFile = AppDomain.CurrentDomain.FriendlyName;

        switch (procedure)
        {
            case "AUTOPATCH":
                modifiedFile = Patch(signedFile, outputFile);
                break;
            case "EDIT":
                modifiedFile = Export(workspaceDir, signedFile, outputFile);
                break;
            case "HELP":
                Console.WriteLine($"Usage: {executableFile} [AUTOPATCH|EDIT|HELP] [OPTIONS]");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Modes");
                Console.WriteLine("AUTOPATCH - Patch a signed OS file to remove generaly recognizable bloatware or obsolete apps.");
                Console.WriteLine("EDIT - Export a signed OS file to a workspace directory for editing live editing.");
                Console.WriteLine("HELP - Shows this documentation.");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Options:");
                Console.WriteLine("--config|c [path]: \nPath to config file that fills any of the parameters defined for this program, for ease of use.");
                Console.WriteLine("");
                Console.WriteLine("--os [path] - REQUIRED: \nPath to the signed OS file.");
                Console.WriteLine("");
                Console.WriteLine("--radio [path] - (Only for generating an autoloader with --autoloader): \nPath to the signed Radio file.");
                Console.WriteLine("");
                Console.WriteLine("--outputDir [path]: \nPath to the output directory, if undefined the current directory will be used.");
                Console.WriteLine("");
                Console.WriteLine("--outputFile [path]: \n Path to the output directory, if undefined it will be created automatically on the output directory.");
                Console.WriteLine("");
                Console.WriteLine("--workspace [path] - (Only for EDIT mode) \nPath to the workspace directory, where all files will be exported for editing, if undefined a workspace directory will be created on the output directory.");
                Console.WriteLine("");
                Console.WriteLine("--autoloader: \nGenerates an autoloader on the output directory. (Requires a CAP binary in the current directory)");
                Console.WriteLine("");
                Console.WriteLine("--writeinput: \nTo write all changes on top of the input os file, instead of creating a new one on the output directory or using the outputFile.");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Examples:");
                Console.WriteLine($"{executableFile} AUTOPATCH --os <path_to_os_file> --output <output_directory>");
                Console.WriteLine("");
                Console.WriteLine($"{executableFile} AUTOPATCH --os <path_to_os_file> --radio <path_to_radio_file> --output <output_directory> --autoloader");
                Console.WriteLine("");
                Console.WriteLine($"{executableFile} EDIT --os <path_to_os_file> --output <output_directory> --workspace <workspace_directory>");
                Console.WriteLine("");
                Console.WriteLine($"{executableFile} EDIT --os <path_to_os_file> --radio <path_to_radio_file> --output <output_directory> --workspace <workspace_directory> --autoloader");
                Console.WriteLine("");
                Console.WriteLine($"{executableFile} HELP");
                break;
            default:
                Console.WriteLine("Invalid procedure.");
                break;
        }

        if (string.IsNullOrEmpty(modifiedFile))
        {
            return;
        }
        _logger.LogInformation("File exported to " + modifiedFile);

        if (autoloader != null)
        {
            var autoloaderPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(radioFile).Replace("Radio_", "") + "-MOD.exe");
            _logger.LogInformation("Creating autoloader at " + autoloaderPath);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            AutoloaderHelper.Create(new[]
            {
               modifiedFile,
               radioFile
            }, autoloaderPath);

            stopWatch.Stop();
            _logger.LogInformation("Autoloader created at " + autoloaderPath + ", operation took" + Math.Round(stopWatch.Elapsed.TotalSeconds, 1) + "s");
        }
    }


    public static Dictionary<string, string> GetOptions(string[] arg)
    {
        var options = new Dictionary<string, string>();
        for (var i = 0; i < arg.Length; i++)
        {
            var currentArgument = arg[i];
            var value = arg.Length > i + 1 ? arg[i + 1] : "true";

            if (currentArgument.StartsWith("--"))
            {
                var key = currentArgument.Substring(2).ToLower();
                options[key] = value;
            }
            else if (arg[i].StartsWith("-"))
            {
                var key = currentArgument.Substring(1).ToLower();
                options[key] = value;
            }
        }
        return options;
    }


    public static Stream GetWorkStream(string originalFile, string outputFile)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        _logger.LogInformation("Setting up work file...");
        outputFile = outputFile ?? originalFile;
        var modifiedPackage = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 262144);

        if (originalFile != outputFile)
        {
            var originalPath = originalFile;
            var original = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 262144);
            modifiedPackage.SetLength(original.Length);
            original.CopyTo(modifiedPackage);
            original.Close();
        }
        stopwatch.Stop();
        _logger.LogInformation("Work file set up, operation took " + Math.Round(stopwatch.Elapsed.TotalSeconds, 1) + "s");
        return modifiedPackage;
    }

    public static string Patch(string originalFile, string outputFile = null)
    {
        var modifiedPackage = GetWorkStream(originalFile, outputFile);
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        _logger.LogInformation("Unpacking image...");
        var extractor = new SignedImageNodeUnpacker();
        var allFiles = extractor.GetUnpackedNodes(modifiedPackage);
        stopWatch.Stop();
        _logger.LogInformation("Unpacked, operation took " + Math.Round(stopWatch.Elapsed.TotalSeconds, 1) + "s");

        stopWatch.Restart();

        var systemFiles = allFiles.Where(x => !x.IsUserNode).ToList();

        var userFiles = allFiles.Where(x => x.IsUserNode).ToList();

        var apps = userFiles.Where(x => x.FullPath == "apps").ToList();

        var deleteMatches = new List<string> {
            "com.twitter",
            "com.evernote",
            "com.linkedin",
            "com.tcs.maps",
            "com.rim.bb.app.facebook",
            "com.rim.bb.app.retaildemoshim",
            "sys.socialconnect.linkedin",
            "sys.socialconnect.twitter",
            "sys.socialconnect.youtube",
            "sys.socialconnect.facebook",
            "sys.cfs.box",
            "sys.cfs.dropbox",
            "sys.uri.youtube",
            "sys.weather",
            "sys.bbm",
            "sys.appworld",
            "sys.howto",
            "sys.help",
            "sys.firstlaunch",
            "sys.deviceswitch",
            "sys.paymentsystem",
            "sys.setupbuffet"
        };

        var breakMeta = new List<string>
        {
            "com.evernote"
        };

        _logger.LogInformation("Deleting bloatware for block liberation.");

        var appListFile = userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/installer/registeredapps/applications");
        if (appListFile != null)
        {
            var registeredAppsLines = appListFile.ReadAllText().Split('\n').ToList();
            var filesToEdit = new List<FileSystemNode>();
            filesToEdit.AddRange(userFiles.Where(x => x.FullPath.StartsWith("var/etc/default_order")));
            filesToEdit.AddRange(userFiles.Where(x => x.FullPath.StartsWith("var/pps/system/navigator/invokes/invoke")));
            filesToEdit.AddRange(userFiles.Where(x => x.FullPath.StartsWith("var/pps/system/installer/appdetails/applications")));
            filesToEdit.Add(userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/navigator/applications/applications"));
            filesToEdit.Add(userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/navigator/urls"));
            filesToEdit.Add(userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/bslauncher"));

            var gidFiles = userFiles.Where(x => x.FullPath.StartsWith("apps/gid2app")).ToList();
            var gidDictionary = new Dictionary<string, FileSystemNode>();

            foreach (var gid in gidFiles)
            {
                var appId = gid.ReadAllText().Replace("/apps/", "");
                gidDictionary[appId] = gid;
            }

            foreach (var match in deleteMatches)
            {
                var appLine = registeredAppsLines.FirstOrDefault(x => x.StartsWith(match));
                if (appLine == null)
                    continue;


                _logger.LogInformation($"Deleting {match}");

                var appId = appLine.Split(',')[0];
                appId = appId.Split("::")[0];


                if (breakMeta.Contains(match))
                {
                    var appMeta = userFiles.FirstOrDefault(x => x.FullPath == "apps/" + appId + "/META-INF/MANIFEST.MF");

                    if (appMeta != null)
                    {
                        _logger.LogInformation("Breaking meta of " + appId);
                        appMeta.WriteAllText(appMeta.ReadAllText().Replace("Application-Development-Mode: false", "Application-Development-Mode: falsa"));
                    }
                }

                var gidFile = gidDictionary.Where(x => x.Key.Contains(appId)).Select(x => x.Value).FirstOrDefault();
                if (gidFile != null)
                {
                    _logger.LogInformation($"Removing GID File {gidFile.FullPath} of {appId}");
                    gidFile.Delete();
                }

                foreach (var file in filesToEdit)
                {
                    if (file == null)
                        continue;

                    var fileContent = file.ReadAllText().Split('\n').ToList();
                    fileContent.RemoveAll(x => x.StartsWith(match));
                    file.WriteAllText(string.Join('\n', fileContent));
                }

                registeredAppsLines.RemoveAll(x => x.StartsWith(appId));
            }

            appListFile.WriteAllText(string.Join("\n", registeredAppsLines));

        }

        var navigatorConfigFile = userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/navigator/config");
        navigatorConfigFile.WriteAllText(navigatorConfigFile.ReadAllText().Replace("autorun::1", "autorun::0").Replace(",\"com.amazon\"", ""));


        var appConfig = userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/appconfig/sys.settings");
        appConfig.WriteAllText(appConfig.ReadAllText().Replace("false", "true"));

        var bbadsConf = userFiles.FirstOrDefault(x => x.FullPath == "var/pps/services/bbads/configuration");
        bbadsConf.WriteAllText(bbadsConf.ReadAllText().Replace("www.blackberry.com/app_includes/asdk", "service.waitberry.com"));

        var otaConfigFile = userFiles.FirstOrDefault(x => x.FullPath == "var/pps/system/ota/serverurls");
        otaConfigFile.WriteAllText(otaConfigFile.ReadAllText()
            .Replace("cs.sl.blackberry.com", "service.waitberry.com")
            .Replace("cp256.pushapi.na.blackberry.com", "service.waitberry.com")
            .Replace("cse.dcs.blackberry.com", "service.waitberry.com")
            .Replace("cse.doc.blackberry.com", "service.waitberry.com"));


        stopWatch.Stop();
        _logger.LogInformation($"Bloatware removed, operation took {Math.Round(stopWatch.Elapsed.TotalSeconds, 1)}s");
        modifiedPackage.Close();
        return outputFile;
    }

    public static string Export(string workspaceDir, string originalFile, string outputFile = null)
    {
        var modifiedPackage = GetWorkStream(originalFile, outputFile);
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        _logger.LogInformation("Unpacking image...");
        var extractor = new SignedImageNodeUnpacker();
        var files = extractor.GetUnpackedNodes(modifiedPackage);
        var editableFiles = files.Where(x => !x.Name.Contains("\0")).ToList();

        if(!_keepSystemNodes)
        {
            editableFiles = editableFiles.Where(x => x.IsUserNode).ToList();
        }

        stopWatch.Stop();
        _logger.LogInformation("Unpacked, operation took " + Math.Round(stopWatch.Elapsed.TotalSeconds, 1) + "s");
        var editingProcessor = new LiveEditingProcessor(editableFiles, workspaceDir, _logger);
        editingProcessor.Build();
        editingProcessor.Start().Wait();
        modifiedPackage.Close();
        return outputFile;
    }
}