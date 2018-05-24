using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticFavicon : Scenario
    {
        public YandexStaticFavicon()
        {
            Name = "YandexStaticFavicon";
            DefaultDuration = 70;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();
            driver.NavigateToUrl(staticUrl);

            for (var i = 0; i < 10; i++)
            {
                driver.NavigateToUrl(staticUrl);
                driver.Wait(1);

                driver.FindElementById("newwindow").Click();
                driver.Wait(1);
            }

            Logger.LogWriteLine(webSrvTask.Result);
        }

        private string GetStaticResourceUrl()
        {          
            return "http://localhost:8080/";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
