using BiliDownload.HelperPage;
using BiliDownload.LoginDialogs;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserLoginPage : Page
    {
        public AppWindow LoginWindow { get; private set; }
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
            var dialog = new QRcodeLoginDialog();
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.LoginOk();
            }
        }
        #region 下面是密码登录用的
        private async void pwdLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            AppWindow loginWindow = await AppWindow.TryCreateAsync();
            this.LoginWindow = loginWindow;
            loginWindow.RequestSize(new Size(600, 800));
            Frame loginFrame = new Frame() { Height = 800, Width = 600 };
            loginFrame.Navigate(typeof(PwdLoginPage));
            ElementCompositionPreview.SetAppWindowContent(loginWindow, loginFrame);
            await loginWindow.TryShowAsync();
        }
        public async Task PwdLoginOk()
        {
            if (this.LoginWindow != null)
            {
                await this.LoginWindow.CloseAsync();
                this.LoginWindow = null;
                this.LoginOk();
            }
        }
        public async Task PwdLoginCancel()
        {
            if (this.LoginWindow != null)
            {
                await this.LoginWindow.CloseAsync();
                this.LoginWindow = null;
            }
        }
        #endregion

        private async void sESSDATALoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SESSDATALoginDialog();
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) this.LoginOk();
        }
        #endregion

    }
}
