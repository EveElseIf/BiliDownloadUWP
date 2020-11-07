using Newtonsoft.Json;
using QRCoder;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace BiliDownload.Helper
{
    public static class BiliLoginHelper
    {
        public static async Task<string[]> LoginUseQRCodeAsync(Image qRCodeImage, CancellationToken token)
        {
            var getJson = JsonConvert.DeserializeObject<GetQRCodeJson>(await NetHelper.HttpGet("https://passport.bilibili.com/qrcode/getLoginUrl", null, null));
            var QRcodeUrl = getJson.data.url;
            var key = getJson.data.oauthKey;

            var QRcode = await GetQRCodeImageAsync(QRcodeUrl);

            await qRCodeImage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
             {
                 qRCodeImage.Source = QRcode;
             });
            while (true)
            {
                if (token.IsCancellationRequested) return null;
                var postRaw = await NetHelper.HttpPostAsync
                    ("http://passport.bilibili.com/qrcode/getLoginInfo", null, $"oauthKey={key}");
                var postJson = JsonConvert.DeserializeObject<VerifyQRCodeJson>(postRaw.Item1);
                int.TryParse(postJson.data.ToString(), out int resultCode);
                if (resultCode == -2) return null;
                if (postJson.status == true)
                {
                    var sESSDATA = Regex.Match(postRaw.Item2.ToString(), "(?<=SESSDATA=)[%|a-z|A-Z|0-9|*]*")?.Value;
                    var uid = Regex.Match(postRaw.Item2.ToString(), "(?<=DedeUserID=)[0-9]*")?.Value;
                    return new string[] { sESSDATA, uid };
                }
                await Task.Delay(1000);
            }
        }
        private static async Task<ImageSource> GetQRCodeImageAsync(string content)
        {
            var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("QRCode.png", CreationCollisionOption.ReplaceExisting);
            var fileStream = (await file.OpenStreamForWriteAsync());

            var generator = new QRCodeGenerator();
            var qrCodeData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var imgBytes = qrCode.GetGraphic(20);
            await fileStream.WriteAsync(imgBytes, 0, imgBytes.Length);
            fileStream.Dispose();

            //var imgSource = new BitmapImage(new Uri(file.Path));
            var imgSource = new BitmapImage();
            await imgSource.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());

            return imgSource;
        }
    }
    public class GetQRCodeJson
    {
        public int code { get; set; }
        public bool status { get; set; }
        public int ts { get; set; }
        public Data data { get; set; }
        public class Data
        {
            public string url { get; set; }
            public string oauthKey { get; set; }
        }
    }
    public class VerifyQRCodeJson
    {
        public int code { get; set; }
        public string message { get; set; }
        public int ts { get; set; }
        public bool status { get; set; }
        public object data { get; set; }
    }
}
