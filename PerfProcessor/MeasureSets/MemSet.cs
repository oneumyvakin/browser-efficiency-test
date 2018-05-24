using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserEfficiencyTest
{
    /// <summary>
    /// Calculates the Memory working sets by process.
    /// </summary>
    internal class MemSet : MeasureSet
    {
        string[] Browsers = { "browser.exe", "brodefault.exe", "chrome.exe", "chromium.exe", "opera.exe", "firefox.exe" };

        public MemSet()
        {
            _wpaProfile = @".\MeasureSetDefinitionAssets\MemSet.wpaProfile";
            WprProfile = "memSet";
            TracingMode = TraceCaptureMode.File;
            Name = "memSet";
            _wpaExportedDataFileNames = new List<string>() { "Virtual_Memory_Snapshots_Default.csv" };
        }

        /// <summary>
        /// Calculates the Duration(microsecconds) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data to use for calculating Duration of file IO activity.</param>
        /// <returns>A dictionary of processes and their Duration time.</returns>
        protected override Dictionary<string, string> CalculateMetrics(Dictionary<string, List<string>> csvData)
        {
            var workingSet = CalculateWorkingSet(csvData);
            var privateWorkingSet = CalculatePrivateWorkingSet(csvData);
            var virtualSize = CalculateVirtualSize(csvData);
            Dictionary<string, string> metrics = workingSet
                .Concat(privateWorkingSet)
                .Concat(virtualSize)                
                .ToDictionary(e => e.Key, e => e.Value);

            return metrics;
        }

        /// <summary>
        /// Calculates the Working Set (B) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data.</param>
        /// <returns>A dictionary of processes and their metrics.</returns>
        private Dictionary<string, string> CalculateWorkingSet(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - MemSet.CalculateWorkingSet", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawDurationData = from row in csvData.First().Value
                                  let fields = SplitCsvString(row)
                                  where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                  select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), WorkingSet = ConvertStringValueToInt(fields[1]) };

            // Compute the FileIO Duration filtered for Browsers and aggregated by process name.
            var durationByBrowser = from row in rawDurationData
                                    where Browsers.Contains(row.ProcessName)
                                    group row by row.ProcessName
                                        into g
                                    select new { ProcessName = g.Key.Trim(), WorkingSet = g.Sum(s => s.WorkingSet) };

            metrics = new Dictionary<string, string>() { };

            foreach (var row in durationByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.WorkingSet.ToString());
                metrics.Add(
                    string.Format("WorkingSet {0} (B)", row.ProcessName),
                    string.Format("\"{0}\"", row.WorkingSet)
                );
            }

            return metrics;
        }

        /// <summary>
        /// Calculates the Private Working Set (B) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data.</param>
        /// <returns>A dictionary of processes and their metrics.</returns>
        private Dictionary<string, string> CalculatePrivateWorkingSet(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - MemSet.CalculatePrivateWorkingSet", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawDurationData = from row in csvData.First().Value
                                  let fields = SplitCsvString(row)
                                  where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                  select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), WorkingSet = ConvertStringValueToInt(fields[2]) };

            // Compute the FileIO Duration filtered for Browsers and aggregated by process name.
            var durationByBrowser = from row in rawDurationData
                                    where Browsers.Contains(row.ProcessName)
                                    group row by row.ProcessName
                                        into g
                                    select new { ProcessName = g.Key.Trim(), WorkingSet = g.Sum(s => s.WorkingSet) };

            metrics = new Dictionary<string, string>() { };

            foreach (var row in durationByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.WorkingSet.ToString());
                metrics.Add(
                    string.Format("PrivateWorkingSet {0} (B)", row.ProcessName),
                    string.Format("\"{0}\"", row.WorkingSet)
                );
            }

            return metrics;
        }

        /// <summary>
        /// Calculates the Virtual Size (B) for specific browser process name.
        /// </summary>
        /// <param name="csvData">The raw csv data.</param>
        /// <returns>A dictionary of processes and their metrics.</returns>
        private Dictionary<string, string> CalculateVirtualSize(Dictionary<string, List<string>> csvData)
        {
            Dictionary<string, string> metrics = null;

            Console.WriteLine("[{0}] - MemSet.CalculateVirtualSize", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process the raw string data into a usable format.
            var rawDurationData = from row in csvData.First().Value
                                  let fields = SplitCsvString(row)
                                  where (fields[0].IndexOf('(') > -1) // Bugfix for rows like Unknown,"0,017505","0,00"
                                  select new { ProcessWithPID = fields[0], ProcessName = fields[0].Substring(0, fields[0].IndexOf('(')).Trim(), WorkingSet = ConvertStringValueToInt(fields[2]) };

            // Compute the FileIO Duration filtered for Browsers and aggregated by process name.
            var durationByBrowser = from row in rawDurationData
                                    where Browsers.Contains(row.ProcessName)
                                    group row by row.ProcessName
                                        into g
                                    select new { ProcessName = g.Key.Trim(), WorkingSet = g.Sum(s => s.WorkingSet) };

            metrics = new Dictionary<string, string>() { };

            foreach (var row in durationByBrowser)
            {
                Console.WriteLine("[{0}] - Add metrics: '{1}' - '{2}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), row.ProcessName, row.WorkingSet.ToString());
                metrics.Add(
                    string.Format("VirtualSize {0} (B)", row.ProcessName),
                    string.Format("\"{0}\"", row.WorkingSet)
                );
            }

            return metrics;
        }

        /// <summary>
        /// Converts cleaned value like "1234567" to integer 1234567.
        /// </summary>
        /// <param name="value">Cleaned value like "1234567".</param>
        /// <returns>An integer value like 1234567.</returns>
        private decimal ConvertStringValueToInt(string value)
        {
            return Convert.ToInt64(value);
        }
    }
}
