using System;
using System.IO;

namespace Elevator
{
    /// <summary>
    /// Parses the command line arguments and provides access to the various 
    /// arguments and options.
    /// </summary>
    internal class Arguments
    {
        private const string DefaultTraceProfile = "DefaultTraceProfile.wprp";

        public bool ArgumentsAreValid { get; private set; }
        public string TraceProfile { get; private set; }        
        public bool EnableProcMon { get; private set; }
        public bool EnablePowerCfg { get; private set; }
        public bool EnableIntelPowerLog { get; private set; }
        public bool EnableIppet { get; private set; }
        public bool EnableGpuRestart { get; private set; }
        public bool EnableEmptyStandbyList { get; private set; }
        public bool EnableSocWatch { get; private set; }
        public bool EnableAMDuProfCLI { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Arguments class and processes the passed in array of command line arguments.
        /// Based on user input in the args, here we break them apart and determine:
        ///  - TraceProfile path
        ///  - Using ProcMon.exe
        ///  - Using Windows utility PowerCfg.exe
        ///  - Using Intel PowerLog3.0.exe utility
        ///  - Using EmptyStandbyList.exe utility
        /// </summary>
        /// <param name="args">Array of strings containing the command line arguments.</param>
        public Arguments(string[] args)
        {
            TraceProfile = DefaultTraceProfile;
            EnableProcMon = false;
            EnablePowerCfg = false;
            EnableIntelPowerLog = false;
            EnableIppet = false;
            EnableGpuRestart = false;
            EnableEmptyStandbyList = false;
            EnableSocWatch = false;
            EnableAMDuProfCLI = false;

            ArgumentsAreValid = ProcessArgs(args);
        }
        
        /// <summary>
        /// Go through and process the list of passed in commandline arguments.
        /// Here we'll decide which browser, scenarios, and number of loops to run.
        /// If any of the arguments are invalid, this method returns false.
        /// </summary>
        private bool ProcessArgs(string[] args)
        {
            bool argumentsAreValid = true;

            for (int argNum = 0; (argNum < args.Length) && argumentsAreValid; argNum++)
            {
                var arg = args[argNum].ToLowerInvariant();
                switch (arg)
                {
                    case "-traceprofile":
                        // Path to trace profile should be specified after the -traceprofile option.
                        while (argumentsAreValid && ((argNum + 1) < args.Length) && !(args[argNum + 1].StartsWith("-")))
                        {
                            argNum++;

                            TraceProfile = args[argNum].ToLowerInvariant();
                        }
                        break;
                    case "-procmon":

                        EnableProcMon = true;

                        break;
                    case "-powercfg":

                        EnablePowerCfg = true;

                        break;
                    case "-intelpowerlog":

                        EnableIntelPowerLog = true;

                        break;
                    case "-ippet":

                        EnableIppet = true;

                        break;
                    case "-gpurestart":

                        EnableGpuRestart = true;

                        break;
                    case "-emptystandbylist":

                        EnableEmptyStandbyList = true;

                        break;
                    case "-socwatch":

                        EnableSocWatch = true;

                        break;
                    case "-amduprofcli":

                        EnableAMDuProfCLI = true;

                        break;
                    default:
                        argumentsAreValid = false;
                        Console.WriteLine(string.Format("Invalid argument encountered '{0}'", args[argNum]), false);
                        DisplayUsage();

                        break;
                }
            }            

            return argumentsAreValid;
        }

        private void DumpArgsToFile(string[] args, string etlPath)
        {
            string resultFilePath = Path.Combine(etlPath, "args") + ".log";
            string argStr = String.Join(" ", args);
            File.WriteAllText(resultFilePath, argStr);
        }

        // Output the command line options.
        private void DisplayUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("ElevatorServer.exe "
                                + "[-traceprofile <path to .wprp file> // Default: DefaultTraceProfile.wprp"
                                + "[-procmon // Enable ProcMon.exe Default: false] "
                                + "[-ippet // Enable ProcMon.exe Default: false] "
                                + "[-emptystandbylist // Enable emptystandbylist.exe Default: false] "
                                + "[-socwatch // Enable socwatch.exe Default: false] "
                                + "[-amduprofcli // Enable amduprofcli.exe Default: false] "
                                + "[-powercfg // Enable powercfg.exe Default: false] "
                                + "[-gpurestart // Enable restart GPU driver for intel power log. Default: false] "
                                + "[-intelpowerlog // Enable intelpowerlog.exe Default: false] "
            );
        }
    }
}
