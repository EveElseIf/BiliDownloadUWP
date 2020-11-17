using BiliDownload.Models.Json;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiliDownload.Helper
{
    public static class BiliBangumiListHelper
    {
        public static async Task<List<BiliBangumi>> GetBangumiListAsync(int page, long uid, string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }

            var json = JsonConvert.DeserializeObject<BiliBangumiListJson>(await NetHelper.HttpGet
                ("https://api.bilibili.com/x/space/bangumi/follow/list", cookies, $"type=1&pn={page}&ps=15&vmid={uid}"));
            var list = new List<BiliBangumi>();
            if (!(json.data.list?.Count > 0)) return null;
            foreach (var bangumi in json.data.list)
            {
                list.Add(new BiliBangumi()
                {
                    Title = bangumi.title + " - " + bangumi.season_title,
                    SeasonId = bangumi.season_id,
                    Cover = bangumi.cover
                });
            }
            return list;
        }
    }
}
