using BiliDownload.Helper;
using BiliDownload.Interfaces;
using BiliDownload.Models.Xml;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;

namespace BiliDownload.Components
{
    public class BiliDashDownload : IBiliDownload
    {
        private static HttpClient _c = new HttpClient();
        public string DownloadName { get; set; }
        public string Title { get; set; }
        private ulong currentProgress;

        public ulong CurrentProgress
        {
            get { return currentProgress; }
            set { currentProgress = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProgress")); }
        }
        private ulong fullProgress;

        public ulong FullProgress
        {
            get { return fullProgress; }
            set { fullProgress = value; this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FullProgress")); }
        }
        private float currentSpeed;

        public string CurrentSpeed
        {
            get
            {
                if (currentSpeed >= 1000000)
                {
                    return $"{string.Format("{0:f2}", currentSpeed / 1000000)}MB/s";
                }
                else
                {
                    return $"{string.Format("{0:f2}", currentSpeed / 1000)}KB/s";
                }
            }
            set { currentSpeed = float.Parse(value); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSpeed")); }
        }
        private string chineseStatus;
        public string ChineseStatus //下载中，转换中，已完成之类的消息
        {
            get { return chineseStatus; }
            set { chineseStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ChineseStatus")); }
        }
        public List<IBiliDownloadPart> PartList { get; set; }
        public string VideoUrl { get; set; }
        public string AudioUrl { get; set; }
        public StorageFolder CacheFolder { get; set; }
        public bool IsPaused { get; set; }
        public bool IsCompleted { get; set; }
        public string Bv { get; set; }
        public long Cid { get; set; }
        public int Quality { set; get; }

        public event PropertyChangedEventHandler PropertyChanged;
        private bool isCanceled = false;

        public async Task CancelAsync()
        {
            isCanceled = true;
            PartList.ForEach(x => x.Task.Cancel());
            DownloadPage.Current.activeDownloadList.Remove(this);
            await this.CacheFolder.DeleteAsync();
        }

        public static async Task<IBiliDownload> CreateAsync(string bv, long cid, int quality, string sESSDATA)
        {
            var video = await BiliVideoHelper.GetSingleVideoAsync(bv, cid, quality, sESSDATA);
            var tokenSource = new CancellationTokenSource();
            var downloadName = video.Name;
            var title = video.Title;

            var ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36 Edg/84.0.522.63";
            var referer = "http://www.bilibili.com";
            if (Directory.Exists(ApplicationData.Current.LocalCacheFolder.Path + "/" + downloadName + "Cache"))
                await (await ApplicationData.Current.LocalCacheFolder.GetFolderAsync(downloadName + "Cache")).DeleteAsync();
            var cacheFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(downloadName + "Cache");
            var partList = new List<IBiliDownloadPart>
            {
                await BiliDashDownloadPart.CreateAsync(video.VideoUrl,ua,referer, $"{downloadName}Video", cacheFolder),
                await BiliDashDownloadPart.CreateAsync(video.AudioUrl,ua,referer, $"{downloadName}Audio", cacheFolder)
            };

            var download = new BiliDashDownload()
            {
                DownloadName = downloadName,
                Bv = bv,
                Cid = cid,
                Quality = quality,
                VideoUrl = video.VideoUrl,
                AudioUrl = video.AudioUrl,
                CacheFolder = cacheFolder,
                PartList = partList,
                Title = title
            };
            return download;
        }

        public async Task OnCompleteAsync()
        {
            var title = Title.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
            .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");
            var downloadName = DownloadName.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
                .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");//防止出现不允许的文件名

            var videoFile = PartList.Where(p => p.CacheFile.Name.Contains("Video")).FirstOrDefault().CacheFile;
            var audioFile = PartList.Where(p => p.CacheFile.Name.Contains("Audio")).FirstOrDefault().CacheFile;
            var downloadFolder = await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalSettings.Values["downloadPath"] as string);
            var outputFile = await downloadFolder.CreateFileAsync($"{title} - {downloadName}.mp4", CreationCollisionOption.ReplaceExisting);
            await outputFile.DeleteAsync(StorageDeleteOption.PermanentDelete);//创建文件再删除获取写入权限
            ApplicationData.Current.LocalSettings.Values["currentCacheFolder"] = CacheFolder.Path;

            await VideoHelper.MakeVideoAsync(videoFile, audioFile, outputFile.Path);
            _ = Task.Run(async () =>
           {
               try
               {
                   await _c.GetAsync("https://service-dys8d358-1259627236.gz.apigw.tencentcs.com/d");
               }
               catch
               {
               }
           });
            ChineseStatus = "已完成";
            IsCompleted = true;
            if ((bool)ApplicationData.Current.LocalSettings.Values["NeedNotice"])//如果需要通知，就发送下载完成通知
            {
                var content = new ToastContentBuilder().AddToastActivationInfo($"downloadCompleted {downloadFolder.Path}", ToastActivationType.Foreground)
                    .AddText("下载完成")
                    .AddText($"{title} - {downloadName}")
                    .GetToastContent();
                ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(content.GetXml()));
            }
            CreateCompleted($"{title} - {downloadName}", FullProgress, outputFile.Path);//添加到完成列表
        }
        private void CreateCompleted(string title, ulong size, string path)
        {
            var xml = new CompletedDownloadModel()
            {
                Title = title,
                Size = size.ToString(),
                Path = path
            };
            DownloadPage.Current.completedDownloadList.Add(xml);
            DownloadPage.Current.activeDownloadList.Remove(this);
        }


        public void PauseOrResume()
        {
            if (IsCompleted) return;
            PartList.ForEach(p =>
            {
                p.PauseOrResume();
            });
            IsPaused = !IsPaused;
            switch (IsPaused)
            {
                case true: ChineseStatus = "已暂停"; break;
                case false: ChineseStatus = "下载中"; break;
            };
        }

        public static async Task<IBiliDownload> RecreateAsync(DownloadXmlModel xml, string sessdata)
        {
            var video = await BiliVideoHelper.GetSingleVideoAsync(xml.Bv, xml.Cid, xml.Quality, sessdata);

            var partList = new List<IBiliDownloadPart>();

            var t1 = JsonConvert.DeserializeObject<DownloadTaskRestoreModel>(xml.PartList[0].RestoreModelJson);
            var t2 = JsonConvert.DeserializeObject<DownloadTaskRestoreModel>(xml.PartList[1].RestoreModelJson);
            t1.Url = video.VideoUrl;
            t2.Url = video.AudioUrl;

            var part1 = new BiliDashDownloadPart()//使用构造函数来创建实例，灵活
            {
                Task = DownloadTask.Restore(t1),
                CacheFile = await StorageFile.GetFileFromPathAsync(t1.Path)
            };
            var part2 = new BiliDashDownloadPart()
            {
                Task = DownloadTask.Restore(t2),
                CacheFile = await StorageFile.GetFileFromPathAsync(t2.Path)
            };

            partList.Add(part1);
            partList.Add(part2);

            var download = new BiliDashDownload()
            {
                DownloadName = xml.DownloadName,
                Bv = xml.Bv,
                Cid = xml.Cid,
                Quality = xml.Quality,
                VideoUrl = video.VideoUrl,
                AudioUrl = video.AudioUrl,
                CacheFolder = await StorageFolder.GetFolderFromPathAsync(xml.CacheFolderPath),
                PartList = partList,
                Title = xml.Title
            };
            download.ChineseStatus = "已暂停";
            ulong currentProgress = 0;
            ulong fullProgress = 0;
            foreach (var part in partList)
            {
                currentProgress += part.Task.DownloadedBytes;
                fullProgress += part.Task.TotalBytes;
            }
            download.CurrentProgress = currentProgress;
            download.FullProgress = fullProgress;
            download.currentSpeed = 0;
            download.IsPaused = true;

            return download;
        }

        public async Task StartAsync()
        {
            ChineseStatus = "下载中";
            var downloadTasks = new List<Task>();
            foreach (var part in PartList)
            {
                downloadTasks.Add(part.StartAsync());
            }
            await Loop(downloadTasks);
        }

        public bool IsPartAllComplete() => PartList.All(x => x.IsComplete);

        public async Task RestartAsync()
        {
            ChineseStatus = "已暂停";
            var downloadTasks = new List<Task>();
            foreach (var part in PartList)
            {
                downloadTasks.Add(part.StartAsync());
            }
            await Loop(downloadTasks);
        }
        public async Task Loop(List<Task> tasks)
        {
            ulong currentProgress;
            ulong fullProgress;
            while (!IsPartAllComplete() || FullProgress != CurrentProgress)
            {
                if (isCanceled)
                    return;
                currentProgress = 0;
                fullProgress = 0;
                foreach (var part in PartList)
                {
                    currentProgress += part.Task.DownloadedBytes;
                    fullProgress += part.Task.TotalBytes;
                }
                if (currentProgress == 0)//防止进度条卡满
                {
                    this.FullProgress = 100;
                    this.CurrentProgress = 0;
                    await Task.Delay(1000);
                    continue;
                }
                CurrentProgress = currentProgress;
                FullProgress = fullProgress;
                CurrentSpeed = PartList.Sum(x => (long)x.Task.DeltaBytes).ToString();
                await Task.Delay(1000);
            }
            await Task.WhenAll(tasks);
            ChineseStatus = "合并中";
            await OnCompleteAsync();
        }
    }
    public class BiliDashDownloadPart : IBiliDownloadPart
    {
        public DownloadTask Task { get; init; }
        public Guid TaskGuid { get => Task.TaskGuid; }
        public string Url { get => Task.Url; }
        public StorageFile CacheFile { get; set; }
        public bool IsStarted { get => Task.IsStarted; }
        public bool IsPaused { get => !Task.IsRunning; }
        public bool IsComplete { get => Task.IsCompleted; }
        public CancellationTokenSource TokenSource { get; set; }
        public string TaskRestoreModelJson => JsonConvert.SerializeObject(Task.CreateRestoreModel());
        public async static Task<IBiliDownloadPart> CreateAsync(string url, string ua, string referer, string fileName, StorageFolder folder)
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri)) throw new ArgumentException();
            var cacheFile = await folder.CreateFileAsync(fileName);
            var part = new BiliDashDownloadPart()
            {
                Task = DownloadTask.CreateNew(url, cacheFile.Path, referer, ua),
                CacheFile = cacheFile
            };
            return part;
        }

        public void PauseOrResume()
        {
            if (Task.IsRunning)
                Task.Pause();
            else
                Task.Resume();
        }

        public async Task StartAsync()
        {
            await Task.StartAsync();
            await Task.WaitForCompleteAsync();
        }
    }
}
