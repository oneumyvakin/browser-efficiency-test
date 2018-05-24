using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticBootstrapJavaScriptOffCSS : Scenario
    {
        public YandexStaticBootstrapJavaScriptOffCSS()
        {
            Name = "YandexStaticBootstrapJavaScriptOffCSS";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(5);

            // Launch live modal
            var liveModalLaunchButton = driver.FindElementById("live-example-modal-launch");
            //liveModalActions.MoveToElement(liveModalLaunchButton);
            driver.ExecuteScript("arguments[0].scrollIntoView(true);", liveModalLaunchButton);            
            driver.Wait(5);
            driver.ClickElement(liveModalLaunchButton);
            driver.Wait(5);

            // Close live modal
            var liveModalCloseButton = driver.FindElementById("live-example-modal-close");            
            driver.ExecuteScript("arguments[0].scrollIntoView(true);", liveModalCloseButton);            
            driver.Wait(5);
            driver.ClickElement(liveModalCloseButton);
            driver.Wait(5);

            // Move to dropdowns 
            var examplesDropdown = driver.FindElementById("dropdowns-examples");
            //dropdownsExamplesActions.MoveToElement(examplesDropdown);
            driver.ExecuteScript("arguments[0].scrollIntoView(true);", examplesDropdown);            
            driver.Wait(5);

            // Click on dropdowns
            var dropdownButton = driver.FindElementById("dropdown-example-open");
            driver.ExecuteScript("arguments[0].scrollIntoView(true);", dropdownButton);
            driver.Wait(10);
            driver.ClickElement(dropdownButton);
            driver.Wait(5);

            // Move to carousel 
            var examplesCarousel = driver.FindElementById("carousel-focus-here");
            driver.ExecuteScript("arguments[0].scrollIntoView(true);", dropdownButton);
            
            driver.Wait(5);
            WebSrv.StopWebSrv(Name);
        }

        private string GetStaticResourceUrl()
        {
            return $"http://localhost:{WebSrv.DefaultPort}/";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
