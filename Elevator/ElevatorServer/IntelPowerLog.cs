using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Elevator
{
    internal class IntelPowerLog
    {
        private bool Enabled;
        private bool GpuRestartEnabled;
        
        const string IntelPowerLogBin = @"C:\Program Files\Intel\Power Gadget 3.5\PowerLog3.0.exe";
        const string DevCon = @"C:\Program Files (x86)\Windows Kits\10\tools\x64\devcon.exe";

        public bool IsInstalled()
        {
            if (System.IO.File.Exists(IntelPowerLogBin))
            {
                return true;
            }
            return false;
        }

        public bool DevConIsInstalled()
        {
            if (System.IO.File.Exists(DevCon))
            {
                return true;
            }
            return false;
        }

        public IntelPowerLog(bool intelPowerLogEnabled, bool gpuRestartEnabled)
        {
            Enabled = intelPowerLogEnabled;
            GpuRestartEnabled = gpuRestartEnabled;
        }

        /// <summary>
        /// Start PowerLog3.0.exe.
        /// </summary>
        /// <param name="logFile">The CSV file name to save the recording to.</param>
        /// <param name="duration">Duration of recording.</param>
        /// <returns>True if completed with status 0.</returns>
        public async Task<bool> Start(string logFile = "IntelPowerLog.csv", int duration = 0)
        {
            if (!Enabled)
            {
                return false;
            }

            if (!IsInstalled())
            {
                return false;
            }
            
            //string logFile = $"IntelPowerLog_{browser}_{iteraton}_{scenario}.csv";
            string commandLine = $"-file {logFile} -duration {duration} -resolution 1";

            var IntelPowerLogIsSuccess = RunIntelPowerLog(commandLine);
            await Task.Delay(1);

            // Restart video driver to release device for browser
            var devconIsSuccess = RunDevCon("restart \"PCI\\CC_0300\"");

            return IntelPowerLogIsSuccess.Result && devconIsSuccess.Result;
        }

        private async Task<bool> RunIntelPowerLog(string cmdLine, bool ignoreError = false)
        {
            await Task.Delay(1);
            if (!Enabled)
            {
                return false;
            }

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {IntelPowerLogBin} {cmdLine}");

            bool isSuccess = false;
            
            string output = "";
            string errorOutput = "";
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(IntelPowerLogBin);
                processInfo.Arguments = cmdLine;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;

                Process commandProcess = new Process();
                commandProcess.StartInfo = processInfo;
                commandProcess.Start();

                output = commandProcess.StandardOutput.ReadToEnd();
                // capture any error output. We'll use this to throw an exception.
                errorOutput = commandProcess.StandardError.ReadToEnd();

                commandProcess.WaitForExit();
                // output the standard error to the console window. The standard output is routed to the console window by default.
                Console.WriteLine(output);
                Console.WriteLine(errorOutput);
                if (!ignoreError)
                {
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        throw new Exception(errorOutput.ToString());
                    }
                }

                isSuccess = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception trying to run {IntelPowerLogBin}!");
                Console.WriteLine(e.Message);

                return false;
            }
            
            return isSuccess;
        }

        private async Task<bool> RunDevCon(string cmdLine, bool ignoreError = false)
        {
            if (!GpuRestartEnabled)
            {
                return false;
            }

            await Task.Delay(500); // Thread.Sleep(1000); // Try to wait for PowerLog is running
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {DevCon} {cmdLine}");

            bool isSuccess = false;

            string output = "";
            string errorOutput = "";
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(DevCon);
                processInfo.Arguments = cmdLine;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;

                Process commandProcess = new Process();
                commandProcess.StartInfo = processInfo;
                commandProcess.Start();
                output = commandProcess.StandardOutput.ReadToEnd();
                // capture any error output. We'll use this to throw an exception.
                errorOutput = commandProcess.StandardError.ReadToEnd();
                await Task.Delay(1);
                commandProcess.WaitForExit();
                // output the standard error to the console window. The standard output is routed to the console window by default.
                Console.WriteLine(output);
                Console.WriteLine(errorOutput);
                if (!ignoreError)
                {
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        throw new Exception(errorOutput.ToString());
                    }
                }

                isSuccess = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception trying to run {DevCon}!");
                Console.WriteLine(e.Message);

                return false;
            }

            return isSuccess;
        }
    }
}
