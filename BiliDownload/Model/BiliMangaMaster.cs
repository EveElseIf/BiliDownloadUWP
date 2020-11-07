using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model
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
