using OpenQA.Selenium.Remote;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoYouTube1 : Scenario
    {
        public YandexStaticDemoYouTube1()
        {
            Name = "YandexStaticDemoYouTube1";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            driver.NavigateToUrl("https://www.youtube.com/watch?v=AVivvO-kCwU");            
        }
    }
}
