using BiliDownload.Helper;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace BiliDownload
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // 订阅退出事件
                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += async (s, args) =>
                {
                    var deferral = args.GetDeferral();
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
                            SecondaryButtonText = "取消"
                        };
                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Secondary) args.Handled = true;
                        else
                        {
                            DownloadPage.Current.PauseAll();
                            DownloadXmlHelper.StoreCurrentDownloads();//退出时就储存未完成的任务
                        }
                    }
                    if (VideoHelper.Locked)
                    {
                        var dialog = new ContentDialog()
                        {
                            Title = "正在等待转换完成",
                            Content = new ProgressRing()
                            {
                                IsActive = true
                            }
                        };
                        await dialog.ShowAsync();
                        while (VideoHelper.Locked) await Task.Delay(1000);
                    }
                    CompleteXmlHelper.StoreCompletedDownloads();//储存已完成任务列表
                    deferral.Complete();
                };
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }
        /// <summary>
        /// 删除所有缓存文件
        /// </summary>
        /// <returns></returns>
        public static async Task CleanCacheAsync()
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var files = (await folder.GetFilesAsync()).ToList();
            foreach (var file in files)
            {
                await file.DeleteAsync();
            }
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new System.Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }
    }
}
