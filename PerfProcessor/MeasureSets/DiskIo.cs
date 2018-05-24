using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserEfficiencyTest
{
    /// <summary>
    /// Disk Usage measure. Calculates the number of bytes read from disk, the number of bytes written to disk,
    /// and the time spent by the disk serving read/write requests by process.
    /// </summary>
    internal class DiskIo : MeasureSet
    {
        string[] Browsers = { "browser.exe", "brodefault.exe", "chrome.exe", "chromium.exe", "opera.exe", "firefox.exe" };

        public DiskIo()
        {
            _wpaProfile = @".\MeasureSetDefinitionAssets\diskIo.wpaProfile";
            WprProfile = "diskIo";
            TracingMode = TraceCaptureMode.File;
            Name = "diskIo";
            _wpaExportedDataFileNames = new List<string>() { "Disk_Usage_Service_Time_by_Process,_Path_Name,_Stack.csv" };
        }

        /// <summary>
        /// Calculates the Disk Usage metrics for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating Disk Usage metrics.</param>
        /// <returns>A dictionary of processes and their total bytes written to disk, total bytes read from disk,
        /// and the total disk service time.</returns>
        protected override Dictionary<string, string> CalculateMetrics(Dictionary<string, List<string>> csvData)
        {
            var ioTime = CalculateIoTime(csvData);
            var size = CalculateSize(csvData);
            var diskServiceTime = CalculateDiskServiceTime(csvData);
            Dictionary<string, string> metrics = ioTime
                .Concat(size).ToDictionary(e => e.Key, e => e.Value)
                .Concat(diskServiceTime).ToDictionary(e => e.Key, e => e.Value);

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
        /// Calculates the Disk IO Time(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating IO usage time.</param>
        /// <returns>A dictionary of processes and their IO time.</returns>
        private Dictionary<string, string> CalculateIoTime(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - DiskIo.CalculateIoTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawIoTimeData = from row in csvData.First().Value
                                  let fields = SplitCsvString(row)
                                  where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                  select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), IoTime = ConvertCleanedTimeValueToRealMicroseconds(fields[4]) };

            // Compute the IoTime filtered for Browsers and aggregated by process name.
            var ioTimeByBrowser = from row in rawIoTimeData
                                  where Browsers.Contains(row.ProcessName)
                                    group row by row.ProcessName
                                        into g
                                    select new { ProcessName = g.Key.Trim(), IoTime = g.Sum(s => s.IoTime) };

            metrics = new Dictionary<string, string>() { };

            foreach (var row in ioTimeByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.IoTime.ToString());
                metrics.Add(
                    string.Format("Disk IO Time {0} (μs)", row.ProcessName),
                    string.Format("\"{0}\"", row.IoTime)
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

            Console.WriteLine("[{0}] - DiskIo.CalculateSize", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawFileIoSizeData = from row in csvData.First().Value
                                    let fields = SplitCsvString(row)
                                    where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                    select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), Size = Convert.ToDecimal(fields[6]) };

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
                    string.Format("Disk IO Size {0}", row.ProcessName),
                    string.Format("\"{0}\"", row.Size)
                );
            }

            return metrics;
        }

        /// <summary>
        /// Calculates the Disk Service Time(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data.</param>
        /// <returns>A dictionary of processes and their Disk Service time.</returns>
        private Dictionary<string, string> CalculateDiskServiceTime(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - DiskIo.DiskServiceTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawDiskServiceTimeData = from row in csvData.First().Value
                                let fields = SplitCsvString(row)
                                where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), IoTime = ConvertCleanedTimeValueToRealMicroseconds(fields[9]) };

            // Compute the IoTime filtered for Browsers and aggregated by process name.
            var DiskServiceTimeByBrowser = from row in rawDiskServiceTimeData
                                           where Browsers.Contains(row.ProcessName)
                                  group row by row.ProcessName
                                        into g
                                  select new { ProcessName = g.Key.Trim(), IoTime = g.Sum(s => s.IoTime) };

            metrics = new Dictionary<string, string>() { };

            foreach (var row in DiskServiceTimeByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.IoTime.ToString());
                metrics.Add(
                    string.Format("Disk Service Time {0} (μs)", row.ProcessName),
                    string.Format("\"{0}\"", row.IoTime)
                );
            }

            return metrics;
        }
    }
}
