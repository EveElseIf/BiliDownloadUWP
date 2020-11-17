using BiliDownload.Exceptions;
using BiliDownload.Helper;
using BiliDownload.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiliDownload.Helpers
{
    public static class BiliMangaHelper
    {
        public static async Task<BiliMangaMaster> GetBiliMangaMasterAsync(int mcid, string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }

            var payload = JsonConvert.SerializeObject(new { comic_id = mcid });
            var json = JsonConvert.DeserializeObject<dynamic>(
                (await NetHelper.HttpPostJsonAsync("https://manga.bilibili.com/twirp/comic.v1.Comic/ComicDetail?device=pc&platform=web", cookies, payload)));
            if (json.code != 0) throw new MangaNotFoundException();
            var master = new BiliMangaMaster()
            {
                Mcid = json.data.id,
                Title = json.data.title,
                HorizontalCoverUrl = json.data.horizontal_cover,
                VerticalCoverUrl = json.data.vertical_cover,
                SquareCoverUrl = json.data.square_cover,
                EpList = new List<BiliManga>()
            };
            foreach (var ep in json.data.ep_list)
            {
                master.EpList.Add(new BiliManga()
                {
                    Epid = ep.id,
                    Title = ep.title,
                    CoverUrl = ep.cover,
                    Order = ep.ord
                });
            }
            return master;
        }
    }
}
