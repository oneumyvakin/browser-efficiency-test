using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexBenchmarkSpeedometer : Scenario
    {
        public YandexBenchmarkSpeedometer()
        {
            Name = "YandexBenchmarkSpeedometer";
            DefaultDuration = 200;            
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var benchmarkUrl = "http://browserbench.org/Speedometer/";
            driver.NavigateToUrl(benchmarkUrl);

            StartBenchmark(driver);
            driver.Wait(DefaultDuration - 5);
            SaveBenchmarkResult(driver);
        }

        private void StartBenchmark(RemoteWebDriver driver)
        {
            driver.ExecuteScript("startTest()");
        }

        private void SaveBenchmarkResult(RemoteWebDriver driver)
        {
            BenchmarkResult = driver.FindElementById("result-number").Text;
        }

    }
}
