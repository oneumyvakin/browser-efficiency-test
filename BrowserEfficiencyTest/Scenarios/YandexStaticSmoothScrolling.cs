using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticSmoothScrolling : Scenario
    {
        public YandexStaticSmoothScrolling()
        {
            Name = "YandexStaticSmoothScrolling";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath(), "8082", "8083");

            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());

            WebSrv.StopWebSrv(Name);
        }

        private string GetStaticResourceUrl()
        {
            return $"http://{GetHost()}:8082/";
        }

        public static string GetHost()
        {
            return new RemoteAgent().DefaultHost;
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine("StaticResources", Name);
        }
    }
}
