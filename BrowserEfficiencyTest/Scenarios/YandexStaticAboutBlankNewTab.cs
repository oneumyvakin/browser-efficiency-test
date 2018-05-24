using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticAboutBlankNewTab : Scenario
    {
        public YandexStaticAboutBlankNewTab()
        {
            Name = "YandexStaticAboutBlankNewTab";
            DefaultDuration = 30;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to URL to have same start conditions
            var url = "about:blank";
            driver.NavigateToUrl(url);

            for (var i = 0; i < 4; i++)
            {
                driver.Wait(1);

                driver.CreateNewTab();
                driver.Wait(1);
            }
        }
    }
}
