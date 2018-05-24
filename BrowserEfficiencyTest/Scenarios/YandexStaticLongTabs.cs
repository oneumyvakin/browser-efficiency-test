using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticLongTabs : Scenario
    {
        public YandexStaticLongTabs()
        {
            Name = "YandexStaticLongTabs";
            DefaultDuration = 960; // 16 min 
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());
            
            for (var i = 1; i < 15; i++)
            {                
                for (var n = 1; n < 6; n++)
                {
                    driver.CreateNewTab();
                    driver.Wait(1);
                    driver.NavigateToUrl($"http://site{n}.local:8080/index.html");
                    driver.Wait(9);
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
