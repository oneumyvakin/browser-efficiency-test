using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls12DesCbc3Sha : Scenario
    {
        public YandexTls12DesCbc3Sha()
        {
            Name = "YandexTls12DesCbc3Sha";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls12-des-cbc3-sha.xsstest.ru/youtube/";            
            driver.NavigateToUrl(sber);
            driver.Wait(1);
        }
    }
}
