using DanmakuToAss.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Helpers
{
    public static class DanmakuHelper
    {
        public static async Task MakeAssAsync(string xml, string fileName, string outputPath)
        {
            try
            {
                await StorageFolder.GetFolderFromPathAsync(outputPath);
            }
            catch (FileNotFoundException)
            {
                throw new DirectoryNotFoundException("找不到指定下载目录:" + outputPath);
            }
            var dir = await StorageFolder.GetFolderFromPathAsync(outputPath);
            var file = await dir.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            var assContent = DanmakuParser.LoadXmlFromString(xml).ToAss(1920, 1080);
            var bytes = Encoding.UTF8.GetBytes(assContent);
            using var stream = await file.OpenStreamForWriteAsync();
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        public static async Task MakeMultiAssAsync(Dictionary<string, string> fileName_xmlDic, string outputPath)
        {
            try
            {
                await StorageFolder.GetFolderFromPathAsync(outputPath);
            }
            catch (FileNotFoundException)
            {
                throw new DirectoryNotFoundException("找不到指定下载目录:" + outputPath);
            }
            var dir = await StorageFolder.GetFolderFromPathAsync(outputPath);
            var tasks = new List<Task>();
            foreach (var item in fileName_xmlDic)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var file = await dir.CreateFileAsync(item.Key);
                    var assContent = DanmakuParser.LoadXmlFromString(item.Value).ToAss(1920, 1080);
                    var bytes = Encoding.UTF8.GetBytes(assContent);
                    using var stream = await file.OpenStreamForWriteAsync();
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }));
            }
            await Task.WhenAll(tasks);
        }
    }
}
