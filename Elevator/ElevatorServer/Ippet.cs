using System.Threading.Tasks;

namespace Elevator
{
    // https://github.yandex-team.ru/oneumyvakin/ippet
    internal class Ippet
    {
        private bool Enabled;
        const string BinaryPath = @"ippet\ippet.exe";

        public Ippet(bool ippetEnabled)
        {
            Enabled = ippetEnabled;
        }

        /// <summary>
        /// Start ippet\ipppet.exe to log events in TSV format.
        /// </summary>
        /// <param name="logFilePrefix">Log file prefix in TSV format will be saved as prefix_processes.xls.</param>
        /// <param name="duration">Duration for monitoring in seconds.</param>
        /// <returns>True if completed with status 0.</returns>
        public async Task<bool> Start(string logFilePrefix, int duration)
        {
            if (!Enabled)
            {
                return false;
            }

            bool isSuccess = false;

            string commandLine = $"-o y -enable_web n -zip n -time_end {duration} -log_file {logFilePrefix}";

            await Task.Delay(1);
            isSuccess = RunBinary.Run(BinaryPath, commandLine);
                        
            await Task.Delay(1);

            return isSuccess;
        }        
    }
}
