using BiliDownload.Helper;
using BiliDownload.LoginDialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserPage : Page
    {
        bool loginStatus;
        public UserPage()
        {
            this.InitializeComponent();
            if (ApplicationData.Current.LocalSettings.Values["isLogined"] == null)
                ApplicationData.Current.LocalSettings.Values["isLogined"] = false;
            if ((bool)ApplicationData.Current.LocalSettings.Values["isLogined"]) loginStatus = true;
            else loginStatus = false;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (loginStatus)//如果登录了，就移除登录界面
            {
                contentGrid.Children.Remove(this.FindName("loginGrid") as UIElement);
                initializingProgressRing.Visibility = Visibility.Visible;
                initializingProgressRing.IsActive = true;

                await InitializeUserAsync();//初始化用户信息

                logoutBtn.Visibility = Visibility.Visible;
                avatarAndNameGrid.Visibility = Visibility.Visible;
                initializingProgressRing.Visibility = Visibility.Collapsed;
                initializingProgressRing.IsActive = false;
            }
        }
        private async Task InitializeUserAsync()
        {
            var user = await BiliUserHelper.GetCurrentUserInfoAsync();
            this.avatarImage.Source = user.AvatarImg;
            this.userNameTextBlock.Text = user.Name;
        }

        private async void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new QRcodeLoginDialog();
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.Frame.Navigate(typeof(UserPage));
            }
        }

        private void pwdLoginBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!loginStatus) return;
            ApplicationData.Current.LocalSettings.Values["isLogined"] = false;
            ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] = string.Empty;
            ApplicationData.Current.LocalSettings.Values["biliUserUid"] = 0;
            this.Frame.Navigate(typeof(UserPage));
        }

        private async void sESSDATALoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SESSDATALoginDialog();
            var result = await dialog.ShowAsync();
            if (result==ContentDialogResult.Primary)
            {
                this.Frame.Navigate(typeof(UserPage));
            }
        }
    }
}
