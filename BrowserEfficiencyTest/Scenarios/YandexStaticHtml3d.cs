using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticHtml3d : Scenario
    {
        public YandexStaticHtml3d()
        {
            Name = "YandexStaticHtml3d";
            DefaultDuration = 110;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var blank = "about:blank";
            var html3d = "https://playcanvas.com/";

            driver.CreateNewTab();
            for (var i = 0; i < 20; i++)
            {                
                driver.Wait(1);
                driver.NavigateToUrl(html3d);
                driver.Wait(2);                
            }

            driver.NavigateToUrl(blank);
        }
    }
}
