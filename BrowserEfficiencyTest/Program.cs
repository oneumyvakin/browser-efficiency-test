//--------------------------------------------------------------
//
// Browser Efficiency Test
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            int returnValue = 0;

            Arguments arguments = new Arguments(args);
            if (arguments.ArgumentsAreValid)
            {
                var heartBeatCtx = new CancellationTokenSource();
                Task<bool> heartBeatTask = StartLogHeartBeat(heartBeatCtx, arguments.EtlPath);

                ScenarioRunner scenarioRunner = new ScenarioRunner(arguments);

                // Run the automation. This will write traces to the current or provided directory if the user requested it
                if (arguments.Browsers.Count > 0 && arguments.Scenarios.Count > 0)
                {
                    scenarioRunner.Run();
                }

                // If traces have been written, process them into a csv of results
                // Only necessary if we're tracing and/or measuring responsiveness
                if ((arguments.UsingTraceController && arguments.DoPostProcessing) || arguments.MeasureResponsiveness)
                {
                    PerfProcessor perfProcessor = new PerfProcessor((arguments.SelectedMeasureSets).ToList());
                    perfProcessor.Execute(arguments.EtlPath, arguments.EtlPath, scenarioRunner.GetResponsivenessResults(), ScenarioRunner._extensionsNameAndVersion);
                    generateCharts(arguments.EtlPath, arguments.EtlPath, arguments.GenerateChartsWithSymbols);
                }

                heartBeatCtx.Cancel();
                var heartBeat = heartBeatTask.Result;
            }
            else
            {
                returnValue = 1;
            }
            return returnValue;
        }

        private static void generateCharts(string csvFolderPath, string pngFolderPath, bool withSymbols)
        {
            var chartGenerator = @"generatecharts.exe";
            var chartGeneratorArgs = $"-csv {csvFolderPath} -png {pngFolderPath}";
            if (withSymbols)
            {
                chartGeneratorArgs += " -withSymbols";
            }
            System.Console.WriteLine($"Generate PNG charts {chartGenerator} {chartGeneratorArgs}");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = chartGenerator;
            startInfo.Arguments = chartGeneratorArgs;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string errOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();
            process.Close();
            System.Console.WriteLine($"{output}\n{errOutput}");
        }

        private static async Task<bool> StartLogHeartBeat(CancellationTokenSource ctx, string saveFolder)
        {
            string heartBeatFile = Path.Combine(saveFolder, "heartbeat.log");
            while (!ctx.IsCancellationRequested)
            {
                File.AppendAllText(heartBeatFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n");

                await Task.Delay(60 * 1000);
            }
            
            return true;
        }
    }  
}
