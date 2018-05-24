using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexCoubCom : Scenario
    {
        public YandexCoubCom()
        {
            Name = "YandexCoubCom";
            DefaultDuration = 120;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var site = "https://coub.com/";
            driver.NavigateToUrl(site);
            
            for(var i = 0; i < 4; i++)
            {
                driver.ScrollPage(1);
                driver.Wait(1);

                driver.ScrollPage(1);
                driver.Wait(5);

                driver.ScrollPage(1);
                driver.Wait(1);
            }
        }
    }
}
