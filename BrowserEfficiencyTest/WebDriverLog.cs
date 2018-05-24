using Newtonsoft.Json;

namespace BrowserEfficiencyTest
{
    internal class WebDriverLogString
    {
        [JsonProperty("webview")]
        public string Webview { get; set; }

        [JsonProperty("message")]
        public WebDriverLogMessage Message { get; set; }
    }

    internal class WebDriverLogMessage
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public Newtonsoft.Json.Linq.JObject Params { get; set; }
    }
}
