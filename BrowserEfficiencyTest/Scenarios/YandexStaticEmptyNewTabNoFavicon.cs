using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticEmptyNewTabNoFavicon : Scenario
    {
        public YandexStaticEmptyNewTabNoFavicon()
        {
            Name = "YandexStaticEmptyNewTabNoFavicon";
            DefaultDuration = 30;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to static resource
            var indexHtml = GetStaticResourceUrl();
            driver.NavigateToUrl(indexHtml);
            
            for(var i = 0; i < 4; i++)
            {
                driver.Wait(1);

                driver.CreateNewTab();
                driver.Wait(1);

                driver.NavigateToUrl(indexHtml);
            }
        }

        private string GetStaticResourceUrl()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return "file://" + System.IO.Path.Combine(cwd, "StaticResources", Name, "index.html");
        }
    }
}
