using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticMp4LoopHttpsTLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 : Scenario
    {
        public YandexStaticMp4LoopHttpsTLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256()
        {
            Name = "YandexStaticMp4LoopHttpsTLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256";
            DefaultDuration = 18000;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath(), "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256");

        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
        }

        private string GetStaticResourceUrl()
        {
            return $"https://localhost:{WebSrv.DefaultPortTLS}/";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
