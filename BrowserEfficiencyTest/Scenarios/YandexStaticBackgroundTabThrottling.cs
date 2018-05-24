using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticBackgroundTabThrottling : Scenario
    {
        public YandexStaticBackgroundTabThrottling()
        {
            Name = "YandexStaticBackgroundTabThrottling";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticWebUrl());
            driver.Wait(2);
            
            driver.CreateNewTab();
            driver.Wait(1);

            // Open second tab with local resource
            driver.NavigateToUrl(GetStaticWebUrl());
            driver.Wait(2);

            driver.CreateNewTab();
            driver.Wait(1);

            // Open second tab with local resource
            driver.NavigateToUrl(GetStaticWebUrl());
            driver.Wait(2);

            driver.CreateNewTab();
            driver.Wait(1);

            driver.NavigateToUrl(GetStaticWebUrl("empty.html"));
            WebSrv.StopWebSrv(Name);
        }
        
        private string GetStaticWebUrl(string file = "index.html")
        {
            return "http://localhost:8080/" + file;
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
