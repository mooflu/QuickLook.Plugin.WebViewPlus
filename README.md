![QuickLook icon](https://user-images.githubusercontent.com/1687847/29485863-8cd61b7c-84e2-11e7-97d5-eacc2ba10d28.png)

# QuickLook.Plugin.WebViewPlus

[QuickLook](https://github.com/QL-Win/QuickLook) plugin for previewing various file types with [WebViewPlus](https://github.com/mooflu/WebViewPlus).

## Try out

1. Go to [Release page](https://github.com/mooflu/QuickLook.Plugin.WebViewPlus/releases) and download the latest version.
2. Make sure that you have QuickLook running in the background. Go to your Download folder, and press <key>Spacebar</key> on the downloaded `.qlplugin` file.
3. Click the “Install” button in the popup window.
4. Restart QuickLook.
5. To configure which file types to preview via WebViewPlus, open an html file and click the gears button on the bottom right.

## Experimental
This plugin currently uses experimental MS Edge APIs and requires a dev or beta build of WebView2.
Install MS Edge from one of the insider channels: https://www.microsoftedgeinsider.com/en-us/download
and then set a system environment variable so indicate which should be used.
E.g. WEBVIEW2_RELEASE_CHANNEL_PREFERENCE = 1
https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/set-preview-channel#using-an-environment-variable


## Development

 1. Clone this project. Do not forget to update submodules.
 2. Build project with `Release` profile.
 3. Run `Scripts\pack-zip.ps1`.
 4. You should find a file named `QuickLook.Plugin.WebViewPlus.qlplugin` in the project directory.

## License

MIT License.
