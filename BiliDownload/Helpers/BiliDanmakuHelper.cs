using BiliDownload.Helper;
using BiliDownload.Model;
using BiliDownload.Others;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Helpers
{
    public static class BiliDanmakuHelper
    {
        const string danmakuUrl = "http://comment.bilibili.com/";
        private static readonly HttpClient client = new HttpClient();
        public static async Task<string> GetDanmakuXmlAsync(long cid)
        {
            var url = danmakuUrl + $"{cid}.xml";
            var stream = new DeflateStream(await client.GetStreamAsync(url), CompressionMode.Decompress);
            var xml = await new StreamReader(stream).ReadToEndAsync();
            return xml;
        }
        public static async Task DownloadDanmakuAsync(string videoTitle, string videoName, long cid)
        {
            var xml = await GetDanmakuXmlAsync(cid);
            var fileName = $"{videoTitle} - {videoName}.xml";
            await DanmakuHelper.MakeAssAsync(xml, fileName, ApplicationData.Current.LocalSettings.Values["downloadPath"] as string);
        }
        public static async Task DownloadMultiDanmakuAsync(List<BiliVideo> videoList)
        {
            var dicTask = new Dictionary<BiliVideo, Task<string>>();
            foreach (var v in videoList)
            {
                dicTask.Add(v, GetDanmakuXmlAsync(v.Cid));
            }
            var dic = new Dictionary<string, string>();
            await Task.WhenAll(dicTask.Values);
            foreach (var item in dicTask)
            {
                var title = item.Key.Title.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
                .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");
                var name = item.Key.Name.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
                .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");
                dic.Add($"{title} - {name}.xml", item.Value.Result);
            }
            //var dic = videoList.ToDictionary(v => $"{v.Title.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "").Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "")} - {v.Name.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "").Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "")}", v => v.Cid);
            await DanmakuHelper.MakeMultiAssAsync(dic, Settings.DownloadPath);
        }
    }
}
