using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexBenchmarkMotionMark : Scenario
    {
        public YandexBenchmarkMotionMark()
        {
            Name = "YandexBenchmarkMotionMark";
            DefaultDuration = 420;            
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            var benchmarkUrl = "http://browserbench.org/MotionMark/";
            driver.NavigateToUrl(benchmarkUrl);

            StartBenchmark(driver);
            driver.Wait(DefaultDuration - 5);
            SaveBenchmarkResult(driver);
        }

        private void StartBenchmark(RemoteWebDriver driver)
        {
            driver.ExecuteScript("benchmarkController.startBenchmark()");
        }

        private void SaveBenchmarkResult(RemoteWebDriver driver)
        {
            BenchmarkResult = driver.FindElementByXPath("//div[@class='score']").Text;
        }

    }
}
