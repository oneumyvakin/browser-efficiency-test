using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls12EcdheRsaAes128Sha : Scenario
    {
        public YandexTls12EcdheRsaAes128Sha()
        {
            Name = "YandexTls12EcdheRsaAes128Sha";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls12-ecdhe-rsa-aes128-sha.xsstest.ru/youtube/";            
            driver.NavigateToUrl(sber);
            driver.Wait(1);                     
        }
    }
}
