using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoYouTube2 : Scenario
    {
        public YandexStaticDemoYouTube2()
        {
            Name = "YandexStaticDemoYouTube2";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {            
            driver.NavigateToUrl("https://www.youtube.com/watch?v=yti2UVDt-h8");            
        }
    }
}
