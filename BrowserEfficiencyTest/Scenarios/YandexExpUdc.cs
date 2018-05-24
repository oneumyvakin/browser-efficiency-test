using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    // udc experiment described at BROWSER-80174
    internal class YandexExpUdc : Scenario
    {
        public YandexExpUdc()
        {
            Name = "YandexExpUdc";
            DefaultDuration = 50;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // auth url to return mail.yandex.ru
            var auth = "https://passport.yandex.ru/passport?mode=auth&from=mail&retpath=https%3A%2F%2Fmail.yandex.ru&origin=hostroot_ru_nol_mobile_enter";
            var disk = "https://mail.yandex.ru/my/#disk";
            var znak = "https://znak.com";
            var vk = "https://vk.com/";
            var yaRu = "http://ya.ru";
            // mail.yandex.ru/?uid=

            driver.NavigateToUrl(auth);
            driver.Wait(5);
            var loginField = driver.FindElementByName("login");
            var passwdField = driver.FindElementByName("passwd");
            var submitBtn = driver.FindElementByXPath("//button[@type='submit']"); 
            loginField.SendKeys("nsktester@yandex.ru");
            passwdField.SendKeys("Qqwerty11");
            submitBtn.Click();
            driver.Wait(10);

            driver.NavigateToUrl(disk);
            driver.Wait(5);

            driver.NavigateToUrl(znak);
            driver.Wait(3);

            driver.NavigateToUrl(vk);
            driver.Wait(3);
            
            driver.NavigateToUrl(yaRu);
            driver.Wait(3);
        }
    }
}
