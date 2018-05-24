using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticWebmFullscreenSwitching : Scenario
    {
        public YandexStaticWebmFullscreenSwitching()
        {
            Name = "YandexStaticWebmFullscreenSwitching";
            DefaultDuration = 50;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());

            driver.Wait(1);

            for(var i = 0; i < 9; i++)
            {
                // Toggle video to fullscreen
                var fullscreenButton = driver.FindElementById("fullscreen");
                Actions fullscreenActions = new Actions(driver);
                fullscreenActions.MoveToElement(fullscreenButton);
                fullscreenActions.Click().Perform();
                driver.Wait(5);
            }
        }

        private string GetStaticResourceUrl()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return "file://" + System.IO.Path.Combine(cwd, "StaticResources", Name, "index.html");
        }
    }
}
