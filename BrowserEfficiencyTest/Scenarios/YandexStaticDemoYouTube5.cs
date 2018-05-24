using OpenQA.Selenium.Remote;

namespace BrowserEfficiencyTest
{
    internal class YandexStaticDemoYouTube5 : Scenario
    {
        public YandexStaticDemoYouTube5()
        {
            Name = "YandexStaticDemoYouTube5";
            DefaultDuration = 60;
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {            
            driver.NavigateToUrl("https://www.youtube.com/watch?v=MzTcsI6tn-0&vq=hd1080");

            // Toggle Theater Mode after YouTube finishes loading a video.
            driver.Wait(5);
            driver.ExecuteScript("document.querySelector('#movie_player').clientWidth < 1000 ? document.querySelector('button.ytp-size-button').click() : console.log('already wide');");
            playVideo(driver);
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Stop video.
            driver.ExecuteScript("document.querySelector('button.ytp-play-button').click();");
        }

        private void playVideo(RemoteWebDriver driver)
        {
            string defVideoElem = "var yvid = document.querySelector('video.video-stream');";
            string defIsPlaying = "var isYvidPlaying = (yvid.currentTime > 0 && !yvid.paused && !yvid.ended && yvid.readyState > 2);";
            string defPlayBtn = "var playButton = document.querySelector('button.ytp-play-button');";
            string checkAndClick = "isYvidPlaying ? console.log('playing') : playButton.click();";
            string script = defVideoElem + defIsPlaying + defPlayBtn + checkAndClick;
            driver.ExecuteScriptSafe(script);
        }
    }
}
