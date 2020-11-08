using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace BiliDownload.Helper
{
    public static class VideoHelper
    {
        public static bool Locked { get; set; } = false;
        private static readonly object locker = new object();
        public static async Task MakeVideoAsync(StorageFile video, StorageFile audio, string outputPath)
        {
            lock (locker)//使用锁来防止ffmpeg卡bug
            {
                Locked = true;
                ApplicationData.Current.LocalSettings.Values["lock"] = true;//锁定，这是不同程序之间通信用的锁
                string para;
                if ((bool)ApplicationData.Current.LocalSettings.Values["cpuLimit"])
                    para = $"-i \"{video.Path}\" -i \"{audio.Path}\" -c:v copy -c:a copy -strict experimental -threads 2 \"{outputPath}\"";//设置cpu限制
                else
                    para = $"-i \"{video.Path}\" -i \"{audio.Path}\" -c:v copy -c:a copy -strict experimental \"{outputPath}\"";
                ApplicationData.Current.LocalSettings.Values["para"] = para;
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                {
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                    FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("VideoGroup");
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                }
                while ((bool)ApplicationData.Current.LocalSettings.Values["lock"]) Thread.Sleep(100);
                Locked = false;
            }
        }
    }
}
