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
using System.Collections.Generic;
using System.Linq;

namespace BrowserEfficiencyTest
{
    /// <summary>
    /// GPU Usage measure. Calculates the GPU Usage time by process.
    /// </summary>
    internal class GpuUsage : MeasureSet
    {
        string[] Browsers = { "browser.exe", "brodefault.exe", "chrome.exe", "chromium.exe", "opera.exe", "firefox.exe" };

        public GpuUsage()
        {
            _wpaProfile = @".\MeasureSetDefinitionAssets\GpuUsage.wpaProfile";
            WprProfile = "gpuUsage";
            TracingMode = TraceCaptureMode.File;
            Name = "gpuUsage";
            _wpaExportedDataFileNames = new List<string>() { "GPU_Utilization_Table_GPU_by_Process.csv" };
        }

        /// <summary>
        /// Calculates the GPU Time(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating GPU usage time.</param>
        /// <returns>A dictionary of processes and their GPU usage in GPU time.</returns>
        protected override Dictionary<string, string> CalculateMetrics(Dictionary<string, List<string>> csvData)
        {
            var gpuTime = CalculateGPUTime(csvData);
            var gpuPerc = CalculateMetricsGPUPercentage(csvData);
            var gpuPackets = CalculateGPUPackets(csvData);
            Dictionary<string, string> metrics = gpuTime
                .Concat(gpuPerc).ToDictionary(e => e.Key, e => e.Value)
                .Concat(gpuPackets).ToDictionary(e => e.Key, e => e.Value);

            return metrics;
        }

        /// <summary>
        /// Converts cleaned GPU Time value like 1234567 to "1234,567".
        /// </summary>
        /// <param name="csvData">Cleaned GPU Time like 1234567.</param>
        /// <returns>A quoted string of GPU percents like "1234,567".</returns>
        private string ConvertCleanedGPUTimeValueToRealMicroseconds(string value)
        {
            return String.Format("\"{0}\"", Convert.ToString(Convert.ToDecimal(value) / 1000));
        }

        /// <summary>
        /// Calculates the GPU Time(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating GPU usage time.</param>
        /// <returns>A dictionary of processes and their GPU usage in GPU time.</returns>
        private Dictionary<string, string> CalculateGPUTime(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - GpuUsage.CalculateMetrics", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawGpuUsageTimeData = from row in csvData.First().Value
                                      let fields = SplitCsvString(row)
                                      select new { ProcessWithPID = fields[1], ProcessName = fields[0], GpuTime = ConvertCleanedGPUTimeValueToRealMicroseconds(fields[11]) };

            metrics = new Dictionary<string, string>() { };
            
            var gpuTimeByBrowser = from row in rawGpuUsageTimeData
                                         where Browsers.Contains(row.ProcessName)
                                         select row;

            foreach (var row in gpuTimeByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.GpuTime.ToString());
                metrics.Add(string.Format("GPU Time {0} (μs)", row.ProcessName), row.GpuTime.ToString());
            }

            return metrics;
        }

        /// <summary>
        /// Converts cleaned Percentage value like 3500 to "35,00".
        /// </summary>
        /// <param name="csvData">Cleaned Percentage like 3500.</param>
        /// <returns>A quoted string of GPU percents like "35,00".</returns>
        private string ConvertCleanedPercentValueToRealPercent(string value)
        {
            return String.Format("\"{0}\"", Convert.ToString(Convert.ToDecimal(value) / 100));            
        }

        /// <summary>
        /// Calculates the GPU % for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating GPU usage time.</param>
        /// <returns>A dictionary of processes and their GPU usage in percents.</returns>
        private Dictionary<string, string> CalculateMetricsGPUPercentage(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - GpuUsage.CalculateMetrics", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawGpuUsageTimeData = from row in csvData.First().Value
                                      let fields = SplitCsvString(row)
                                      select new { ProcessWithPID = fields[1], ProcessName = fields[0], GpuPercentage = ConvertCleanedPercentValueToRealPercent(fields[10]) };

            metrics = new Dictionary<string, string>() { };
            
            var gpuPercentageByBrowser = from row in rawGpuUsageTimeData
                                        where Browsers.Contains(row.ProcessName)
                                        select row;

            foreach(var row in gpuPercentageByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.GpuPercentage.ToString());
                metrics.Add(string.Format("GPU Percentage {0} (%)", row.ProcessName), row.GpuPercentage.ToString() );
            }

            return metrics;
        }

        /// <summary>
        /// Calculates the count of GPU work packets for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating GPU work packets.</param>
        /// <returns>A dictionary of processes and their GPU usage in GPU work packets.</returns>
        private Dictionary<string, string> CalculateGPUPackets(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - GpuUsage.CalculateMetrics", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawGpuPacketsData = from row in csvData.First().Value
                                      let fields = SplitCsvString(row)
                                      select new { ProcessWithPID = fields[1], ProcessName = fields[0], GpuPackets = fields[3] };

            metrics = new Dictionary<string, string>() { };
            
            var gpuPacketsByBrowser = from row in rawGpuPacketsData
                                   where Browsers.Contains(row.ProcessName)
                                   select row;

            foreach (var row in gpuPacketsByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.GpuPackets.ToString());
                metrics.Add(string.Format("GPU Packets {0}", row.ProcessName), row.GpuPackets.ToString());
            }

            return metrics;
        }
    }
}
