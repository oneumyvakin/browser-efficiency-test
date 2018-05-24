using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexRedirectTo : Scenario
    {
        public YandexRedirectTo()
        {
            Name = "YandexRedirectTo";
            DefaultDuration = 70;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();

            driver.NavigateToUrl(staticUrl + "?redirectTo=https://ya.ru/");
            driver.Wait(10);

            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl(staticUrl + "?redirectTo=https://google.ru/");
            driver.Wait(14);

            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl(staticUrl + "?redirectTo=https://amazon.com/");
            driver.Wait(14);

            driver.CreateNewTab();
            driver.Wait(1);
            driver.NavigateToUrl(staticUrl + "?redirectTo=https://youtube.com/");
            driver.Wait(14);

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
