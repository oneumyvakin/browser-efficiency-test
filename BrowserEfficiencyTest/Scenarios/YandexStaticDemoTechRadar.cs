using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoTechRadar : Scenario
    {
        string httpPort = "8084";
        string httpPortTls = "8085";

        public YandexStaticDemoTechRadar()
        {
            Name = "YandexStaticDemoTechRadar";
            DefaultDuration = 60;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Stop replay server if it still runned from another scenario
            WebPageReplay.StopWebSrv(Name);

            driver.Wait(5);

            // Start local web server for slow reload
            Task<string> webSrvTask = WebSrv.StartWebSrv(0, GetWebRootPath(), httpPort, httpPortTls);

            // Start replay server
            Task<string> wprSrvTask = WebPageReplay.StartWebPageReplay(GetWebPageReplayRecordPath());
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            
        }

        private string GetWebPageReplayRecordPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine("StaticResources", Name, Name + ".wprgo");
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Navigate
            driver.NavigateToUrl("https://www.techradar.com/");
            driver.Wait(5);
            // Hide modal pop-up
            driver.ExecuteScriptSafe("document.querySelector('a.omaha-element-close').click();");

            driver.ScrollPageSmoothDown(3); // Takes seconds Ntimes * 2

            driver.ScrollPageSmoothUp(3); // Takes seconds Ntimes * 2

            driver.NavigateToUrl("https://www.techradar.com/news/best-movies-on-netflix-uk");
            driver.Wait(5);
            // Hide modal pop-up
            driver.ExecuteScriptSafe("document.querySelector('a.omaha-element-close').click();");

            driver.ScrollPageSmoothDown(3); // Takes seconds Ntimes * 2 

            driver.ScrollPageSmoothUp(3); // Takes seconds Ntimes * 2
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine("StaticResources", Name);
        }
    }
}
