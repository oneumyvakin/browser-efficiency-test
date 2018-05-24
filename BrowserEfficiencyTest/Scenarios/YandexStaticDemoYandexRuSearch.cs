using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoYandexRuSearch : Scenario
    {
        public YandexStaticDemoYandexRuSearch()
        {
            Name = "YandexStaticDemoYandexRuSearch";
            DefaultDuration = 40;
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
            driver.NavigateToUrl("http://yandex.ru");
            driver.Wait(3);
            driver.ScrollPageSmoothDown(3); // Takes seconds Ntimes * 2
            driver.ScrollPageSmoothUp(3); // Takes seconds Ntimes * 2

            string defTextInput = "var textInput = document.querySelector('#text');";
            string checkAndType = "textInput ? textInput.value = 'браузер' : console.log('text input not found');";
            driver.ExecuteScriptSafe(defTextInput + checkAndType);
            driver.Wait(1);
            string defButton = "var searchButton = document.querySelector('button.button');";
            string checkAndClick = "searchButton ? searchButton.click() : console.log('button not found');";
            driver.ExecuteScriptSafe(defButton + checkAndClick);
            driver.Wait(3);
            // https://yandex.ru/search/?lr=65&text=browser
            driver.ScrollPageSmoothDown(2); // Takes seconds Ntimes * 2 

            driver.ScrollPageSmoothUp(2); // Takes seconds Ntimes * 2             
        }
    }
}
