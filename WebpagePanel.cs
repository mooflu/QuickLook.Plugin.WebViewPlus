// Copyright © 2022 Frank Becker

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using QuickLook.Common.Helpers;

// Note: see webview2 sample app:
// https://github.com/MicrosoftEdge/WebView2Samples/blob/main/SampleApps/WebView2WpfBrowser/MainWindow.xaml.cs

public class FileData
{
    public string fileName { get; set; }
    public long fileSize { get; set; }
    public bool isBinary { get; set; }
    public string textContent { get; set; }
}

public class CommandMessage
{
    public string command { get; set; }
    public string[] data { get; set; }
}

namespace QuickLook.Plugin.WebViewPlus
{
    public class WebpagePanel : UserControl
    {
        public static readonly string DefaultExtensions =
            "html,htm,mht,mhtml,pdf,csv,xlsx,svg,md,markdown,gltf,glb,c++,h++,bat,c,cmake,cpp,cs,css,go,h,hpp,java,js,json,jsx,lua,perl,pl,ps1,psm1,py,rb,sass,scss,sh,sql,tex,ts,tsx,txt,webp,yaml,yml";
        public string[] Extensions = { };

        private static readonly string[] _binExtensions = "pdf,xlsx,xls,ods,gltf,glb,fbx,obj,webp,jpg,jpeg,png,apng,gif,bmp".Split(',');
        private Uri _currentUri;
        private WebView2 _webView;
        private bool _webAppReady = false;
        private CoreWebView2SharedBuffer _sharedBuffer = null;
        private FileInfo _activeFileInfo = null;
        private CoreWebView2Environment _webViewEnvironment;

        public WebpagePanel()
        {
            Extensions = SettingHelper.Get("ExtensionList", WebpagePanel.DefaultExtensions, "QuickLook.Plugin.WebViewPlus").Split(',');

            var unavailReason = WebpagePanel.WebView2UnavailableReason();
            if (unavailReason != null)
            {
                Content = CreateDownloadButton(unavailReason);
            }
            else
            {
                _webView = new WebView2
                {
                    CreationProperties = new CoreWebView2CreationProperties
                    {
                        // Note: using a different user data folder than the built-in HTmlViewer plugin
                        // This plugin starts the webview once and keeps it running and would effectively
                        // block the HTmlViewer if it used the same folder.
                        // See "Process model for WebView2 apps"
                        UserDataFolder = Path.Combine(SettingHelper.LocalDataPath, @"WebViewPlus_Data\\"),
                        Language = CultureInfo.CurrentUICulture.Name
                    }
                };
                _webView.NavigationStarting += NavigationStarting_CancelNavigation;
                _webView.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
                _webView.EnsureCoreWebView2Async();
                Content = _webView;
            }
        }

        public static string  WebView2UnavailableReason()
        {
            try
            {
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                // verion format example: "110.0.1556.0 dev"

                // This plugin uses an experimental feature introduced in 1466
                // https://learn.microsoft.com/en-us/microsoft-edge/webview2/release-notes?tabs=dotnetcsharp#101466-prerelease
                var buildNum = Int32.Parse(version.Split('.')[2]);
                if (buildNum < 1466)
                {
                    var reason = $"QuickLook.Plugin.WebViewPlus found incompatible webview2: {version} - 1466 or higher needed";
                    return reason;
                }

                // TODO: validate api availability.

                return null;
            }
            catch (Exception)
            {
                return "Viewing this file requires Microsoft Edge WebView2 (build 1466 or higher) to be installed.";
            }
        }

        CoreWebView2Environment WebViewEnvironment
        {
            get
            {
                if (_webViewEnvironment == null && _webView?.CoreWebView2 != null)
                {
                    _webViewEnvironment = _webView.CoreWebView2.Environment;
                }
                return _webViewEnvironment;
            }
        }

        public void NavigateToFile(string path)
        {
            _activeFileInfo = new FileInfo(path);
            if (_webAppReady)
            {
                sendFileData();
            }
        }

        void sendFileData()
        {
            if (_sharedBuffer != null)
            {
                _sharedBuffer.Dispose();
                _sharedBuffer = null;
            }
            var extension = _activeFileInfo.Extension.ToLower().Substring(1);
            var isBinary = _binExtensions.Any(extension.Equals);
            var textContent = "";
            if (isBinary)
            {
                _sharedBuffer = WebViewEnvironment.CreateSharedBuffer((ulong)_activeFileInfo.Length);
                using (Stream stream = _sharedBuffer.OpenStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(File.ReadAllBytes(_activeFileInfo.FullName));
                    }
                }
            }
            else
            {
                _sharedBuffer = WebViewEnvironment.CreateSharedBuffer(1);
                textContent = File.ReadAllText(_activeFileInfo.FullName);
            }
            var json = JsonConvert.SerializeObject(
                new FileData
                {
                    fileName = _activeFileInfo.Name,
                    fileSize = _activeFileInfo.Length,
                    isBinary = isBinary,
                    textContent = textContent
                }
            );

            _webView.CoreWebView2.PostSharedBufferToScript(_sharedBuffer, CoreWebView2SharedBufferAccess.ReadOnly, json);
        }

        void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var msg = JsonConvert.DeserializeObject<CommandMessage>(args.WebMessageAsJson);
            switch(msg.command)
            {
                case "AppReadyForData":
                    _webAppReady = true;
                    // If the user profile was previously generated (WebView2_Data), it doesn't seem to change the navigator.language setting
                    // when a webview is created with a different language in 'new WebView2' above.
                    // You'd have to delete the WebView2_Data folder first.
                    // Oh, well. Explicitly sending current language to web app, instead.
                    _webView.CoreWebView2.PostWebMessageAsString($"setLanguage:{CultureInfo.CurrentUICulture.Name}");
                    sendFileData();
                    break;

                case "Extensions":
                    SettingHelper.Set("ExtensionList", String.Join(",", msg.data), "QuickLook.Plugin.WebViewPlus");
                    Extensions = SettingHelper.Get("ExtensionList", WebpagePanel.DefaultExtensions, "QuickLook.Plugin.WebViewPlus").Split(',');
                    break;

                default:
                    break;
            }
        }

        private void CoreWebView2InitializationCompleted(object sender, EventArgs e)
        {
            _webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
            _webView.CoreWebView2.NewWindowRequested += NewWindowRequested;
            _webView.CoreWebView2.FrameNavigationStarting += FrameNavigationStarting;
            _webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            _webView.CoreWebView2.Profile.ClearBrowsingDataAsync(
                CoreWebView2BrowsingDataKinds.DiskCache |
                CoreWebView2BrowsingDataKinds.BrowsingHistory |
                CoreWebView2BrowsingDataKinds.CacheStorage |
                CoreWebView2BrowsingDataKinds.Cookies |
                CoreWebView2BrowsingDataKinds.DownloadHistory
            );

            // 3 places to get web app:
            // 1. a url via WebAppUrl (e.g. locally hosted vite dev version of app)
            // 2. in SettingHelper.LocalDataPath (same place as config files)
            // 3. bundled with plugin
            var webAppInConfigFolder = Path.Combine(SettingHelper.LocalDataPath, "webviewplus");
            var webAppInBundledFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "webviewplus");
            var uri = new Uri("https://webviewplus.mooflu.com/index.html");
            var webAppUrl = SettingHelper.Get<string>("WebAppUrl", null, "QuickLook.Plugin.WebViewPlus");
            if (webAppUrl != null)
            {
                ProcessHelper.WriteLog($"QuickLook.Plugin.WebViewPlus using app via custom url: {webAppUrl}");
                uri = new Uri(webAppUrl);
            }
            else
            {
                // map webviewplus folder to a private virtual hostname
                if (File.Exists(Path.Combine(webAppInConfigFolder, "index.html")))
                {
                    ProcessHelper.WriteLog("QuickLook.Plugin.WebViewPlus using app from config");
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("webviewplus.mooflu.com", webAppInConfigFolder, CoreWebView2HostResourceAccessKind.Allow);
                }
                else if(File.Exists(Path.Combine(webAppInBundledFolder, "index.html")))
                {
                    ProcessHelper.WriteLog("QuickLook.Plugin.WebViewPlus using app from plugin");
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("webviewplus.mooflu.com", webAppInBundledFolder, CoreWebView2HostResourceAccessKind.Allow);
                }
            }
            _webView.Source = uri;
            _currentUri = _webView.Source;
        }
        private void NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Prevent new windows being opened (e.g. ctrl-click link)
            e.Handled = true;
            if (_webAppReady)
            {
                _webView.CoreWebView2.PostWebMessageAsString("newWindowRejected");
            }
        }
        private void NavigationStarting_CancelNavigation(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var newUri = new Uri(e.Uri);
            if (newUri != _currentUri) e.Cancel = true;
        }

        private void FrameNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!e.Uri.StartsWith("blob:") && !e.Uri.StartsWith("about:"))
            {
                e.Cancel = true;
                if (_webAppReady)
                {
                    _webView.CoreWebView2.PostWebMessageAsString("frameNavigationRejected");
                }
            }
        }

        public void UnloadData()
        {
            _activeFileInfo = null;
            _sharedBuffer?.Dispose();
            _sharedBuffer = null;
            if (_webAppReady)
            {
                _webView.CoreWebView2.PostWebMessageAsString("unload");
            }
        }

        public void Dispose() // Not used - unloading only; see above
        {
            _activeFileInfo = null;
            _sharedBuffer?.Dispose();
            _sharedBuffer = null;
            _webView?.Dispose();
            _webView = null;
        }

        private object CreateDownloadButton(string reason)
        {
            var button = new Button
            {
                Content = reason,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20, 6, 20, 6)
            };
            button.Click += (sender, e) => Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");

            return button;
        }
    }
}
