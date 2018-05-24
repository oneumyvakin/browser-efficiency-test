using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls11EcdheRsaAes256Sha : Scenario
    {
        public YandexTls11EcdheRsaAes256Sha()
        {
            Name = "YandexTls11EcdheRsaAes256Sha";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls11-ecdhe-rsa-aes256-sha.xsstest.ru/youtube/";
            driver.NavigateToUrl(sber);
            driver.Wait(1);
        }
    }
}
