using System;
using System.Threading.Tasks;

namespace Elevator
{
    internal class AMDuProfCLI
    {
        private bool Enabled;
        const string BinaryPath = @"C:\Program Files\AMD\AMDuProf\bin\AMDuProfCLI.exe";

        public AMDuProfCLI(bool EnableAMDuProfCLI)
        {
            Enabled = EnableAMDuProfCLI;
        }

        public bool IsInstalled()
        {
            return System.IO.File.Exists(BinaryPath);
        }

        /// <summary>
        /// Start AMDuProfCLI.exe to log events.
        /// </summary>
        /// <param name="logFilesPath">Path to store logfiles.</param>
        /// <param name="duration">Duration for monitoring in seconds.</param>
        /// <returns>True if completed with status 0.</returns>
        public async Task<bool> Start(string logFilesPath, int duration)
        {
            if (!Enabled)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: AMDuProfCLI is disabled");
                return false;
            }

            if (!IsInstalled())
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: AMDuProfCLI is not installed");
                return false;
            }

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logFilesPath));

            bool isSuccess = false;

            string commandLine = $"collect --verbose 3 --system-wide --config power --duration {duration} --output {logFilesPath}";

            await Task.Delay(1);
            isSuccess = RunBinary.Run(BinaryPath, commandLine, true);
                        
            await Task.Delay(1);

            return isSuccess;
        }        
    }
}
