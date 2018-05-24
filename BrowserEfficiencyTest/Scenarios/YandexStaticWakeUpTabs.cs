using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticWakeUpTabs : Scenario
    {
        public YandexStaticWakeUpTabs()
        {
            Name = "YandexStaticWakeUpTabs";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            driver.WakeupTabs();
        }
    }
}
