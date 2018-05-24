using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticReadability : Scenario
    {
        public YandexStaticReadability()
        {
            Name = "YandexStaticReadability";
            DefaultDuration = 90;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();
            driver.NavigateToUrl(staticUrl);

            for (var i = 0; i < 9; i++)
            {
                driver.Wait(1);

                driver.CreateNewTab();
                driver.Wait(1);

                driver.NavigateToUrl(staticUrl);
            }
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            WebSrv.StopWebSrv(Name);
        }

        private string GetStaticResourceUrl()
        {          
            return $"http://localhost:{WebSrv.DefaultPort}/news/";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
