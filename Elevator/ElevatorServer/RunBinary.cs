using System;
using System.Diagnostics;

namespace Elevator
{
    internal class RunBinary
    {
        public static bool Run(string binaryPath, string cmdLine, bool ignoreError = false)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {binaryPath} {cmdLine}");

            bool isSuccess = false;

            string output = "";
            string errorOutput = "";
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(binaryPath);
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
                Console.WriteLine($"Exception trying to run {binaryPath}!");
                Console.WriteLine(e.Message);

                return false;
            }

            return isSuccess;
        }
    }
}
