//--------------------------------------------------------------
//
// Microsoft Edge Elevator
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files(the ""Software""),
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//--------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Elevator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Arguments arguments = new Arguments(args);
            
            if (!arguments.ArgumentsAreValid)
            {
                Console.WriteLine("Specified arguments are invalid");
                Console.WriteLine("Exiting....");
                Environment.Exit(1);
            }
            String traceProfile = arguments.TraceProfile;

            // If the trace profile doesn't exist exit the program.
            if (!File.Exists(traceProfile))
            {
                Console.WriteLine("Unable to find the trace profile \"{0}\". Make sure the path and name are correct.", traceProfile);
                Console.WriteLine("Exiting....");
                Environment.Exit(1);
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            Task task;
            using (var server = new ElevatorServer())
            {
                // run the control server in an asynchronous task            
#pragma warning disable CS4014 // We are using Task as a thread so await is not needed.
                task = new Task(() => RunTracingControlServer(server, traceProfile, tokenSource.Token, arguments), tokenSource.Token);
#pragma warning restore CS4014
                task.Start();

                Console.WriteLine("Press ESC to stop and exit the Tracing Controller.");

                // Wait until the user presses the ESC key.
                while (Console.ReadKey().Key != ConsoleKey.Escape)
                {
                }
                server.Shutdown();
                tokenSource.Cancel();
            }

            // cancel the server task and clean up before exiting
            try
            {
                task.Wait();
            }
            catch (OperationCanceledException)
            {
                // expected exception since we canceled the cancel token.
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        // This method runs the main loop of the tracing controller. 
        // It is run as an asynchronous task and takes a CancellationToken as a parameter.
        private static async Task RunTracingControlServer(ElevatorServer server, string traceProfile, CancellationToken cancelToken, Arguments arguments)
        {
            string wprTraceModeDisabled = "DISABLED";
            bool wprTracingEnabled = true; 
            AutomateWPR wpr = new AutomateWPR(traceProfile);
            AMDuProfCLI amdProfCli = new AMDuProfCLI(arguments.EnableAMDuProfCLI);
            SocWatch socWatch = new SocWatch(arguments.EnableSocWatch);
            PowerCfg powerCfg = new PowerCfg(arguments.EnablePowerCfg);
            ProcMon procMon = new ProcMon(arguments.EnableProcMon);
            Ippet ippet = new Ippet(arguments.EnableIppet);
            IntelPowerLog intelPowerLog = new IntelPowerLog(arguments.EnableIntelPowerLog, arguments.EnableGpuRestart);
            EmptyStandbyList emptyStandbyList = new EmptyStandbyList(arguments.EnableEmptyStandbyList);

            string powerCfgSrumReportFileName = "";
            Task<bool> amdProfCliTask = null;
            Task<bool> procMonTask = null;
            Task<bool> intelPowerLogTask = null;
            Task<bool> ippetTask = null;
            Task<bool> socWatchTask = null;

            
            string amdProfCliLogFilesPath = "";
            string procMonLogFileName = "";
            string intelPowerLogFileName = "";
            string ippetLogFileName = "";
            string socWatchLogFilesPath = "";
            string etlFileName = "";
            string etlFolderPath = "";
            Regex invalidCharacters = new Regex(@"\W");

            Console.WriteLine("{0}: Tracing Controller Server starting....", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            while (!cancelToken.IsCancellationRequested)
            {
                bool isPassEnded = false;

                Console.WriteLine("{0}: Waiting for client connection.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                try
                {
                    await server.ConnectAsync(cancelToken);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }

                // Begin interacting with the client
                while (!cancelToken.IsCancellationRequested && !isPassEnded)
                {
                    // A command line from the client is delimited by spaces
                    var messageTokens = await server.GetCommandAsync();

                    // the first token of the command line is the actual command
                    string command = messageTokens[0];

                    switch (command)
                    {
                        case Commands.START_PASS:
                            Console.WriteLine("{0}: Client is starting the test pass.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            // If there is more than one message token then the client has passed a folder path for where to save the ETL files.
                            if (messageTokens.Length > 1)
                            {
                                if (Directory.Exists(messageTokens[1]))
                                {
                                    etlFolderPath = messageTokens[1];
                                }
                                else
                                {
                                    throw new DirectoryNotFoundException("Passed in directory was not found! Directory: " + messageTokens[1]);
                                }
                            }
                            else
                            {
                                etlFolderPath = Directory.GetCurrentDirectory();
                            }
                            break;
                        case Commands.START_BROWSER:
                            string wprProfile = "defaultProfile";
                            bool useFileMode = true;
                            string recordingMode = "FileMode";

                            // If a client sends a message with 10 tokens then assume they passed a WPR profile name and the trace recording mode.
                            if (messageTokens.Length > 10)
                            {
                                // The seventh token is the WPR profile name.
                                wprProfile = messageTokens[7];

                                // Check if wprProfile contains any non-alphanumeric characters other than underscores.
                                if (invalidCharacters.IsMatch(wprProfile))
                                {
                                    Console.WriteLine("Invalid WPR Profile!");
                                    throw new Exception("The WPR Profile name is invalid!");
                                }
                                
                                if (messageTokens[9] == wprTraceModeDisabled)
                                {
                                    Console.WriteLine($"{DateTime.Now.ToString("yyyy - MM - dd HH: mm:ss")} WPR is disabled!");
                                    wprTracingEnabled = false;
                                } 

                                // The ninth token denotes the trace recording mode - either Memory or File.
                                if (messageTokens[9] == "Memory")
                                {
                                    useFileMode = false;
                                    recordingMode = "MemoryMode";
                                }
                                else
                                {
                                    useFileMode = true;
                                }                                                               
                            }

                            Console.WriteLine("{0}: -Starting- Iteration: {1}  Browser: {2}  Scenario: {3}  WPR Profile: {4}  TracingMode: {5}  Duration: {6}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), messageTokens[3], messageTokens[1], messageTokens[5], wprProfile, recordingMode, messageTokens[11]);
                            Console.WriteLine("{0}: Starting tracing session.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            // first cancel any currently running trace sessions
                            if (wprTracingEnabled)
                            {
                                wpr.CancelWPR();
                            }

                            procMon.Terminate();

                            // prepare data necessary for tracing
                            procMonLogFileName = Path.Combine(etlFolderPath, messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_procmon_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pml");
                            intelPowerLogFileName = Path.Combine(etlFolderPath, messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_IntelPowerLog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                            ippetLogFileName = Path.Combine(etlFolderPath, messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_ippet_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                            socWatchLogFilesPath = Path.Combine(etlFolderPath, "socwatch", messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_socwatch_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                            // AMDuProfCLI can't handle long file names, so take only main scenario name
                            // var mainScenarioName = messageTokens[5].Split(new[] { '-' })[0];
                            var mainScenarioName = messageTokens[5].Replace("-", "");
                            amdProfCliLogFilesPath = Path.Combine(etlFolderPath, "amdProfCli", messageTokens[1] + "_" + mainScenarioName + "_" + messageTokens[3] + "_amd_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                            
                            // start tracing
                            if (wprTracingEnabled)
                            {
                                wpr.StartWPR(wprProfile, useFileMode);
                            }

                            procMonTask = procMon.Start(procMonLogFileName);
                            if (messageTokens[10] == "DURATION")
                            {
                                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Duration '{messageTokens[11]}'");

                                int duration = 0;
                                if (Int32.TryParse(messageTokens[11], out duration))
                                {
                                    intelPowerLogTask = intelPowerLog.Start(intelPowerLogFileName, duration);
                                    ippetTask = ippet.Start(ippetLogFileName, duration);
                                    socWatchTask = socWatch.Start(socWatchLogFilesPath, duration);
                                    amdProfCliTask = amdProfCli.Start(amdProfCliLogFilesPath, duration);                                    
                                }
                                else
                                {
                                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Duration '{messageTokens[11]}' could not be parsed to int.");
                                }
                            }                                
                            
                            // create the ETL file name which we will use later
                            etlFileName = Path.Combine(etlFolderPath, messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_" + wprProfile + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".etl");
                            powerCfgSrumReportFileName = Path.Combine(etlFolderPath, messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_srum_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");

                            emptyStandbyList.Start(); // Runs synchroniously
                            break;
                        case Commands.END_BROWSER:
                            Console.WriteLine("{0}: -Finished- Browser: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), messageTokens[1]);
                            
                            // end tracing
                            if (wprTracingEnabled)
                            {
                                Console.WriteLine("{0}: Stopping tracing session and saving as ETL: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), etlFileName);
                                wpr.StopWPR(etlFileName);
                                Console.WriteLine("{0}: Done saving ETL file: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), etlFileName);
                            }

                            if (arguments.EnablePowerCfg)
                            {
                                Console.WriteLine("{0}: Saving SRUM report file: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), powerCfgSrumReportFileName);
                                powerCfg.DumpSrumReport(powerCfgSrumReportFileName);
                            }

                            Console.WriteLine("{0}: Terminate Procmon.exe", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            procMon.Terminate();
                            
                            if (arguments.EnableProcMon && procMonTask != null)
                            {
                                Console.WriteLine("{0}: Procmon.exe status: '{1}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), procMonTask.Status);
                            }

                            if (arguments.EnableIntelPowerLog && intelPowerLogTask != null)
                            {
                                Console.WriteLine("{0}: PowerLog3.0.exe status: '{1}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), intelPowerLogTask.Status);
                            }

                            if (arguments.EnableIppet && ippetTask != null)
                            {
                                Console.WriteLine("{0}: ippet.exe status: '{1}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ippetTask.Status);
                            }

                            if (arguments.EnableSocWatch && socWatchTask != null)
                            {
                                Console.WriteLine("{0}: socwatch.exe status: '{1}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), socWatchTask.Status);
                            }

                            if (arguments.EnableAMDuProfCLI && amdProfCliTask != null)
                            {
                                Console.WriteLine("{0}: AMDuProfCLI.exe status: '{1}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), amdProfCliTask.Status);
                            }

                            break;
                        case Commands.END_PASS:
                            Console.WriteLine("{0}: Client test pass has ended.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            isPassEnded = true;

                            break;
                        case Commands.CANCEL_PASS:
                            Console.WriteLine("{0}: Cancelling tracing.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            wpr.CancelWPR();
                            procMon.Terminate();
                            break;
                        default:
                            throw new Exception($"Unknown command encountered: {command}");
                    } // switch (Command)

                    await server.AcknowledgeCommandAsync();
                } // while (!cancelToken.IsCancellationRequested && !isPassEnded)

                server.Disconnect();
            } // while (!cancelToken.IsCancellationRequested)
        }
    }
}