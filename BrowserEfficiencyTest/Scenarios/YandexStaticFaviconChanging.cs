using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticFaviconChanging : Scenario
    {
        public YandexStaticFaviconChanging()
        {
            Name = "YandexStaticFaviconChanging";
            DefaultDuration = 45;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to static resource
            var indexHtml = GetStaticResourceUrl();
            driver.NavigateToUrl(indexHtml);
        }

        private string GetStaticResourceUrl()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return "file://" + System.IO.Path.Combine(cwd, "StaticResources", Name, "index.html");
        }
    }
}
