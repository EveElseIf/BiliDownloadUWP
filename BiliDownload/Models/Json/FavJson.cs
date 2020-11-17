using System.Collections.Generic;

namespace BiliDownload.Models.Json
{
    public class FavJson : BiliJsonBase
    {
        public Data data { get; set; }
        public class Data
        {
            public Info info { get; set; }
            public List<Media> medias { get; set; }
        }
        public class Info
        {
            public int id { get; set; }
            public string title { get; set; }
            public string cover { get; set; }
            public Upper upper { get; set; }
            public int media_count { get; set; }
        }
        public class Media
        {
            public int id { get; set; }
            public int type { get; set; }
            public string title { get; set; }
            public string cover { get; set; }
            public int page { get; set; }//分p号
            public string bvid { get; set; }
            public Upper upper { get; set; }
        }
        public class Upper
        {
            public int mid { get; set; }
            public string name { get; set; }
            public string face { get; set; }
        }
    }
}
