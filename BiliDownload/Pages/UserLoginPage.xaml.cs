using BiliDownload.HelperPage;
using BiliDownload.LoginDialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserLoginPage : Page
    {
        public Window LoginWindow { get; private set; }
        public static string SESSDATA { get => ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string; }
        public static long Uid { get => (long)ApplicationData.Current.LocalSettings.Values["biliUserUid"]; }
        public static UserLoginPage Current { get; private set; }

        public UserLoginPage()
        {
            this.InitializeComponent();
            Current = this;
        }
        private void LoginOk()
        {
            UserPage.Current.contentFrame.Navigate(typeof(UserDetailPage));
        }

        #region 下面都是登录用的
        private async void loginBtn_Click(object sender, RoutedEventArgs e)//二维码登录
        {
            var dialog = new QRcodeLoginDialog(XamlRoot);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.LoginOk();
            }
        }
        #region 下面是密码登录用的
        private void pwdLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new Window();
            LoginWindow = loginWindow;
            var loginFrame = new Frame();
            loginFrame.Navigate(typeof(PwdLoginPage));
            loginWindow.Content = loginFrame;
            loginWindow.Activate();
        }
        public void PwdLoginOk()
        {
            if (this.LoginWindow != null)
            {
                this.LoginWindow.Close();
                this.LoginWindow = null;
                this.LoginOk();
            }
        }
        public void PwdLoginCancel()
        {
            if (this.LoginWindow != null)
            {
                this.LoginWindow.Close();
                this.LoginWindow = null;
            }
        }
        #endregion

        private async void sESSDATALoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SESSDATALoginDialog(XamlRoot);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) this.LoginOk();
        }
        #endregion

    }
}
