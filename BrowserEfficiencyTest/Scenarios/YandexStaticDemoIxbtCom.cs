using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoIxbtCom : Scenario
    {
        public YandexStaticDemoIxbtCom()
        {
            Name = "YandexStaticDemoIxbtCom";
            DefaultDuration = 20;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Stop replay server if it still running from another scenario
            WebPageReplay.StopWebSrv(Name);
            driver.Wait(5);

            // Start replay server
            Task<string> webSrvTask = WebPageReplay.StartWebPageReplay(GetWebPageReplayRecordPath());
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
            driver.NavigateToUrl("https://www.ixbt.com");
            driver.Wait(5);

            driver.ScrollPageSmoothDown(3); // Takes seconds Ntimes * 2          
        }
    }
}
