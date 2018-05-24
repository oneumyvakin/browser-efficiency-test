using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls12EcdheRsaAes128GcmSha256: Scenario
    {
        public YandexTls12EcdheRsaAes128GcmSha256()
        {
            Name = "YandexTls12EcdheRsaAes128GcmSha256";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls12-ecdhe-rsa-aes128-gcm-sha256.xsstest.ru/youtube/";
            driver.NavigateToUrl(sber);
            driver.Wait(1);
        }
    }
}
