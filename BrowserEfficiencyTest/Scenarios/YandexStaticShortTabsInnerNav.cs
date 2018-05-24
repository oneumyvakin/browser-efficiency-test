using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticShortTabsInnerNav : Scenario
    {
        public YandexStaticShortTabsInnerNav()
        {
            Name = "YandexStaticShortTabsInnerNav";
            DefaultDuration = 70;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());
            
            for (var i = 1; i < 5; i++)
            {
                driver.CreateNewTab();
                driver.Wait(1);
                driver.NavigateToUrl($"http://site1.local:8080/index.html");
                driver.Wait(1);

                for (var n = 1; n < 8; n++)
                {
                    driver.NavigateToUrl($"http://site{n}.local:8080/index.html");
                    driver.Wait(1);
                }
            }

            Logger.LogWriteLine(webSrvTask.Result);
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
