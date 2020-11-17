using System.Collections.Generic;

namespace BiliDownload.Models.Json
{
    public class VideoStreamJson : BiliJsonBase
    {
        public Data data { get; set; }
        public class Data
        {
            public int quality { get; set; }
            public List<int> accept_quality { get; set; }
            public Dash dash { get; set; }
        }
        public class Dash
        {
            public List<Video> video { get; set; }
            public List<Audio> audio { get; set; }
        }
        public class Video
        {
            public int id { get; set; }//视频质量相关代码，不懂为啥是id
            public string baseUrl { get; set; }
            public long bandwidth { get; set; }
            public string codecs { get; set; }
        }
        public class Audio
        {
            public int id { get; set; }//音频质量相关代码，不懂为啥是id
            public string baseUrl { get; set; }
            public long bandwidth { get; set; }
            public string codecs { get; set; }
        }
    }
}
