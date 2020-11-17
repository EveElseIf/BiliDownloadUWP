using System.Collections.Generic;

namespace BiliDownload.Models.Json
{
    public class BiliBangumi
    {
        public string Title { get; set; }
        public string Cover { get; set; }
        public int SeasonId { get; set; }
        public long MediaId { get; set; }
        public List<BiliVideo> EpisodeList { get; set; }
    }
}
