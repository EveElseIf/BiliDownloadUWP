using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Windows.Storage;

namespace BiliDownload.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var arg = ApplicationData.Current.LocalSettings.Values["para"] as string;
            var cacheFolder = ApplicationData.Current.LocalSettings.Values["currentCacheFolder"] as string;

            var pi = new ProcessStartInfo("ffmpeg.exe", arg)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p1 = Process.Start(pi);
            p1.WaitForExit();

            Directory.Delete(cacheFolder, true);
        }
    }
}
