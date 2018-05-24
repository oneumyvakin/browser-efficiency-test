using System;
using System.Diagnostics;

namespace Elevator
{
    internal class PowerCfg
    {
        private bool Enabled;

        public PowerCfg(bool powerCfgEnabled)
        {
            Enabled = powerCfgEnabled;
        }

        /// <summary>
        /// Dump SRUM report to the specified CSV file name.
        /// </summary>
        /// <param name="srumReportFileName">The CSV file name to save the recording to.</param>
        /// <returns>True if powercfg.exe completed with status 0.</returns>
        public bool DumpSrumReport(string srumReportFileName = "srum.csv")
        {
            if (!Enabled)
            {
                return false;
            }

            bool isSuccess = false;

            string commandLine = "/srumutil /csv /output " + srumReportFileName;

            isSuccess = RunPowerCfg(commandLine);

            return isSuccess;
        }

        // executes the powercfg.exe commandline with the passed in command line parameters
        private bool RunPowerCfg(string cmdLine, bool ignoreError = false)
        {
            if (!Enabled)
            {
                return false;
            }

            bool isSuccess = false;
            string powerCfgExe = "powercfg.exe";
            string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            if (!arch.Equals("AMD64"))
            {
                Console.WriteLine($"Current architecture is {arch}: {powerCfgExe} must be executed in 64-bit mode!");
                return isSuccess;
            }
                        
            string output = "";
            string errorOutput = "";            
            Console.WriteLine($"{powerCfgExe} {cmdLine}");
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(powerCfgExe);
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
                Console.WriteLine("Exception trying to run powercfg.exe!");
                Console.WriteLine(e.Message);

                return false;
            }

            return isSuccess;
        }
    }
}
