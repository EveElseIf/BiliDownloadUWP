using BiliDownload.Helper;
using BiliDownload.Interfaces;
using BiliDownload.Models.Xml;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace BiliDownload.Components
{
    public class BiliDashDownload : IBiliDownload
    {
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
        public CancellationTokenSource TokenSource { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task CancelAsync()
        {
            this.TokenSource.Cancel();
            DownloadPage.Current.activeDownloadList.Remove(this);
            await this.CacheFolder.DeleteAsync();
        }

        public static async Task<IBiliDownload> CreateAsync(string bv, long cid, int quality, string sESSDATA)
        {
            var video = await BiliVideoHelper.GetSingleVideoAsync(bv, cid, quality, sESSDATA);
            var tokenSource = new CancellationTokenSource();
            var downloadName = video.Name;
            var title = video.Title;

            var downloader = new BackgroundDownloader();
            downloader.SetRequestHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36 Edg/84.0.522.63");
            downloader.SetRequestHeader("referer", "http://www.bilibili.com");
            if (Directory.Exists(ApplicationData.Current.LocalCacheFolder.Path + "/" + downloadName + "Cache"))
                await (await ApplicationData.Current.LocalCacheFolder.GetFolderAsync(downloadName + "Cache")).DeleteAsync();
            var cacheFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(downloadName + "Cache");
            var partList = new List<IBiliDownloadPart>
            {
                await BiliDashDownloadPart.CreateAsync(video.VideoUrl, $"{downloadName}Video", cacheFolder, downloader, tokenSource),
                await BiliDashDownloadPart.CreateAsync(video.AudioUrl, $"{downloadName}Audio", cacheFolder, downloader, tokenSource)
            };

            var download = new BiliDashDownload()
            {
                DownloadName = downloadName,
                VideoUrl = video.VideoUrl,
                AudioUrl = video.AudioUrl,
                CacheFolder = cacheFolder,
                PartList = partList,
                Title = title,
                TokenSource = tokenSource
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

            while (VideoHelper.Locked) await Task.Delay(100);
            await VideoHelper.MakeVideoAsync(videoFile, audioFile, outputFile.Path);
            ChineseStatus = "已完成";
            IsCompleted = true;
            if ((bool)ApplicationData.Current.LocalSettings.Values["NeedNotice"])//如果需要通知，就发送下载完成通知
            {
                var content = new ToastContentBuilder().AddToastActivationInfo("downloadCompleted", ToastActivationType.Foreground)
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

        public static async Task<IBiliDownload> ReCreateAsync(DownloadXmlModel xml, List<DownloadOperation> operations)
        {
            var tokenSource = new CancellationTokenSource();
            var partList = new List<IBiliDownloadPart>();

            var part1 = new BiliDashDownloadPart()//使用构造函数来创建实例，灵活
            {
                Operation = operations.Where(o => o.Guid == xml.PartList[0].OperationGuid)?.FirstOrDefault(),
                OperationGuid = xml.PartList[0].OperationGuid,
                CacheFile = await StorageFile.GetFileFromPathAsync(xml.PartList[0].CacheFilePath),
                Url = xml.PartList[0].Url,
                IsRecreated = true,
                TokenSource = tokenSource
            };
            var part2 = new BiliDashDownloadPart()
            {
                Operation = operations.Where(o => o.Guid == xml.PartList[1].OperationGuid)?.FirstOrDefault(),
                OperationGuid = xml.PartList[1].OperationGuid,
                CacheFile = await StorageFile.GetFileFromPathAsync(xml.PartList[1].CacheFilePath),
                Url = xml.PartList[1].Url,
                IsRecreated = true,
                TokenSource = tokenSource
            };

            partList.Add(part1);
            partList.Add(part2);

            foreach (var part in partList)
            {
                if (part.Operation == null) part.IsComplete = true;
            }

            var download = new BiliDashDownload()
            {
                DownloadName = xml.DownloadName,
                VideoUrl = xml.VideoUrl,
                AudioUrl = xml.AudioUrl,
                CacheFolder = await StorageFolder.GetFolderFromPathAsync(xml.CacheFolderPath),
                PartList = partList,
                Title = xml.Title,
                TokenSource = tokenSource
            };
            download.ChineseStatus = "已暂停";
            ulong currentProgress = 0;
            ulong fullProgress = 0;
            foreach (var part in partList)
            {
                if (part.Operation != null)
                {
                    currentProgress += part.Operation.Progress.BytesReceived;
                    fullProgress += part.Operation.Progress.TotalBytesToReceive;
                }
            }
            download.CurrentProgress = currentProgress;
            download.FullProgress = fullProgress;
            download.currentSpeed = 0;
            download.IsPaused = true;

            return download;
        }

        public async Task ReStartAsync()
        {
            ChineseStatus = "已暂停";
            var downloadTasks = new List<Task>();
            ulong currentProgress;
            ulong fullProgress;
            ulong lastBytes = 0;
            foreach (var part in PartList)
            {
                downloadTasks.Add(part.StartAsync());
            }
            while (!IsPartAllComplete() || FullProgress != CurrentProgress)
            {
                if (TokenSource.IsCancellationRequested) return;//设置取消
                currentProgress = 0;
                fullProgress = 0;
                foreach (var part in PartList)
                {
                    if (part.Operation != null)
                    {
                        currentProgress += part.Operation.Progress.BytesReceived;
                        fullProgress += part.Operation.Progress.TotalBytesToReceive;
                    }
                }
                CurrentProgress = currentProgress;
                FullProgress = fullProgress;
                if (lastBytes != 0)
                {
                    CurrentSpeed = (currentProgress - lastBytes).ToString();
                }
                lastBytes = currentProgress;
                await Task.Delay(1000);
            }
            await Task.WhenAll(downloadTasks);
            ChineseStatus = "合并中";
            await OnCompleteAsync();
        }

        public async Task StartAsync()
        {
            ChineseStatus = "下载中";
            var downloadTasks = new List<Task>();
            ulong currentProgress;
            ulong fullProgress;
            ulong lastBytes = 0;
            foreach (var part in PartList)
            {
                downloadTasks.Add(part.StartAsync());
            }
            while (!IsPartAllComplete() || FullProgress != CurrentProgress)
            {
                if (TokenSource.IsCancellationRequested) return;//可取消
                currentProgress = 0;
                fullProgress = 0;
                foreach (var part in PartList)
                {
                    if (part.Operation != null)
                    {
                        currentProgress += part.Operation.Progress.BytesReceived;
                        fullProgress += part.Operation.Progress.TotalBytesToReceive;
                    }
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
                if (lastBytes != 0)
                {
                    CurrentSpeed = (currentProgress - lastBytes).ToString();
                }
                lastBytes = currentProgress;
                await Task.Delay(1000);
            }
            await Task.WhenAll(downloadTasks);
            ChineseStatus = "合并中";
            await OnCompleteAsync();
        }

        public bool IsPartAllComplete()
        {
            foreach (var part in this.PartList)
            {
                if (part.IsComplete == false) return false;
            }
            return true;
        }
    }
    public class BiliDashDownloadPart : IBiliDownloadPart
    {
        public DownloadOperation Operation { get; set; }
        public Guid OperationGuid { get; set; }
        public string Url { get; set; }
        public StorageFile CacheFile { get; set; }
        public bool IsStarted { get; set; }
        public bool IsPaused { get; set; }
        public bool IsComplete { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
        public bool IsRecreated { get; set; }

        public async static Task<IBiliDownloadPart> CreateAsync(string url, string fileName, StorageFolder folder, BackgroundDownloader downloader, CancellationTokenSource tokenSource)
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri)) throw new ArgumentException();
            var cacheFile = await folder.CreateFileAsync(fileName);
            var part = new BiliDashDownloadPart()
            {
                Operation = downloader.CreateDownload(uri, cacheFile),
                Url = url,
                CacheFile = cacheFile,
                TokenSource = tokenSource
            };
            part.OperationGuid = part.Operation.Guid;//添加Guid，当重启程序后，使用这个东西来reattach下载
            return part;
        }

        public void PauseOrResume()
        {
            if (IsRecreated) { Operation?.Resume(); IsRecreated = false; return; }
            if (Operation?.Progress.Status == BackgroundTransferStatus.PausedByApplication) Operation?.Resume();
            if (Operation?.Progress.Status == BackgroundTransferStatus.Running) Operation?.Pause();
        }

        public async Task StartAsync()
        {
            if (IsRecreated)//如果是重建的下载任务，就attach
            {
                IsStarted = true;
                if (Operation == null) return;
                var task = Operation.AttachAsync().AsTask(TokenSource.Token);
                await task;
            }
            else
            {
                IsStarted = true;
                var task = Operation.StartAsync().AsTask(TokenSource.Token);
                await task;
            }
            IsComplete = true;
        }
    }
}
