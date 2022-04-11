using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.Dialogs
{
    public sealed partial class ErrorDialog : ContentDialog
    {
        public ErrorDialog(string message, XamlRoot xamlRoot)
        {
            this.InitializeComponent();
            this.messageTextBlock.Text = message;
            XamlRoot = xamlRoot;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
