using System.Threading.Tasks;
using System.Net.Http;

namespace BrowserEfficiencyTest
{
    internal class WebSrv
    {
        public static string DefaultPort = "8080";
        public static string DefaultPortTLS = "8081";

        private static string TLSECDSACert = "websrv-tls-ecdsa.crt";
        private static string TLSECDSAKey = "websrv-tls-ecdsa.key";

        public static async Task<string> StartWebSrv(int stopAfter, string webRootPath, string defaultPort = "8080", string defaultPortTLS = "8081")
        {
            await Task.Delay(1);

            var output = Start($"-stopAfter {stopAfter - 2} -webRoot \"{webRootPath}\" -port {defaultPort} -portTls {defaultPortTLS}");

            await Task.Delay(1);

            return output;
        }

        public static async Task<string> StartWebSrv(int stopAfter, string webRootPath, string tlsCipher)
        {
            await Task.Delay(1);
            string tlsKeyCertArg = "";
            if (tlsCipher.Contains("ECDSA"))
            {
                tlsKeyCertArg = $" -tlsCertFile {TLSECDSACert} -tlsKeyFile {TLSECDSAKey}";
            }

            var output = Start($"-stopAfter {stopAfter - 2} -tlsCipher {tlsCipher} -webRoot \"{webRootPath}\" {tlsKeyCertArg}");

            await Task.Delay(1);

            return output;
        }

        public static void StopWebSrv(string fromTestName)
        {
            Logger.LogWriteLine($"Stop web server from: {fromTestName}");
            HttpClient client = new HttpClient();
            try
            {
                client.GetStringAsync($"http://{GetHost()}:{DefaultPort}/stop?from=" + fromTestName);
            }
            catch (System.Exception ex)
            {
                Logger.LogWriteLine($"Stop web exception: {ex.Message}");
            }

        }

        public static async Task<string> StartProxy(int stopAfter, string url)
        {
            await Task.Delay(1);

            var output = Start($"-stopAfter {stopAfter - 2} -proxyTo \"{url}\"");

            await Task.Delay(1);

            return output;
        }

        public static string GetHost()
        {
            return new RemoteAgent().DefaultHost;
        }

        private static string Start(string args)
        {
            if (GetHost() == "localhost")
            {
                return StartLocal(args);
            }

            return StartRemote(args);
        }

        private static string StartLocal(string args)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            string cwd = System.IO.Directory.GetCurrentDirectory();
            string webSrvBinPath = System.IO.Path.Combine(cwd, "websrv.exe");
            startInfo.FileName = webSrvBinPath;
            startInfo.Arguments = args;
            Logger.LogWriteLine($"Start local web server for test: {startInfo.FileName} {startInfo.Arguments}");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string errOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();
            process.Close();
            Logger.LogWriteLine($"Local web server returns: {output}\n{errOutput}");
            return output;
        }

        private static string StartRemote(string args)
        {
            Logger.LogWriteLine($"Start remote web server for test: {args}");

            return new RemoteAgent().Execute("websrv.exe", args);
        }
    }
}

