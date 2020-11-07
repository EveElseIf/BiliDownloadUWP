using System.Collections.Generic;

namespace BiliDownload.Model.Json
{
    public class VideoInfoJson
    {
        public Data data { get; set; }
        public class Data
        {
            public string bvid { get; set; }
            public int aid { get; set; }
            public int videos { get; set; }
            public string pic { set; get; }
            public string title { get; set; }
            public List<Page> pages { get; set; }
        }

        public class Page
        {
            public int cid { get; set; }
            public int page { get; set; }//当前分p顺序号
            public string part { get; set; }//标题
        }
    }
}
