using BiliDownload.Components;
using BiliDownload.Dialogs;
using BiliDownload.Helper;
using BiliDownload.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 用于显示下载内容的下载页。
    /// </summary>
    public sealed partial class DownloadPage : Page
    {
        public ObservableCollection<IBiliDownload> activeDownloadList;
        public ObservableCollection<CompletedDownloadModel> completedDownloadList;
        static bool IsResumed { get; set; }
        public static DownloadPage Current { private set; get; }
        public DownloadPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            if (Current == null) Current = this;//添加静态引用

            if (activeDownloadList == null) activeDownloadList = new ObservableCollection<IBiliDownload>();
            downloadList.ItemsSource = activeDownloadList;
            if (completedDownloadList == null) completedDownloadList = new ObservableCollection<CompletedDownloadModel>();
            completedList.ItemsSource = completedDownloadList;

            if (ApplicationData.Current.LocalSettings.Values["ignoreEx"] == null)
                ApplicationData.Current.LocalSettings.Values["ignoreEx"] = false;

            IsResumed = false;//未恢复未完成的任务
            this.downloadList.Loaded += DownloadList_Loaded;
        }

        private async void DownloadList_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsResumed)
            {
                IsResumed = true;
                CheckComplete();//检查是否有已完成的任务，有就添加到完成列表
                await CheckDownloadAsync();//检查是否有未完成的任务，有就重建
            }
        }

        private async Task CheckDownloadAsync()//检查一下未完成的任务
        {
            if (DownloadXmlHelper.CheckXml() == false) return;//不存在xml，直接返回

            await RecreateDownloadAsync();

            var tasks = new List<Task>();
            foreach (var download in activeDownloadList)
            {
                tasks.Add(download.RestartAsync());
            }
            await Task.WhenAll(tasks);
        }
        private async Task RecreateDownloadAsync()//有未完成的任务，重新创建
        {
            var xmlList = DownloadXmlHelper.GetCurrentDownloads();

            if (xmlList.Count < 1) return;

            foreach (var xml in xmlList)
            {
                try
                {
                    var download = await BiliDashDownload
                        .RecreateAsync(xml, ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string);
                    this.activeDownloadList.Add(download);
                }
                catch (Exception ex)
                {
                    var dialog = new ExceptionDialog(ex.Message, XamlRoot);
                    await dialog.ShowAsync();
                }
            }
        }
        private void CheckComplete()//检查一下已完成的任务
        {
            if (CompleteXmlHelper.CheckXml() == false) return;
            RecreateComplete();
        }
        private void RecreateComplete()
        {
            var xmlList = CompleteXmlHelper.GetCompleteDownloads();
            foreach (var xml in xmlList)
            {
                var model = new CompletedDownloadModel()
                {
                    Title = xml.Title,
                    Size = xml.Size.ToString(),
                    Path = xml.Path
                };
                this.completedDownloadList.Add(model);
            }
        }

        public void PauseAll()
        {
            var list = this.activeDownloadList;
            foreach (var download in list)
            {
                download.PauseOrResume();
            }
        }
        private void pauseOrResumeBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var download = btn?.DataContext as IBiliDownload;
            if (download is null) return;
            download.PauseOrResume();
            if (download.IsPaused) btn.Content = char.ConvertFromUtf32(0xE102);
            else btn.Content = char.ConvertFromUtf32(0xE103);
        }
        private void pauseOrResumeBtn_Loaded(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var download = btn?.DataContext as IBiliDownload;
            if (download is null) return;
            if (download.IsPaused) btn.Content = char.ConvertFromUtf32(0xE102);
            else btn.Content = char.ConvertFromUtf32(0xE103);
        }

        private async void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "提示",
                PrimaryButtonText = "确认",
                SecondaryButtonText = "取消",
                Content = new TextBlock()
                {
                    Text = "是否取消下载？",
                    FontFamily = new FontFamily("Microsoft Yahei UI"),
                    FontSize = 20
                },
                XamlRoot = XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary) return;
            var download = (sender as Button)?.DataContext as IBiliDownload;
            if (!download.IsCompleted)
                await download.CancelAsync();
        }

        private async void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(ApplicationData.Current.LocalSettings.Values["downloadPath"] is string path))
            {
                var dialog = new ErrorDialog("未设置下载储存文件夹，请前往设置以更改", XamlRoot)
                {
                    PrimaryButtonText = null
                };
                await dialog.ShowAsync();
                return;
            }
            await Launcher.LaunchFolderPathAsync(path);
        }

        private void removeCompleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var model = (sender as HyperlinkButton).DataContext as CompletedDownloadModel;
            this.completedDownloadList.Remove(model);
        }

        private void clearAllCompleteBtn_Click(object sender, RoutedEventArgs e)
        {
            this.completedDownloadList.Clear();
        }
    }
    public class CompletedDownloadModel
    {
        public string Title { get; set; }
        private ulong size;

        public string Size
        {
            get => $"大小 {size / 1000000}Mb";
            set { size = ulong.Parse(value); }
        }
        public ulong SizeNative { get => size; }

        public string Path { get; set; }
    }
}
