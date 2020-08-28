using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model.Json
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
