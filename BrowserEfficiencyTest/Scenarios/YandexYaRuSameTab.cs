using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexYaRuSameTab : Scenario
    {
        public YandexYaRuSameTab()
        {
            Name = "YandexYaRuSameTab";
            DefaultDuration = 30;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to about:blank
            var yaRu = "http://ya.ru";
            driver.NavigateToUrl(yaRu);
            
            for(var i = 0; i < 4; i++)
            {
                driver.Wait(1);
                
                driver.Wait(1); // Keep for same time of tab displaying

                driver.NavigateToUrl(yaRu);
            }
        }
    }
}
