using BiliDownload.Models;
using BiliDownload.Models.Json;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliDownload.Helper
{
    public static class BiliFavHelper
    {
        public static async Task<List<BiliFav>> GetUserFavListInfoAsync(long uid, string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }

            var json = JsonConvert.DeserializeObject<FavListJson>
                (await NetHelper.HttpGet("https://api.bilibili.com/x/v3/fav/folder/created/list-all", cookies, $"up_mid={uid}", "jsonp=jsonp"));
            var list = json.data.list.Select(l => new BiliFav()
            {
                Id = l.id,
                Title = l.title
            }).ToList();
            return list;
        }
        public static async Task<BiliFav> GetBiliFavAsync(int id, int pn, string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }

            var json = JsonConvert.DeserializeObject<FavJson>
                (await NetHelper.HttpGet("https://api.bilibili.com/x/v3/fav/resource/list", cookies, $"media_id={id}", $"pn={pn}", "ps=20", "order=mtime"));
            if (json.data.medias == null) return null;
            var fav = new BiliFav()
            {
                Id = json.data.info.id,
                Title = json.data.info.title,
                MediaCount = json.data.info.media_count,
                VideoList = json.data.medias.Select(m => new BiliVideoMaster()
                {
                    Bv = m.bvid,
                    Picture = m.cover,
                    Title = m.title
                }).ToList()
            };
            return fav;
        }
    }
}
