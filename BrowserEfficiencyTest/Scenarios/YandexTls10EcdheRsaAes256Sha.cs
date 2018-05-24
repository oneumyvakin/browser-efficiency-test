using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls10EcdheRsaAes256Sha : Scenario
    {
        public YandexTls10EcdheRsaAes256Sha()
        {
            Name = "YandexTls10EcdheRsaAes256Sha";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls10-ecdhe-rsa-aes256-sha.xsstest.ru/youtube/";
            driver.NavigateToUrl(sber);
            driver.Wait(1);
        }
    }
}
