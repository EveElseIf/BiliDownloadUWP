using BiliDownload.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Helpers
{
    public static class MangaDownloadHelper
    {
        private static HttpClient client = new HttpClient();
        public static async Task<List<string>> GetPicUrlsAsync(int mcid, int epid,string sESSDATA)
        {
            if (!string.IsNullOrWhiteSpace(sESSDATA)) client.DefaultRequestHeaders.Add("SESSDATA", sESSDATA);
            var resp1 = await client.PostAsync("https://manga.bilibili.com/twirp/comic.v1.Comic/GetImageIndex?device=pc&platform=web",
                new StringContent(JsonConvert.SerializeObject(new { ep_id = epid }), Encoding.UTF8, "application/json"));
            var json1 = JsonConvert.DeserializeObject<dynamic>(await resp1.Content.ReadAsStringAsync());
            if (json1.msg == "episode need buy") throw new MangaEpisodeNeedBuyException();
            string url = json1.data.host + json1.data.path;
            var resp2 = await client.GetAsync(url);
            var stream = await resp2.Content.ReadAsStreamAsync();
            using var data = new MemoryStream();
            await stream.CopyToAsync(data);
            var key = new int[]
                {
                epid&0xff,
                epid>>8&0xff,
                epid>>16&0xff,
                epid>>24&0xff,
                mcid&0xff,
                mcid>>8&0xff,
                mcid>>16&0xff,
                mcid>>24&0xff
                };
            var bytes = data.ToArray().Skip(9).ToArray();
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= (byte)key[i % 8];
            }
            using var zip = new ZipArchive(new MemoryStream(bytes));
            var json2 = await new StreamReader(zip.GetEntry("index.dat").Open()).ReadToEndAsync();
            var picList = JsonConvert.DeserializeObject<dynamic>(json2).pics;
            var picList2 = new List<string>();
            foreach (var item in picList)
            {
                picList2.Add($"\"{item}\"");
            }
            var payload = JsonConvert.SerializeObject(new { urls = $"[{string.Join(",", picList2)}]" });
            var resp = await client.PostAsync("https://manga.bilibili.com/twirp/comic.v1.Comic/ImageToken?device=pc&platform=web",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var json = JsonConvert.DeserializeObject<dynamic>(await resp.Content.ReadAsStringAsync());
            if (json.code != 0) throw new Exception("解析失败");
            var picUrlList = new List<string>();
            foreach (var item in json.data)
            {
                picUrlList.Add($"{item.url}?token={item.token}");
            }
            return picUrlList;
        }
    }
}
