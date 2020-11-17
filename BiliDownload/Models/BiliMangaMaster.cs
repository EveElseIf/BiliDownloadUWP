using System.Collections.Generic;

namespace BiliDownload.Models
{
    public class BiliMangaMaster
    {
        public int Mcid { get; set; }
        public string Title { get; set; }
        public string HorizontalCoverUrl { get; set; }
        public string VerticalCoverUrl { get; set; }
        public string SquareCoverUrl { get; set; }
        public List<BiliManga> EpList { get; set; }
    }
}
