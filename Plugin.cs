// Copyright © 2022 Frank Becker

using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.WebViewPlus
{
    public class Plugin : IViewer
    {
        private static WebpagePanel _panel;

        public int Priority => 1;

        public void Init()
        {
            _panel = new WebpagePanel();
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && _panel.Extensions.Any(path.ToLower().EndsWith);
        }

        public void Prepare(string path, ContextObject context)
        {
            var desiredSize = new Size(1200, 1600);
            context.SetPreferredSizeFit(desiredSize, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            context.ViewerContent = _panel;
            context.Title = Path.IsPathRooted(path) ? Path.GetFileName(path) : path;

            _panel.NavigateToFile(path);
            _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
        }

        public void Cleanup()
        {
            _panel.UnloadData();
            //_panel?.Dispose();
            //_panel = null;
        }
    }
}
