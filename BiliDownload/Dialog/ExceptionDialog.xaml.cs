using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace BiliDownload.Dialog
{
    public sealed partial class ExceptionDialog : ContentDialog
    {
        public ExceptionDialog(string Msg)
        {
            this.InitializeComponent();
            this.PrimaryButtonText = "确定";
            this.msgTextBlock.Text = Msg;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
