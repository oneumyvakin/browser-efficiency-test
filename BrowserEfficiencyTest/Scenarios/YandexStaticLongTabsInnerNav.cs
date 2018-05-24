using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticLongTabsInnerNav : Scenario
    {
        public YandexStaticLongTabsInnerNav()
        {
            Name = "YandexStaticLongTabsInnerNav";
            DefaultDuration = 480;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());
            
            for (var iter = 0; iter < 4; iter++)
            {
                for (var tabN = 1; tabN < 6; tabN++)
                {
                    driver.CreateNewTab();
                    driver.Wait(1);
                    driver.NavigateToUrl($"http://site{tabN}.local:8080/index.html");
                    driver.Wait(1);

                    for (var innerNav = 1; innerNav < 15; innerNav++)
                    {
                        driver.NavigateToUrl($"http://site{innerNav}.local:8080/index.html");
                        driver.Wait(1);
                    }
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
