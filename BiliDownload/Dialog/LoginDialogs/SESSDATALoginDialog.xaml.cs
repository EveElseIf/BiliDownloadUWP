using BiliDownload.Helper;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.LoginDialogs
{
    public sealed partial class SESSDATALoginDialog : ContentDialog
    {
        bool needToClose = false;
        string sESSDATA;
        long cid;
        public SESSDATALoginDialog()
        {
            this.InitializeComponent();
            this.IsPrimaryButtonEnabled = false;
            this.Closing += (s, e) =>
            {
                if (!needToClose) e.Cancel = true;
            };
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ComfirmLogin();
            this.needToClose = true;
        }
        private void ComfirmLogin()
        {
            ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] = sESSDATA;
            ApplicationData.Current.LocalSettings.Values["biliUserUid"] = cid;
            ApplicationData.Current.LocalSettings.Values["isLogined"] = true;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.needToClose = true;
        }

        private async void verifyBtn_Click(object sender, RoutedEventArgs e)
        {
            var sESSDATA = this.sESSDATATextBox.Text;
            if (string.IsNullOrWhiteSpace(sESSDATA)) return;

            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Content = new ProgressRing()
            {
                IsActive = true
            };

            var result = await BiliUserHelper.VerifySessDataAsync(sESSDATA);
            if (result.Item1 == false)
            {
                btn.IsEnabled = true;
                btn.Content = "失败";
                await Task.Delay(2000);
                if (btn.Content as string == "失败") btn.Content = "校验";
                return;
            }
            else
            {
                btn.Content = "成功";
                this.sESSDATA = result.Item2;
                this.cid = result.Item3;
                this.IsPrimaryButtonEnabled = true;
            }
        }
    }
}
