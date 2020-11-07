using BiliDownload.Model;
using BiliDownload.Model.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace BiliDownload.Helper
{
    public static class BiliUserHelper
    {
        public static async Task<BiliUser> GetCurrentUserInfoAsync()
        {
            var sESSDATA = ApplicationData.Current.LocalSettings.Values["biliUserSESSDATA"] as string;
            if (sESSDATA == null) return null;

            var cookies = new List<(string, string)>()
            {
                ("SESSDATA",sESSDATA)
            };
            var json = JsonConvert.DeserializeObject<SelfInfoJson>(await NetHelper.HttpGet("http://api.bilibili.com/x/space/myinfo", cookies, null));
            var user = new BiliUser();

            var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("avatar", CreationCollisionOption.ReplaceExisting);
            var imgStream = await NetHelper.HttpGetStreamAsync(json.data.face, null, null);
            var fileStream = await file.OpenStreamForWriteAsync();
            await imgStream.CopyToAsync(fileStream);
            imgStream.Close();
            fileStream.Close();
            var imgSource = new BitmapImage();
            await imgSource.SetSourceAsync((await file.OpenStreamForReadAsync()).AsRandomAccessStream());

            user.Uid = json.data.mid;
            user.Name = json.data.name;
            user.AvatarUrl = json.data.face;
            user.AvatarImg = imgSource;
            user.VipStatus = (VipStatus)json.data.vip.status;

            return user;
        }
        public static async Task<(bool, string, long)> VerifySessDataAsync(string sESSDATA)
        {
            var cookies = new List<(string, string)>()
            {
                ("SESSDATA",sESSDATA)
            };
            var json = JsonConvert.DeserializeObject<SelfInfoJson>(await NetHelper.HttpGet("http://api.bilibili.com/x/space/myinfo", cookies, null));
            if (json.code == -101 || json.code != 0) return (false, null, 0);
            else return (true, sESSDATA, json.data.mid);
        }
    }
}
