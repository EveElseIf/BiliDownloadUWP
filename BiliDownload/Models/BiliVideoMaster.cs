using System.Collections.Generic;

namespace BiliDownload.Models
{
    public class BiliVideoMaster
    {
        public string Bv { get; set; }
        public string Title { get; set; }
        public List<BiliVideo> VideoList { get; set; }
        public string Picture { get; set; }
    }
}
