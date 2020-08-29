using BiliDownload.Exception;
using BiliDownload.Helper;
using BiliDownload.Model;
using BiliDownload.SearchDialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace BiliDownload
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        public static SearchPage Current { private set; get; }
        public SearchPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            if (Current == null) Current = this;
        }

        public async void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            //if (!Regex.IsMatch(searchTextbox.Text, "[a-zA-z]+://[^\\s]*")) return;

            if (string.IsNullOrWhiteSpace(searchTextbox.Text)) return;
                        
            var sESSDATA = ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string;

            searchBtn.IsEnabled = false;
            searchProgressRing.IsActive = true;
            searchProgressRing.Visibility = Visibility.Visible;

            var info = await AnalyzeVideoUrlAsync(searchTextbox.Text, sESSDATA); //分析输入的url，提取bv或者av，是否指定分p

            if (info.Item3 == UrlType.BangumiEP)
            {
                var bangumi = await BiliVideoHelper.GetBangumiInfoAsync(info.Item4, 0, sESSDATA);
                var dialog = await BangumiDialog.CreateAsync(bangumi);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary)
                {
                    searchBtn.IsEnabled = true;
                    searchProgressRing.IsActive = false;
                    searchProgressRing.Visibility = Visibility.Collapsed;
                    return;
                }
            }
            if (info.Item3 == UrlType.BangumiSS)
            {
                var bangumi = await BiliVideoHelper.GetBangumiInfoAsync(info.Item4, 1, sESSDATA);
                var dialog = await BangumiDialog.CreateAsync(bangumi);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary)
                {
                    searchBtn.IsEnabled = true;
                    searchProgressRing.IsActive = false;
                    searchProgressRing.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            else if (info.Item3 == UrlType.SingelVideo) //指定了分p的时候
            {
                try
                {
                    var dialog = await SingleVideoDialog.CreateAsync
                        (await BiliVideoHelper.GetSingleVideoInfoAsync(info.Item1, info.Item2, 64, sESSDATA));
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Secondary)
                    {
                        searchBtn.IsEnabled = true;
                        searchProgressRing.IsActive = false;
                        searchProgressRing.Visibility = Visibility.Collapsed;
                        return;
                    }
                    var quality = dialog.QualitySelectionProperty;

                    await CreateDownloadAsync(info.Item1, info.Item2, quality, sESSDATA);
                }
                catch(ParsingVideoException ex)
                {
                    var dialog = new ErrorDialog(ex.Message);
                    var result = await dialog.ShowAsync();

                    searchBtn.IsEnabled = true;
                    searchProgressRing.IsActive = false;
                    searchProgressRing.Visibility = Visibility.Collapsed;

                    if (result == ContentDialogResult.Primary)
                    {
                        this.Frame.Navigate(typeof(UserPage));
                        MainPage.NavView.SelectedItem = MainPage.NavViewItems[2];
                    }
                }
            }
            else if (info.Item3 == UrlType.MasteredVideo) //没有指定分p的时候
            {
                var master = await BiliVideoHelper.GetVideoMasterInfoAsync(info.Item1, sESSDATA);

                var dialog = await MasteredVideoDialog.CreateAsync(master);
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Secondary)
                {
                    Reset();
                    return;
                }
            }
            else throw new System.Exception("没找到下载的方法");
        }
        public void Reset()
        {
            searchBtn.IsEnabled = true;
            searchProgressRing.IsActive = false;
            searchProgressRing.Visibility = Visibility.Collapsed;
        }
        public async Task CreateDownloadAsync(string bv,long cid, int quality, string sESSDATA) //创建下载
        {
            var video = await BiliVideoHelper.GetSingleVideoAsync(bv, cid, quality, sESSDATA);

            if (searchBtn.IsEnabled == false)
                searchBtn.IsEnabled = true;
            if (searchProgressRing.IsActive == true) 
                searchProgressRing.IsActive = false;
            if (searchProgressRing.Visibility == Visibility.Visible) 
                searchProgressRing.Visibility = Visibility.Collapsed;

            MainPage.NavView.SelectedItem = MainPage.NavViewItems[0];
            MainPage.ContentFrame.Navigate(typeof(DownloadPage), new DownloadNavigateModel()
            {
                VideoList = new List<BiliVideo>()
                {
                    video
                }
            });
        }
        public async Task CreateDownloadsAsync(List<BiliVideoInfo> videos,int quality,string sESSDATA)
        {
            var videoList = new List<BiliVideo>();
            foreach (var video in videos)
            {
                videoList.Add(await BiliVideoHelper.GetSingleVideoAsync(video.Bv, video.Cid, quality, sESSDATA));
            }

            if (searchBtn.IsEnabled == false)
                searchBtn.IsEnabled = true;
            if (searchProgressRing.IsActive == true)
                searchProgressRing.IsActive = false;
            if (searchProgressRing.Visibility == Visibility.Visible)
                searchProgressRing.Visibility = Visibility.Collapsed;

            MainPage.NavView.SelectedItem = MainPage.NavViewItems[0];
            this.Frame.Navigate(typeof(DownloadPage), new DownloadNavigateModel()
            {
                VideoList = videoList
            });
        }
        private enum UrlType
        {
            SingelVideo,
            MasteredVideo,
            BangumiEP,
            BangumiSS
        }
                           //bv     cid  类型    ep或ss
        private async Task<(string,long,UrlType,int)> AnalyzeVideoUrlAsync(string url,string sESSDATA)
            //分析输入的url，提取bv
        {
            BiliVideoMaster master;
            long cid = 0;
            int p = 0;
            string bv;
            long av;
            int ep = 0;
            int ss = 0;
            UrlType type = UrlType.MasteredVideo;

            if (Regex.IsMatch(searchTextbox.Text, "\\?p=[0-9]*"))//判断分p
            {
                p = int.Parse(Regex.Match(searchTextbox.Text, "p=[0-9]*").Value.Remove(0, 2));
                type = UrlType.SingelVideo;
            };

            if (Regex.IsMatch(url, "[B|b][V|v][a-z|A-Z|0-9]*"))//判断bv
            {
                bv = Regex.Match(url, "[B|b][V|v][a-z|A-Z|0-9]*").Value;
                master = await BiliVideoHelper.GetVideoMasterInfoAsync(bv, sESSDATA);
            }
            else if (Regex.IsMatch(url,"[a|A][v|V][0-9]*"))//判断av
            {
                av = long.Parse(Regex.Match(url, "[a|A][v|V][0-9]*").Value.Remove(0, 2));
                master = await BiliVideoHelper.GetVideoMasterInfoAsync(av, sESSDATA);
            }
            else if (Regex.IsMatch(url,"[e|E][p|P][0-9]*"))
            {
                p = 0;
                master = null;
                type = UrlType.BangumiEP;
                ep = int.Parse(Regex.Match(url, "[e|E][p|P][0-9]*").Value.Remove(0, 2));
            }
            else if (Regex.IsMatch(url,"[s|S][s|S][0-9]*"))
            {
                p = 0;
                master = null;
                type = UrlType.BangumiSS;
                ss = int.Parse(Regex.Match(url, "[s|S][s|S][0-9]*").Value.Remove(0, 2));
            }
            else
            {
                throw new ArgumentException("Url不合法");
            }
            if (p!=0)
            {
                cid = (long)(master.VideoList.Where(v => v.P == p)?.FirstOrDefault().Cid);
            }
            if (type == UrlType.BangumiEP) return (null, 0, type, ep);
            if (type == UrlType.BangumiSS) return (null, 0, type, ss);
            if (master != null)//只要是番剧，就把master赋值为空
                return (master.Bv, cid, type, 0);
            else throw new System.Exception();
        }
    }
    internal class DownloadNavigateModel
    {
        public List<BiliVideo> VideoList { get; set; }
    }
}
