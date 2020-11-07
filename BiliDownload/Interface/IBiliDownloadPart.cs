using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace BiliDownload.Interface
{
    public interface IBiliDownloadPart
    {
        public Task StartAsync();
        public void PauseOrResume();
        public DownloadOperation Operation { get; set; }
        public Guid OperationGuid { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
        public string Url { get; set; }
        public StorageFile CacheFile { get; set; }
        public bool IsStarted { get; set; }
        public bool IsPaused { get; set; }
        public bool IsComplete { get; set; }
        public bool IsRecreated { get; set; }
    }
}
