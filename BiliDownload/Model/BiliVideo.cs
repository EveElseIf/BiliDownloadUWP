using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model
{
    public class BiliVideo
    {
        public string Bv { get; set; }
        public long Cid { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string Cover { get; set; }
        public int P { get; set; }         //分P的P，数字顺序。。。
        public string VideoUrl { get; set; }
        public string AudioUrl { get; set; }
        public BiliVideoQuality Quality { set; get; }
    }
    public enum BiliVideoQuality
    {
        Q360P = 16,
        Q480P = 32,
        Q720P = 64,
        Q720P60 = 74,
        Q1080P = 80,
        Q1080PPlus = 112,
        Q1080P60 = 116,
        Q4K = 120,
    }
}
