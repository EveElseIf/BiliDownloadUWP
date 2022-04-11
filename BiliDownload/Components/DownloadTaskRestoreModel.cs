using System;

namespace BiliDownload.Components
{
    public class DownloadTaskRestoreModel
    {
        internal DownloadTaskRestoreModel() { }
        public Guid TaskGuid { get; set; }
        public ulong TotalBytes { get; set; }
        public ulong DownloadedBytes { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
        public string Referer { get; set; }
        public string UA { get; set; }
    }
}
