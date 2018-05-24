using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoAdFilter1 : Scenario
    {
        public YandexStaticDemoAdFilter1()
        {
            Name = "YandexStaticDemoAdFilter1";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();
            driver.NavigateToUrl(staticUrl);

            driver.Wait(5);
            driver.ExecuteScript("setInterval(function() { window.scrollBy({ top: 6, left: 0, behavior: \"smooth\" });}, 10);");
                       
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
