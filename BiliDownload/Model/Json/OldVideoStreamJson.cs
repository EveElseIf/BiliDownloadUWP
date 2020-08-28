using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDownload.Model.Json
{
    public class OldVideoStreamJson : BiliJsonBase
    {
        public Data data { get; set; }
        public class Data
        {
            public int quality { get; set; }
            public string format { get; set; }
            public List<int> accept_quality { get; set; }
            public List<Durl> durl { get; set; }
        }
        public class Durl
        {
            public int order { get; set; }
            public string url { get; set; }
        }
    }
}
