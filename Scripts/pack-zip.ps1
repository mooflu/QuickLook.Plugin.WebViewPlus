Remove-Item ..\QuickLook.Plugin.WebViewPlus.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path ..\bin\Release\ -Exclude *.pdb,*.xml
Compress-Archive $files ..\QuickLook.Plugin.WebViewPlus.zip
Move-Item ..\QuickLook.Plugin.WebViewPlus.zip ..\QuickLook.Plugin.WebViewPlus.qlplugin