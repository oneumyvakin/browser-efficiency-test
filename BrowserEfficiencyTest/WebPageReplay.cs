using System.Threading.Tasks;
using System.Net.Http;
using System;

namespace BrowserEfficiencyTest
{
    internal class WebPageReplay
    {
        public static string BinaryPath = @"WebPageReplay\WebPageReplay.exe";
        public static string CertPath = @"WebPageReplay\wpr_cert.pem";
        public static string KeyPath = @"WebPageReplay\wpr_key.pem";
        public static string ScriptPath = @"WebPageReplay\deterministic.js";

        public static string DefaultHttpPort = "80";
        public static string DefaultHttpsPort = "443";

        public static async Task<string> StartWebPageReplay(string recordPath)
        {
            await Task.Delay(1);
            
            var output = Start($"replay " +
                $"--host={GetHost()} " +
                $"--http_port={DefaultHttpPort} " +
                $"--https_port={DefaultHttpsPort} " +
                $"--https_cert_file={CertPath} " +
                $"--https_key_file={KeyPath} " +
                $"--inject_scripts={ScriptPath} " +
                $"\"{recordPath}\"");
            
            await Task.Delay(1);

            return output;
        }

        public static void StopWebSrv(string fromTestName)
        {
            Logger.LogWriteLine($"Stop web page replay from: {fromTestName}");
            try
            {
                using (var client = new HttpClient())
                {
                    var result = client.GetStringAsync($"http://{GetHost()}:{DefaultHttpPort}/web-page-replay-command-exit?from=" + fromTestName);
                    Logger.LogWriteLine($"Stop web page replay result: '{result.Result}'");
                }
            } catch (System.Exception ex)
            {
                Logger.LogWriteLine($"Stop web page replay exception: {ex.Message} {ex.InnerException.Message}");
            }            
        }

        public static string GetHost()
        {
            return new RemoteAgent().DefaultHost;
        }

        private static string Start(string args)
        {
            if (GetHost() == "localhost")
            {
                return StartLocal(BinaryPath, args);
            }

            return StartRemote(args);
        }

        private static string StartLocal(string binaryPath, string args)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                string cwd = System.IO.Directory.GetCurrentDirectory();
                string webSrvBinPath = System.IO.Path.Combine(cwd, binaryPath);
                startInfo.FileName = webSrvBinPath;
                startInfo.Arguments = args;
                Logger.LogWriteLine($"Start local web page replay for test: {startInfo.FileName} {startInfo.Arguments}");
                startInfo.UseShellExecute = false;
                //startInfo.RedirectStandardOutput = true;
                //startInfo.RedirectStandardError = true;
                process.StartInfo = startInfo;
                process.Start();
                //string output = process.StandardOutput.ReadToEnd();
                //string errOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();
                process.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception trying to run {binaryPath}!");
                Console.WriteLine(e.Message);

                return "Local Web page replay returns: " + e.Message;
            }
            //Logger.LogWriteLine($"Web page replay returns: {output}\n{errOutput}");
            return "Local Web page replay returns";
        }

        private static string StartRemote(string args)
        {
            Logger.LogWriteLine($"Start remote Web page replay: {args}");

            return new RemoteAgent().Execute(BinaryPath, args);
        }
    }    
}

