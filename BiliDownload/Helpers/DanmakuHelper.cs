using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace BiliDownload.Helpers
{
    public static class DanmakuHelper
    {
        public static bool Locked { set; get; } = false;
        private static readonly object locker = new object();
        public static async Task MakeAssAsync(string xml,string fileName,string outputPath)
        {
            var path = await StorageFolder.GetFolderFromPathAsync(outputPath);
            var xmlFile = await path.CreateFileAsync(fileName,CreationCollisionOption.ReplaceExisting);
            var stream = await xmlFile.OpenStreamForWriteAsync();
            var bytes = Encoding.UTF8.GetBytes(xml);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            stream.Dispose();
            lock (locker)
            {
                Locked = true;
                ApplicationData.Current.LocalSettings.Values["lock2"] = true;//锁定，这是不同程序之间通信用的锁
                ApplicationData.Current.LocalSettings.Values["para2"] = "\"" + Path.Combine(path.Path, xmlFile.Name) + "\"";
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                {
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                    FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("XmlGroup");
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                }
                while ((bool)ApplicationData.Current.LocalSettings.Values["lock2"]) Thread.Sleep(100);
                Locked = false;
            }
            await xmlFile.DeleteAsync(StorageDeleteOption.PermanentDelete);//删除xml
        }
        public static async Task MakeMultiAssAsync(Dictionary<string,string> fileName_xmlDic,string outputPath)
        {
            var path = await StorageFolder.GetFolderFromPathAsync(outputPath);
            var xmlFileList = new List<StorageFile>();
            foreach (var item in fileName_xmlDic)
            {
                var xmlFile = await path.CreateFileAsync(item.Key, CreationCollisionOption.ReplaceExisting);
                xmlFileList.Add(xmlFile);
                var stream = await xmlFile.OpenStreamForWriteAsync();
                var bytes = Encoding.UTF8.GetBytes(item.Value);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                stream.Dispose();
            }
            lock (locker)
            {
                Locked = true;
                ApplicationData.Current.LocalSettings.Values["lock2"] = true;//锁定，这是不同程序之间通信用的锁
                ApplicationData.Current.LocalSettings.Values["para2"] = MakeParam(fileName_xmlDic.Keys.ToList(),outputPath);
                    
                if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
                {
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                    FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("XmlGroup");
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                }
                while ((bool)ApplicationData.Current.LocalSettings.Values["lock2"]) Thread.Sleep(100);
                Locked = false;
            }
            xmlFileList.ForEach(async f => await f.DeleteAsync(StorageDeleteOption.PermanentDelete));
        }
        private static string MakeParam(List<string> names,string outputPath)
        {
            var str = "";
            foreach (var item in names)
            {
                str += $"\"{Path.Combine(outputPath, item)}\" ";
            }
            return str;
        }
    }
}
