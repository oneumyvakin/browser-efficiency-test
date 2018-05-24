using System.Net.Http;

namespace BrowserEfficiencyTest
{
    internal class RemoteAgent
    {
        public string ConfigFile = "remoteagent.json";
        public string DefaultHost = "localhost";
        public string DefaultPort = "8086";
        public string DefaultPortTLS = "8087";

        public RemoteAgent()
        {
            RemoteAgentConfig config = GetConfig();
            if (config.Host != DefaultHost) {
                DefaultHost = config.Host;
                DefaultPort = config.Port;
                DefaultPortTLS = config.PortTls;
            }
        }

        public string Execute(string cmd, string args)
        {
            var remoteCmd = new RemoteAgentCommand
            {
                Cmd = cmd,
                Args = args
            };

            HttpResponseMessage response = null;
            try
            {
                using (var client = new HttpClient())
                {
                    var uri = new System.Uri($"http://{DefaultHost}:{DefaultPort}/");
                    string cmdJson = Newtonsoft.Json.JsonConvert.SerializeObject(remoteCmd);
                    var stringContent = new StringContent(cmdJson, System.Text.Encoding.UTF8, "application/json");
                    Logger.LogWriteLine($"RemoteAgent.SendCommand: '{uri.ToString()}' '{cmdJson}'");
                    response = client.PutAsync(uri, stringContent).Result;
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogWriteLine($"RemoteAgent.SendCommand exception:\r\n{ex.Message}\r\n{ex.InnerException}");
            }

            return response.ToString();
        }

        private RemoteAgentConfig GetConfig()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            string configPath = System.IO.Path.Combine(cwd, ConfigFile);
            RemoteAgentConfig config = new RemoteAgentConfig();

            config.Host = DefaultHost;
            config.Port = DefaultPort;
            config.PortTls = DefaultPortTLS;

            if (System.IO.File.Exists(configPath))
            {
                string jsonString = System.IO.File.ReadAllText(configPath);
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<RemoteAgentConfig>(jsonString);
            }

            Logger.LogWriteLine($"RemoteAgent config: Host:'{config.Host}' Port:'{config.Port}' PortTls:'{config.PortTls}'");
            return config;
        }
    }

    public class RemoteAgentCommand
    {
        [Newtonsoft.Json.JsonProperty("cmd")]
        public string Cmd { get; set; }

        [Newtonsoft.Json.JsonProperty("args")]
        public string Args { get; set; }
    }

    public class RemoteAgentConfig
    {
        [Newtonsoft.Json.JsonProperty("host")]
        public string Host { get; set; }

        [Newtonsoft.Json.JsonProperty("port")]
        public string Port { get; set; }

        [Newtonsoft.Json.JsonProperty("portTls")]
        public string PortTls { get; set; }
    }
}

