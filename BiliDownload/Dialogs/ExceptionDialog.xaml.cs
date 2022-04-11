using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.Dialogs
{
    public sealed partial class ExceptionDialog : ContentDialog
    {
        public ExceptionDialog(string Msg, XamlRoot xamlRoot)
        {
            this.InitializeComponent();
            this.PrimaryButtonText = "确定";
            this.msgTextBlock.Text = Msg;
            this.XamlRoot = xamlRoot;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
