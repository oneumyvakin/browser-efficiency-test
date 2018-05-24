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
    internal class FileIo : MeasureSet
    {
        string[] Browsers = { "browser.exe", "brodefault.exe", "chrome.exe", "chromium.exe", "opera.exe", "firefox.exe" };

        public FileIo()
        {
            _wpaProfile = @".\MeasureSetDefinitionAssets\FileIo.wpaProfile";
            WprProfile = "fileIo";
            TracingMode = TraceCaptureMode.File;
            Name = "fileIo";
            _wpaExportedDataFileNames = new List<string>() { "File_I_O_Activity_by_Process,_Thread,_Type.csv" };
        }

        /// <summary>
        /// Calculates the Duration(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating Duration of file IO activity.</param>
        /// <returns>A dictionary of processes and their Duration time.</returns>
        protected override Dictionary<string, string> CalculateMetrics(Dictionary<string, List<string>> csvData)
        {
            var duration = CalculateDuration(csvData);
            var size = CalculateSize(csvData);
            Dictionary<string, string> metrics = duration
                .Concat(size).ToDictionary(e => e.Key, e => e.Value);

            return metrics;
        }

        /// <summary>
        /// Converts cleaned value like 1234567 to decimal 1234,567.
        /// </summary>
        /// <param name="csvData">Cleaned value like 1234567.</param>
        /// <returns>A deciaml value like 1234,567.</returns>
        private decimal ConvertCleanedTimeValueToRealMicroseconds(string value)
        {
            return Convert.ToDecimal(value) / 1000;
        }

        /// <summary>
        /// Calculates the GPU Time(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating GPU usage time.</param>
        /// <returns>A dictionary of processes and their GPU usage in GPU time.</returns>
        private Dictionary<string, string> CalculateDuration(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - FileIo.CalculateDuration", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawDurationData = from row in csvData.First().Value
                                  let fields = SplitCsvString(row)
                                  where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                  select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), Duration = ConvertCleanedTimeValueToRealMicroseconds(fields[4]) };
            
            // Compute the FileIO Duration filtered for Browsers and aggregated by process name.
            var durationByBrowser = from row in rawDurationData
                                        where Browsers.Contains(row.ProcessName)
                                        group row by row.ProcessName
                                        into g
                                        select new { ProcessName = g.Key.Trim(), Duration = g.Sum(s => s.Duration) };
            
            metrics = new Dictionary<string, string>() { };

            foreach (var row in durationByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.Duration.ToString());
                metrics.Add(
                    string.Format("File IO Duration Time {0} (μs)", row.ProcessName), 
                    string.Format("\"{0}\"", row.Duration)
                );
            }

            return metrics;
        }

        /// <summary>
        /// Calculates the count of Size In Bytes for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating Size In Bytes.</param>
        /// <returns>A dictionary of processes and their Size In Bytes.</returns>
        private Dictionary<string, string> CalculateSize(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - FileIo.CalculateSize", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawFileIoSizeData = from row in csvData.First().Value
                                    let fields = SplitCsvString(row)
                                    where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                    select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), Size = Convert.ToDecimal(fields[7]) };
            
            // Compute the FileIO Duration filtered for Browsers and aggregated by process name.
            var fileIoSizeByBrowser = from row in rawFileIoSizeData
                                      where Browsers.Contains(row.ProcessName)
                                      group row by row.ProcessName
                                      into g
                                      select new { ProcessName = g.Key.Trim(), Size = g.Sum(s => s.Size) };
            
            metrics = new Dictionary<string, string>() { }; 

            foreach (var row in fileIoSizeByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.Size.ToString());
                metrics.Add(
                    string.Format("File IO Size {0}", row.ProcessName), 
                    string.Format("\"{0}\"", row.Size)
                );
            }

            return metrics;
        }
    }
}
