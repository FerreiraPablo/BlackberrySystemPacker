using System.Diagnostics;

namespace BlackberrySystemPacker.Helpers.Autoloaders
{
    public class AutoloaderHelper
    {
        public static string Create(string[] signedFiles, string output = null)
        {
            output = output ?? Path.GetTempFileName() + ".Signed";

            var processInfo = new ProcessStartInfo()
            {
                FileName = File.Exists("./cap.exe") || File.Exists("./cap") ? @"./cap" : "cap",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = $"CREATE {string.Join(' ', signedFiles)} {output}"
            };

            var process = new Process()
            {
                StartInfo = processInfo
            };

            process.OutputDataReceived += (sender, e) =>
            {

            };
            process.Start();
            process.WaitForExit();

            return output;
        }
    }
}
