using BiliDownload.Helper;
using BiliDownload.HelperPage;
using BiliDownload.LoginDialogs;
using BiliDownload.Model;
using BiliDownload.Model.Json;
using BiliDownload.Pages;
using BiliDownload.SearchDialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

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
