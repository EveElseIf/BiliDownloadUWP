using BiliDownload.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Helper
{
    public static class MainHelper
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
            var video = await BiliVideoHelper.GetSingleVideoAsync(bv, cid, quality, sESSDATA);

            MainPage.NavView.SelectedItem = MainPage.NavViewItems[0];
            MainPage.ContentFrame.Navigate(typeof(DownloadPage), new DownloadNavigateModel()
            {
                VideoList = new List<BiliVideo>()
                {
                    video
                }
            });
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
            var videoList = new List<BiliVideo>();
            foreach (var video in videos)
            {
                videoList.Add(await BiliVideoHelper.GetSingleVideoAsync(video.Bv, video.Cid, quality, sESSDATA));
            }

            MainPage.NavView.SelectedItem = MainPage.NavViewItems[0];
            MainPage.ContentFrame.Navigate(typeof(DownloadPage), new DownloadNavigateModel()
            {
                VideoList = videoList
            });
        }
    }
    public class DownloadNavigateModel
    {
        public List<BiliVideo> VideoList { get; set; }
    }
}
