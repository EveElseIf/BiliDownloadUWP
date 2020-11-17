using System.Collections.Generic;

namespace BiliDownload.Models.Json
{
    public class BiliBangumiListJson : BiliJsonBase
    {
        public Data data { get; set; }
        public class Data
        {
            public List<Bangumi> list { get; set; }
        }
        public class Bangumi
        {
            public int season_id { get; set; }
            public string title { get; set; }
            public string cover { get; set; }
            public string season_title { get; set; }
        }
    }
}
