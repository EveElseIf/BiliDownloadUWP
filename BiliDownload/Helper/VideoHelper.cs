using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace BiliDownload.Helper
{
    public static class VideoHelper
    {
        public static async Task MakeVideoAsync(StorageFile video,StorageFile audio,string outputPath)
        {
            string para;
            if ((bool)ApplicationData.Current.LocalSettings.Values["cpuLimit"])
                para = $"-i \"{video.Path}\" -i \"{audio.Path}\" -c:v copy -c:a copy -strict experimental -threads 2 \"{outputPath}\"";//设置cpu限制
            else 
                para = $"-i \"{video.Path}\" -i \"{audio.Path}\" -c:v copy -c:a copy -strict experimental \"{outputPath}\"";
            ApplicationData.Current.LocalSettings.Values["para"] = para;
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
        }
    }
}
