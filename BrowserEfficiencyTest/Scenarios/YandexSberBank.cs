using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexSberBank : Scenario
    {
        public YandexSberBank()
        {
            Name = "YandexSberBank";
            DefaultDuration = 35;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var blank = "about:blank";
            var sber = "https://online.sberbank.ru";

            for (var i = 0; i < 5; i++)
            {
                driver.NavigateToUrl(blank);
                driver.Wait(1);

                driver.NavigateToUrl(sber);
                driver.Wait(1);
            }            
        }
    }
}
