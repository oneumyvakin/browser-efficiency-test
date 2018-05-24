using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticTranslateInfobar : Scenario
    {
        public YandexStaticTranslateInfobar()
        {
            Name = "YandexStaticTranslateInfobar";
            DefaultDuration = 15;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start local web server
            Task<string> webSrvTask = WebSrv.StartWebSrv(DefaultDuration + 4, GetWebRootPath());
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Stop web server
            WebSrv.StopWebSrv(Name);
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to static resource
            var staticUrl = GetStaticResourceUrl();
            driver.NavigateToUrl(staticUrl);

            for (var i = 0; i < 5; i++)
            {
                driver.Wait(1);

                driver.NavigateToUrl(GetStaticResourceUrl());
            }
        }

        private string GetStaticResourceUrl(string args = "")
        {          
            return $"http://localhost:{WebSrv.DefaultPort}/{args}";
        }
        

        private string GetWebRootPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name);
        }
    }
}
