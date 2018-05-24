using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexWikipediaNewTab : Scenario
    {
        public YandexWikipediaNewTab()
        {
            Name = "YandexWikipediaNewTab";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var url = "https://ru.wikipedia.org/wiki/%D0%9D%D0%B5%D1%80%D0%B2%D0%B0";
            driver.NavigateToUrl(url);
            
            for(var i = 0; i < 9; i++)
            {
                driver.Wait(1);

                driver.CreateNewTab();
                driver.Wait(1);

                driver.NavigateToUrl(url);
            }
        }
    }
}
