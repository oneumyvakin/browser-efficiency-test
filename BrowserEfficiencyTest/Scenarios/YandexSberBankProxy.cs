using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexSberBankProxy : Scenario
    {
        public YandexSberBankProxy()
        {
            Name = "YandexSberBankProxy";
            DefaultDuration = 35;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var blank = "about:blank";
            var sber = "https://online.sberbank.ru";
            var localhost = "http://localhost:8080/CSAFront/index.do";

            // Start local web server
            Task<string> webSrvTask = WebSrv.StartProxy(DefaultDuration, sber);

            for (var i = 0; i < 5; i++)
            {
                driver.NavigateToUrl(blank);
                driver.Wait(1);

                driver.NavigateToUrl(localhost);
                driver.Wait(1);
            }

            Logger.LogWriteLine(webSrvTask.Result);
        }
    }
}
