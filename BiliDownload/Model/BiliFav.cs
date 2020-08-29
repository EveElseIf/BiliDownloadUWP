using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model
{
    public class BiliFav
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int MediaCount { get => VideoList.Count; }
        public List<BiliVideoMaster> VideoList { get; set; }
    }
}
