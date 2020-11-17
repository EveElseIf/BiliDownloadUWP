using System.Collections.Generic;

namespace BiliDownload.Models
{
    public class BiliFav
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int MediaCount { get; set; }
        public List<BiliVideoMaster> VideoList { get; set; }
    }
}
