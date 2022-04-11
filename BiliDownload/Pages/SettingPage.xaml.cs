using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        public SettingPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            this.locationTextBox.Text = ApplicationData.Current.LocalSettings.Values["downloadPath"] as string ?? string.Empty;
            //if ((bool)ApplicationData.Current.LocalSettings.Values["cpuLimit"]) this.cpuLimitSwitch.IsOn = true;
            if ((bool)ApplicationData.Current.LocalSettings.Values["HEVC"]) this.hevcDownloadSwitch.IsOn = true;
            if ((bool)ApplicationData.Current.LocalSettings.Values["NeedNotice"]) this.needNoticeSwitch.IsOn = true;
            if ((bool)ApplicationData.Current.LocalSettings.Values["autoDownloadDanmaku"]) this.autoDownloadDanmakuSwitch.IsOn = true;
            if ((bool)ApplicationData.Current.LocalSettings.Values["ignoreEx"]) this.ignoreExSwitch.IsOn = true;
        }

        private async void selectLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("downloadPath", folder);//注册downloadPath到权限刘表
            ApplicationData.Current.LocalSettings.Values["downloadPath"] = folder.Path;
            locationTextBox.Text = ApplicationData.Current.LocalSettings.Values["downloadPath"] as string;
        }

        private void cpuLimitSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            //ApplicationData.Current.LocalSettings.Values["cpuLimit"] = cpuLimitSwitch.IsOn;
        }

        private void hevcDownloadSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["HEVC"] = hevcDownloadSwitch.IsOn;
        }

        private void needNoticeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["NeedNotice"] = needNoticeSwitch.IsOn;
        }

        private void autoDownloadDanmakuSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["autoDownloadDanmaku"] = autoDownloadDanmakuSwitch.IsOn;
        }

        private void ignoreExSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["ignoreEx"] = ignoreExSwitch.IsOn;
        }
    }
}
