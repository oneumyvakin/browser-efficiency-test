using System;
using System.Threading.Tasks;

namespace Elevator
{
    internal class SocWatch
    {
        private bool Enabled;
        const string BinaryPath = @"socwatch\socwatch.exe";

        public SocWatch(bool SocWatchEnabled)
        {
            Enabled = SocWatchEnabled;
        }

        /// <summary>
        /// Start socwatch\socwatch.exe to log events.
        /// </summary>
        /// <param name="logFilesPath">Path to store logfiles.</param>
        /// <param name="duration">Duration for monitoring in seconds.</param>
        /// <returns>True if completed with status 0.</returns>
        public async Task<bool> Start(string logFilesPath, int duration)
        {
            if (!Enabled)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: SocWatch is disabled");
                return false;
            }

            bool isSuccess = false;

            string commandLine = $"--polling --interval 1 --max-detail -f sys --time {duration} -o {logFilesPath}";

            await Task.Delay(1);
            isSuccess = RunBinary.Run(BinaryPath, commandLine);
                        
            await Task.Delay(1);

            return isSuccess;
        }        
    }
}
