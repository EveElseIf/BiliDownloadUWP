using BiliDownload.Components;
using BiliDownload.Dialogs;
using BiliDownload.Helper;
using BiliDownload.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

            //if (ApplicationData.Current.LocalSettings.Values["cpuLimit"] == null)//设置cpu限制默认值
            //    ApplicationData.Current.LocalSettings.Values["cpuLimit"] = false;
            if (ApplicationData.Current.LocalSettings.Values["ignoreEx"] == null)
                ApplicationData.Current.LocalSettings.Values["ignoreEx"] = false;

            IsResumed = false;//未恢复未完成的任务
        }
        private async Task CheckDownloadAsync()//检查一下未完成的任务
        {
            if (DownloadXmlHelper.CheckXml() == false) return;//不存在xml，直接返回
            var list = await BackgroundDownloader.GetCurrentDownloadsAsync();
            if (list == null) return;//list里没东西，直接返回

            await RecreateDownloadAsync(list);
        }
        private async Task RecreateDownloadAsync(IReadOnlyList<DownloadOperation> operations)//有未完成的任务，重新创建
        {
            var xmlList = DownloadXmlHelper.GetCurrentDownloads();
            var resumeList = new List<IBiliDownload>();

            if (xmlList.Count < 1) return;

            foreach (var xml in xmlList)
            {
                try
                {
                    var download = await BiliDashDownload
                        .ReCreateAsync(xml, operations.ToList());
                    resumeList.Add(download);
                    this.activeDownloadList.Add(download);
                }
                catch (IOException)
                {
                    var dialog = new ContentDialog()
                    {
                        PrimaryButtonText = "确定",
                        Title = "提示",
                        Content = new TextBlock()
                        {
                            Text = "似乎有本地缓存文件丢失(不是什么大问题)，如有未完成的下载任务缺失，请重新下载",
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new FontFamily("Microsoft Yahei UI"),
                            FontSize = 20
                        }
                    };
                    await dialog.ShowAsync();
                }
                catch (System.Exception ex)
                {
                    var dialog = new ExceptionDialog(ex.Message);
                    await dialog.ShowAsync();
                }
            }

            var tasks = new List<Task>();
            foreach (var download in activeDownloadList)
            {
                tasks.Add(download.ReStartAsync());
            }
            await Task.WhenAll(tasks);
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
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!IsResumed)
            {
                IsResumed = true;
                await CheckDownloadAsync();
                CheckComplete();
            }//检查是否有未完成的任务，有就重建
             //检查是否有已完成的任务，有就添加到完成列表

            //if (!(e.Parameter is DownloadNavigateModel)) return;

            //var model = e.Parameter as DownloadNavigateModel;
            //var downloads = new List<DownloadViewModel>();
            //foreach (var video in model.VideoList)
            //{
            //    var download = await DownloadViewModel.CreateDownloadAsync
            //        (video.Name, video.Title, video.VideoUrl, video.AudioUrl,new CancellationTokenSource());//创建一个下载
            //    downloads.Add(download);//一个downlaod就是一个视频的下载，包括视频和音频两部分
            //    this.activeDownloadList.Add(download);//添加到页面上
            //};
            //var tasks = new List<Task>();
            //foreach (var download in downloads)
            //{
            //    tasks.Add(download.StartDownloadAsync());//启动下载
            //};
            //await Task.WhenAll(tasks);
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
            download.PauseOrResume();
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
                }
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary) return;
            var download = (sender as Button)?.DataContext as IBiliDownload;
            if (!download.IsCompleted)
                await download.CancelAsync();
        }

        private async void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            //var model = (sender as Button)?.DataContext as CompletedDownloadModel;
            if (!(ApplicationData.Current.LocalSettings.Values["downloadPath"] is string path))
            {
                var dialog = new ErrorDialog("未设置下载储存文件夹，请前往设置以更改")
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
    //public class DownloadViewModel : INotifyPropertyChanged
    //{
    //    private DownloadViewModel() { }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    private string downloadName;

    //    public string DownloadName
    //    {
    //        get { return downloadName; }
    //        set { downloadName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DownloadName")); }
    //    }

    //    private ulong currentProgress;

    //    public ulong CurrentProgress
    //    {
    //        get { return currentProgress; }
    //        set { currentProgress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProgress")); }
    //    }

    //    private ulong fullProgress;

    //    public ulong FullProgress
    //    {
    //        get { return fullProgress; }
    //        set { fullProgress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FullProgress")); }
    //    }
    //    private float currentSpeed;

    //    public string CurrentSpeed
    //    {
    //        get
    //        {
    //            if (currentSpeed>=1000000)
    //            {
    //                return $"{string.Format("{0:f2}", currentSpeed / 1000000)}Mb/s";
    //            }
    //            else
    //            {
    //                return $"{string.Format("{0:f2}", currentSpeed / 1000)}Kb/s";
    //            }
    //        }
    //        set { currentSpeed = float.Parse(value); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSpeed")); }
    //    }
    //    private string chineseStatus;

    //    public string ChineseStatus //下载中，转换中，已完成之类的消息
    //    {
    //        get { return chineseStatus; }
    //        set { chineseStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ChineseStatus")); }
    //    }

    //    public List<DownloadPart> PartList { get; set; }
    //    public string VideoUrl { get; set; }
    //    public string AudioUrl { get; set; }
    //    public StorageFolder CacheFolder { set; get; }
    //    public string CacheFolderPath { set; get; }
    //    public bool IsPaused { set; get; } = false;
    //    public bool IsCompleted { get; set; } = false;
    //    public string Title { get; set; }
    //    public CancellationTokenSource TokenSource { get; set; }

    //    public static async Task<DownloadViewModel> CreateDownloadAsync(string downloadName,string title, string videoUrl,string audioUrl,CancellationTokenSource tokenSource)
    //    {
    //        var downloader = new BackgroundDownloader();
    //        downloader.SetRequestHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36 Edg/84.0.522.63");
    //        downloader.SetRequestHeader("referer", "http://www.bilibili.com");
    //        if (Directory.Exists(ApplicationData.Current.LocalCacheFolder.Path + "/" + downloadName + "Cache"))
    //            await (await ApplicationData.Current.LocalCacheFolder.GetFolderAsync(downloadName + "Cache")).DeleteAsync();
    //        var cacheFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(downloadName + "Cache");
    //        var partList = new List<DownloadPart>();

    //        partList.Add(await DownloadPart.CreateAsync(videoUrl, $"{downloadName}Video", cacheFolder, downloader,tokenSource));
    //        partList.Add(await DownloadPart.CreateAsync(audioUrl, $"{downloadName}Audio", cacheFolder, downloader,tokenSource));

    //        var model = new DownloadViewModel()
    //        {
    //            DownloadName = downloadName,
    //            VideoUrl = videoUrl,
    //            AudioUrl = audioUrl,
    //            CacheFolder = cacheFolder,
    //            CacheFolderPath = cacheFolder.Path,
    //            PartList = partList,
    //            Title = title,
    //            TokenSource = tokenSource
    //        };
    //        return model;
    //    }
    //    public static async Task<DownloadViewModel> RecreateDownloadAsync(DownloadXmlModel model,List<DownloadOperation> operations,CancellationTokenSource tokenSource)
    //    {
    //        var partList = new List<DownloadPart>();

    //        var part1 = new DownloadPart()//使用构造函数来创建实例，灵活
    //        {
    //            Operation = operations.Where(o => o.Guid == model.PartList[0].OperationGuid)?.FirstOrDefault(),
    //            OperationGuid = model.PartList[0].OperationGuid,
    //            CacheFilePath = model.PartList[0].CacheFilePath,
    //            CacheFile = await StorageFile.GetFileFromPathAsync(model.PartList[0].CacheFilePath),
    //            Url = model.PartList[0].Url,
    //            IsRecreated = true,
    //            TokenSource = tokenSource
    //        };
    //        part1.CacheFileName = part1.CacheFile.Name;
    //        var part2 = new DownloadPart()
    //        {
    //            Operation = operations.Where(o => o.Guid == model.PartList[1].OperationGuid)?.FirstOrDefault(),
    //            OperationGuid = model.PartList[1].OperationGuid,
    //            CacheFilePath = model.PartList[1].CacheFilePath,
    //            CacheFile = await StorageFile.GetFileFromPathAsync(model.PartList[1].CacheFilePath),
    //            Url = model.PartList[1].Url,
    //            IsRecreated = true,
    //            TokenSource = tokenSource
    //        };
    //        part2.CacheFileName = part2.CacheFile.Name;

    //        partList.Add(part1);
    //        partList.Add(part2);

    //        foreach (var part in partList)
    //        {
    //            if (part.Operation == null) part.IsComplete = true;
    //        }

    //        var viewModel = new DownloadViewModel()
    //        {
    //            DownloadName = model.DownloadName,
    //            VideoUrl = model.VideoUrl,
    //            AudioUrl = model.AudioUrl,
    //            CacheFolder = await StorageFolder.GetFolderFromPathAsync(model.CacheFolderPath),
    //            CacheFolderPath = model.CacheFolderPath,
    //            PartList = partList,
    //            Title = model.Title,
    //            TokenSource = tokenSource
    //        };
    //        viewModel.ChineseStatus = "已暂停";
    //        ulong currentProgress = 0;
    //        ulong fullProgress = 0;
    //        foreach (var part in partList)
    //        {
    //            if (part.Operation != null)
    //            {
    //                currentProgress += part.Operation.Progress.BytesReceived;
    //                fullProgress += part.Operation.Progress.TotalBytesToReceive;
    //            }
    //        }
    //        viewModel.CurrentProgress = currentProgress;
    //        viewModel.FullProgress = fullProgress;
    //        viewModel.currentSpeed = 0;
    //        viewModel.IsPaused = true;

    //        return viewModel;
    //    }
    //    public async Task StartDownloadAsync()
    //    {
    //        ChineseStatus = "下载中";
    //        var downloadTasks = new List<Task>();
    //        ulong currentProgress;
    //        ulong fullProgress;
    //        ulong lastBytes = 0;
    //        foreach (var part in PartList)
    //        {
    //            downloadTasks.Add(part.StartDownloadAsync());
    //        }
    //        while (!IsPartAllComplete(PartList) || FullProgress != CurrentProgress)
    //        {
    //            if (TokenSource.IsCancellationRequested) return;//可取消
    //            currentProgress = 0;
    //            fullProgress = 0;
    //            foreach (var part in PartList)
    //            {
    //                if (part.Operation != null)
    //                {
    //                    currentProgress += part.Operation.Progress.BytesReceived;
    //                    fullProgress += part.Operation.Progress.TotalBytesToReceive;
    //                }
    //            }
    //            if (currentProgress == 0)//防止进度条卡满
    //            {
    //                this.FullProgress = 100;
    //                this.CurrentProgress = 0;
    //                await Task.Delay(1000);
    //                continue;
    //            }
    //            CurrentProgress = currentProgress;
    //            FullProgress = fullProgress;
    //            if (lastBytes != 0)
    //            {
    //                CurrentSpeed = (currentProgress - lastBytes).ToString();
    //            }
    //            lastBytes = currentProgress;
    //            await Task.Delay(1000);
    //        }
    //        await Task.WhenAll(downloadTasks);
    //        ChineseStatus = "转换中";
    //        await OnComplete();
    //    }
    //    public async Task RestartDownloadAsync()
    //    {
    //        ChineseStatus = "已暂停";
    //        var downloadTasks = new List<Task>();
    //        ulong currentProgress;
    //        ulong fullProgress;
    //        ulong lastBytes = 0;
    //        foreach (var part in PartList)
    //        {
    //            downloadTasks.Add(part.StartDownloadAsync());
    //        }
    //        while (!IsPartAllComplete(PartList) || FullProgress != CurrentProgress)
    //        {
    //            if (TokenSource.IsCancellationRequested) return;//设置取消
    //            currentProgress = 0;
    //            fullProgress = 0;
    //            foreach (var part in PartList)
    //            {
    //                if (part.Operation != null)
    //                {
    //                    currentProgress += part.Operation.Progress.BytesReceived;
    //                    fullProgress += part.Operation.Progress.TotalBytesToReceive;
    //                }
    //            }
    //            CurrentProgress = currentProgress;
    //            FullProgress = fullProgress;
    //            if (lastBytes != 0)
    //            {
    //                CurrentSpeed = (currentProgress - lastBytes).ToString();
    //            }
    //            lastBytes = currentProgress;
    //            await Task.Delay(1000);
    //        }
    //        await Task.WhenAll(downloadTasks);
    //        ChineseStatus = "转换中";
    //        await OnComplete();
    //    }
    //    public async Task CancelDownloadAsync()
    //    {
    //        this.TokenSource.Cancel();
    //        //DownloadPage.Current.activeDownloadList.Remove(this);
    //        await this.CacheFolder.DeleteAsync();
    //    }
    //    private bool IsPartAllComplete(IEnumerable<DownloadPart> parts)//检测是否完成
    //    {
    //        foreach (var part in parts)
    //        {
    //            if (part.IsComplete == false) return false;
    //        }
    //        return true;
    //    }
    //    public void PauseOrResume()
    //    {
    //        if (IsCompleted) return;
    //        PartList.ForEach(p =>
    //        {
    //            p.PauseOrResume();
    //        });
    //        IsPaused = !IsPaused;
    //        switch (IsPaused)
    //        {
    //            case true: ChineseStatus = "已暂停"; break;
    //            case false: ChineseStatus = "下载中"; break;
    //        };
    //    }
    //    public async Task OnComplete()//完成时的转换工作
    //    {
    //        var title = Title.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
    //            .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");
    //        var downloadName = DownloadName.Replace("\\", "").Replace("/", "").Replace(":", "").Replace("*", "")
    //            .Replace("?", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "");//防止出现不允许的文件名

    //        var videoFile = PartList.Where(p => p.CacheFileName.Contains("Video")).FirstOrDefault().CacheFile;
    //        var audioFile = PartList.Where(p => p.CacheFileName.Contains("Audio")).FirstOrDefault().CacheFile;
    //        var downloadFolder = await StorageFolder.GetFolderFromPathAsync(ApplicationData.Current.LocalSettings.Values["downloadPath"] as string);
    //        var outputFile = await downloadFolder.CreateFileAsync($"{title} - {downloadName}.mp4", CreationCollisionOption.ReplaceExisting);
    //        await outputFile.DeleteAsync(StorageDeleteOption.PermanentDelete);//创建文件再删除获取写入权限
    //        ApplicationData.Current.LocalSettings.Values["currentCacheFolder"] = CacheFolder.Path;

    //        while (VideoHelper.Locked) await Task.Delay(100);
    //        await VideoHelper.MakeVideoAsync(videoFile, audioFile, outputFile.Path);
    //        ChineseStatus = "已完成";
    //        IsCompleted = true;
    //        CreateCompleted($"{DownloadName} - {title}", FullProgress, outputFile.Path);
    //    }
    //    private void CreateCompleted(string title,ulong size,string path)
    //    {
    //        var model = new CompletedDownloadModel()
    //        {
    //            Title = title,
    //            Size = size.ToString(),
    //            Path = path
    //        };
    //        DownloadPage.Current.completedDownloadList.Add(model);
    //        //DownloadPage.Current.activeDownloadList.Remove(this);
    //    }
    //}
    //public class DownloadPart
    //{
    //    public DownloadOperation Operation { set; get; }
    //    public Guid OperationGuid { get; set; }
    //    public string Url { set; get; }
    //    public StorageFile CacheFile { set; get; }
    //    public string CacheFileName { set; get; }
    //    public string CacheFilePath { get; set; }
    //    public CancellationTokenSource TokenSource { get; set; }
    //    public bool IsRecreated { get; set; } = false;
    //    public bool IsStarted { set; get; } = false;
    //    public bool IsPaused { set; get; } = false;
    //    public bool IsComplete { set; get; } = false;
    //    public DownloadPart() { }
    //    public static async Task<DownloadPart> CreateAsync(string url,string fileName,StorageFolder folder,BackgroundDownloader downloader,CancellationTokenSource tokenSource)
    //    {
    //        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri)) throw new ArgumentException();
    //        var cacheFile = await folder.CreateFileAsync(fileName);
    //        var part = new DownloadPart()
    //        {
    //            Operation = downloader.CreateDownload(uri, cacheFile),
    //            Url = url,
    //            CacheFile = cacheFile,
    //            CacheFileName = fileName,
    //            CacheFilePath = cacheFile.Path,
    //            TokenSource = tokenSource
    //        };
    //        part.OperationGuid = part.Operation.Guid;//添加Guid，当重启程序后，使用这个东西来reattach下载
    //        return part;
    //    }
    //    public async Task StartDownloadAsync()
    //    {
    //        if (IsRecreated)//如果是重建的下载任务，就attach
    //        {
    //            IsStarted = true;
    //            if (Operation == null) return;
    //            var task = Operation.AttachAsync().AsTask(TokenSource.Token);
    //            await task;
    //        }
    //        else
    //        {
    //            IsStarted = true;
    //            var task = Operation.StartAsync().AsTask(TokenSource.Token);
    //            await task;
    //        }
    //        IsComplete = true;

    //    }
    //    public void PauseOrResume()
    //    {
    //        if (IsRecreated) { Operation?.Resume(); IsRecreated = false; return; }
    //        if (Operation?.Progress.Status == BackgroundTransferStatus.PausedByApplication) Operation?.Resume();
    //        if(Operation?.Progress.Status == BackgroundTransferStatus.Running) Operation?.Pause();
    //    }
    //}
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
