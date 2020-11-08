using BiliDownload.Component;
using BiliDownload.Dialog;
using BiliDownload.Helpers;
using BiliDownload.Interface;
using BiliDownload.Model;
using BiliDownload.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace BiliDownload.Helper
{
    public static class DownloadHelper
    {
        /// <summary>
        /// 创建下载后自动navigate
        /// </summary>
        /// <param name="bv"></param>
        /// <param name="cid"></param>
        /// <param name="quality"></param>
        /// <param name="sESSDATA"></param>
        /// <returns></returns>
        public static async Task CreateDownloadAsync(string bv, long cid, int quality, string sESSDATA) //创建下载
        {
            if (string.IsNullOrWhiteSpace(ApplicationData.Current.LocalSettings.Values["downloadPath"] as string))//检查下载目录是否为空
            {
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

            if (Settings.AutoDownloadDanmaku)
            {
                var video = await BiliVideoHelper.GetSingleVideoAsync(bv, cid, quality, sESSDATA);
                _ = BiliDanmakuHelper.DownloadDanmakuAsync(video.Title, video.Name, cid);
            }

            try
            {
                var download = await BiliDashDownload.CreateAsync(bv, cid, quality, sESSDATA);
                DownloadPage.Current.activeDownloadList.Add(download);
                var task = download.StartAsync();
                MainPage.NavView.SelectedItem = MainPage.NavViewItems[0];
                MainPage.ContentFrame.Navigate(typeof(DownloadPage));
                await task;
            }
            catch (Exception ex)
            {
                var dialog = new ExceptionDialog(ex.Message);
                await dialog.ShowAsync();
            }
        }
        /// <summary>
        /// 创建下载后自动navigate
        /// </summary>
        /// <param name="videos"></param>
        /// <param name="quality"></param>
        /// <param name="sESSDATA"></param>
        /// <returns></returns>
        public static async Task CreateDownloadsAsync(List<BiliVideoInfo> videos, int quality, string sESSDATA)
        {
            if (ApplicationData.Current.LocalSettings.Values["downloadPath"] as string == null)//检查下载目录是否为空
            {
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

            var videoList = new List<BiliVideo>();
            foreach (var video in videos)
            {
                videoList.Add(await BiliVideoHelper.GetSingleVideoAsync(video.Bv, video.Cid, quality, sESSDATA));
            }

            if (Settings.AutoDownloadDanmaku)
            {
                _ = BiliDanmakuHelper.DownloadMultiDanmakuAsync(videoList);
            }

            var downloadList = new List<IBiliDownload>();
            foreach (var video in videoList)
            {
                var download = await BiliDashDownload.CreateAsync(video.Bv, video.Cid, quality, sESSDATA);
                downloadList.Add(download);
                DownloadPage.Current.activeDownloadList.Add(download);
            }

            MainPage.NavView.SelectedItem = MainPage.NavViewItems[0];
            MainPage.ContentFrame.Navigate(typeof(DownloadPage));

            var tasks = new List<Task>();
            foreach (var download in downloadList)
            {
                tasks.Add(download.StartAsync());
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (System.Exception ex)
            {
                var dialog = new ExceptionDialog(ex.Message);
                await dialog.ShowAsync();
            }
        }
    }
}
