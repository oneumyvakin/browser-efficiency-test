using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Elevator
{
    internal class ProcMon
    {
        private bool Enabled;

        public ProcMon(bool procMonEnabled)
        {
            Enabled = procMonEnabled;
        }
        
        /// <summary>
         /// Start procmon.exe to log events in procmon format.
         /// </summary>
         /// <param name="logFile">The PML file name to save the recording to.</param>
         /// <returns>True if procmon.exe completed with status 0.</returns>
        public async Task<bool> Start(string logFile = "procmon.pml")
        {
            if (!Enabled)
            {
                return false;
            }

            bool isSuccess = false;

            string commandLine = "/AcceptEula /Minimized /LoadConfig ProcmonConfiguration.pmc /BackingFile " + logFile;

            await Task.Delay(1);
            isSuccess = RunProcMon(commandLine);
            await Task.Delay(1);

            return isSuccess;
        }

        /// <summary>
        /// Terminate all instances of procmon.exe.
        /// </summary>
        /// <returns>True if procmon.exe completed with status 0.</returns>
        public bool Terminate()
        {
            if (!Enabled)
            {
                return false;
            }

            bool isSuccess = false;

            string commandLine = "/Terminate";

            isSuccess = RunProcMon(commandLine);

            return isSuccess;
        }

        // executes the powercfg.exe commandline with the passed in command line parameters
        private bool RunProcMon(string cmdLine, bool ignoreError = false)
        {
            if (!Enabled)
            {
                return false;
            }

            bool isSuccess = false;
            string procMonExe = "procmon.exe";
            string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            if (!arch.Equals("AMD64"))
            {
                Console.WriteLine($"Current architecture is {arch}: {procMonExe} must be executed in 64-bit mode!");
                return isSuccess;
            }
                        
            string output = "";
            string errorOutput = "";            
            Console.WriteLine($"{procMonExe} {cmdLine}");
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(procMonExe);
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
                if (!ignoreError && !string.IsNullOrEmpty(errorOutput))
                {
                    Console.WriteLine($"Error at run {procMonExe}:");
                    Console.WriteLine(errorOutput.ToString());

                    return false;                    
                }

                isSuccess = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception trying to run {procMonExe}!");
                Console.WriteLine(e.Message);

                return false;
            }

            return isSuccess;
        }
    }
}
