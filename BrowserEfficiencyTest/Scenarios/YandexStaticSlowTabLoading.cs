using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticSlowTabLoading : Scenario
    {
        public YandexStaticSlowTabLoading()
        {
            Name = "YandexStaticSlowTabLoading";
            DefaultDuration = 45;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            string cwd = System.IO.Directory.GetCurrentDirectory();
            string webRootPath = System.IO.Path.Combine(cwd, "StaticResources", Name, "websrv.exe");
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, webRootPath);            

            // Nagivate to static resource
            var staticYaRu = GetStaticResourceUrl();
            driver.NavigateToUrl(staticYaRu);

            for (var i = 0; i < 2; i++)
            {
                driver.Wait(1);

                driver.NavigateToUrl(staticYaRu);
            }

            WebSrv.StopWebSrv(Name);
        }

        private string GetStaticResourceUrl()
        {            
            return "http://localhost:8080/slow?s=10";
        }
    }
}
