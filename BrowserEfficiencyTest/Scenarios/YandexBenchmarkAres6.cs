using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexBenchmarkAres6 : Scenario
    {
        public YandexBenchmarkAres6()
        {
            Name = "YandexBenchmarkAres6";
            DefaultDuration = 270;            
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to about:blank
            var benchmarkUrl = "http://browserbench.org/ARES-6/";
            driver.NavigateToUrl(benchmarkUrl);

            StartBenchmark(driver);
            driver.Wait(DefaultDuration - 30);
            SaveBenchmarkResult(driver);
        }

        private void StartBenchmark(RemoteWebDriver driver)
        {
            driver.FindElementById("status").Click();
        }

        private void SaveBenchmarkResult(RemoteWebDriver driver)
        {
            BenchmarkResult = driver.FindElementByXPath("//span[@id='Geomean']/span[@class='value']").Text;
        }

    }
}
