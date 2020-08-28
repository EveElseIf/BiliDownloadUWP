using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model
{
    public class BiliVideoInfo
    {
        public string Bv { get; set; }
        public long Cid { get; set; }
        public string Name { get; set; }
        public string CoverUrl { get; set; }
        public List<int> QualityCodeList { get; set; }
    }
}
