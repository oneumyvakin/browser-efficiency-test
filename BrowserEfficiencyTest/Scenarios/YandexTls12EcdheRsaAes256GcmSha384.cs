using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls12EcdheRsaAes256GcmSha384 : Scenario
    {
        public YandexTls12EcdheRsaAes256GcmSha384()
        {
            Name = "YandexTls12EcdheRsaAes256GcmSha384";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls12-ecdhe-rsa-aes256-gcm-sha384.xsstest.ru/youtube/";
            driver.NavigateToUrl(sber);
            driver.Wait(1);
        }
    }
}
