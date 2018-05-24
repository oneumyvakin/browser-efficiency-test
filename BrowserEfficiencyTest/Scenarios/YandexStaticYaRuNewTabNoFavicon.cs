using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticYaRuNewTabNoFavicon : Scenario
    {
        public YandexStaticYaRuNewTabNoFavicon()
        {
            Name = "YandexStaticYaRuNewTabNoFavicon";
            DefaultDuration = 30;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to static resource with content of http://ya.ru
            var staticYaRu = GetStaticResourceUrl();
            driver.NavigateToUrl(staticYaRu);
            
            for(var i = 0; i < 4; i++)
            {
                driver.Wait(1);

                driver.CreateNewTab();
                driver.Wait(1);

                driver.NavigateToUrl(staticYaRu);
            }
        }

        private string GetStaticResourceUrl()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return "file://" + System.IO.Path.Combine(cwd, "StaticResources", Name, "index.html");
        }
    }
}
