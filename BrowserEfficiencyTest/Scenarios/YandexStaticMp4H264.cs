using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticMp4H264 : Scenario
    {
        public YandexStaticMp4H264()
        {
            Name = "YandexStaticMp4H264";
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
