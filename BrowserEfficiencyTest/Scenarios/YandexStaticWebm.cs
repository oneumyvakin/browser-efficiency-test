using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticWebm : Scenario
    {
        public YandexStaticWebm()
        {
            Name = "YandexStaticWebm";
            DefaultDuration = 50;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
        }

        private string GetStaticResourceUrl()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return "file://" + System.IO.Path.Combine(cwd, "StaticResources", Name, "index.html");
        }
    }
}
