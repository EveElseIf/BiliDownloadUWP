using BiliDownload.Dialogs;
using BiliDownload.Helper;
using BiliDownload.HelperPages;
using BiliDownload.Helpers;
using BiliDownload.Models;
using BiliDownload.Others;
using BiliDownload.SearchDialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        public static SearchPage Current { private set; get; }
        private Dictionary<BiliMangaMaster, AppWindow> mangaWindowDic = new Dictionary<BiliMangaMaster, AppWindow>();
        public SearchPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            if (Current == null) Current = this;
            this.searchTextbox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter) this.searchBtn_Click(this, new RoutedEventArgs());
            };
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)//首次打开给小提示
        {
            base.OnNavigatedTo(e);
            if (ApplicationData.Current.LocalSettings.Values["searchPageFirstOpen"] == null)
            {
                var dialog = new ContentDialog()
                {
                    Title = "提示",
                    Content = new TextBlock()
                    {
                        FontFamily = new FontFamily("Microsoft Yahei UI"),
                        FontSize = 20,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        Text = "搜索视频时，可以看到所有支持的清晰度，但是您的权限不足时，程序会自动为您选择可用的较高清晰度"
                    },
                    PrimaryButtonText = "明白了"
                };
                await dialog.ShowAsync();
                ApplicationData.Current.LocalSettings.Values["searchPageFirstOpen"] = true;
            }
        }

        public async void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTextbox.Text)) return;

            var sESSDATA = ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string;

            searchBtn.IsEnabled = false;
            searchProgressRing.IsActive = true;
            searchProgressRing.Visibility = Visibility.Visible;

            (string, long, UrlType, int) info = (null, 0, 0, 0);
            try
            {
                info = await AnalyzeVideoUrlAsync(searchTextbox.Text, sESSDATA); //分析输入的url，提取bv或者av，是否指定分p
            }
            catch (NullReferenceException)
            {
                Reset();
                var dialog = new ExceptionDialog("未找到视频");
                await dialog.ShowAsync();
                return;
            }
            catch (Exception ex)
            {
                Reset();
                var dialog = new ExceptionDialog(ex.Message);
                await dialog.ShowAsync();
                return;
            }

            if (info.Item3 == UrlType.BangumiEP)//下载ep番剧
            {
                var bangumi = await BiliVideoHelper.GetBangumiInfoAsync(info.Item4, 0, sESSDATA);
                var dialog = await BangumiDialog.CreateAsync(bangumi);
                Reset();
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary) return;
            }
            else if (info.Item3 == UrlType.BangumiSS)//下载ss番剧
            {
                var bangumi = await BiliVideoHelper.GetBangumiInfoAsync(info.Item4, 1, sESSDATA);
                var dialog = await BangumiDialog.CreateAsync(bangumi);
                Reset();
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary) return;
            }
            //else if (info.Item3 == UrlType.SingelVideo) //指定了分p的时候，废弃这种用法
            //{
            //    var dialog = await SingleVideoDialog.CreateAsync
            //        (await BiliVideoHelper.GetSingleVideoInfoAsync(info.Item1, info.Item2, 64, sESSDATA));
            //    Reset();
            //    var result = await dialog.ShowAsync();
            //    if (result == ContentDialogResult.Secondary) return;
            //}
            else if (info.Item3 == UrlType.MasteredVideo) //下载普通视频集合
            {
                var master = await BiliVideoHelper.GetVideoMasterInfoAsync(info.Item1, sESSDATA);
                var dialog = await MasteredVideoDialog.CreateAsync(master);
                Reset();
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary) return;
            }
            else if (info.Item3 == UrlType.MangaMC) //下载漫画
            {
                if (Settings.DownloadPath == null)//检查下载目录是否为空
                {
                    Reset();
                    var dialog = new ErrorDialog("未设置下载储存文件夹，请前往设置以更改")
                    {
                        PrimaryButtonText = "前往设置"
                    };
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Secondary) return;
                    else
                    {
                        MainPage.ContentFrame.Navigate(typeof(SettingPage));
                        MainPage.NavView.SelectedItem = MainPage.NavView.SettingsItem;
                        return;
                    }
                }
                try
                {
                    await StorageFolder.GetFolderFromPathAsync(Settings.DownloadPath);
                }
                catch (FileNotFoundException)
                {
                    Reset();
                    var dialog = new ExceptionDialog("找不到指定下载目录:" + Settings.DownloadPath);
                    await dialog.ShowAsync();
                    return;
                }
                var master = await BiliMangaHelper.GetBiliMangaMasterAsync(info.Item4, sESSDATA);
                Reset();
                AppWindow mangaWindow = await AppWindow.TryCreateAsync();
                mangaWindow.RequestSize(new Size(1024, 768));
                Frame mangaFrame = new Frame();
                mangaFrame.Navigate(typeof(MangaDownloadPage), master);
                ElementCompositionPreview.SetAppWindowContent(mangaWindow, mangaFrame);
                this.mangaWindowDic.Add(master, mangaWindow);
                await mangaWindow.TryShowAsync();
            }
            else
            {
                Reset();
                var dialog = new ContentDialog()
                {
                    Content = "解析失败，没有找到合适的下载方法"
                };
                await dialog.ShowAsync();
            }
        }
        public async Task CloseMangaWindow(BiliMangaMaster master)
        {
            await this.mangaWindowDic[master].CloseAsync();
        }
        public void Reset()
        {
            this.searchBtn.IsEnabled = true;
            this.searchProgressRing.IsActive = false;
            this.searchProgressRing.Visibility = Visibility.Collapsed;
        }
        private enum UrlType
        {
            //SingelVideo,
            MasteredVideo,
            BangumiEP,
            BangumiSS,
            MangaMC
        }
        //bv     cid  类型    ep或ss或mc
        private async Task<(string, long, UrlType, int)> AnalyzeVideoUrlAsync(string url, string sESSDATA)
        //分析输入的url，提取bv
        {
            BiliVideoMaster master;
            long cid = 0;
            int p = 0;
            string bv;
            long av;
            int ep = 0;
            int ss = 0;
            int mc = 0;
            UrlType type = UrlType.MasteredVideo;

            //if (Regex.IsMatch(searchTextbox.Text, "\\?p=[0-9]*"))//判断分p
            //{
            //    p = int.Parse(Regex.Match(searchTextbox.Text, "p=[0-9]*").Value.Remove(0, 2));
            //    type = UrlType.SingelVideo;
            //};

            if (Regex.IsMatch(url, "[B|b][V|v][a-z|A-Z|0-9]*"))//判断bv
            {
                bv = Regex.Match(url, "[B|b][V|v][a-z|A-Z|0-9]*").Value;
                master = await BiliVideoHelper.GetVideoMasterInfoAsync(bv, sESSDATA);
            }
            else if (Regex.IsMatch(url, "[a|A][v|V][0-9]*"))//判断av
            {
                av = long.Parse(Regex.Match(url, "[a|A][v|V][0-9]*").Value.Remove(0, 2));
                master = await BiliVideoHelper.GetVideoMasterInfoAsync(av, sESSDATA);
            }
            else if (Regex.IsMatch(url, "[e|E][p|P][0-9]*"))//判断ep
            {
                p = 0;
                master = null;
                type = UrlType.BangumiEP;
                ep = int.Parse(Regex.Match(url, "[e|E][p|P][0-9]*").Value.Remove(0, 2));
            }
            else if (Regex.IsMatch(url, "[s|S][s|S][0-9]*"))//判断ss
            {
                p = 0;
                master = null;
                type = UrlType.BangumiSS;
                ss = int.Parse(Regex.Match(url, "[s|S][s|S][0-9]*").Value.Remove(0, 2));
            }
            else if (Regex.IsMatch(url, "[m|M][c|C][0-9]*"))
            {
                p = 0;
                master = null;
                type = UrlType.MangaMC;
                mc = int.Parse(Regex.Match(url, "[m|M][c|C][0-9]*").Value.Remove(0, 2));
            }
            else
            {
                throw new ArgumentException("Url不合法");
            }
            if (p != 0)
            {
                cid = (long)(master.VideoList.Where(v => v.P == p)?.FirstOrDefault().Cid);
            }
            if (type == UrlType.MangaMC) return (null, 0, type, mc);
            if (type == UrlType.BangumiEP) return (null, 0, type, ep);
            if (type == UrlType.BangumiSS) return (null, 0, type, ss);
            if (master != null)//只要是番剧，就把master赋值为空
                return (master.Bv, cid, type, 0);
            else throw new Exception();
        }
    }
}
