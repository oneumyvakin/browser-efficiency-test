using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticWindowResize : Scenario
    {
        public YandexStaticWindowResize()
        {
            Name = "YandexStaticWindowResize";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to local static resource
            driver.NavigateToUrl("about:blank");

            driver.Wait(5);

            var curHeight = driver.Manage().Window.Size.Height;
            var curWidth = driver.Manage().Window.Size.Width;
            var initWindowPos = driver.Manage().Window.Position;
            var initWindowSize = new System.Drawing.Size(curWidth, curHeight);
            var windowSize = new System.Drawing.Size(curWidth, curHeight - 100);

            for(var i = 0; i < 4; i++)
            {
                driver.Wait(2);

                driver.Manage().Window.Maximize();

                driver.Wait(2);

                driver.Manage().Window.Size = windowSize;
                driver.Manage().Window.Position = new System.Drawing.Point(0, 0);                
            }

            // Return window in initial state
            driver.Manage().Window.Size = initWindowSize;
            driver.Manage().Window.Position = initWindowPos;
            driver.Wait(5);
        }
    }
}
