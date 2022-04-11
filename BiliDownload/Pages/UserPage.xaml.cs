using BiliDownload.Pages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage;
using Windows.UI.WindowManagement;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserPage : Page
    {
        private bool loginStatus;
        public static string SESSDATA { get => ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string; }
        public static long Uid { get => (long)ApplicationData.Current.LocalSettings.Values["biliUserUid"]; }
        public AppWindow LoginWindow { get; private set; }
        public static UserPage Current { get; private set; }
        public UserPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            if (Current == null) Current = this;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ApplicationData.Current.LocalSettings.Values["isLogined"] == null)
                ApplicationData.Current.LocalSettings.Values["isLogined"] = false;
            if ((bool)ApplicationData.Current.LocalSettings.Values["isLogined"]) loginStatus = true;
            else loginStatus = false;
            this.contentFrame.Navigate(loginStatus ? typeof(UserDetailPage) : typeof(UserLoginPage));
        }
    }
}
