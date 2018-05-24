using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoVkCom : Scenario
    {
        public YandexStaticDemoVkCom()
        {
            Name = "YandexStaticDemoVkCom";
            DefaultDuration = 70;
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
            driver.NavigateToUrl("https://vk.com/prohitec");
            driver.Wait(5);
            for (var n = 0; n < 2; n++)
            {
                driver.ScrollPageSmoothDown(7);

                driver.ScrollPageSmoothUp(7);
            }

        }
    }
}
