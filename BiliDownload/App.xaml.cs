using BiliDownload.Helper;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BiliDownload
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
            Window = m_window;
            Window.Closed += Window_ClosedAsync;
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values["ignoreEx"] as bool? ?? false)
                e.Handled = true;
            if (e.Exception is Exception)
                e.Handled = true;
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            var args = ToastArguments.Parse(e.Argument);
            var path = args.ToString()[18..];
            _ = Launcher.LaunchFolderPathAsync(path);
        }

        private bool canClose = false;
        private async void Window_ClosedAsync(object sender, WindowEventArgs args)
        {
            if (canClose)
            {
                return;
            }
            else
            {
                args.Handled = true;
                if (DownloadPage.Current.activeDownloadList?.Count < 1)
                    DownloadXmlHelper.DeleteCurrentDownloads();//当下载列表没任务的时候，删除xml
                if (DownloadPage.Current.activeDownloadList?.Count > 0)//当下载列表存在任务时
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "提示",
                        Content = new TextBlock()
                        {
                            Text = "有下载任务正在进行，确认退出吗？",
                            FontFamily = new FontFamily("Microsoft Yahei UI"),
                            FontSize = 20
                        },
                        PrimaryButtonText = "确定",
                        SecondaryButtonText = "取消",
                        XamlRoot = DownloadPage.Current.XamlRoot
                    };
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        DownloadPage.Current.PauseAll();
                        DownloadXmlHelper.StoreCurrentDownloads();
                        canClose = true;
                    }
                    else
                    {
                        return;
                    }
                }
                CompleteXmlHelper.StoreCompletedDownloads();//储存已完成任务列表
                canClose = true;
                Window.Close();
            }
        }

        private MainWindow m_window;
        public static MainWindow Window { get; private set; }
    }
}
