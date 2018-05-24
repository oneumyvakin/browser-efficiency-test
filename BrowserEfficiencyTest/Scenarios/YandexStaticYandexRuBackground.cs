using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticYandexRuBackground : Scenario
    {
        public YandexStaticYandexRuBackground()
        {
            Name = "YandexStaticYandexRuBackground";
            DefaultDuration = 90;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start replay server
            Task<string> webSrvTask = WebPageReplay.StartWebPageReplay(GetWebPageReplayRecordPath());

            // Navigate
            driver.NavigateToUrl("http://yandex.ru");
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl("http://yandex.ru");
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl("http://yandex.ru");
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl("http://yandex.ru");
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl("http://yandex.ru");
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl("about:blank");
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Stop replay server
            WebPageReplay.StopWebSrv(Name);
        }

        private string GetWebPageReplayRecordPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name, Name + ".wprgo");
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            
        }
    }
}
