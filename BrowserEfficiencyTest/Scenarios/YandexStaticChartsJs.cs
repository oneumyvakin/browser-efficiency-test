using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticChartsJs : Scenario
    {
        public YandexStaticChartsJs()
        {
            Name = "YandexStaticChartsJs";
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(5);

            // Randomize Data
            var randomizeDataButton = driver.FindElementById("randomizeData");
            Actions randomizeDataActions = new Actions(driver);
            randomizeDataActions.MoveToElement(randomizeDataButton);
            randomizeDataActions.Click().Perform();
            driver.Wait(3);

            randomizeDataButton.Click();
            driver.Wait(3);

            WebSrv.StopWebSrv(Name);
        }

        private string GetStaticResourceUrl()
        {
            return "http://localhost:8080/";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
