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
using OpenQA.Selenium.Remote;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserEfficiencyTest
{
    internal class WikipediaUnitedStates : Scenario
    {
        public WikipediaUnitedStates()
        {
            // Specifify name and that it's 30s
            Name = "WikipediaUnitedStates";
            DefaultDuration = 30;
        }

        public override void SetUp(RemoteWebDriver driver)
        {
            // Start replay server
            Task<string> webSrvTask = WebPageReplay.StartWebPageReplay(GetWebPageReplayRecordPath());
        }

        public override void TearDown(RemoteWebDriver driver)
        {
            // Stop replay server
            WebPageReplay.StopWebSrv(Name);
        }

        private string GetWebPageReplayRecordPath()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(cwd, "StaticResources", Name, Name + ".wprgo");
        }

        public override void Run(RemoteWebDriver driver, string browser, CredentialManager credentialManager, ResponsivenessTimer timer)
        {
            // Nagivate to Wikipedia/United_States
            driver.NavigateToUrl("https://en.wikipedia.org/wiki/United_States");
            driver.Wait(5);

            // Scroll a bit
            driver.ScrollPage(12);

            timer.ExtractPageLoadTime("Wikipedia article");
        }
    }
}
