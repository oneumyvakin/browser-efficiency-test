//--------------------------------------------------------------
//
// Browser Efficiency Test
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files(the ""Software""),
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//--------------------------------------------------------------

using OpenQA.Selenium.Remote;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class IxlEighthGradeScience : Scenario
    {
        public IxlEighthGradeScience()
        {
            Name = "IxlEighthGradeScience";
            DefaultDuration = 60;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Stop replay server if it still runned from another scenario
            WebPageReplay.StopWebSrv(Name);

            driver.Wait(5);

            // Start replay server
            Task<string> wprSrvTask = WebPageReplay.StartWebPageReplay(GetWebPageReplayRecordPath());
        }

        public override void TearDown(RemoteWebDriver driver)
        {

        }

        private string GetWebPageReplayRecordPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name, Name + ".wprgo");
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Go to IXL
            driver.NavigateToUrl("http://www.ixl.com");
            driver.Wait(5);

            driver.ScrollPage(2);

            // Go to 8th grade science
            driver.ClickElement(driver.FindElementByClassName("itr9").FindElement(By.ClassName("science")).FindElement(By.ClassName("lk-skills")));

            driver.Wait(5);

            // Go to test on density, mass, and volume
            driver.ClickElement(driver.FindElementByXPath("//*[@href='/science/grade-8/calculate-density-mass-and-volume']"));

            driver.Wait(3);
            driver.ScrollPage(2);

            // Start the test
            driver.ClickElement(driver.FindElement(By.XPath("//*[contains(@class, 'crisp-button') and contains(text(), 'Start')]")));

            driver.Wait(3);

            // Try three questions
            for (int i = 0; i < 3; i++)
            {
                // Supply an incorrect answer (unless we get really lucky and 1234 is correct)
                driver.TypeIntoField(driver.FindElementByClassName("fillIn"), "1234" + Keys.Enter);

                driver.Wait(3);
                driver.ScrollPage(2);

                // After looking at explanation, click "got it"
                driver.ClickElement(driver.FindElementByClassName("got-it-bottom"));
                driver.Wait(3);
            }
        }
    }
}
