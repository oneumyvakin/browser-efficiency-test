using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoReddit : Scenario
    {
        string httpPort = "8084";
        string httpPortTls = "8085";

        public YandexStaticDemoReddit()
        {
            Name = "YandexStaticDemoReddit";
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
            driver.NavigateToUrl("https://www.reddit.com/");
            driver.Wait(5);
            // Hide modal pop-up
            driver.ExecuteScriptSafe("document.querySelector('a.skip-for-now').click();");

            driver.ScrollPageSmoothDown(3); // Takes seconds Ntimes * 2

            driver.ScrollPageSmoothUp(3); // Takes seconds Ntimes * 2

            driver.NavigateToUrl("https://www.reddit.com/r/BikiniBottomTwitter/comments/8fw3xi/probably_a_repost/");
            driver.Wait(5);
            // Hide modal pop-up
            driver.ExecuteScriptSafe("document.querySelector('a.skip-for-now').click();");

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
