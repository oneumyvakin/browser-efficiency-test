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

using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class AmazonSearch : Scenario
    {
        public AmazonSearch()
        {
            Name = "AmazonSearch";
            DefaultDuration = 45;
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
            // Navigate
            driver.NavigateToUrl("https://www.amazon.com");
            driver.Wait(5);

            // Type "Game of Thrones" in the search box and hit enter
            driver.TypeIntoField(driver.FindElementById("twotabsearchtextbox"), "Game of Thrones" + Keys.Enter);
            driver.Wait(5);

            // Click into "Game of Thrones Season 1"
            driver.ClickElement(driver.FindElementByXPath("//*[@title='Game of Thrones Season 1']"));
            driver.Wait(2);

            // Scroll down to reviews
            driver.ScrollPage(5);
        }
    }
}
