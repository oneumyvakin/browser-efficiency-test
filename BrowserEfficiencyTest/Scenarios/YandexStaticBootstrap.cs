using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticBootstrap : Scenario
    {
        public YandexStaticBootstrap()
        {
            Name = "YandexStaticBootstrap";
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(3);

            driver.ScrollPage(4);
            driver.Wait(3);

            driver.ScrollPage(4);
            driver.Wait(3);

            driver.ScrollPage(4);

            WebSrv.StopWebSrv(Name);
        }

        private string GetStaticResourceUrl()
        {
            return $"http://localhost:{WebSrv.DefaultPort}/";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
