using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticYoutube : Scenario
    {
        public YandexStaticYoutube()
        {
            Name = "YandexStaticYoutube";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();
            driver.NavigateToUrl(staticUrl);
            
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
