using BiliDownload.Exception;
using BiliDownload.Model;
using BiliDownload.Model.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BiliDownload.Helper
{
    public static class BiliVideoHelper
    {
        public static async Task<BiliVideoMaster> GetVideoMasterInfoAsync(string bv, string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA)) 
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }

            var json = JsonConvert.DeserializeObject<VideoInfoJson>(await NetHelper.HttpGet("http://api.bilibili.com/x/web-interface/view", cookies, $"bvid={bv}"));

            var videoList = new List<BiliVideo>(); //此处并未请求视频流
            json.data.pages.ForEach(v =>
            {
                videoList.Add(new BiliVideo()
                {
                    Bv = bv,
                    Cid = v.cid,
                    Name = v.part,
                    P = v.page,
                    Title = json.data.title,
                    Cover = json.data.pic
                });
            });
            var master = new BiliVideoMaster()
            {
                Bv = bv,
                Picture = json.data.pic,
                Title = json.data.title,
                VideoList = videoList
            };
            return master;
        }
        public static async Task<BiliVideoMaster> GetVideoMasterInfoAsync(long av, string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }
            var json = JsonConvert.DeserializeObject<VideoInfoJson>(await NetHelper.HttpGet("http://api.bilibili.com/x/web-interface/view", cookies, $"aid={av}"));

            var videoList = new List<BiliVideo>(); //此处并未请求视频流
            json.data.pages.ForEach(v =>
            {
                videoList.Add(new BiliVideo()
                {
                    Bv = json.data.bvid,
                    Cid = v.cid,
                    Name = v.part,
                    P = v.page,
                    Title = json.data.title,
                    Cover = json.data.pic
                });
            });
            var master = new BiliVideoMaster()
            {
                Bv = json.data.bvid,
                Picture = json.data.pic,
                Title = json.data.title,
                VideoList = videoList
            };
            return master;
        }
        /// <summary>
        /// 获取番剧信息
        /// </summary>
        /// <param name="eporss"></param>
        /// <param name="type">0是ep，其他随便一个数字是ss</param>
        /// <param name="sESSDATA"></param>
        /// <returns></returns>
        public static async Task<BiliBangumi> GetBangumiInfoAsync(int eporss,int type,string sESSDATA)
        {
            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }
            BiliBangumiInfoJson json;
            if (type == 0)
            {
                json = JsonConvert.DeserializeObject<BiliBangumiInfoJson>
                    (await NetHelper.HttpGet("http://api.bilibili.com/pgc/view/web/season", cookies, $"ep_id={eporss}"));
            }
            else
            {
                json = JsonConvert.DeserializeObject<BiliBangumiInfoJson>
                    (await NetHelper.HttpGet("http://api.bilibili.com/pgc/view/web/season", cookies, $"season_id={eporss}"));
            }

            var videoList = new List<BiliVideo>();
            json.result.episodes.ForEach(v =>
            {
                videoList.Add(new BiliVideo()
                {
                    Bv = v.bvid,
                    Cid = v.cid,
                    Name = v.long_title,
                    Title = json.result.season_title,
                    P = 1,
                    Cover = v.cover
                });
            });
            var bangumi = new BiliBangumi()
            {
                Title = json.result.season_title,
                Cover = json.result.cover,
                MediaId = json.result.media_id,
                SeasonId = json.result.season_id,
                EpisodeList = videoList
            };
            return bangumi;
        }
        public static async Task<BiliVideo> GetSingleVideoAsync(string bv, long cid, int quality, string sESSDATA)
        {
            var master = await GetVideoMasterInfoAsync(bv, sESSDATA);

            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }
            if (quality == 0) quality = 64;

            var json = JsonConvert.DeserializeObject<VideoStreamJson>
                (await NetHelper.HttpGet("http://api.bilibili.com/x/player/playurl",
                cookies, $"bvid={bv}", $"cid={cid}", $"qn={quality}", "fnval=16"));

            if (json.data == null)
            {
                throw new ParsingVideoException("视频解析失败，请检查是否缺少大会员权限");
            }

            var video = new BiliVideo();

            video.Bv = bv;
            video.Cid = cid;
            video.Name = master.VideoList.Where(v => v.Cid == cid).FirstOrDefault().Name;
            video.P = master.VideoList.Where(v => v.Cid == cid).FirstOrDefault().P;
            video.Quality = (BiliVideoQuality)json.data.quality;
            video.Title = master.Title;

            if ((bool)ApplicationData.Current.LocalSettings.Values["HEVC"])//优先使用hevc
            {
                if (json.data.dash.video.Where(v => v.id == quality && v.codecs.Contains("hev")).Count() > 0)
                {
                    video.VideoUrl = json.data.dash.video.Where(v => v.id == quality && v.codecs.Contains("hev")).FirstOrDefault().baseUrl;
                }
                else if(json.data.dash.video.Where(v => v.codecs.Contains("hev")).Count() > 0)
                {
                    var list = json.data.dash.video.Where(v => v.codecs.Contains("hev")).OrderByDescending(v => v.id).ToList();
                    video.VideoUrl = list.First().baseUrl;
                }
                else if (json.data.dash.video.Where(v => v.id == quality && v.codecs.Contains("avc")).Count() > 0)
                {
                    video.VideoUrl = json.data.dash.video.Where(v => v.id == quality && v.codecs.Contains("avc")).FirstOrDefault().baseUrl;
                }
                else
                {
                    var list = json.data.dash.video.Where(v => v.codecs.Contains("avc")).OrderByDescending(v => v.id).ToList();
                    video.VideoUrl = list.First().baseUrl;
                }
            }
            else
            {
                if (json.data.dash.video.Where(v => v.id == quality && v.codecs.Contains("avc")).Count() > 0)
                {
                    video.VideoUrl = json.data.dash.video.Where(v => v.id == quality && v.codecs.Contains("avc")).FirstOrDefault().baseUrl;
                }
                else
                {
                    var list = json.data.dash.video.Where(v => v.codecs.Contains("avc")).OrderByDescending(v => v.id).ToList();
                    video.VideoUrl = list.First().baseUrl;
                }
            }

            if (json.data.dash.audio.Where(v => v.id == 30280).Count() > 0)
            {
                video.AudioUrl = json.data.dash.audio.Where(v => v.id == 30280).FirstOrDefault().baseUrl;
            }
            else
            {
                var list = json.data.dash.audio.OrderByDescending(v => v.id).ToList();
                video.AudioUrl = list.First().baseUrl;
            }

            return video;
        }
        public static async Task<BiliVideoInfo> GetSingleVideoInfoAsync(string bv, long cid, int quality, string sESSDATA)
        {
            var master = await GetVideoMasterInfoAsync(bv, sESSDATA);

            List<(string, string)> cookies = null;
            if (!string.IsNullOrWhiteSpace(sESSDATA))
            {
                cookies = new List<(string, string)>();
                cookies.Add(("SESSDATA", sESSDATA));
            }
            var json = JsonConvert.DeserializeObject<VideoStreamJson>
                (await NetHelper.HttpGet("http://api.bilibili.com/x/player/playurl",
                cookies, $"bvid={bv}", $"cid={cid}", $"qn={quality}", "fnval=16"));

            if (json.data == null)
            {
                throw new ParsingVideoException("视频解析失败，请检查是否缺少大会员权限");
            }

            var info = new BiliVideoInfo()
            {
                Bv = master.Bv,
                Cid = cid,
                Name = master.Title + " " + master.VideoList.Where(v => v.Cid == cid)?.FirstOrDefault().Name,
                CoverUrl = master.VideoList.Where(v => v.Cid == cid)?.FirstOrDefault().Cover ?? master.Picture,
                QualityCodeList = json.data.accept_quality
            };

            return info;
        }
    }
}
