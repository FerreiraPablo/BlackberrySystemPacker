using System.Diagnostics;

namespace BlackberrySystemPacker.Decompressors
{
    public class NativeLzoDecompressor: IDecompressor
    {
        private byte[] _workMemory = new byte[16384 * 4];

        public NativeLzoDecompressor()
        {
        }

        public byte[] Compress(byte[] data)
        {
            var temporaryFile = Path.GetTempFileName();
            File.WriteAllBytes(temporaryFile, data);

            var temporaryOutputFile = Path.GetTempFileName();

            var processInfo = new ProcessStartInfo()
            {
                FileName = @"C:\bin\lzop.exe",
                Arguments = $"-9 {Path.GetFullPath(temporaryFile)} -o {Path.GetFullPath(temporaryOutputFile)} -f -f --quiet --no-mode --no-name -P"
            };

            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardInput = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;

            var process = Process.Start(processInfo);
            process.WaitForExit();


            var input = File.ReadAllBytes(temporaryFile);
            var result = File.ReadAllBytes(temporaryOutputFile);
            var results = new List<byte>();

            var byteSuccessionRequired = new byte[] { 0x2E, 0x74, 0x6D, 0x70 };
            var headerDone = false;
            for(var i = 0; i < result.Length; i++)
            {
                if(!headerDone) { 
                    var successiveBytes = new byte[4] { 
                        result[i],
                        result[i+1],
                        result[i+2],
                        result[i+3],
                    };

                    if(MatchingBytes(successiveBytes, byteSuccessionRequired))
                    {
                        i += 20;
                        headerDone = true;
                    } else
                    {
                        continue;
                    }
                }

                results.Add(result[i]);
            }

            for(var i = 0; i < 4; i++)
            {

                results.RemoveAt(results.Count - 1);
            }

            File.Delete(temporaryOutputFile);
            File.Delete(temporaryFile);
            return results.ToArray();
        }

        private bool MatchingBytes(byte[] bytes, byte[] expectedByte)
        {
            if(bytes.Length != expectedByte.Length)
                return false;

            for (var i = 0; i < bytes.Length; i++)
            {
                if(expectedByte[i] != bytes[i]) { 
                    return false;
                }
            }

            return true;
        }

        public byte[] Decompress(byte[] data)
        {
            var temporaryFile = Path.GetTempFileName();
            File.WriteAllBytes(temporaryFile, data);

            var temporaryOutputFile = Path.GetTempFileName();

            var processInfo = new ProcessStartInfo()
            {
                FileName = @"C:\bin\lzop.exe",
                Arguments = $"-d {Path.GetFullPath(temporaryFile)} -o {Path.GetFullPath(temporaryOutputFile)} -f -f --quiet --no-mode --no-name -P"
            };

            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardInput = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;

            var process = Process.Start(processInfo);
            process.WaitForExit();

            var result = File.ReadAllBytes(temporaryOutputFile);
            File.Delete(temporaryOutputFile);
            File.Delete(temporaryFile);
            return data;
        }
    }
}
