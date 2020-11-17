using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Interfaces
{
    public interface IBiliDownload : INotifyPropertyChanged
    {
        public Task StartAsync();
        public Task ReStartAsync();
        public Task CancelAsync();
        public void PauseOrResume();
        public bool IsPartAllComplete();
        public Task OnCompleteAsync();
        public CancellationTokenSource TokenSource { get; set; }
        public string DownloadName { get; set; }
        public string Title { get; set; }
        public ulong CurrentProgress { get; set; }
        public ulong FullProgress { get; set; }
        public string CurrentSpeed { get; set; }
        public string ChineseStatus { get; set; }
        public List<IBiliDownloadPart> PartList { get; set; }
        public StorageFolder CacheFolder { get; set; }
        public bool IsPaused { get; set; }
        public bool IsCompleted { get; set; }
    }
}
