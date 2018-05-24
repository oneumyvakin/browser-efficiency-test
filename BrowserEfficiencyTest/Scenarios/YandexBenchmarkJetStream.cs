using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexBenchmarkJetStream : Scenario
    {
        public YandexBenchmarkJetStream()
        {
            Name = "YandexBenchmarkJetStream";
            DefaultDuration = 300;            
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var benchmarkUrl = "http://browserbench.org/JetStream/";
            driver.NavigateToUrl(benchmarkUrl);

            StartBenchmark(driver);
            driver.Wait(DefaultDuration - 30);
            SaveBenchmarkResult(driver);
        }

        private void StartBenchmark(RemoteWebDriver driver)
        {
            driver.Wait(5);
            driver.ExecuteScript("JetStream.start()");
        }

        private void SaveBenchmarkResult(RemoteWebDriver driver)
        {
            BenchmarkResult = driver.FindElementByClassName("score").Text.Substring(0, 6);
        }

    }
}
