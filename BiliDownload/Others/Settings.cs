using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Others
{
    public static class Settings
    {
        public static string DownloadPath { get => ApplicationData.Current.LocalSettings.Values["downloadPath"] as string ?? null; }
        public static bool NeedNotice { get => (bool)ApplicationData.Current.LocalSettings.Values["NeedNotice"]; }
        public static bool AutoDownloadDanmaku { get => (bool)ApplicationData.Current.LocalSettings.Values["autoDownloadDanmaku"]; }
        public static bool HEVC { get => (bool)ApplicationData.Current.LocalSettings.Values["HEVC"]; }
        public static bool CpuLimit { get => (bool)ApplicationData.Current.LocalSettings.Values["cpuLimit"]; }
    }
}
