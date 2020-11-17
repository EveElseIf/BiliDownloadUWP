using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace BiliDownload.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[2] == "videoConverter")
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

                ApplicationData.Current.LocalSettings.Values["lock"] = false;//解锁

                try
                {
                    Directory.Delete(cacheFolder, true);
                }
                catch { }
            }
            //已经不再使用Kaedei.Danmu2Ass.exe
            //else if (args[2] == "xmlConverter")
            //{
            //    var arg = ApplicationData.Current.LocalSettings.Values["para2"] as string;
            //    var pi = new ProcessStartInfo("Kaedei.Danmu2Ass.exe", arg)
            //    {
            //        UseShellExecute = false,
            //        CreateNoWindow = true
            //    };
            //    var p1 = Process.Start(pi);
            //    p1.WaitForExit();

            //    ApplicationData.Current.LocalSettings.Values["lock2"] = false;//解锁
            //}
        }
    }
}
