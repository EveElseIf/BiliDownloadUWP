using System.Collections.Generic;

namespace BiliDownload.Model.Json
{
    public class BiliBangumiInfoJson : BiliJsonBase
    {
        public Result result { get; set; }
        public class Result
        {
            public string cover { get; set; }//不懂谁的封面
            public List<Episode> episodes { get; set; }
            public long media_id { get; set; }//就是md id，不懂怎么用。。。
            public int season_id { get; set; }//就是ss id
            public string season_title { get; set; }//这季番的标题
        }
        public class Episode
        {
            public long aid { get; set; }
            public string bvid { get; set; }
            public long cid { get; set; }
            public string cover { get; set; }//封面
            public int id { get; set; }//就是ep id
            public string long_title { get; set; }//这集的标题
            public string share_copy { get; set; }//xx番剧 第几话 xx标题
        }
    }
}
