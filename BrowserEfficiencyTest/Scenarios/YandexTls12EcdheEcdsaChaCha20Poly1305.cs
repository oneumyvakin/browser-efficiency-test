﻿using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexTls12EcdheEcdsaChaCha20Poly1305 : Scenario
    {
        public YandexTls12EcdheEcdsaChaCha20Poly1305()
        {
            Name = "YandexTls12EcdheEcdsaChaCha20Poly1305";
            DefaultDuration = 40;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var sber = "https://tls12-ecdhe-ecdsa-chacha20-poly1305.xsstest.ru/youtube/";
            driver.NavigateToUrl(sber);
            driver.Wait(1);
        }
    }
}
