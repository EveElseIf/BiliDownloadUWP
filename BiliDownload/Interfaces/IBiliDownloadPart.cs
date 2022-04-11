using BiliDownload.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Interfaces
{
    public interface IBiliDownloadPart
    {
        public Task StartAsync();
        public void PauseOrResume();
        public DownloadTask Task { get; }
        public string TaskRestoreModelJson { get; }
        public Guid TaskGuid { get; }
        public CancellationTokenSource TokenSource { get; }
        public string Url { get; }
        public StorageFile CacheFile { get; }
        public bool IsStarted { get; }
        public bool IsPaused { get; }
        public bool IsComplete { get; }
    }
}
