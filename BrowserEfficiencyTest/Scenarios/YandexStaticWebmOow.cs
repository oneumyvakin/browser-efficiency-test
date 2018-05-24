using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticWebmOow : Scenario
    {
        public YandexStaticWebmOow()
        {
            Name = "YandexStaticWebmOow";
            DefaultDuration = 20;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration, GetWebRootPath());

            // Nagivate to local static resource
            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(2);
            driver.LeftClick(391, 101);
            driver.CursorPos(390, 100);
            driver.Wait(1);
            driver.CursorPos(391, 101);
            driver.Wait(2);
            driver.CursorPos(390, 100);
            driver.Wait(1);
            driver.CursorPos(391, 101);
            driver.Wait(1);
            driver.LeftClick(390, 100); // for Yabro
            driver.LeftClick(400, 110); // for Opera
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(2);
            driver.NavigateToUrl(GetStaticResourceUrl() + "/scroll.html");
            driver.Wait(2);
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Stop web server
            WebSrv.StopWebSrv(Name);
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            driver.Wait(10);
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
