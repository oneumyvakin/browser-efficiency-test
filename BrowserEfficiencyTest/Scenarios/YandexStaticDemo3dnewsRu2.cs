using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemo3dnewsRu2 : Scenario
    {
        public YandexStaticDemo3dnewsRu2()
        {
            Name = "YandexStaticDemo3dnewsRu2";
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
            driver.NavigateToUrl("http://3dnews.ru");
            driver.Wait(5);

            driver.ScrollPageSmoothDown(3); // Takes seconds Ntimes * 2          
        }
    }
}
