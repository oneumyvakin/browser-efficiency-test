using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticSlowMultiTabLoading : Scenario
    {
        string httpPort = "8082";
        string httpPortTls = "8083";

        public YandexStaticSlowMultiTabLoading()
        {
            Name = "YandexStaticSlowMultiTabLoading";
            DefaultDuration = 10;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(0, GetWebRootPath(), httpPort, httpPortTls);

            // Navigate
            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);

            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);

            driver.NavigateToUrl(GetStaticResourceUrl());
            driver.Wait(2);
            driver.CreateNewTab();
            driver.Wait(1);

            driver.NavigateToUrl("about:blank");
            driver.Wait(2);
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Do not stop server because we need it run in background // WebSrv.StopWebSrv(Name);
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {                

        }

        private string GetStaticResourceUrl()
        {            
            return $"http://localhost:{httpPort}/slow/index.html";
        }

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
