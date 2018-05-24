using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticImagesWebp : Scenario
    {
        public YandexStaticImagesWebp()
        {
            Name = "YandexStaticImagesWebp";
            DefaultDuration = 11;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Stop web server
            WebSrv.StopWebSrv(Name);
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(5);
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
