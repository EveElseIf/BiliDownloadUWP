using BiliDownload.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.LoginDialogs
{
    public sealed partial class QRcodeLoginDialog : ContentDialog
    {
        CancellationTokenSource token;
        public QRcodeLoginDialog()
        {
            this.InitializeComponent();
            token = new CancellationTokenSource();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            token.Cancel();
            token = new CancellationTokenSource();
        }

        private async void getQRcodeBtn_Click(object sender, RoutedEventArgs e)
        {
            (sender as HyperlinkButton).Content = new ProgressRing()
            { IsActive = true, Height = 100, Width = 100 };
            var result = await BiliLoginHelper.LoginUseQRCodeAsync(QRcodeImage, token.Token);
            if (!string.IsNullOrWhiteSpace(result?[0]) && !string.IsNullOrWhiteSpace(result?[1]))
            {
                PrimaryButtonText = "确认登录";
                ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] = result[0];
                ApplicationData.Current.LocalSettings.Values["biliUserUid"] = long.Parse(result[1]);
                ApplicationData.Current.LocalSettings.Values["isLogined"] = true;
                this.IsPrimaryButtonEnabled = true;
            }
        }
    }
}