using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticStatusBar : Scenario
    {
        public YandexStaticStatusBar()
        {
            Name = "YandexStaticStatusBar";
            DefaultDuration = 115;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();
            driver.NavigateToUrl(staticUrl);

            var emptyA = driver.FindElementById("anchor-empty");
            var a1 = driver.FindElementById("anchor1");
            var a2 = driver.FindElementById("anchor2");

            for (int i = 0; i <= 10; i++)
            {
                var emptyActions1 = new OpenQA.Selenium.Interactions.Actions(driver);
                emptyActions1.MoveToElement(emptyA).Build().Perform();
                driver.Wait(2);

                var a1Actions = new OpenQA.Selenium.Interactions.Actions(driver);
                a1Actions.MoveToElement(a1).Build().Perform();
                driver.Wait(3);

                var emptyActions2 = new OpenQA.Selenium.Interactions.Actions(driver);
                emptyActions2.MoveToElement(emptyA).Build().Perform();
                driver.Wait(2);

                var a2Actions = new OpenQA.Selenium.Interactions.Actions(driver);
                a2Actions.MoveToElement(a2).Build().Perform();
                driver.Wait(3);
            }

            var emptyActionsEnd = new OpenQA.Selenium.Interactions.Actions(driver);
            emptyActionsEnd.MoveToElement(emptyA).Build().Perform();

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
