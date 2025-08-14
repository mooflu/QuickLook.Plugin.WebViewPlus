// Copyright © 2022 Frank Becker

using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.WebViewPlus
{
    public class Plugin : IViewer
    {
        private WebpagePanel _panel;

        public int Priority => 1;
        private static double _width = SettingHelper.Get("WindowWidth", 1000, "QuickLook.Plugin.WebViewPlus");
        private static double _height = SettingHelper.Get("WindowHeight", 1200, "QuickLook.Plugin.WebViewPlus");

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            var extension = Path.GetExtension(path).ToLower().Substring(1);
            return !Directory.Exists(path) && WebpagePanel.Extensions.Any(extension.Equals);
        }

        public void Prepare(string path, ContextObject context)
        {
            var desiredSize = new Size(_width, _height);
            context.SetPreferredSizeFit(desiredSize, 0.9);
        }

        public void View(string path, ContextObject context)
        {
            _panel = new WebpagePanel();
            context.ViewerContent = _panel;
            context.Title = Path.IsPathRooted(path) ? Path.GetFileName(path) : path;

            _panel.NavigateToFile(path);
            _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
        }

        public void Cleanup()
        {
            _width = _panel.ActualWidth;
            _height = _panel.ActualHeight;

            SettingHelper.Set("WindowWidth", _width, "QuickLook.Plugin.WebViewPlus");
            SettingHelper.Set("WindowHeight", _height, "QuickLook.Plugin.WebViewPlus");

            _panel?.Dispose();
            _panel = null;
        }
    }
}
