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

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Opera;
using OpenQA.Selenium.Remote;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;

namespace BrowserEfficiencyTest
{
    /// <summary>
    /// Extension class for the RemoteWebDriver class.
    /// Additional WebDriver functionality can be added to this class and will extend the
    /// RemoteWebDriver class.
    /// </summary>
    public static class RemoteWebDriverExtension
    {
        private static int _port = -1;
        private static int _edgeWebDriverBuildNumber = 0;
        private static int _edgeBrowserBuildNumber = 0;
        private static string _hostName = "localhost";
        private static string _browser = "";
        private static string UserDataStored = "UserDataStored";
        private static string UserDataTmp = "UserDataTmp";
        private static Dictionary<string, string> CurrentUserProfile = new Dictionary<string, string>()
            {
                { "opera" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\Opera Software\Opera Stable" },
                { "operabeta" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\Opera Software\Opera Next" },
                { "yabro" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Local\Yandex\YandexBrowser\User Data" },
                { "brodefault" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Local\Yandex\YandexBrowser\User Data" },
                { "chrome" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Local\Google\Chrome\User Data" },
                { "chromium" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Local\Google\Chrome\User Data" },
                { "firefox" ,  @"C:\Users\" + Environment.UserName + @"\AppData\Local\Mozilla\Firefox\Profiles" }
            };

        /// <summary>
        /// Navigates to the url passed as a string
        /// A wrapper for RemoteWebDriver.Navigate().GoToUrl(...) method but includes tracing events and pageloading waits
        /// </summary>
        /// <param name="url">Url to navigate to in string form.</param>
        /// <param name="timeoutSec">Number of seconds to wait for the page to load before timing out.</param>
        public static void NavigateToUrl(this RemoteWebDriver remoteWebDriver, string url, int timeoutSec = 30)
        {
            ScenarioEventSourceProvider.EventLog.NavigateToUrl(url);
            remoteWebDriver.Navigate().GoToUrl(url);
            remoteWebDriver.WaitForPageLoad();
        }

        /// <summary>
        /// Navigates back one page.
        /// A wrapper for RemoteWebDriver.Navigate().Back() method but includes tracing events and pageloading waits
        /// </summary>
        /// <param name="remoteWebDriver"></param>
        /// <param name="timeoutSec">Number of seconds to wait for the page to load before timing out.</param>
        public static void NavigateBack(this RemoteWebDriver remoteWebDriver, int timeoutSec = 30)
        {
            ScenarioEventSourceProvider.EventLog.NavigateBack();
            remoteWebDriver.Navigate().Back();
            remoteWebDriver.WaitForPageLoad();
        }

        /// <summary>
        /// Creates a new tab in the browser.
        /// </summary>
        public static void CreateNewTab(this RemoteWebDriver remoteWebDriver)
        {
            int originalTabCount = remoteWebDriver.WindowHandles.Count;
            int endingTabCount = 0;

            ScenarioEventSourceProvider.EventLog.OpenNewTab(originalTabCount, originalTabCount + 1);

            if (IsNewTabCommandSupported(remoteWebDriver))
            {
                Logger.LogWriteLine(" New Tab: Attempting to create a new tab using the Edge newTab webdriver command.");
                CallEdgeNewTabCommand(remoteWebDriver).Wait();
            }
            else
            {
                // Use some JS. Note that this means you have to disable popup blocking in Microsoft Edge
                // You actually have to in Opera too, but that's provided in a flag below
                Logger.LogWriteLine(" New Tab: Attempting to create a new tab using the javascript method window.open()");
                remoteWebDriver.ExecuteScript("window.open()");
            }

            endingTabCount = remoteWebDriver.WindowHandles.Count;

            // sanity check to make sure we in fact did get a new tab opened.
            if (endingTabCount != (originalTabCount + 1))
            {
                throw new Exception(string.Format("New tab was not created as expected! Expected {0} tabs but found {1} tabs.", (originalTabCount + 1), endingTabCount));
            }

            // Go to that tab
            remoteWebDriver.SwitchTab(remoteWebDriver.WindowHandles[remoteWebDriver.WindowHandles.Count - 1]);

            // Give the browser more than enough time to open the tab and get to it so the next commands from the
            // scenario don't get lost
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Switches the browser tab to the tab referred to by tabHandle.
        /// </summary>
        /// <param name="tabHandle">The webdriver tabHandle of the desired tab to switch to.</param>
        public static void SwitchTab(this RemoteWebDriver remoteWebDriver, string tabHandle)
        {
            ScenarioEventSourceProvider.EventLog.SwitchTab(tabHandle);
            remoteWebDriver.SwitchTo().Window(tabHandle);
        }

        /// <summary>
        /// Closes the browser.
        /// </summary>
        public static void CloseBrowser(this RemoteWebDriver remoteWebDriver, string browser)
        {
            if (browser == "opera")
            {
                // Opera wouldn't close the window using .Quit() Instead, thespeed dial would remain open, which
                // would interfere with other tests. This key combination is used as a workaround.
                remoteWebDriver.FindElement(By.TagName("body")).SendKeys(Keys.Control + Keys.Shift + 'x');
            }
            else
            {
                remoteWebDriver.Quit();
            }
            ScenarioEventSourceProvider.EventLog.CloseBrowser(browser);
        }

        /// <summary>
        /// Scrolls down a web page using the page down key.
        /// </summary>
        /// <param name="timesToScroll">An abstract quantification of how much to scroll</param>
        public static void ScrollPage(this RemoteWebDriver remoteWebDriver, int timesToScroll)
        {
            // Webdriver examples had scrolling by executing Javascript. That approach seemed troublesome because the
            // browser is scrolling in a way very different from how it would with a real user, so we don't do it.
            // Page down seemed to be the best compromise in terms of it behaving like a real user scrolling, and it
            // working reliably across browsers.
            // Use the page down key.
            for (int i = 0; i < timesToScroll; i++)
            {
                ScenarioEventSourceProvider.EventLog.ScrollEvent();
                if (remoteWebDriver.ToString().ToLower().Contains("firefoxdriver"))
                {
                    // Send the commands to the body element for Firefox.
                    IWebElement body = remoteWebDriver.FindElementByTagName("body");
                    body.SendKeys(Keys.PageDown);
                }
                else
                {
                    remoteWebDriver.Keyboard.SendKeys(Keys.PageDown);
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Scrolls down a web page using the JavaScript.
        /// </summary>
        /// <param name="timesToScroll">An abstract quantification of how much to scroll</param>
        public static void ScrollPageSmoothDown(this RemoteWebDriver remoteWebDriver, int timesToScroll)
        {
            if (remoteWebDriver.ToString().ToLower().Contains("edgedriver")) {
                int horCenter = remoteWebDriver.Manage().Window.Size.Width / 2;
                int vertCenter = remoteWebDriver.Manage().Window.Size.Height / 2;
                for (var i = 0; i < timesToScroll; i++)
                {
                    remoteWebDriver.MouseWheelScrollDown(horCenter, vertCenter, 1333);
                    remoteWebDriver.Wait(2);
                }
                return;
            }
            // Webdriver examples had scrolling by executing Javascript. That approach seemed troublesome because the
            for (var i = 0; i < timesToScroll; i++)
            {
                remoteWebDriver.ExecuteScript("window.scrollBy({ top: 1000, left: 0, behavior: \"smooth\" });");
                remoteWebDriver.Wait(2);
            }
        }

        /// <summary>
        /// Scrolls up a web page using the JavaScript.
        /// </summary>
        /// <param name="timesToScroll">An abstract quantification of how much to scroll</param>
        public static void ScrollPageSmoothUp(this RemoteWebDriver remoteWebDriver, int timesToScroll)
        {
            if (remoteWebDriver.ToString().ToLower().Contains("edgedriver"))
            {
                int horCenter = remoteWebDriver.Manage().Window.Size.Width / 2;
                int vertCenter = remoteWebDriver.Manage().Window.Size.Height / 2;
                for (var i = 0; i < timesToScroll; i++)
                {
                    remoteWebDriver.MouseWheelScrollUp(horCenter, vertCenter, 1333);
                    remoteWebDriver.Wait(2);
                }
                return;
            }
            // Webdriver examples had scrolling by executing Javascript. That approach seemed troublesome because the
            for (var i = 0; i < timesToScroll; i++)
            {
                remoteWebDriver.ExecuteScript("window.scrollBy({ top: -1000, left: 0, behavior: \"smooth\" });");
                remoteWebDriver.Wait(2);
            }
        }

        /// <summary>
        /// Execute JavaScript with handling exceptions
        /// </summary>
        /// <param name="script">JS string to execute</param>
        public static void ExecuteScriptSafe(this RemoteWebDriver remoteWebDriver, string script)
        {
            try
            {
                remoteWebDriver.ExecuteScript(script);
            }
            catch (System.InvalidOperationException e)
            {
                Logger.LogWriteLine($"Handle exception at ExecuteScript({script}):\n" + e.ToString());

            }
        }

        /// <summary>
        /// Sends keystrokes to the browser. Not to a specific element.
        /// Wrapper for driver.Keyboard.SendKeys(...)
        /// </summary>
        /// <param name="keys">Keystrokes to send to the browser.</param>
        public static void SendKeys(this RemoteWebDriver remoteWebDriver, string keys)
        {
            ScenarioEventSourceProvider.EventLog.SendKeysStart(keys.Length);
            // Firefox driver does not currently support sending keystrokes to the browser.
            // So instead, get the body element and send the keystrokes to that element.
            if (remoteWebDriver.ToString().ToLower().Contains("firefoxdriver"))
            {
                IWebElement body = remoteWebDriver.FindElementByTagName("body");
                body.SendKeys(keys);
            }
            else
            {
                remoteWebDriver.Keyboard.SendKeys(keys);
            }
            ScenarioEventSourceProvider.EventLog.SendKeysStop(keys.Length);
        }

        /// <summary>
        /// Waits for the specified amount of time before executing the next command.
        /// </summary>
        /// <param name="secondsToWait">The number of seconds to wait</param>
        public static void Wait(this RemoteWebDriver remoteWebDriver, double secondsToWait)
        {
            ScenarioEventSourceProvider.EventLog.WaitStart(secondsToWait, "");
            Thread.Sleep((int)(secondsToWait * 1000));
            ScenarioEventSourceProvider.EventLog.WaitStop(secondsToWait, "");
        }

        /// <summary>
        /// Waits for the specified amount of time before executing the next command.
        /// </summary>
        /// <param name="secondsToWait">The number of seconds to wait</param>
        /// <param name="waitEventTag">String to be inserted with the wait start and stop trace event</param>
        public static void Wait(this RemoteWebDriver remoteWebDriver, double secondsToWait, string waitEventTag)
        {
            ScenarioEventSourceProvider.EventLog.WaitStart(secondsToWait, waitEventTag);
            Thread.Sleep((int)(secondsToWait * 1000));
            ScenarioEventSourceProvider.EventLog.WaitStop(secondsToWait, waitEventTag);
        }

        /// <summary>
        /// Types into the given WebElement the specified text
        /// </summary>
        /// <param name="element">The WebElement to type into</param>
        /// <param name="text">The text to type</param>
        public static void TypeIntoField(this RemoteWebDriver remoteWebdriver, IWebElement element, string text)
        {
            ScenarioEventSourceProvider.EventLog.TypeIntoFieldStart(text.Length);
            foreach (char c in text)
            {
                element.SendKeys(c.ToString());
                Thread.Sleep(75);
            }
            ScenarioEventSourceProvider.EventLog.TypeIntoFieldStop(text.Length);
        }

        /// <summary>
        /// Types the given text into whichever field has focus
        /// </summary>
        /// <param name="text">The text to type</param>
        public static void TypeIntoField(this RemoteWebDriver remoteWebDriver, string text)
        {
            ScenarioEventSourceProvider.EventLog.TypeIntoFieldStart(text.Length);
            foreach (char c in text)
            {
                remoteWebDriver.Keyboard.SendKeys(c.ToString());
                Thread.Sleep(75);
            }
            ScenarioEventSourceProvider.EventLog.TypeIntoFieldStop(text.Length);
        }

        /// <summary>
        /// Clicks on the given web element. Makes multiple attempts if necessary.
        /// </summary>
        /// <param name="element">The WebElement to click on</param>
        public static void ClickElement(this RemoteWebDriver remoteWebDriver, IWebElement element, int maxAttemptsToMake = 3)
        {
            int attempt = 0;
            bool isClickSuccessful = false;

            while (isClickSuccessful == false)
            {
                try
                {
                    ScenarioEventSourceProvider.EventLog.ClickElement(element.Text);
                    // Send the empty string to give focus, then enter. We do this instead of click() because
                    // click() has a bug on high DPI screen we're working around
                    element.SendKeys(string.Empty);
                    element.SendKeys(Keys.Enter);
                    isClickSuccessful = true;
                }
                catch (Exception)
                {
                    attempt++;

                    Logger.LogWriteLine("Failed attempt " + attempt + " to click element " + element.ToString());

                    Thread.Sleep(1000);

                    if (attempt >= maxAttemptsToMake)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a RemoteWebDriver instance based on the browser passed to this method. Opens the browser and maximizes its window.
        /// </summary>
        /// <param name="browser">The browser to get instantiate the Web Driver for.</param>
        /// <param name="browserProfilePath">The folder path to the browser user profile to use with the browser.</param>
        /// <returns>The RemoteWebDriver of the browser passed in to the method.</returns>
        public static RemoteWebDriver CreateDriverAndMaximize(
            string browser, bool clearBrowserCache, bool enableVerboseLogging = false, 
            string browserProfilePath = "", List<string> extensionPaths = null, int port = 17556, 
            string hostName = "localhost", bool enableBrowserTracing = false, string windowMode = "max", 
            Dictionary<string, string> broArgs = null)
        {
            var customArgs = new Dictionary<string, string[]>();
            if (broArgs.Count > 0)
            {
                if (broArgs.ContainsKey("all"))
                {
                    customArgs["all"] = broArgs["all"].Trim().Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToArray();
                }

                if (broArgs.ContainsKey(browser))
                {
                    customArgs[browser] = broArgs[browser].Trim().Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToArray();
                }
            }
            
            // Create a webdriver for the respective browser, depending on what we're testing.
            RemoteWebDriver driver = null;
            _browser = browser.ToLowerInvariant();
            ScenarioEventSourceProvider.EventLog.LaunchWebDriver(browser);
            switch (browser)
            {
                case "opera":
                case "operabeta":
                    OperaOptions oOption = new OperaOptions();
                    if (broArgs.ContainsKey("all"))
                    {
                        foreach (var arg in customArgs["all"])
                        {
                            oOption.AddArgument(arg);
                        }
                    }

                    if (broArgs.ContainsKey(browser))
                    {
                        foreach (var arg in customArgs[browser])
                        {
                            oOption.AddArgument(arg);
                        }
                    }
                    oOption.AddExcludedArgument("test-type"); // Video Pop-Up doesn work with "--test-type=webdriver"
                    oOption.AddArgument("--ignore-certificate-errors-spki-list=PhrPvGIaAMmd29hj8BCZOq096yj7uMpRNHpn5PDxI6I=,EuaYdplDHskaJ0cWyHwrDPG6osUDmFn8rNz5KIDz4Ok");
                    oOption.AddArgument("--lang=en");
                    oOption.AddArgument("--start-maximized"); // need to calc window height / width for "fair" mode
                    if (windowMode == "kiosk") {
                        Logger.LogWriteLine("Switch window to kiosk mode");
                        oOption.AddArgument("--kiosk");
                    }

                    oOption.AddArgument("--disable-popup-blocking");
                    oOption.AddArgument("--disable-infobars");

                    string operaPathEnv = Environment.GetEnvironmentVariable("OPERA_PATH");
                    if (operaPathEnv != null)
                        oOption.BinaryLocation = operaPathEnv;
                    else
                    {
                        oOption.BinaryLocation = @"C:\Program Files (x86)\Opera\launcher.exe";
                        if (browser == "operabeta")
                        {
                            // TODO: Ideally, this code would look inside the Opera beta folder for opera.exe
                            // rather than depending on flaky hard-coded version in directory
                            oOption.BinaryLocation = @"C:\Program Files (x86)\Opera beta\38.0.2220.25\opera.exe";
                        }
                    }
                    string operaProfilePathOverride = oOption.Arguments.FirstOrDefault(s => s.Contains("--user-data-dir="));
                    if (String.IsNullOrEmpty(operaProfilePathOverride))
                    {
                        if (!string.IsNullOrEmpty(browserProfilePath))
                        {
                            oOption.AddArgument("--user-data-dir=" + browserProfilePath);
                        }
                        else
                        {
                            oOption.AddArgument("--user-data-dir=" + CreateTmpProfile(browser));
                        }
                    }
                    else
                    {
                        Logger.LogWriteLine("Profile path override " + operaProfilePathOverride);
                    }

                    driver = new OperaDriver(oOption);

                    if (windowMode == "fair")
                    {
                        Logger.LogWriteLine("Switch window to fair size");
                        var curHeight = driver.Manage().Window.Size.Height;
                        var curWidth = driver.Manage().Window.Size.Width;
                        var windowSize = new System.Drawing.Size(curWidth, curHeight - 100); // Fair window size
                        driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                        driver.Manage().Window.Size = windowSize;
                    }

                    break;
                case "firefox":
                    FirefoxOptions firefoxOptions = new FirefoxOptions();
                    firefoxOptions.AddArgument($"-profile");
                    firefoxOptions.AddArgument($"{CreateTmpProfile(browser)}");
                    firefoxOptions.Profile = new FirefoxProfile(CreateTmpProfile(browser));

                    string firefoxPathEnv = Environment.GetEnvironmentVariable("FIREFOX_PATH");
                    if (firefoxPathEnv != null)
                    {
                        firefoxOptions.BrowserExecutableLocation = firefoxPathEnv;
                    }
                    FirefoxDriverService ffSvc = FirefoxDriverService.CreateDefaultService();
                    ffSvc.BrowserCommunicationPort = 2828;

                    driver = new FirefoxDriver(ffSvc, firefoxOptions, new TimeSpan(60000000000));
                    
                    if (windowMode == "fair")
                    {
                        var curHeight = driver.Manage().Window.Size.Height;
                        var curWidth = driver.Manage().Window.Size.Width;
                        var windowSize = new System.Drawing.Size(curWidth, curHeight - 100); // Fair window size
                        driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                        driver.Manage().Window.Size = windowSize;
                    }
                    break;
                case "chrome":
                case "chromium":
                    ChromeOptions option = new ChromeOptions();
                    option.AddUserProfilePreference("profile.default_content_setting_values.notifications", 1);

                    if (broArgs.ContainsKey("all"))
                    {
                        foreach (var arg in customArgs["all"])
                        {
                            option.AddArgument(arg);
                        }
                    }

                    if (broArgs.ContainsKey(browser))
                    {
                        foreach (var arg in customArgs[browser])
                        {
                            option.AddArgument(arg);
                        }
                    }

                    option.AddArgument("--lang=en");
                    option.AddArgument("--start-maximized");
                    if (windowMode == "kiosk")
                    {
                        option.AddArgument("--kiosk");
                    }
                    option.AddArgument("--disable-infobars");
                    //option.AddArgument("--disable-internal-flash");
                    //option.AddArgument("--disable-bundled-ppapi-flash");
                    //option.AddArgument("--disable-client-side-phishing-detection");
                    //option.AddArgument("--disable-component-extensions-with-background-pages");
                    option.AddArgument("--disable-component-update");
                    //option.AddArgument("--disable-default-apps");
                    //option.AddArgument("--disable-extensions");
                    //option.AddArgument("--disable-print-preview");
                    //option.AddArgument("--no-experiments");
                    option.AddArgument("--no-pings");
                    //option.AddArgument("--ya-disable-wallpaper-animation");
                    //option.AddArgument("--ya-wallpaper-update-period-minutes=0");
                    //option.AddArgument("--disable-custo-contents-task");
                    //option.AddArgument("--disable-dxt-image-limit");
                    //option.AddArgument("--disable-omnibox-infobar-panel");
                    //option.AddArgument("--disable-instaserp");
                    //option.AddArgument("--prerender-drop-opener=disabled");
                    //option.AddArgument("--push-to-call-disabled");
                    option.AddArgument("--safebrowsing-disable-extension-blacklist");
                    option.AddArgument("--safebrowsing-disable-fraud-protection");
                    //option.AddArgument("--disable-share-panel");
                    //option.AddArgument("--disable-context-panel");
                    //option.AddArgument("--ya-disable-reader");
                    //option.AddArgument("--on-startup-dont-wait-for-tablo");
                    option.AddArgument("--ya-hips-prompt-delay=6000");
                    //option.AddArgument("--ya-disable-render-context-menu-for-images");
                    //option.AddArgument("--ya-disable-smartback");
                    //option.AddArgument("--ya-disable-deferred-media");
                    //option.AddArgument("--disable-custo-page-loading-at-startup");
                    //option.AddArgument("--disable-custopage-transparency");
                    //option.AddArgument("--disable-merge-of-custo-processes");
                    //option.AddArgument("--ya-disable-bookmarks-validation");
                    //option.AddArgument("--disable-bits");

                    if (enableBrowserTracing)
                    {
                        //Add "performance" logging with all tracing categories
                        var perfOption = new ChromePerformanceLoggingPreferences();
                        perfOption.AddTracingCategory("audio");
                        perfOption.AddTracingCategory("base");
                        perfOption.AddTracingCategory("benchmark");
                        perfOption.AddTracingCategory("blink");
                        perfOption.AddTracingCategory("blink_gc");
                        perfOption.AddTracingCategory("blink_style");
                        perfOption.AddTracingCategory("blink.net");
                        perfOption.AddTracingCategory("blink.user_timing");
                        perfOption.AddTracingCategory("Blob");
                        perfOption.AddTracingCategory("browser");
                        perfOption.AddTracingCategory("CacheStorage");
                        perfOption.AddTracingCategory("cc");
                        perfOption.AddTracingCategory("cdp.perf");
                        perfOption.AddTracingCategory("content");
                        //perfOption.AddTracingCategory("devtools.timeline");
                        //perfOption.AddTracingCategory("devtools.timeline.async");
                        perfOption.AddTracingCategory("dwrite");
                        perfOption.AddTracingCategory("gpu");
                        perfOption.AddTracingCategory("gpu.angle");
                        perfOption.AddTracingCategory("identity");
                        perfOption.AddTracingCategory("IndexedDB");
                        perfOption.AddTracingCategory("input");
                        perfOption.AddTracingCategory("io");
                        perfOption.AddTracingCategory("ipc");
                        perfOption.AddTracingCategory("latencyInfo");
                        perfOption.AddTracingCategory("leveldb");
                        perfOption.AddTracingCategory("loader");
                        perfOption.AddTracingCategory("loading");
                        perfOption.AddTracingCategory("media");
                        perfOption.AddTracingCategory("mojom");
                        perfOption.AddTracingCategory("navigation");
                        perfOption.AddTracingCategory("net");
                        perfOption.AddTracingCategory("netlog");
                        perfOption.AddTracingCategory("omnibox");
                        perfOption.AddTracingCategory("rail");
                        perfOption.AddTracingCategory("renderer");
                        perfOption.AddTracingCategory("renderer_host");
                        perfOption.AddTracingCategory("renderer.scheduler");
                        perfOption.AddTracingCategory("RLZ");
                        perfOption.AddTracingCategory("service_manager");
                        perfOption.AddTracingCategory("ServiceWorker");
                        perfOption.AddTracingCategory("shutdown");
                        perfOption.AddTracingCategory("SiteEngagement");
                        perfOption.AddTracingCategory("skia");
                        perfOption.AddTracingCategory("startup");
                        perfOption.AddTracingCategory("task_scheduler");
                        perfOption.AddTracingCategory("test_gpu");
                        perfOption.AddTracingCategory("toplevel");
                        perfOption.AddTracingCategory("ui");
                        perfOption.AddTracingCategory("v8");
                        perfOption.AddTracingCategory("v8.execute");
                        perfOption.AddTracingCategory("ValueStoreFrontend::Backend");
                        perfOption.AddTracingCategory("views");
                        perfOption.AddTracingCategory("viz");
                        perfOption.AddTracingCategory("WebCore");
                        perfOption.AddTracingCategory("blink_gc");
                        perfOption.AddTracingCategory("blink.debug");
                        perfOption.AddTracingCategory("blink.debug.layout");
                        perfOption.AddTracingCategory("blink.debug.layout.trees");
                        perfOption.AddTracingCategory("blink.feature_usage");
                        perfOption.AddTracingCategory("blink.invalidation");
                        perfOption.AddTracingCategory("cc.debug");
                        perfOption.AddTracingCategory("cc.debug.cdp-perf");
                        perfOption.AddTracingCategory("cc.debug.display_items");
                        perfOption.AddTracingCategory("cc.debug.ipc");
                        perfOption.AddTracingCategory("cc.debug.overdraw");
                        perfOption.AddTracingCategory("cc.debug.picture");
                        perfOption.AddTracingCategory("cc.debug.quads");
                        perfOption.AddTracingCategory("cc.debug.scheduler");
                        perfOption.AddTracingCategory("cc.debug.scheduler.frames");
                        perfOption.AddTracingCategory("cc.debug.scheduler.now");
                        perfOption.AddTracingCategory("cc.debug.triangles");
                        perfOption.AddTracingCategory("compositor-worker");
                        //perfOption.AddTracingCategory("devtools.timeline");
                        //perfOption.AddTracingCategory("devtools.timeline.frame");
                        //perfOption.AddTracingCategory("devtools.timeline.invalidationTracking");
                        //perfOption.AddTracingCategory("devtools.timeline.layers");
                        //perfOption.AddTracingCategory("devtools.timeline.picture");
                        perfOption.AddTracingCategory("file");
                        perfOption.AddTracingCategory("gpu_decoder");
                        perfOption.AddTracingCategory("gpu.debug");
                        perfOption.AddTracingCategory("gpu.device");
                        perfOption.AddTracingCategory("gpu.service");
                        perfOption.AddTracingCategory("ipc.flow");
                        perfOption.AddTracingCategory("loading");
                        perfOption.AddTracingCategory("memory-infra");
                        perfOption.AddTracingCategory("net");
                        perfOption.AddTracingCategory("network");
                        perfOption.AddTracingCategory("renderer.scheduler");
                        perfOption.AddTracingCategory("renderer.scheduler.debug");
                        perfOption.AddTracingCategory("skia");
                        perfOption.AddTracingCategory("skia.gpu");
                        perfOption.AddTracingCategory("skia.gpu.cache");
                        perfOption.AddTracingCategory("system_stats");
                        perfOption.AddTracingCategory("toplevel.flow");
                        perfOption.AddTracingCategory("v8.compile");
                        perfOption.AddTracingCategory("v8.cpu_profiler");
                        perfOption.AddTracingCategory("v8.cpu_profiler.hires");
                        perfOption.AddTracingCategory("v8.gc");
                        perfOption.AddTracingCategory("v8.gc_stats");
                        perfOption.AddTracingCategory("v8.ic_stats");
                        perfOption.AddTracingCategory("v8.runtime");
                        perfOption.AddTracingCategory("v8.runtime_stats");
                        perfOption.AddTracingCategory("v8.runtime_stats_sampling");
                        perfOption.AddTracingCategory("worker.scheduler");
                        perfOption.AddTracingCategory("worker.scheduler.debug");
                        option.PerformanceLoggingPreferences = perfOption;
                        option.SetLoggingPreference("performance", LogLevel.All);
                    }                    
                    
                    string chromePathEnv = Environment.GetEnvironmentVariable("CHROME_PATH");
                    if (chromePathEnv != null)
                    {
                        option.BinaryLocation = chromePathEnv;
                    }
                    if (browser == "chromium")
                    {
                        var destFile = Path.GetDirectoryName(option.BinaryLocation) + @"\chromium.exe";
                        File.Copy(option.BinaryLocation, destFile, true);
                        option.BinaryLocation = destFile;
                    }

                    ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                    if (enableVerboseLogging)
                    {
                        chromeDriverService.EnableVerboseLogging = true;
                    }

                    string profilePathOverride = option.Arguments.FirstOrDefault(s => s.Contains("--user-data-dir="));
                    if (String.IsNullOrEmpty(profilePathOverride))
                    {
                        if (!string.IsNullOrEmpty(browserProfilePath))
                        {
                            option.AddArgument("--user-data-dir=" + browserProfilePath);
                        }
                        else
                        {
                            option.AddArgument("--user-data-dir=" + CreateTmpProfile(browser));
                        }
                    } else
                    {
                        Logger.LogWriteLine("Profile path override " + profilePathOverride);
                    }
                    
                    
                    driver = new ChromeDriver(chromeDriverService, option);
                    // Close infobar "Chrome is being contolled by automation software"                    
                    // driver.LeftClick(1250, 90);
                    driver.CursorPos(driver.Manage().Window.Size.Width / 2, driver.Manage().Window.Size.Height); // Position cursor outside of working area

                    if (windowMode == "fair")
                    {
                        var curHeight = driver.Manage().Window.Size.Height;
                        var curWidth = driver.Manage().Window.Size.Width;
                        var fairHeigh = curHeight - 100 + 25;  // increase Chrome render area to be equal with YaBro 
                        var windowSize = new System.Drawing.Size(curWidth, fairHeigh); 
                        driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                        driver.Manage().Window.Size = windowSize;
                    }
                    break;
                case "yabro":
                case "brodefault":
                    ChromeOptions ya_option = new ChromeOptions();
                    ya_option.AddUserProfilePreference("profile.default_content_setting_values.notifications", 1);

                    if (broArgs.ContainsKey("all"))
                    {
                        foreach (var arg in customArgs["all"])
                        {
                            ya_option.AddArgument(arg);
                        }
                    }

                    if (broArgs.ContainsKey(browser))
                    {
                        foreach (var arg in customArgs[browser])
                        {
                            ya_option.AddArgument(arg);
                        }
                    }
                    //ya_option.AddArgument("--host-rules=MAP *:80 127.0.0.1:8080,MAP *:443 127.0.0.1:8081,EXCLUDE localhost");
                    ya_option.AddArgument("--ignore-certificate-errors-spki-list=PhrPvGIaAMmd29hj8BCZOq096yj7uMpRNHpn5PDxI6I=,EuaYdplDHskaJ0cWyHwrDPG6osUDmFn8rNz5KIDz4Ok");
                    ya_option.AddArgument("--lang=en");
                    ya_option.AddArgument("--start-maximized");
                    if (windowMode == "kiosk")
                    {
                        ya_option.AddArgument("--kiosk");
                    }
                    ya_option.AddArgument("--disable-infobars");
                    //ya_option.AddArgument("--disable-internal-flash");
                    //ya_option.AddArgument("--disable-bundled-ppapi-flash");
                    //ya_option.AddArgument("--disable-client-side-phishing-detection");
                    //ya_option.AddArgument("--disable-component-extensions-with-background-pages");
                    ya_option.AddArgument("--disable-component-update");
                    //ya_option.AddArgument("--disable-default-apps");
                    //ya_option.AddArgument("--disable-extensions");
                    //ya_option.AddArgument("--disable-print-preview");
                    //ya_option.AddArgument("--no-experiments");
                    ya_option.AddArgument("--no-pings");
                    //ya_option.AddArgument("--ya-disable-wallpaper-animation");
                    //ya_option.AddArgument("--ya-wallpaper-update-period-minutes=0");
                    //ya_option.AddArgument("--disable-custo-contents-task");
                    //ya_option.AddArgument("--disable-dxt-image-limit");
                    //ya_option.AddArgument("--disable-omnibox-infobar-panel");
                    //ya_option.AddArgument("--disable-instaserp");
                    //ya_option.AddArgument("--prerender-drop-opener=disabled");
                    //ya_option.AddArgument("--push-to-call-disabled");
                    ya_option.AddArgument("--safebrowsing-disable-extension-blacklist");
                    ya_option.AddArgument("--safebrowsing-disable-fraud-protection");
                    //ya_option.AddArgument("--disable-share-panel");
                    //ya_option.AddArgument("--disable-context-panel");
                    //ya_option.AddArgument("--ya-disable-reader");
                    //ya_option.AddArgument("--on-startup-dont-wait-for-tablo");
                    ya_option.AddArgument("--ya-hips-prompt-delay=6000");
                    //ya_option.AddArgument("--ya-disable-render-context-menu-for-images");
                    //ya_option.AddArgument("--ya-disable-smartback");
                    //ya_option.AddArgument("--ya-disable-deferred-media");
                    //ya_option.AddArgument("--disable-custo-page-loading-at-startup");
                    //ya_option.AddArgument("--disable-custopage-transparency");
                    //ya_option.AddArgument("--disable-merge-of-custo-processes");
                    //ya_option.AddArgument("--ya-disable-bookmarks-validation");
                    //ya_option.AddArgument("--disable-bits");

                    if (enableBrowserTracing)
                    {
                        //Add "performance" logging with all tracing categories
                        var yaPerfOption = new ChromePerformanceLoggingPreferences();
                        yaPerfOption.AddTracingCategory("audio");
                        yaPerfOption.AddTracingCategory("base");
                        yaPerfOption.AddTracingCategory("BaseBubbleView");
                        yaPerfOption.AddTracingCategory("benchmark");
                        yaPerfOption.AddTracingCategory("blink");
                        yaPerfOption.AddTracingCategory("blink_gc");
                        yaPerfOption.AddTracingCategory("blink_style");
                        yaPerfOption.AddTracingCategory("blink.net");
                        yaPerfOption.AddTracingCategory("blink.user_timing");
                        yaPerfOption.AddTracingCategory("Blob");
                        yaPerfOption.AddTracingCategory("browser");
                        yaPerfOption.AddTracingCategory("CacheStorage");
                        yaPerfOption.AddTracingCategory("cc");
                        yaPerfOption.AddTracingCategory("cdp.perf");
                        yaPerfOption.AddTracingCategory("content");
                        yaPerfOption.AddTracingCategory("custo");
                        yaPerfOption.AddTracingCategory("DownloadableResource");
                        yaPerfOption.AddTracingCategory("dwrite");
                        yaPerfOption.AddTracingCategory("event");
                        yaPerfOption.AddTracingCategory("gpu");
                        yaPerfOption.AddTracingCategory("gpu.angle");
                        yaPerfOption.AddTracingCategory("IndexedDB");
                        yaPerfOption.AddTracingCategory("input");
                        yaPerfOption.AddTracingCategory("io");
                        yaPerfOption.AddTracingCategory("ipc");
                        yaPerfOption.AddTracingCategory("latencyInfo");
                        yaPerfOption.AddTracingCategory("leveldb");
                        yaPerfOption.AddTracingCategory("loader");
                        yaPerfOption.AddTracingCategory("loading");
                        yaPerfOption.AddTracingCategory("media");
                        yaPerfOption.AddTracingCategory("mojom");
                        yaPerfOption.AddTracingCategory("navigation");
                        yaPerfOption.AddTracingCategory("net");
                        yaPerfOption.AddTracingCategory("netlog");
                        yaPerfOption.AddTracingCategory("oowv");
                        yaPerfOption.AddTracingCategory("ProtectBubble");
                        yaPerfOption.AddTracingCategory("rail");
                        yaPerfOption.AddTracingCategory("renderer");
                        yaPerfOption.AddTracingCategory("renderer_host");
                        yaPerfOption.AddTracingCategory("renderer.scheduler");
                        yaPerfOption.AddTracingCategory("server_configs");
                        yaPerfOption.AddTracingCategory("service_manager");
                        yaPerfOption.AddTracingCategory("ServiceWorker");
                        yaPerfOption.AddTracingCategory("shutdown");
                        yaPerfOption.AddTracingCategory("SiteEngagement");
                        yaPerfOption.AddTracingCategory("sovetnik");
                        yaPerfOption.AddTracingCategory("split");
                        yaPerfOption.AddTracingCategory("startup");
                        yaPerfOption.AddTracingCategory("svs");
                        yaPerfOption.AddTracingCategory("tablo");
                        yaPerfOption.AddTracingCategory("task_scheduler");
                        yaPerfOption.AddTracingCategory("test_tracing");
                        yaPerfOption.AddTracingCategory("toplevel");
                        yaPerfOption.AddTracingCategory("ui");
                        yaPerfOption.AddTracingCategory("v8");
                        yaPerfOption.AddTracingCategory("v8.execute");
                        yaPerfOption.AddTracingCategory("ValueStoreFrontend::Backend");
                        yaPerfOption.AddTracingCategory("views");
                        yaPerfOption.AddTracingCategory("viz");
                        yaPerfOption.AddTracingCategory("wallpapers");
                        yaPerfOption.AddTracingCategory("WebCore");
                        yaPerfOption.AddTracingCategory("yandex.ProtectedHosts");
                        yaPerfOption.AddTracingCategory("blink_gc");
                        yaPerfOption.AddTracingCategory("blink.debug");
                        yaPerfOption.AddTracingCategory("blink.debug.layout");
                        yaPerfOption.AddTracingCategory("blink.debug.layout.trees");
                        yaPerfOption.AddTracingCategory("blink.feature_usage");
                        yaPerfOption.AddTracingCategory("blink.invalidation");
                        yaPerfOption.AddTracingCategory("cc.debug");
                        yaPerfOption.AddTracingCategory("cc.debug.cdp-perf");
                        yaPerfOption.AddTracingCategory("cc.debug.display_items");
                        yaPerfOption.AddTracingCategory("cc.debug.ipc");
                        yaPerfOption.AddTracingCategory("cc.debug.overdraw");
                        yaPerfOption.AddTracingCategory("cc.debug.picture");
                        yaPerfOption.AddTracingCategory("cc.debug.quads");
                        yaPerfOption.AddTracingCategory("cc.debug.scheduler");
                        yaPerfOption.AddTracingCategory("cc.debug.scheduler.frames");
                        yaPerfOption.AddTracingCategory("cc.debug.scheduler.now");
                        yaPerfOption.AddTracingCategory("cc.debug.triangles");
                        yaPerfOption.AddTracingCategory("compositor-worker");
                        //yaPerfOption.AddTracingCategory("devtools.timeline");
                        //yaPerfOption.AddTracingCategory("devtools.timeline.async");
                        //yaPerfOption.AddTracingCategory("devtools.timeline.frame");
                        //yaPerfOption.AddTracingCategory("devtools.timeline.invalidationTracking");
                        //yaPerfOption.AddTracingCategory("devtools.timeline.layers");
                        //yaPerfOption.AddTracingCategory("devtools.timeline.picture");
                        //yaPerfOption.AddTracingCategory("devtools.timeline.stack");
                        yaPerfOption.AddTracingCategory("file");
                        yaPerfOption.AddTracingCategory("focus");
                        yaPerfOption.AddTracingCategory("gpu_decoder");
                        yaPerfOption.AddTracingCategory("gpu.debug");
                        yaPerfOption.AddTracingCategory("gpu.device");
                        yaPerfOption.AddTracingCategory("gpu.service");
                        yaPerfOption.AddTracingCategory("ipc.flow");
                        yaPerfOption.AddTracingCategory("loading");
                        yaPerfOption.AddTracingCategory("memory-infra");
                        yaPerfOption.AddTracingCategory("net");
                        yaPerfOption.AddTracingCategory("network");
                        yaPerfOption.AddTracingCategory("renderer.scheduler");
                        yaPerfOption.AddTracingCategory("renderer.scheduler.debug");
                        yaPerfOption.AddTracingCategory("skia");
                        yaPerfOption.AddTracingCategory("skia.gpu");
                        yaPerfOption.AddTracingCategory("skia.gpu.cache");
                        yaPerfOption.AddTracingCategory("svs.sync");
                        yaPerfOption.AddTracingCategory("system_stats");
                        yaPerfOption.AddTracingCategory("toplevel.flow");
                        yaPerfOption.AddTracingCategory("v8.compile");
                        yaPerfOption.AddTracingCategory("v8.cpu_profiler");
                        yaPerfOption.AddTracingCategory("v8.cpu_profiler.hires");
                        yaPerfOption.AddTracingCategory("v8.gc");
                        yaPerfOption.AddTracingCategory("v8.gc_stats");
                        yaPerfOption.AddTracingCategory("v8.ic_stats");
                        yaPerfOption.AddTracingCategory("v8.runtime");
                        yaPerfOption.AddTracingCategory("v8.runtime_stats");
                        yaPerfOption.AddTracingCategory("v8.runtime_stats_sampling");
                        yaPerfOption.AddTracingCategory("worker.scheduler");
                        yaPerfOption.AddTracingCategory("worker.scheduler.debug");


                        ya_option.PerformanceLoggingPreferences = yaPerfOption;
                        ya_option.SetLoggingPreference("performance", LogLevel.All);
                    }

                    string yaPathEnv = Environment.GetEnvironmentVariable("YB_PATH");
                    if (yaPathEnv != null)
                        ya_option.BinaryLocation = yaPathEnv;
                    else
                        ya_option.BinaryLocation = @"C:\Users\" + Environment.UserName + @"\AppData\Local\Yandex\YandexBrowser\Application\browser.exe";
                    if (browser == "brodefault")
                    {
                        var destFile = Path.GetDirectoryName(ya_option.BinaryLocation) + @"\brodefault.exe";
                        File.Copy(ya_option.BinaryLocation, destFile, true);
                        ya_option.BinaryLocation = destFile;
                    }

                    ChromeDriverService ya_chromeDriverService = ChromeDriverService.CreateDefaultService();
                    if (enableVerboseLogging)
                    {
                        ya_chromeDriverService.EnableVerboseLogging = true;
                    }

                    string yaProfilePathOverride = ya_option.Arguments.FirstOrDefault(s => s.Contains("--user-data-dir="));
                    if (String.IsNullOrEmpty(yaProfilePathOverride))
                    {
                        if (!string.IsNullOrEmpty(browserProfilePath))
                        {
                            ya_option.AddArgument("--user-data-dir=" + browserProfilePath);
                        }
                        else
                        {
                            ya_option.AddArgument("--user-data-dir=" + CreateTmpProfile(browser));
                            // Delete system user profile to avoid issues like https://st.yandex-team.ru/BROWSER-65114
                            /*
                            if (Directory.Exists(CurrentUserProfile[browser]))
                            {
                                DeleteDirectory(CurrentUserProfile[browser]);
                            }
                            */
                        }
                    } else
                    {
                        Logger.LogWriteLine("Profile path override: " + yaProfilePathOverride);
                    }

                    driver = new ChromeDriver(ya_chromeDriverService, ya_option);
                    // Close infobar "Chrome is being contolled by automation software" 
                    // driver.LeftClick(848, 75); // Need to calculate for Russian localization
                    // driver.LeftClick(390, 85); // For English localization
                    driver.CursorPos(driver.Manage().Window.Size.Width / 2, driver.Manage().Window.Size.Height); // Position cursor outside of working area

                    if (windowMode == "fair")
                    {
                        var curHeight = driver.Manage().Window.Size.Height;
                        var curWidth = driver.Manage().Window.Size.Width;
                        var windowSize = new System.Drawing.Size(curWidth, curHeight - 100); //
                        driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                        driver.Manage().Window.Size = windowSize;
                    }
                    break;
                default:
                    EdgeOptions edgeOptions = new EdgeOptions();
                    edgeOptions.AddAdditionalCapability("browserName", "Microsoft Edge");

                    EdgeDriverService edgeDriverService = null;

                    if (extensionPaths != null && extensionPaths.Count != 0)
                    {
                        // Create the extensionPaths capability object
                        edgeOptions.AddAdditionalCapability("extensionPaths", extensionPaths);
                        foreach (var path in extensionPaths)
                        {
                            Logger.LogWriteLine("Sideloading extension(s) from " + path);
                        }
                    }

                    if (hostName.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Using localhost so create a local EdgeDriverService and instantiate an EdgeDriver object with it.
                        // We have to use EdgeDriver here instead of RemoteWebDriver because we need a
                        // DriverServiceCommandExecutor object which EdgeDriver creates when instantiated

                        edgeDriverService = EdgeDriverService.CreateDefaultService();
                        if(enableVerboseLogging)
                        {
                            edgeDriverService.UseVerboseLogging = true;
                        }

                        _port = edgeDriverService.Port;
                        _hostName = hostName;

                        Logger.LogWriteLine(string.Format("  Instantiating EdgeDriver object for local execution - Host: {0}  Port: {1}", _hostName, _port));
                        driver = new EdgeDriver(edgeDriverService, edgeOptions);
                    }
                    else
                    {
                        // Using a different host name.
                        // We will make the assumption that this host name is the host of a remote webdriver instance.
                        // We have to use RemoteWebDriver here since it is capable of being instantiated without automatically
                        // opening a local MicrosoftWebDriver.exe instance (EdgeDriver automatically launches a local process with
                        // MicrosoftWebDriver.exe).

                        _port = port;
                        _hostName = hostName;
                        var remoteUri = new Uri("http://" + _hostName + ":" + _port + "/");

                        Logger.LogWriteLine(string.Format("  Instantiating RemoteWebDriver object for remote execution - Host: {0}  Port: {1}", _hostName, _port));
                        driver = new RemoteWebDriver(remoteUri, edgeOptions.ToCapabilities());
                    }

                    Thread.Sleep(2000);

                    if (clearBrowserCache)
                    {
                        Logger.LogWriteLine("   Clearing Edge browser cache...");
                        ScenarioEventSourceProvider.EventLog.ClearEdgeBrowserCacheStart();
                        // Warning: this blows away all Microsoft Edge data, including bookmarks, cookies, passwords, etc
                        HttpClient client = new HttpClient();
                        client.DeleteAsync($"http://{_hostName}:{_port}/session/{driver.SessionId}/ms/history").Wait();
                        ScenarioEventSourceProvider.EventLog.ClearEdgeBrowserCacheStop();
                    }

                    _edgeBrowserBuildNumber = GetEdgeBuildNumber(driver);
                    Logger.LogWriteLine(string.Format("   Browser Version - MicrosoftEdge Build Version: {0}", _edgeBrowserBuildNumber));

                    string webDriverServerVersion = GetEdgeWebDriverVersion(driver);
                    Logger.LogWriteLine(string.Format("   WebDriver Server Version - MicrosoftWebDriver.exe File Version: {0}", webDriverServerVersion));
                    _edgeWebDriverBuildNumber = Convert.ToInt32(webDriverServerVersion.Split('.')[2]);

                    if (windowMode == "fair")
                    {
                        driver.Manage().Window.Maximize();
                        var curHeight = driver.Manage().Window.Size.Height;
                        var curWidth = driver.Manage().Window.Size.Width;
                        var windowSize = new System.Drawing.Size(curWidth, curHeight - 100 - 37); // Fair window size
                        driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                        driver.Manage().Window.Size = windowSize;
                    }

                    break;
            }

            Thread.Sleep(1000);

            return driver;
        }

        // Gets the Edge build version number.
        private static int GetEdgeBuildNumber(RemoteWebDriver remoteWebDriver)
        {
            int edgeBuildNumber = 0;

            string response = (string)remoteWebDriver.ExecuteScript("return navigator.userAgent;");
            string edgeVersionToken = response.Split(' ').ToList().FirstOrDefault(s => s.StartsWith("Edge"));

            if (string.IsNullOrEmpty(edgeVersionToken))
            {
                Logger.LogWriteLine("   Unable to extract Edge build version!");
            }
            else
            {
                var edgeBuildTokens = edgeVersionToken.Split('.');
                if (edgeBuildTokens.Length > 1)
                {
                    if (int.TryParse(edgeBuildTokens[1], out int returnedInt))
                    {
                        edgeBuildNumber = returnedInt;
                    }
                    else
                    {
                        Logger.LogWriteLine(string.Format("   Unable to extract Edge build version from {0}", edgeVersionToken));
                    }
                }
            }
            return edgeBuildNumber;
        }
        /// <summary>
        /// Waits up to timeoutSec for the page load to complete
        /// </summary>
        /// <param name="timeoutSec">Number of seconds to wait</param>
        public static void WaitForPageLoad(this RemoteWebDriver driver, int timeoutSec = 30)
        {
            WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, timeoutSec));
            wait.Until(wd => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
            ScenarioEventSourceProvider.EventLog.PageReadyState();
        }

        /// <summary>
        /// Activate all tabs and than return to last one
        /// </summary>
        /// <param name="remoteWebDriver"></param>
        /// <param name="count">Count of tabs to wakeup. Default: all</param>
        public static void WakeupTabs(this RemoteWebDriver remoteWebDriver, int count = 0)
        {
            var tabs = remoteWebDriver.WindowHandles;
            var lastTabId = tabs.Count - 1;
            if (count == 0 || count > tabs.Count)
            {
                count = tabs.Count;
            }
            for (var i = 0; i < count; i++)
            {
                remoteWebDriver.SwitchTab(tabs[i]);
                remoteWebDriver.Wait(3);
            }
            remoteWebDriver.SwitchTab(tabs[lastTabId]);
            remoteWebDriver.SwitchTo().DefaultContent();
        }

        /// <summary>
        /// For Edge only - Checks to see if the Edge browser and MicrosoftWebDriver.exe support the Edge specific 'NewTab' command.
        /// </summary>
        /// <param name="remoteWebDriver"></param>
        /// <returns>True if the current Edge browser and MicrosoftWebDriver support the new</returns>
        private static bool IsNewTabCommandSupported(this RemoteWebDriver remoteWebDriver)
        {
            bool isNewTabCommandSupported = false;

            if (_browser.Equals("edge"))
            {
                if (_edgeBrowserBuildNumber > 16203 && _edgeWebDriverBuildNumber > 16203)
                {
                    isNewTabCommandSupported = true;
                }
            }

            return isNewTabCommandSupported;
        }

        /// <summary>
        /// For Edge only - executes the webdriver tab command which opens a new tab without using the javascript window.Open() command
        /// </summary>
        /// <param name="remoteWebDriver"></param>
        /// <returns></returns>
        private static async Task CallEdgeNewTabCommand(this RemoteWebDriver remoteWebDriver)
        {
            string response = "";
            HttpContent content = new StringContent("");
            HttpResponseMessage newTabResponse = null;

            HttpClient client = new HttpClient();
            newTabResponse = await client.PostAsync($"http://{_hostName}:{_port}/session/{remoteWebDriver.SessionId}/ms/tab", content);
            response = await newTabResponse.Content.ReadAsStringAsync();

            if (response == "Unknown command received")
            {
                throw new Exception("New Tab command functionality is not implemented!!!");
            }
        }

        // Retrieves the WebDriver server version
        private static string GetEdgeWebDriverVersion(this RemoteWebDriver remoteWebDriver)
        {
            var statusResponse = Newtonsoft.Json.Linq.JObject.Parse(CallStatusCommand(remoteWebDriver).Result);
            string webDriverVersion = (string)statusResponse["value"]["build"]["version"];

            return webDriverVersion;
        }

        // Calls the WebDriver status command
        private static async Task<string> CallStatusCommand(this RemoteWebDriver remoteWebDriver)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage statusResponse = await client.GetAsync($"http://{_hostName}:{_port}/status");
            string response = await statusResponse.Content.ReadAsStringAsync();

            return response;
        }

        // Returns path to temporary user profile
        private static string CreateTmpProfile(string browser)
        {
            string cwd = Directory.GetCurrentDirectory(); 
            string storedProfile = CreateStoredProfile(browser);
            string tmpProfile = Path.Combine(cwd, browser + UserDataTmp);

            int tries = 0;
            while (true)
            {
                try
                {
                    if (Directory.Exists(tmpProfile))
                    {
                        DeleteDirectory(tmpProfile);
                    }
                    Directory.CreateDirectory(tmpProfile);

                    CopyDirectory(storedProfile, tmpProfile);

                    return tmpProfile;
                }
                catch (Exception ex)
                {
                    if (tries > 3)
                    {
                        throw ex;
                    }
                    Console.WriteLine(ex.Message + "\nRetry...");
                    Thread.Sleep(1000);
                    tries++;
                }
            }
        }

        /// <summary>
        /// Stores browser's User Data to stored folder from current user's system profile.
        /// Returns path to stored user profile.
        /// </summary>
        private static string CreateStoredProfile(string browser)
        {
            string cwd = Directory.GetCurrentDirectory();
            string userDataStoredPath = Path.Combine(cwd, browser + UserDataStored);

            if (Directory.Exists(userDataStoredPath))
            {
                return userDataStoredPath;
            }
            
            Console.WriteLine($"Create Stored Profile from '{CurrentUserProfile[browser]}' to '{userDataStoredPath}'");
            CopyDirectory(CurrentUserProfile[browser], userDataStoredPath);

            return userDataStoredPath;
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAllDirectory(diSource, diTarget);
        }

        private static void CopyAllDirectory(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAllDirectory(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static void DeleteDirectory(string target)
        {
            string[] files = Directory.GetFiles(target);
            string[] dirs = Directory.GetDirectories(target);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target, true);
        }

        public static void FlushChromeDriverLogs(RemoteWebDriver driver)
        {
            var trash = driver.Manage().Logs.GetLog("performance").ToList();
        }

        public static async Task<List<LogEntry>> GetChromeDriverLogs(RemoteWebDriver driver, CancellationTokenSource ctx)
        {
            List<LogEntry> logs = new List<LogEntry>();
            while (!ctx.IsCancellationRequested)
            {
                logs.AddRange(driver.Manage().Logs.GetLog("performance").ToList());                    
                await Task.Delay(300);                    
            }
            return logs;
        }

        public static void DumpChromeDriverLogs(string traceFile, List<LogEntry> logs)
        {
            if (File.Exists(traceFile))
            {
                File.Delete(traceFile);
            }
            StreamWriter sw = File.AppendText(traceFile);
            string data = GetDataFromWebDriverLogMessage(logs);
            //Console.WriteLine(data);
            sw.WriteLine(data);
            sw.Close();
        }

        private static string GetDataFromWebDriverLogMessage(List<LogEntry> logStrings)
        {
            List<Newtonsoft.Json.Linq.JObject> batch = new List<Newtonsoft.Json.Linq.JObject>();

            foreach (LogEntry log in logStrings)
            {                
                var param = GetParamsFromWebDriverLogMessage(log.Message);
                batch.Add(param);
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(batch);
        }

        private static Newtonsoft.Json.Linq.JObject GetParamsFromWebDriverLogMessage(string logString)
        {
            var logObj = Newtonsoft.Json.JsonConvert.DeserializeObject<WebDriverLogString>(logString);
            return logObj.Message.Params;
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            MOUSEEVENTF_WHEEL = 0x0800,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        public static void LeftClick(this RemoteWebDriver remoteWebDriver, int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
        }

        public static void CursorPos(this RemoteWebDriver remoteWebDriver, int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void MouseWheelScrollDown(this RemoteWebDriver remoteWebDriver, int x, int y, int amount)
        {
            SetCursorPos(x, y);
            mouse_event((int)(MouseEventFlags.MOUSEEVENTF_WHEEL), 0, 0, -amount, 0);
        }

        public static void MouseWheelScrollUp(this RemoteWebDriver remoteWebDriver, int x, int y, int amount)
        {
            SetCursorPos(x, y);
            mouse_event((int)(MouseEventFlags.MOUSEEVENTF_WHEEL), 0, 0, amount, 0);
        }
    }
}
